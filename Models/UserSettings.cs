namespace GuideCraft.Models;

/// <summary>用户设置</summary>
public class UserSettings
{
    /// <summary>API Key（仅内存中存在明文，落盘为 DPAPI 密文）</summary>
    public string ApiKey { get; set; } = string.Empty;

    public string PreferredModel { get; set; } = "deepseek-v4-flash";

    public string Theme { get; set; } = "Light";

    /// <summary>系统提示词使用的中文引导语气</summary>
    public bool ShowGuideHint { get; set; } = true;
}
