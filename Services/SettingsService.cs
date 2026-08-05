using System.Security.Cryptography;
using System.Text;
using GuideCraft.Models;
using Microsoft.Extensions.DependencyInjection;

namespace GuideCraft.Services;

/// <summary>设置服务实现：API Key 经 DPAPI 加密存 SQLite，模型/主题直接落盘</summary>
public sealed class SettingsService : ISettingsService
{
    private const string KeyApiKey = "api_key";
    private const string KeyModel = "preferred_model";
    private const string KeyTheme = "theme";

    private readonly ILocalStorageService _storage;
    private readonly IDeepSeekApiClient _api;
    private UserSettings? _cache;

    public SettingsService(ILocalStorageService storage, IDeepSeekApiClient api)
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
                PreferredModel = _storage.GetSetting(KeyModel) ?? "deepseek-v4-flash",
                Theme = _storage.GetSetting(KeyTheme) ?? "Light"
            };
            return _cache;
        }
    }

    public void SaveApiKey(string apiKey)
    {
        var encrypted = Encrypt(apiKey);
        _storage.SetSetting(KeyApiKey, encrypted);
        Settings.ApiKey = apiKey;
    }

    public void SaveModel(string model)
    {
        _storage.SetSetting(KeyModel, model);
        Settings.PreferredModel = model;
    }

    public void SaveTheme(string theme)
    {
        _storage.SetSetting(KeyTheme, theme);
        Settings.Theme = theme;
    }

    public async Task<bool> TestConnectionAsync(string apiKey, string model, CancellationToken ct = default)
    {
        try
        {
            var reply = await _api.ChatAsync(
                new[] { new ChatApiMessage(ChatRole.User, "你好，请回复：连接成功") },
                apiKey, model, ct);
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
