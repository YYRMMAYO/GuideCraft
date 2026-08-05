using System.IO;
using GuideCraft.Models;
using Microsoft.Data.Sqlite;

namespace GuideCraft.Services;

/// <summary>单次 API 用量记录</summary>
public sealed class UsageRecord
{
    public DateTime Timestamp { get; set; } = DateTime.Now;

    public string ModelId { get; set; } = string.Empty;

    public long PromptTokens { get; set; }

    public long CompletionTokens { get; set; }

    public long TotalTokens { get; set; }

    public long CacheHitTokens { get; set; }

    public long CacheMissTokens { get; set; }

    /// <summary>预估费用（元）</summary>
    public double EstimatedCost { get; set; }

    /// <summary>缓存命中率（0-100），无缓存数据返回 null</summary>
    public double? CacheHitRate => CacheHitTokens + CacheMissTokens > 0
        ? 100.0 * CacheHitTokens / (CacheHitTokens + CacheMissTokens)
        : null;

    /// <summary>是否存在缓存数据（用于 UI 显示命中率徽章）</summary>
    public bool HasCacheData => CacheHitTokens + CacheMissTokens > 0;
}

/// <summary>累计统计汇总</summary>
public sealed class UsageTotals
{
    public int RequestCount { get; set; }

    public long PromptTokens { get; set; }

    public long CompletionTokens { get; set; }

    public long TotalTokens { get; set; }

    public long CacheHitTokens { get; set; }

    public long CacheMissTokens { get; set; }

    public double EstimatedCost { get; set; }

    public double? CacheHitRate => CacheHitTokens + CacheMissTokens > 0
        ? 100.0 * CacheHitTokens / (CacheHitTokens + CacheMissTokens)
        : null;
}

/// <summary>用量统计服务：记录每次 API 调用，SQLite 持久化，供统计面板展示</summary>
public interface IUsageTracker
{
    /// <summary>记录一次 API 调用用量（模型、token、缓存、费用估算）</summary>
    void Record(UsageRecord record);

    /// <summary>累计统计（全部时间）</summary>
    UsageTotals GetTotals();

    /// <summary>最近 N 天记录（用于趋势展示）</summary>
    IReadOnlyList<UsageRecord> GetRecent(int days);

    /// <summary>整体缓存命中率（0-100，无数据返回 null）</summary>
    double? GetCacheHitRate();
}

/// <summary>SQLite 实现：usage_logs 表，字段加密不需要（统计指标非敏感，模型名/计数）</summary>
public sealed class UsageTracker : IUsageTracker
{
    private const string Table = "usage_logs";
    private readonly string _dbPath;
    private readonly string _connectionString;

    public UsageTracker()
    {
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
        cmd.CommandText = $"""
            CREATE TABLE IF NOT EXISTS {Table} (
                id                INTEGER PRIMARY KEY AUTOINCREMENT,
                timestamp         TEXT NOT NULL,
                model_id          TEXT NOT NULL,
                prompt_tokens     INTEGER NOT NULL DEFAULT 0,
                completion_tokens INTEGER NOT NULL DEFAULT 0,
                total_tokens      INTEGER NOT NULL DEFAULT 0,
                cache_hit_tokens  INTEGER NOT NULL DEFAULT 0,
                cache_miss_tokens INTEGER NOT NULL DEFAULT 0,
                estimated_cost    REAL NOT NULL DEFAULT 0
            );
            CREATE INDEX IF NOT EXISTS idx_usage_ts ON {Table}(timestamp);
            """;
        cmd.ExecuteNonQuery();
    }

    private SqliteConnection Open()
    {
        var c = new SqliteConnection(_connectionString);
        c.Open();
        return c;
    }

    public void Record(UsageRecord record)
    {
        if (record.TotalTokens == 0 && record.CacheHitTokens == 0 && record.CacheMissTokens == 0)
            return; // 无有效用量不记录

        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"""
            INSERT INTO {Table}
                (timestamp, model_id, prompt_tokens, completion_tokens, total_tokens,
                 cache_hit_tokens, cache_miss_tokens, estimated_cost)
            VALUES
                ($ts, $model, $pt, $ct, $tt, $cht, $cmt, $cost)
            """;
        cmd.Parameters.AddWithValue("$ts", record.Timestamp.ToString("O"));
        cmd.Parameters.AddWithValue("$model", record.ModelId);
        cmd.Parameters.AddWithValue("$pt", record.PromptTokens);
        cmd.Parameters.AddWithValue("$ct", record.CompletionTokens);
        cmd.Parameters.AddWithValue("$tt", record.TotalTokens);
        cmd.Parameters.AddWithValue("$cht", record.CacheHitTokens);
        cmd.Parameters.AddWithValue("$cmt", record.CacheMissTokens);
        cmd.Parameters.AddWithValue("$cost", record.EstimatedCost);
        cmd.ExecuteNonQuery();
    }

    public UsageTotals GetTotals()
    {
        var totals = new UsageTotals();
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"""
            SELECT COUNT(*),
                   COALESCE(SUM(prompt_tokens),0),
                   COALESCE(SUM(completion_tokens),0),
                   COALESCE(SUM(total_tokens),0),
                   COALESCE(SUM(cache_hit_tokens),0),
                   COALESCE(SUM(cache_miss_tokens),0),
                   COALESCE(SUM(estimated_cost),0)
            FROM {Table}
            """;
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            totals.RequestCount = reader.GetInt32(0);
            totals.PromptTokens = reader.GetInt64(1);
            totals.CompletionTokens = reader.GetInt64(2);
            totals.TotalTokens = reader.GetInt64(3);
            totals.CacheHitTokens = reader.GetInt64(4);
            totals.CacheMissTokens = reader.GetInt64(5);
            totals.EstimatedCost = reader.GetDouble(6);
        }
        return totals;
    }

    public IReadOnlyList<UsageRecord> GetRecent(int days)
    {
        var list = new List<UsageRecord>();
        var since = DateTime.Now.AddDays(-days);
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"""
            SELECT timestamp, model_id, prompt_tokens, completion_tokens, total_tokens,
                   cache_hit_tokens, cache_miss_tokens, estimated_cost
            FROM {Table}
            WHERE timestamp >= $since
            ORDER BY id ASC
            """;
        cmd.Parameters.AddWithValue("$since", since.ToString("O"));
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new UsageRecord
            {
                Timestamp = DateTime.TryParse(reader.GetString(0), null, System.Globalization.DateTimeStyles.RoundtripKind, out var ts) ? ts : DateTime.Now,
                ModelId = reader.GetString(1),
                PromptTokens = reader.GetInt64(2),
                CompletionTokens = reader.GetInt64(3),
                TotalTokens = reader.GetInt64(4),
                CacheHitTokens = reader.GetInt64(5),
                CacheMissTokens = reader.GetInt64(6),
                EstimatedCost = reader.GetDouble(7)
            });
        }
        return list;
    }

    public double? GetCacheHitRate()
    {
        var totals = GetTotals();
        return totals.CacheHitRate;
    }
}
