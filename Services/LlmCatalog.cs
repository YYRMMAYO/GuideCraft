namespace GuideCraft.Services;

/// <summary>大模型提供方（OpenAI 兼容端点均可直连）</summary>
public enum LlmProvider
{
    Qwen,       // 阿里云百炼
    DeepSeek,   // DeepSeek
    OpenAI,     // OpenAI GPT
    Claude,     // Anthropic（OpenAI 兼容端点）
    Gemini,     // Google Gemini（OpenAI 兼容端点）
    GLM,        // 智谱 Zhipu
    Kimi,       // 月之暗面 Moonshot
    MiniMax,    // MiniMax
    Hunyuan,    // 腾讯混元
    Grok,       // xAI Grok
    Mistral,    // Mistral
    Custom      // 自定义 OpenAI 兼容端点
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

    /// <summary>上下文窗口（tokens，用于 token 预算提示）</summary>
    public int ContextWindow { get; init; } = 128_000;

    /// <summary>单次请求默认最大输出 tokens（防超长浪费）</summary>
    public int DefaultMaxTokens { get; init; } = 4096;

    /// <summary>是否支持服务端上下文缓存（缓存命中计费与统计）</summary>
    public bool SupportsContextCache { get; init; } = true;
}

/// <summary>模型目录（单一事实源）：全球主流提供方 + 自定义端点</summary>
public static class LlmCatalog
{
    private const string QwenBase = "https://dashscope.aliyuncs.com/compatible-mode/v1";
    private const string DeepSeekBase = "https://api.deepseek.com";
    private const string OpenAiBase = "https://api.openai.com/v1";
    private const string ClaudeBase = "https://api.anthropic.com/v1";
    private const string GeminiBase = "https://generativelanguage.googleapis.com/v1beta/openai";
    private const string GlmBase = "https://open.bigmodel.cn/api/paas/v4";
    private const string KimiBase = "https://api.moonshot.cn/v1";
    private const string MiniMaxBase = "https://api.minimax.chat/v1";
    private const string HunyuanBase = "https://api.hunyuan.cloud.tencent.com/v1";
    private const string GrokBase = "https://api.x.ai/v1";
    private const string MistralBase = "https://api.mistral.ai/v1";

