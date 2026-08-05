using System.Security.Cryptography;
using System.Text;
using GuideCraft.Models;
using Microsoft.Extensions.DependencyInjection;

namespace GuideCraft.Services;

/// <summary>设置服务：API Key（DPAPI 加密）、模型、主题、语言、导航栏位置的持久化读写</summary>
public interface ISettingsService
{
    /// <summary>获取设置（内存缓存）</summary>
    UserSettings Settings { get; }

    /// <summary>当前模型接入信息（按 PreferredModel 推导，未知则回退默认）</summary>
    LlmModelInfo CurrentModelInfo { get; }

    /// <summary>保存 API Key（加密落盘）</summary>
    void SaveApiKey(string apiKey);

    /// <summary>保存偏好模型</summary>
    void SaveModel(string model);

    /// <summary>保存主题</summary>
    void SaveTheme(string theme);

    /// <summary>保存语言</summary>
    void SaveLanguage(string language);

    /// <summary>保存导航栏位置</summary>
    void SaveSidebarPosition(string position);

    /// <summary>保存沙盒开关</summary>
    void SaveSandboxEnabled(bool enabled);

    /// <summary>保存沙盒超时秒数</summary>
    void SaveSandboxTimeout(int seconds);

    /// <summary>保存用量统计显示开关</summary>
    void SaveShowUsageStats(bool show);

    /// <summary>保存统计面板开关</summary>
    void SaveShowStatsPanel(bool show);

    /// <summary>标记首次引导已展示</summary>
    void MarkWelcomeShown();

    /// <summary>测试 API Key 是否可用（调用轻量接口）</summary>
    Task<bool> TestConnectionAsync(string apiKey, string modelId, string baseUrl, CancellationToken ct = default);
}

/// <summary>设置服务实现：API Key 经 DPAPI 加密存 SQLite，其余设置直接落盘</summary>
public sealed class SettingsService : ISettingsService
{
    private const string KeyApiKey = "api_key";
    private const string KeyModel = "preferred_model";
    private const string KeyTheme = "theme";
    private const string KeyLanguage = "language";
    private const string KeySidebar = "sidebar_position";
    private const string KeyWelcomeShown = "welcome_shown";
    private const string KeySandboxEnabled = "sandbox_enabled";
    private const string KeySandboxTimeout = "sandbox_timeout";
    private const string KeyShowUsageStats = "show_usage_stats";
    private const string KeyShowStatsPanel = "show_stats_panel";

    private readonly ILocalStorageService _storage;
    private readonly ILlmClient _api;
    private UserSettings? _cache;

    public SettingsService(ILocalStorageService storage, ILlmClient api)
    {
        _storage = storage;
        _api = api;
    }

    /// <summary>内存缓存设置；API Key 解密失败时静默降级为空</summary>
    public UserSettings Settings
    {
        get
        {
            if (_cache is not null) return _cache;
            _cache = new UserSettings
            {
                ApiKey = TryDecrypt(_storage.GetSetting(KeyApiKey) ?? string.Empty),
                PreferredModel = _storage.GetSetting(KeyModel) ?? "qwen-plus",
                Theme = _storage.GetSetting(KeyTheme) ?? "Light",
                Language = _storage.GetSetting(KeyLanguage) ?? "zh-CN",
                SidebarPosition = _storage.GetSetting(KeySidebar) ?? "Right",
                WelcomeShown = _storage.GetSetting(KeyWelcomeShown) == "1",
                SandboxEnabled = _storage.GetSetting(KeySandboxEnabled) is not "0",
                SandboxTimeoutSeconds = int.TryParse(_storage.GetSetting(KeySandboxTimeout), out var t) && t > 0 ? t : 30,
                ShowUsageStats = _storage.GetSetting(KeyShowUsageStats) is not "0",
                ShowStatsPanel = _storage.GetSetting(KeyShowStatsPanel) == "1"
            };
            return _cache;
        }
    }

    public LlmModelInfo CurrentModelInfo
        => LlmCatalog.Find(Settings.PreferredModel) ?? LlmCatalog.Default;

    public void SaveApiKey(string apiKey)
    {
        var trimmed = (apiKey ?? string.Empty).Trim();
        var encrypted = Encrypt(trimmed);
        _storage.SetSetting(KeyApiKey, encrypted);
        Settings.ApiKey = trimmed;
    }

    public void SaveModel(string model)
    {
        if (LlmCatalog.Find(model) is null) return; // 拒绝未知模型
        _storage.SetSetting(KeyModel, model);
        Settings.PreferredModel = model;
    }

    public void SaveTheme(string theme)
    {
        _storage.SetSetting(KeyTheme, theme);
        Settings.Theme = theme;
    }

    public void SaveLanguage(string language)
    {
        if (language is not (Localization.LocalizationManager.Zh or Localization.LocalizationManager.En)) return;
        _storage.SetSetting(KeyLanguage, language);
        Settings.Language = language;
    }

    public void SaveSidebarPosition(string position)
    {
        if (position is not ("Left" or "Right")) return;
        _storage.SetSetting(KeySidebar, position);
        Settings.SidebarPosition = position;
    }

    public void SaveSandboxEnabled(bool enabled)
    {
        _storage.SetSetting(KeySandboxEnabled, enabled ? "1" : "0");
        Settings.SandboxEnabled = enabled;
    }

    public void SaveSandboxTimeout(int seconds)
    {
        if (seconds <= 0) return;
        _storage.SetSetting(KeySandboxTimeout, seconds.ToString());
        Settings.SandboxTimeoutSeconds = seconds;
    }

    public void SaveShowUsageStats(bool show)
    {
        _storage.SetSetting(KeyShowUsageStats, show ? "1" : "0");
        Settings.ShowUsageStats = show;
    }

    public void SaveShowStatsPanel(bool show)
    {
        _storage.SetSetting(KeyShowStatsPanel, show ? "1" : "0");
        Settings.ShowStatsPanel = show;
    }

    public void MarkWelcomeShown()
    {
        _storage.SetSetting(KeyWelcomeShown, "1");
        Settings.WelcomeShown = true;
    }

    public async Task<bool> TestConnectionAsync(string apiKey, string modelId, string baseUrl, CancellationToken ct = default)
    {
        try
        {
            var reply = await _api.ChatAsync(
                new[] { new ChatApiMessage(ChatRole.User, "你好，请回复：连接成功") },
                apiKey, baseUrl, modelId, ct);
            return !string.IsNullOrWhiteSpace(reply);
        }
        catch (Exception)
        {
            return false;
        }
    }

    // ---------- DPAPI ----------

    private static string Encrypt(string plain)
    {
        if (string.IsNullOrEmpty(plain)) return string.Empty;
        var bytes = ProtectedData.Protect(
            Encoding.UTF8.GetBytes(plain),
            null,
            DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(bytes);
    }

    private static string TryDecrypt(string cipher)
    {
        if (string.IsNullOrEmpty(cipher)) return string.Empty;
        try
        {
            var bytes = ProtectedData.Unprotect(
                Convert.FromBase64String(cipher),
                null,
                DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(bytes);
        }
        catch (CryptographicException)
        {
            // 用户配置文件变更或密文损坏：无法解密，返回空并提示重新输入
            return string.Empty;
        }
    }
}

/// <summary>API 请求的消息载体（OpenAI 兼容格式）</summary>
public record ChatApiMessage(ChatRole Role, string Content)
{
    /// <summary>转为 API 需要的 role 字符串</summary>
    public string RoleName => Role switch
    {
        ChatRole.System => "system",
        ChatRole.User => "user",
        _ => "assistant"
    };
}
