namespace GuideCraft.Models;

/// <summary>用户设置</summary>
public class UserSettings
{
    /// <summary>API Key（仅内存中存在明文，落盘为 DPAPI 密文）</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>偏好模型 ID（如 qwen-plus / deepseek-v4-flash）</summary>
    public string PreferredModel { get; set; } = "qwen-plus";

    public string Theme { get; set; } = "Light";

    /// <summary>界面语言（zh-CN / en-US）</summary>
    public string Language { get; set; } = "zh-CN";

    /// <summary>导航栏位置（Left / Right）</summary>
    public string SidebarPosition { get; set; } = "Right";

    /// <summary>是否已展示首次引导</summary>
    public bool WelcomeShown { get; set; }

    // ---------- Agent 行为设置（v2.0） ----------

    /// <summary>是否启用代码沙盒试运行（默认开启）</summary>
    public bool SandboxEnabled { get; set; } = true;

    /// <summary>沙盒试运行超时秒数（默认 30s）</summary>
    public int SandboxTimeoutSeconds { get; set; } = 30;

    /// <summary>是否在对话中显示用量/费用统计（默认开启）</summary>
    public bool ShowUsageStats { get; set; } = true;

    /// <summary>是否自动打开用量统计面板（默认关闭）</summary>
    public bool ShowStatsPanel { get; set; }
}