    private static readonly List<LlmModelInfo> _models = new()
    {
        // ---------- 千问 Qwen（推荐 · 新用户免费额度） ----------
        new()
        {
            Id = "qwen-plus", Provider = LlmProvider.Qwen, BaseUrl = QwenBase,
            KeyUrl = "https://bailian.console.aliyun.com/", KeyHintKey = "Str.SettingsKeyHint.Qwen",
            ContextWindow = 131_072, DefaultMaxTokens = 8192, SupportsContextCache = true
        },
        new()
        {
            Id = "qwen-turbo", Provider = LlmProvider.Qwen, BaseUrl = QwenBase,
            KeyUrl = "https://bailian.console.aliyun.com/", KeyHintKey = "Str.SettingsKeyHint.Qwen",
            ContextWindow = 131_072, DefaultMaxTokens = 8192, SupportsContextCache = true
        },
        new()
        {
            Id = "qwen-max", Provider = LlmProvider.Qwen, BaseUrl = QwenBase,
            KeyUrl = "https://bailian.console.aliyun.com/", KeyHintKey = "Str.SettingsKeyHint.Qwen",
            ContextWindow = 131_072, DefaultMaxTokens = 8192, SupportsContextCache = true
        },
        new()
        {
            Id = "qwen-flash", Provider = LlmProvider.Qwen, BaseUrl = QwenBase,
            KeyUrl = "https://bailian.console.aliyun.com/", KeyHintKey = "Str.SettingsKeyHint.Qwen",
            ContextWindow = 131_072, DefaultMaxTokens = 8192, SupportsContextCache = true
        },
        new()
        {
            Id = "qwen-long", Provider = LlmProvider.Qwen, BaseUrl = QwenBase,
            KeyUrl = "https://bailian.console.aliyun.com/", KeyHintKey = "Str.SettingsKeyHint.Qwen",
            ContextWindow = 10_000_000, DefaultMaxTokens = 8192, SupportsContextCache = true
        },

        // ---------- DeepSeek（v4 系列，缓存命中价格低） ----------
        new()
        {
            Id = "deepseek-v4-flash", Provider = LlmProvider.DeepSeek, BaseUrl = DeepSeekBase,
            KeyUrl = "https://platform.deepseek.com", KeyHintKey = "Str.SettingsKeyHint.DeepSeek",
            ContextWindow = 128_000, DefaultMaxTokens = 8192, SupportsContextCache = true
        },
        new()
        {
            Id = "deepseek-v4-pro", Provider = LlmProvider.DeepSeek, BaseUrl = DeepSeekBase,
            KeyUrl = "https://platform.deepseek.com", KeyHintKey = "Str.SettingsKeyHint.DeepSeek",
            ContextWindow = 128_000, DefaultMaxTokens = 8192, SupportsContextCache = true
        },
        new()
        {
            Id = "deepseek-chat", Provider = LlmProvider.DeepSeek, BaseUrl = DeepSeekBase,
            KeyUrl = "https://platform.deepseek.com", KeyHintKey = "Str.SettingsKeyHint.DeepSeek",
            ContextWindow = 128_000, DefaultMaxTokens = 8192, SupportsContextCache = true
        },
        new()
        {
            Id = "deepseek-reasoner", Provider = LlmProvider.DeepSeek, BaseUrl = DeepSeekBase,
            KeyUrl = "https://platform.deepseek.com", KeyHintKey = "Str.SettingsKeyHint.DeepSeek",
            ContextWindow = 128_000, DefaultMaxTokens = 8192, SupportsContextCache = true
        },

        // ---------- OpenAI ----------
        new()
        {
            Id = "gpt-4o", Provider = LlmProvider.OpenAI, BaseUrl = OpenAiBase,
            KeyUrl = "https://platform.openai.com/api-keys", KeyHintKey = "Str.SettingsKeyHint.OpenAI",
            ContextWindow = 128_000, DefaultMaxTokens = 4096, SupportsContextCache = true
        },
        new()
        {
            Id = "gpt-4o-mini", Provider = LlmProvider.OpenAI, BaseUrl = OpenAiBase,
            KeyUrl = "https://platform.openai.com/api-keys", KeyHintKey = "Str.SettingsKeyHint.OpenAI",
            ContextWindow = 128_000, DefaultMaxTokens = 4096, SupportsContextCache = true
        },
        new()
        {
            Id = "gpt-4.1", Provider = LlmProvider.OpenAI, BaseUrl = OpenAiBase,
            KeyUrl = "https://platform.openai.com/api-keys", KeyHintKey = "Str.SettingsKeyHint.OpenAI",
            ContextWindow = 1_000_000, DefaultMaxTokens = 32_768, SupportsContextCache = true
        },
        new()
        {
            Id = "o3-mini", Provider = LlmProvider.OpenAI, BaseUrl = OpenAiBase,
            KeyUrl = "https://platform.openai.com/api-keys", KeyHintKey = "Str.SettingsKeyHint.OpenAI",
            ContextWindow = 200_000, DefaultMaxTokens = 8192, SupportsContextCache = true
        },

        // ---------- Anthropic Claude（OpenAI 兼容端点） ----------
        new()
        {
            Id = "claude-sonnet-4", Provider = LlmProvider.Claude, BaseUrl = ClaudeBase,
            KeyUrl = "https://console.anthropic.com/settings/keys", KeyHintKey = "Str.SettingsKeyHint.Claude",
            ContextWindow = 200_000, DefaultMaxTokens = 8192, SupportsContextCache = true
        },
        new()
        {
            Id = "claude-opus-4", Provider = LlmProvider.Claude, BaseUrl = ClaudeBase,
            KeyUrl = "https://console.anthropic.com/settings/keys", KeyHintKey = "Str.SettingsKeyHint.Claude",
            ContextWindow = 200_000, DefaultMaxTokens = 8192, SupportsContextCache = true
        },
        new()
        {
            Id = "claude-haiku-3.5", Provider = LlmProvider.Claude, BaseUrl = ClaudeBase,
            KeyUrl = "https://console.anthropic.com/settings/keys", KeyHintKey = "Str.SettingsKeyHint.Claude",
            ContextWindow = 200_000, DefaultMaxTokens = 4096, SupportsContextCache = true
        },

        // ---------- Google Gemini（OpenAI 兼容端点） ----------
        new()
        {
            Id = "gemini-2.5-flash", Provider = LlmProvider.Gemini, BaseUrl = GeminiBase,
            KeyUrl = "https://aistudio.google.com/apikey", KeyHintKey = "Str.SettingsKeyHint.Gemini",
            ContextWindow = 1_000_000, DefaultMaxTokens = 8192, SupportsContextCache = true
        },
        new()
        {
            Id = "gemini-2.5-pro", Provider = LlmProvider.Gemini, BaseUrl = GeminiBase,
            KeyUrl = "https://aistudio.google.com/apikey", KeyHintKey = "Str.SettingsKeyHint.Gemini",
            ContextWindow = 1_000_000, DefaultMaxTokens = 8192, SupportsContextCache = true
        },
        new()
        {
            Id = "gemini-3-flash", Provider = LlmProvider.Gemini, BaseUrl = GeminiBase,
            KeyUrl = "https://aistudio.google.com/apikey", KeyHintKey = "Str.SettingsKeyHint.Gemini",
            ContextWindow = 1_000_000, DefaultMaxTokens = 8192, SupportsContextCache = true
        },

        // ---------- 智谱 GLM（glm-4-flash 免费） ----------
        new()
        {
            Id = "glm-4-flash", Provider = LlmProvider.GLM, BaseUrl = GlmBase,
            KeyUrl = "https://open.bigmodel.cn/usercenter/apikeys", KeyHintKey = "Str.SettingsKeyHint.GLM",
            ContextWindow = 128_000, DefaultMaxTokens = 4096, SupportsContextCache = true
        },
        new()
        {
            Id = "glm-4-plus", Provider = LlmProvider.GLM, BaseUrl = GlmBase,
            KeyUrl = "https://open.bigmodel.cn/usercenter/apikeys", KeyHintKey = "Str.SettingsKeyHint.GLM",
            ContextWindow = 128_000, DefaultMaxTokens = 4096, SupportsContextCache = true
        },
        new()
        {
            Id = "glm-4-air", Provider = LlmProvider.GLM, BaseUrl = GlmBase,
            KeyUrl = "https://open.bigmodel.cn/usercenter/apikeys", KeyHintKey = "Str.SettingsKeyHint.GLM",
            ContextWindow = 128_000, DefaultMaxTokens = 4096, SupportsContextCache = true
        },

        // ---------- 月之暗面 Kimi ----------
        new()
        {
            Id = "kimi-k2", Provider = LlmProvider.Kimi, BaseUrl = KimiBase,
            KeyUrl = "https://platform.moonshot.cn/console/api-keys", KeyHintKey = "Str.SettingsKeyHint.Kimi",
            ContextWindow = 256_000, DefaultMaxTokens = 8192, SupportsContextCache = true
        },
        new()
        {
            Id = "moonshot-v1-32k", Provider = LlmProvider.Kimi, BaseUrl = KimiBase,
            KeyUrl = "https://platform.moonshot.cn/console/api-keys", KeyHintKey = "Str.SettingsKeyHint.Kimi",
            ContextWindow = 32_000, DefaultMaxTokens = 4096, SupportsContextCache = false
        },

        // ---------- MiniMax ----------
        new()
        {
            Id = "MiniMax-Text-01", Provider = LlmProvider.MiniMax, BaseUrl = MiniMaxBase,
            KeyUrl = "https://platform.minimaxi.com/user-center/basic-information/interface-key", KeyHintKey = "Str.SettingsKeyHint.MiniMax",
            ContextWindow = 1_000_000, DefaultMaxTokens = 8192, SupportsContextCache = false
        },

        // ---------- 腾讯混元 ----------
        new()
        {
            Id = "hunyuan-turbo", Provider = LlmProvider.Hunyuan, BaseUrl = HunyuanBase,
            KeyUrl = "https://console.cloud.tencent.com/hunyuan/api-key", KeyHintKey = "Str.SettingsKeyHint.Hunyuan",
            ContextWindow = 256_000, DefaultMaxTokens = 8192, SupportsContextCache = false
        },
        new()
        {
            Id = "hunyuan-lite", Provider = LlmProvider.Hunyuan, BaseUrl = HunyuanBase,
            KeyUrl = "https://console.cloud.tencent.com/hunyuan/api-key", KeyHintKey = "Str.SettingsKeyHint.Hunyuan",
            ContextWindow = 256_000, DefaultMaxTokens = 8192, SupportsContextCache = false
        },

        // ---------- xAI Grok ----------
        new()
        {
            Id = "grok-4", Provider = LlmProvider.Grok, BaseUrl = GrokBase,
            KeyUrl = "https://console.x.ai/api-keys", KeyHintKey = "Str.SettingsKeyHint.Grok",
            ContextWindow = 256_000, DefaultMaxTokens = 8192, SupportsContextCache = true
        },
        new()
        {
            Id = "grok-4-fast", Provider = LlmProvider.Grok, BaseUrl = GrokBase,
            KeyUrl = "https://console.x.ai/api-keys", KeyHintKey = "Str.SettingsKeyHint.Grok",
            ContextWindow = 256_000, DefaultMaxTokens = 8192, SupportsContextCache = true
        },

        // ---------- Mistral ----------
        new()
        {
            Id = "mistral-large-latest", Provider = LlmProvider.Mistral, BaseUrl = MistralBase,
            KeyUrl = "https://console.mistral.ai/api-keys", KeyHintKey = "Str.SettingsKeyHint.Mistral",
            ContextWindow = 128_000, DefaultMaxTokens = 4096, SupportsContextCache = true
        },
        new()
        {
            Id = "mistral-small-latest", Provider = LlmProvider.Mistral, BaseUrl = MistralBase,
            KeyUrl = "https://console.mistral.ai/api-keys", KeyHintKey = "Str.SettingsKeyHint.Mistral",
            ContextWindow = 128_000, DefaultMaxTokens = 4096, SupportsContextCache = true
        }
    };

