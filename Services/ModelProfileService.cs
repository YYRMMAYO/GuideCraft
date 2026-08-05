using System.IO;
using System.Text.Json;
using GuideCraft.Models;
using Microsoft.Data.Sqlite;

namespace GuideCraft.Services;

/// <summary>模型配置管理：用户自定义模型配置的 CRUD + 默认配置</summary>
public interface IModelProfileService
{
    /// <summary>全部配置（默认在前）</summary>
    List<ModelProfile> GetAll();

    /// <summary>当前默认配置（无则取首个，仍无则返回 null）</summary>
    ModelProfile? GetDefault();

    /// <summary>按 Id 获取</summary>
    ModelProfile? GetById(string id);

    /// <summary>保存（新增或更新），配置内的 ApiKey 明文经加密后存储</summary>
    void Save(ModelProfile profile, string plainApiKey);

    /// <summary>删除配置；若删除的是默认配置，则自动将首个剩余配置设为默认</summary>
    void Delete(string id);

    /// <summary>将指定配置设为默认（其余清除默认标记）</summary>
    void SetDefault(string id);

    /// <summary>解密指定配置的 API Key（失败返回空串）</summary>
    string DecryptApiKey(ModelProfile profile);
}

/// <summary>SQLite 实现：model_profiles 表（配置名称/端点/模型/Key 密文/计费参数）</summary>
public sealed class ModelProfileService : IModelProfileService
{
    private readonly string _dbPath;
    private readonly string _connectionString;
    private readonly ICryptoService _crypto;

    public ModelProfileService(ICryptoService crypto)
    {
        _crypto = crypto;
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GuideCraft");
        Directory.CreateDirectory(dir);
        _dbPath = Path.Combine(dir, "guidecraft.db");
        _connectionString = $"Data Source={_dbPath}";
        EnsureCreated();
    }

    private void EnsureCreated()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS model_profiles (
                id                   TEXT PRIMARY KEY,
                name                 TEXT NOT NULL,
                provider             TEXT NOT NULL,
                base_url             TEXT NOT NULL,
                model_id             TEXT NOT NULL,
                api_key_cipher       TEXT NOT NULL,
                is_default           INTEGER NOT NULL DEFAULT 0,
                input_price_per_m    REAL NOT NULL DEFAULT 1.0,
                output_price_per_m   REAL NOT NULL DEFAULT 2.0,
                cache_hit_price_per_m REAL NOT NULL DEFAULT 0.02,
                enable_cache         INTEGER NOT NULL DEFAULT 1,
                note                 TEXT NOT NULL DEFAULT '',
                created_at           TEXT NOT NULL
            );
            """;
        cmd.ExecuteNonQuery();
    }

    private SqliteConnection Open()
    {
        var c = new SqliteConnection(_connectionString);
        c.Open();
        return c;
    }

    public List<ModelProfile> GetAll()
    {
        var list = new List<ModelProfile>();
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM model_profiles ORDER BY is_default DESC, created_at ASC";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(Map(reader));
        }
        return list;
    }

    public ModelProfile? GetDefault()
    {
        var all = GetAll();
        return all.FirstOrDefault(p => p.IsDefault) ?? all.FirstOrDefault();
    }

    public ModelProfile? GetById(string id)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM model_profiles WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? Map(reader) : null;
    }

    public void Save(ModelProfile profile, string plainApiKey)
    {
        var cipher = string.IsNullOrEmpty(plainApiKey)
            ? profile.ApiKeyCipher  // 未改动 Key 时保留原密文
            : (_crypto.Encrypt(plainApiKey) ?? string.Empty);

        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO model_profiles
                (id, name, provider, base_url, model_id, api_key_cipher, is_default,
                 input_price_per_m, output_price_per_m, cache_hit_price_per_m, enable_cache, note, created_at)
            VALUES
                ($id, $name, $provider, $base, $model, $key, $def,
                 $ip, $op, $chp, $cache, $note, $created)
            ON CONFLICT(id) DO UPDATE SET
                name=$name, provider=$provider, base_url=$base, model_id=$model,
                api_key_cipher=$key, is_default=$def,
                input_price_per_m=$ip, output_price_per_m=$op, cache_hit_price_per_m=$chp,
                enable_cache=$cache, note=$note
            """;
        cmd.Parameters.AddWithValue("$id", profile.Id);
        cmd.Parameters.AddWithValue("$name", profile.Name);
        cmd.Parameters.AddWithValue("$provider", profile.Provider.ToString());
        cmd.Parameters.AddWithValue("$base", profile.BaseUrl);
        cmd.Parameters.AddWithValue("$model", profile.ModelId);
        cmd.Parameters.AddWithValue("$key", cipher);
        cmd.Parameters.AddWithValue("$def", profile.IsDefault ? 1 : 0);
        cmd.Parameters.AddWithValue("$ip", profile.InputPricePerM);
        cmd.Parameters.AddWithValue("$op", profile.OutputPricePerM);
        cmd.Parameters.AddWithValue("$chp", profile.CacheHitPricePerM);
        cmd.Parameters.AddWithValue("$cache", profile.EnableCache ? 1 : 0);
        cmd.Parameters.AddWithValue("$note", profile.Note);
        cmd.Parameters.AddWithValue("$created", DateTime.Now.ToString("O"));
        cmd.ExecuteNonQuery();

