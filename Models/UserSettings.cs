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
}
