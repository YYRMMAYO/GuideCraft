using System.IO;
using System.Text.Json;
using GuideCraft.Models;
using Microsoft.Data.Sqlite;

namespace GuideCraft.Services;

/// <summary>SQLite 本地存储：会话、消息、产物、设置（敏感字段经 CryptoService AES-GCM 加密）</summary>
public interface ILocalStorageService
{
    List<Conversation> GetConversations();
    Conversation? GetConversation(string id);
    void SaveConversation(Conversation conversation);
    void DeleteConversation(string id);
    string? GetSetting(string key);
    void SetSetting(string key, string value);

    /// <summary>读取会话压缩摘要（无则返回 null）</summary>
    string? GetConversationSummary(string conversationId);

    /// <summary>保存会话压缩摘要</summary>
    void SaveConversationSummary(string conversationId, string summary);
}

/// <summary>SQLite 实现：敏感字段（标题/消息内容/代码/描述/需求文档）经 AES-GCM 字段级加密落盘</summary>
public sealed class LocalStorageService : ILocalStorageService
{
    private readonly string _dbPath;
    private readonly string _connectionString;
    private readonly ICryptoService _crypto;

    public LocalStorageService(ICryptoService crypto)
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
            CREATE TABLE IF NOT EXISTS conversations (
                id         TEXT PRIMARY KEY,
                title      TEXT NOT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS messages (
                id              INTEGER PRIMARY KEY AUTOINCREMENT,
                conversation_id TEXT NOT NULL,
                role            TEXT NOT NULL,
                content         TEXT NOT NULL,
                timestamp       TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_messages_conv ON messages(conversation_id);
            CREATE TABLE IF NOT EXISTS projects (
                conversation_id      TEXT PRIMARY KEY,
                type                 TEXT NOT NULL,
                code                 TEXT NOT NULL,
                description          TEXT NOT NULL,
                dependencies         TEXT NOT NULL,
                requirement_document TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS conversation_summaries (
                conversation_id TEXT PRIMARY KEY,
                summary         TEXT NOT NULL,
                updated_at      TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS settings (
                key   TEXT PRIMARY KEY,
                value TEXT NOT NULL
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

    private string Enc(string? plain) => string.IsNullOrEmpty(plain) ? string.Empty : _crypto.Encrypt(plain) ?? string.Empty;
    private string Dec(string cipher) => _crypto.Decrypt(cipher);

    private static string Ts(DateTime t) => t.ToString("O");
    private static DateTime ParseTs(string s) =>
        DateTime.TryParse(s, null, System.Globalization.DateTimeStyles.RoundtripKind, out var v) ? v : DateTime.Now;

    public List<Conversation> GetConversations()
    {
        var list = new List<Conversation>();
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, title, created_at, updated_at FROM conversations ORDER BY updated_at DESC";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new Conversation
            {
                Id = reader.GetString(0),
                Title = Dec(reader.GetString(1)),
                CreatedAt = ParseTs(reader.GetString(2)),
                UpdatedAt = ParseTs(reader.GetString(3))
            });
        }
        return list;
    }

    public Conversation? GetConversation(string id)
    {
        using var conn = Open();
        Conversation? conv = null;

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT id, title, created_at, updated_at FROM conversations WHERE id = $id";
            cmd.Parameters.AddWithValue("$id", id);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                conv = new Conversation
                {
                    Id = reader.GetString(0),
                    Title = Dec(reader.GetString(1)),
                    CreatedAt = ParseTs(reader.GetString(2)),
                    UpdatedAt = ParseTs(reader.GetString(3))
                };
            }
        }
        if (conv is null) return null;

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT role, content, timestamp FROM messages WHERE conversation_id = $id ORDER BY id ASC";
            cmd.Parameters.AddWithValue("$id", id);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                conv.Messages.Add(new ChatMessage
                {
                    ConversationId = id,
                    Role = Enum.TryParse<ChatRole>(reader.GetString(0), out var role) ? role : ChatRole.User,
                    Content = Dec(reader.GetString(1)),
                    Timestamp = ParseTs(reader.GetString(2))
                });
            }
        }

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT type, code, description, dependencies, requirement_document FROM projects WHERE conversation_id = $id";
            cmd.Parameters.AddWithValue("$id", id);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                conv.GeneratedProject = new GeneratedProject
                {
                    Type = Enum.TryParse<ProjectType>(reader.GetString(0), out var t) ? t : ProjectType.PythonScript,
                    Code = Dec(reader.GetString(1)),
                    Description = Dec(reader.GetString(2)),
                    Dependencies = JsonSerializer.Deserialize<List<string>>(Dec(reader.GetString(3))) ?? new List<string>(),
                    RequirementDocument = Dec(reader.GetString(4))
                };
            }
        }

        return conv;
    }

    public void SaveConversation(Conversation conversation)
    {
        conversation.UpdatedAt = DateTime.Now;
        using var conn = Open();
        using var tx = conn.BeginTransaction();

        using (var cmd = conn.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO conversations (id, title, created_at, updated_at) VALUES ($id, $title, $created, $updated)
                ON CONFLICT(id) DO UPDATE SET title = $title, updated_at = $updated
                """;
            cmd.Parameters.AddWithValue("$id", conversation.Id);
            cmd.Parameters.AddWithValue("$title", Enc(conversation.Title));
            cmd.Parameters.AddWithValue("$created", Ts(conversation.CreatedAt));
            cmd.Parameters.AddWithValue("$updated", Ts(conversation.UpdatedAt));
            cmd.ExecuteNonQuery();
        }

        using (var cmd = conn.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = "DELETE FROM messages WHERE conversation_id = $id";
            cmd.Parameters.AddWithValue("$id", conversation.Id);
            cmd.ExecuteNonQuery();
        }
        foreach (var m in conversation.Messages)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "INSERT INTO messages (conversation_id, role, content, timestamp) VALUES ($cid, $role, $content, $ts)";
            cmd.Parameters.AddWithValue("$cid", conversation.Id);
            cmd.Parameters.AddWithValue("$role", m.Role.ToString());
            cmd.Parameters.AddWithValue("$content", Enc(m.Content));
            cmd.Parameters.AddWithValue("$ts", Ts(m.Timestamp));
            cmd.ExecuteNonQuery();
        }

        if (conversation.GeneratedProject is { } p)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO projects (conversation_id, type, code, description, dependencies, requirement_document)
                VALUES ($id, $type, $code, $desc, $deps, $req)
                ON CONFLICT(conversation_id) DO UPDATE SET
                    type = $type, code = $code, description = $desc, dependencies = $deps, requirement_document = $req
                """;
            cmd.Parameters.AddWithValue("$id", conversation.Id);
            cmd.Parameters.AddWithValue("$type", p.Type.ToString());
            cmd.Parameters.AddWithValue("$code", Enc(p.Code));
            cmd.Parameters.AddWithValue("$desc", Enc(p.Description));
            cmd.Parameters.AddWithValue("$deps", Enc(JsonSerializer.Serialize(p.Dependencies)));
            cmd.Parameters.AddWithValue("$req", Enc(p.RequirementDocument));
            cmd.ExecuteNonQuery();
        }

        tx.Commit();
    }

    public void DeleteConversation(string id)
    {
        using var conn = Open();
        using var tx = conn.BeginTransaction();
        foreach (var sql in new[]
                 {
                     "DELETE FROM messages WHERE conversation_id = $id",
                     "DELETE FROM projects WHERE conversation_id = $id",
                     "DELETE FROM conversation_summaries WHERE conversation_id = $id",
                     "DELETE FROM conversations WHERE id = $id"
                 })
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }
        tx.Commit();
    }

    public string? GetSetting(string key)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT value FROM settings WHERE key = $key";
        cmd.Parameters.AddWithValue("$key", key);
        return cmd.ExecuteScalar() as string;
    }

    public string? GetConversationSummary(string conversationId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT summary FROM conversation_summaries WHERE conversation_id = $id";
        cmd.Parameters.AddWithValue("$id", conversationId);
        return cmd.ExecuteScalar() as string;
    }

    public void SaveConversationSummary(string conversationId, string summary)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO conversation_summaries (conversation_id, summary, updated_at)
            VALUES ($id, $summary, $ts)
            ON CONFLICT(conversation_id) DO UPDATE SET summary = $summary, updated_at = $ts
            """;
        cmd.Parameters.AddWithValue("$id", conversationId);
        cmd.Parameters.AddWithValue("$summary", summary);
        cmd.Parameters.AddWithValue("$ts", DateTime.Now.ToString("O"));
        cmd.ExecuteNonQuery();
    }

    public void SetSetting(string key, string value)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO settings (key, value) VALUES ($key, $value)
            ON CONFLICT(key) DO UPDATE SET value = $value
            """;
        cmd.Parameters.AddWithValue("$key", key);
        cmd.Parameters.AddWithValue("$value", value);
        cmd.ExecuteNonQuery();
    }
}