    /// <summary>全部模型</summary>
    public static IReadOnlyList<LlmModelInfo> Models => _models;

    /// <summary>支持的提供方（按推荐顺序）</summary>
    public static IReadOnlyList<LlmProvider> Providers { get; } = new[]
    {
        LlmProvider.Qwen,
        LlmProvider.DeepSeek,
        LlmProvider.OpenAI,
        LlmProvider.Claude,
        LlmProvider.Gemini,
        LlmProvider.GLM,
        LlmProvider.Kimi,
        LlmProvider.MiniMax,
        LlmProvider.Hunyuan,
        LlmProvider.Grok,
        LlmProvider.Mistral,
        LlmProvider.Custom
    };

    /// <summary>默认模型：千问 qwen-plus（新用户免费额度，优先接入）</summary>
    public static LlmModelInfo Default => Find("qwen-plus")!;

    /// <summary>按模型 ID 查找</summary>
    public static LlmModelInfo? Find(string modelId)
        => string.IsNullOrWhiteSpace(modelId)
            ? null
            : _models.FirstOrDefault(m => m.Id == modelId);

    /// <summary>按提供方过滤模型</summary>
    public static IReadOnlyList<LlmModelInfo> ByProvider(LlmProvider provider)
        => _models.Where(m => m.Provider == provider).ToList();