        // 若设为默认，清除其它配置的默认标记
        if (profile.IsDefault)
            ClearOthersDefault(conn, profile.Id);
    }

    private static void ClearOthersDefault(SqliteConnection conn, string exceptId)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE model_profiles SET is_default=0 WHERE id <> $id";
        cmd.Parameters.AddWithValue("$id", exceptId);
        cmd.ExecuteNonQuery();
    }

    public void Delete(string id)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM model_profiles WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();

        // 若删除的是默认配置，则把首个剩余配置设为默认
        var remaining = GetAll();
        if (remaining.Count > 0 && !remaining.Any(p => p.IsDefault))
        {
            var first = remaining[0];
            first.IsDefault = true;
            Save(first, string.Empty);
        }
    }

    public void SetDefault(string id)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE model_profiles SET is_default=0";
        cmd.ExecuteNonQuery();
        using var cmd2 = conn.CreateCommand();
        cmd2.CommandText = "UPDATE model_profiles SET is_default=1 WHERE id=$id";
        cmd2.Parameters.AddWithValue("$id", id);
        cmd2.ExecuteNonQuery();
    }

    public string DecryptApiKey(ModelProfile profile)
    {
        if (string.IsNullOrEmpty(profile.ApiKeyCipher)) return string.Empty;
        return _crypto.Decrypt(profile.ApiKeyCipher);
    }

    private static ModelProfile Map(SqliteDataReader r)
    {
        var profile = new ModelProfile
        {
            Id = r.GetString(r.GetOrdinal("id")),
            Name = r.GetString(r.GetOrdinal("name")),
            Provider = Enum.TryParse<LlmProvider>(r.GetString(r.GetOrdinal("provider")), out var p) ? p : LlmProvider.Qwen,
            BaseUrl = r.GetString(r.GetOrdinal("base_url")),
            ModelId = r.GetString(r.GetOrdinal("model_id")),
            ApiKeyCipher = r.GetString(r.GetOrdinal("api_key_cipher")),
            IsDefault = r.GetInt32(r.GetOrdinal("is_default")) == 1,
            InputPricePerM = r.GetDouble(r.GetOrdinal("input_price_per_m")),
            OutputPricePerM = r.GetDouble(r.GetOrdinal("output_price_per_m")),
            CacheHitPricePerM = r.GetDouble(r.GetOrdinal("cache_hit_price_per_m")),
            EnableCache = r.GetInt32(r.GetOrdinal("enable_cache")) == 1,
            Note = r.GetString(r.GetOrdinal("note"))
        };
        return profile;
    }
}

/// <summary>单次调用用量统计（用于缓存命中率与费用估算）</summary>
public sealed class UsageInfo
{
    public long PromptTokens { get; set; }
    public long CompletionTokens { get; set; }
    public long TotalTokens { get; set; }
    public long CacheHitTokens { get; set; }
    public long CacheMissTokens { get; set; }

    /// <summary>缓存命中率（0-100），无缓存数据返回 null</summary>
    public double? CacheHitRate => CacheHitTokens + CacheMissTokens > 0
        ? 100.0 * CacheHitTokens / (CacheHitTokens + CacheMissTokens)
        : null;
}
