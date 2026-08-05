using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace GuideCraft.Services;

/// <summary>
/// 数据链加密服务：AES-256-GCM 字段级加密，主密钥由 Windows DPAPI (CurrentUser) 保护。
///
/// 用途：对话标题、消息内容、生成代码、需求文档等敏感字段落盘前加密。
/// 密钥从未以明文落盘；解密失败时静默降级返回原值（兼容历史明文数据）。
/// </summary>
public interface ICryptoService
{
    /// <summary>加密字符串，返回 Base64(nonce[12] | ciphertext | tag[16])。失败返回 null 表示不应加密。</summary>
    string? Encrypt(string plain);

    /// <summary>解密 Base64(nonce|ct|tag)。失败返回原值。</summary>
    string Decrypt(string cipher);
}

public sealed class CryptoService : ICryptoService
{
    private const string MasterKeySetting = "encryption_master_key";
    private readonly byte[] _masterKey;

    public CryptoService()
    {
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GuideCraft", "guidecraft.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        using var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}");
        conn.Open();

        // 建表（独立于 LocalStorageService 的 EnsureCreated，安全）
        using (var dd = conn.CreateCommand())
        {
            dd.CommandText = """
                CREATE TABLE IF NOT EXISTS settings (
                    key   TEXT PRIMARY KEY,
                    value TEXT NOT NULL
                );
                """;
            dd.ExecuteNonQuery();
        }

        // 读取已存在的主密钥密文
        string? stored = null;
        using (var sel = conn.CreateCommand())
        {
            sel.CommandText = "SELECT value FROM settings WHERE key=$k";
            sel.Parameters.AddWithValue("$k", MasterKeySetting);
            var raw = sel.ExecuteScalar();
            if (raw is string s && !string.IsNullOrEmpty(s)) stored = s;
        }

        byte[] key;
        if (string.IsNullOrEmpty(stored))
        {
            // 首次启动：生成随机 256-bit 主密钥，DPAPI 加密落盘
            key = RandomNumberGenerator.GetBytes(32);
            var wrapped = ProtectedData.Protect(key, null, DataProtectionScope.CurrentUser);
            using var ins = conn.CreateCommand();
            ins.CommandText = """
                INSERT INTO settings (key, value) VALUES ($k, $v)
                ON CONFLICT (key) DO UPDATE SET value=$v
                """;
            ins.Parameters.AddWithValue("$k", MasterKeySetting);
            ins.Parameters.AddWithValue("$v", Convert.ToBase64String(wrapped));
            ins.ExecuteNonQuery();
        }
        else
        {
            // 解密现有密钥；损坏则重新生成（旧加密数据将无法解密——已记录在 SECURITY.md）
            try
            {
                key = ProtectedData.Unprotect(Convert.FromBase64String(stored), null, DataProtectionScope.CurrentUser);
                if (key.Length != 32)
                    throw new CryptographicException("Invalid master key length");
            }
            catch
            {
                key = RandomNumberGenerator.GetBytes(32);
                var wrapped = ProtectedData.Protect(key, null, DataProtectionScope.CurrentUser);
                using var upd = conn.CreateCommand();
                upd.CommandText = "UPDATE settings SET value=$v WHERE key=$k";
                upd.Parameters.AddWithValue("$v", Convert.ToBase64String(wrapped));
                upd.Parameters.AddWithValue("$k", MasterKeySetting);
                upd.ExecuteNonQuery();
            }
        }
        _masterKey = key;
    }

    public string? Encrypt(string plain)
    {
        if (string.IsNullOrEmpty(plain)) return string.Empty;
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plainBytes = Encoding.UTF8.GetBytes(plain);
        var ct = new byte[plainBytes.Length];
        var tag = new byte[16];
        using (var aes = new AesGcm(_masterKey, AesGcm.TagByteSizes.MaxSize))
        {
            aes.Encrypt(nonce, plainBytes, ct, tag);
        }
        var output = new byte[12 + ct.Length + 16];
        Buffer.BlockCopy(nonce, 0, output, 0, 12);
        Buffer.BlockCopy(ct, 0, output, 12, ct.Length);
        Buffer.BlockCopy(tag, 0, output, 12 + ct.Length, 16);
        return Convert.ToBase64String(output);
    }

    public string Decrypt(string cipher)
    {
        if (string.IsNullOrEmpty(cipher)) return string.Empty;
        // 非 Base64 或长度异常 → 原样返回（兼容明文历史数据）
        try
        {
            var data = Convert.FromBase64String(cipher);
            if (data.Length < 12 + 16) return cipher;
            var nonce = new byte[12];
            var tag = new byte[16];
            var ct = new byte[data.Length - 12 - 16];
            Buffer.BlockCopy(data, 0, nonce, 0, 12);
            Buffer.BlockCopy(data, 12, ct, 0, ct.Length);
            Buffer.BlockCopy(data, 12 + ct.Length, tag, 0, 16);
            var plain = new byte[ct.Length];
            using var aes = new AesGcm(_masterKey, AesGcm.TagByteSizes.MaxSize);
            aes.Decrypt(nonce, ct, tag, plain);
            return Encoding.UTF8.GetString(plain);
        }
        catch
        {
            return cipher;
        }
    }
}