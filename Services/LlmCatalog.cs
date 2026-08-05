namespace GuideCraft.Services;

/// <summary>大模型提供方</summary>
public enum LlmProvider
{
    Qwen,
    DeepSeek
}

/// <summary>模型接入信息（OpenAI 兼容）</summary>
public sealed class LlmModelInfo
{
    /// <summary>模型 ID（API 请求时使用）</summary>
    public required string Id { get; init; }

    /// <summary>所属提供方</summary>
    public required LlmProvider Provider { get; init; }

    /// <summary>OpenAI 兼容 Base URL（不含 /chat/completions）</summary>
    public required string BaseUrl { get; init; }

    /// <summary>申请 API Key 的页面</summary>
    public required string KeyUrl { get; init; }

    /// <summary>Key 获取指引（本地化 key 引用，如 "Str.SettingsKeyHint.Qwen"）</summary>
    public string KeyHintKey { get; init; } = string.Empty;
}

/// <summary>模型目录（单一事实源）：当前支持千问（Qwen）+ DeepSeek，均为 OpenAI 兼容接口</summary>
public static class LlmCatalog
{
    private const string QwenBase = "https://dashscope.aliyuncs.com/compatible-mode/v1";
    private const string DeepSeekBase = "https://api.deepseek.com";

    private static readonly List<LlmModelInfo> _models = new()
    {
        new LlmModelInfo
        {
            Id = "qwen-plus",
            Provider = LlmProvider.Qwen,
            BaseUrl = QwenBase,
            KeyUrl = "https://bailian.console.aliyun.com/",
            KeyHintKey = "Str.SettingsKeyHint.Qwen"
        },
        new LlmModelInfo
        {
            Id = "qwen-turbo",
            Provider = LlmProvider.Qwen,
            BaseUrl = QwenBase,
            KeyUrl = "https://bailian.console.aliyun.com/",
            KeyHintKey = "Str.SettingsKeyHint.Qwen"
        },
        new LlmModelInfo
        {
            Id = "qwen-max",
            Provider = LlmProvider.Qwen,
            BaseUrl = QwenBase,
            KeyUrl = "https://bailian.console.aliyun.com/",
            KeyHintKey = "Str.SettingsKeyHint.Qwen"
        },
        new LlmModelInfo
        {
            Id = "qwen-flash",
            Provider = LlmProvider.Qwen,
            BaseUrl = QwenBase,
            KeyUrl = "https://bailian.console.aliyun.com/",
            KeyHintKey = "Str.SettingsKeyHint.Qwen"
        },
        new LlmModelInfo
        {
            Id = "deepseek-v4-flash",
            Provider = LlmProvider.DeepSeek,
            BaseUrl = DeepSeekBase,
            KeyUrl = "https://platform.deepseek.com",
            KeyHintKey = "Str.SettingsKeyHint.DeepSeek"
        },
        new LlmModelInfo
        {
            Id = "deepseek-v4-pro",
            Provider = LlmProvider.DeepSeek,
            BaseUrl = DeepSeekBase,
            KeyUrl = "https://platform.deepseek.com",
            KeyHintKey = "Str.SettingsKeyHint.DeepSeek"
        }
    };

    /// <summary>全部模型</summary>
    public static IReadOnlyList<LlmModelInfo> Models => _models;

    /// <summary>支持的提供方（按推荐顺序：千问优先）</summary>
    public static IReadOnlyList<LlmProvider> Providers { get; } = new[]
    {
        LlmProvider.Qwen,
        LlmProvider.DeepSeek
    };

    /// <summary>默认模型：千问 qwen-plus（用户有免费额度，优先接入）</summary>
    public static LlmModelInfo Default => Find("qwen-plus")!;

    /// <summary>按模型 ID 查找</summary>
    public static LlmModelInfo? Find(string modelId)
        => string.IsNullOrWhiteSpace(modelId)
            ? null
            : _models.FirstOrDefault(m => m.Id == modelId);

    /// <summary>按提供方过滤模型</summary>
    public static IReadOnlyList<LlmModelInfo> ByProvider(LlmProvider provider)
        => _models.Where(m => m.Provider == provider).ToList();

    /// <summary>提供方名称的本地化 key</summary>
    public static string ProviderNameKey(LlmProvider provider)
        => provider == LlmProvider.Qwen ? "Str.Provider.Qwen" : "Str.Provider.DeepSeek";

    /// <summary>提供方 Key 获取指引的本地化 key</summary>
    public static string ProviderKeyHintKey(LlmProvider provider)
        => provider == LlmProvider.Qwen ? "Str.SettingsKeyHint.Qwen" : "Str.SettingsKeyHint.DeepSeek";
}