    /// <summary>提供方显示名称（本地化 key）</summary>
    public static string ProviderNameKey(LlmProvider provider) => provider switch
    {
        LlmProvider.Qwen => "Str.Provider.Qwen",
        LlmProvider.DeepSeek => "Str.Provider.DeepSeek",
        LlmProvider.OpenAI => "Str.Provider.OpenAI",
        LlmProvider.Claude => "Str.Provider.Claude",
        LlmProvider.Gemini => "Str.Provider.Gemini",
        LlmProvider.GLM => "Str.Provider.GLM",
        LlmProvider.Kimi => "Str.Provider.Kimi",
        LlmProvider.MiniMax => "Str.Provider.MiniMax",
        LlmProvider.Hunyuan => "Str.Provider.Hunyuan",
        LlmProvider.Grok => "Str.Provider.Grok",
        LlmProvider.Mistral => "Str.Provider.Mistral",
        _ => "Str.Provider.Custom"
    };

    /// <summary>提供方 Key 获取指引的本地化 key</summary>
    public static string ProviderKeyHintKey(LlmProvider provider) => provider switch
    {
        LlmProvider.Qwen => "Str.SettingsKeyHint.Qwen",
        LlmProvider.DeepSeek => "Str.SettingsKeyHint.DeepSeek",
        LlmProvider.OpenAI => "Str.SettingsKeyHint.OpenAI",
        LlmProvider.Claude => "Str.SettingsKeyHint.Claude",
        LlmProvider.Gemini => "Str.SettingsKeyHint.Gemini",
        LlmProvider.GLM => "Str.SettingsKeyHint.GLM",
        LlmProvider.Kimi => "Str.SettingsKeyHint.Kimi",
        LlmProvider.MiniMax => "Str.SettingsKeyHint.MiniMax",
        LlmProvider.Hunyuan => "Str.SettingsKeyHint.Hunyuan",
        LlmProvider.Grok => "Str.SettingsKeyHint.Grok",
        LlmProvider.Mistral => "Str.SettingsKeyHint.Mistral",
        _ => string.Empty
    };
}
