using GuideCraft.Models;

namespace GuideCraft.Services;

/// <summary>设置服务：API Key（DPAPI 加密）、模型、主题的持久化读写</summary>
public interface ISettingsService
{
    /// <summary>获取设置（内存缓存）</summary>
    UserSettings Settings { get; }

    /// <summary>保存 API Key（加密落盘）</summary>
    void SaveApiKey(string apiKey);

    /// <summary>保存偏好模型</summary>
    void SaveModel(string model);

    /// <summary>保存主题</summary>
    void SaveTheme(string theme);

    /// <summary>测试 API Key 是否可用（调用轻量接口）</summary>
    Task<bool> TestConnectionAsync(string apiKey, string model, CancellationToken ct = default);
}
