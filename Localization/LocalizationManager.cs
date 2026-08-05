using System.Windows;

namespace GuideCraft.Localization;

/// <summary>
/// 本地化管理器：通过替换 MergedDictionaries 中的 Strings.* 资源字典实现运行时语言切换。
/// XAML 侧用 {DynamicResource Str.xxx} 绑定；C# 侧用 LocalizationManager.Get("Str.xxx") 读取。
/// </summary>
public static class LocalizationManager
{
    public const string Zh = "zh-CN";
    public const string En = "en-US";

    public static string Current { get; private set; } = Zh;

    /// <summary>当前语言是否为中文</summary>
    public static bool IsZh => Current == Zh;

    /// <summary>应用语言（替换 App 资源中的语言字典，后添加者优先查找）</summary>
    public static void Apply(string language)
    {
        Current = language;
        if (Application.Current is null) return;

        var merged = Application.Current.Resources.MergedDictionaries;
        var old = merged.FirstOrDefault(d =>
            d.Source != null && d.Source.OriginalString.Contains("Strings."));
        if (old is not null) merged.Remove(old);

        var newDict = new ResourceDictionary
        {
            Source = new Uri($"pack://application:,,,/Localization/Strings.{language}.xaml", UriKind.Absolute)
        };
        merged.Add(newDict); // 末尾添加 → 最高查找优先级
    }

    /// <summary>从当前语言资源字典读取字符串；缺失时回退中文，再缺失返回 key</summary>
    public static string Get(string key)
    {
        if (Application.Current is not null)
        {
            if (Application.Current.TryFindResource(key) is string s && !string.IsNullOrEmpty(s))
                return s;
        }
        // 回退内置中文表（进程启动早期 / 无 Application 时）
        return FallbackZh.TryGetValue(key, out var v) ? v : key;
    }

    /// <summary>中文字符串回退表（覆盖全部 Str.* key）</summary>
    public static readonly Dictionary<string, string> FallbackZh = new()
    {
        ["Str.AppTitle"] = "GuideCraft · 引导式AI助手",
        ["Str.AppSubtitle"] = "引导式AI助手",
        ["Str.NewChat"] = "＋ 新对话",
        ["Str.ApiKeyNotSet"] = "未配置 API Key",
        ["Str.ApiKeySet"] = "API Key 已配置",
        ["Str.TooltipTheme"] = "切换浅色/深色主题",
        ["Str.TooltipSettings"] = "设置",
        ["Str.InputTooltip"] = "Enter 发送，Shift+Enter 换行",
        ["Str.Send"] = "发送 ➤",
        ["Str.Stop"] = "■ 停止",
        ["Str.ExportZip"] = "📦 导出项目 ZIP",
        ["Str.ExportHint"] = "生成产物后点击下载完整项目（main.py + 依赖 + README）",
        ["Str.ExportSuccess"] = "已导出到：\n{path}",
        ["Str.ExportFailed"] = "导出失败：{error}",
        ["Str.EmptyTitle"] = "欢迎使用 GuideCraft",
        ["Str.EmptySubtitle"] = "我会通过多轮提问帮你把模糊想法澄清为可执行的 Python 自动化脚本。",
        ["Str.EmptyBeforeStart"] = "开始之前",
        ["Str.EmptyBeforeStartHint"] = "点击右上角 ⚙ 设置，配置大模型 API Key 后即可开始引导式对话。",
        ["Str.EmptyExample1"] = "· 我想做一个自动整理每天邮件的脚本",
        ["Str.EmptyExample2"] = "· 帮我从 Excel 提取数据并生成报表",
        ["Str.EmptyExample3"] = "· 想监控某个网站价格变化并通知我",
        ["Str.SettingsTitle"] = "设置",
        ["Str.SettingsApiKeyTitle"] = "🔑 API Key",
        ["Str.SettingsApiKeyGuide"] = "前往平台注册并创建 API Key（免费额度即可满足日常使用）。Key 仅保存在你本机（DPAPI 加密），不会上传。",
        ["Str.SettingsApiKeyLabel"] = "API Key",
        ["Str.SettingsTestConnection"] = "测试连接",
        ["Str.SettingsTesting"] = "测试中...",
        ["Str.SettingsTestNoKey"] = "请先填写 API Key",
        ["Str.SettingsTestOk"] = "✅ 连接成功，API Key 可用",
        ["Str.SettingsTestFail"] = "❌ 连接失败，请检查 Key 是否正确",
        ["Str.SettingsTestNetworkError"] = "❌ 网络异常，请稍后重试",
        ["Str.SettingsProvider"] = "模型提供方",
        ["Str.SettingsModel"] = "模型",
        ["Str.SettingsLanguage"] = "语言 / Language",
        ["Str.SettingsTheme"] = "界面主题",
        ["Str.SettingsThemeLight"] = "浅色",
        ["Str.SettingsThemeDark"] = "深色",
        ["Str.SettingsSidebar"] = "导航栏位置",
        ["Str.SettingsSidebarLeft"] = "左侧",
        ["Str.SettingsSidebarRight"] = "右侧",
        ["Str.SettingsSave"] = "保存",
        ["Str.SettingsCancel"] = "取消",
        ["Str.SettingsAbout"] = "关于",
        ["Str.SettingsVersion"] = "版本",
        ["Str.SettingsCheckUpdate"] = "检查更新",
        ["Str.SettingsCheckingUpdate"] = "检查中...",
        ["Str.SettingsUpToDate"] = "✅ 已是最新版本",
        ["Str.SettingsNewVersion"] = "发现新版本 {version}",
        ["Str.SettingsOpenRelease"] = "前往下载",
        ["Str.SettingsUpdateFailed"] = "检查更新失败（网络异常）",
        ["Str.Chat.NoApiKeyHint"] = "请先配置 API Key：点击右上角 ⚙ 设置，前往平台申请 Key 后填入。",
        ["Str.Chat.ApiKeyInvalid"] = "API Key 无效或已失效，请到设置中更新 Key。",
        ["Str.Chat.RateLimited"] = "请求过于频繁（限流），请稍后再试。",
        ["Str.Chat.ServerError"] = "服务暂时不可用，请稍后重试。",
        ["Str.Chat.NetworkError"] = "网络连接异常，请检查网络后重试。",
        ["Str.Chat.StreamTimeout"] = "响应超时，请稍后重试。",
        ["Str.Chat.ConfirmGenerateHint"] = "✅ 需求已明确。回复「确认」即可生成 Python 代码，或继续补充说明。",
        ["Str.Chat.StopGenerated"] = "（已停止生成）",
        ["Str.Chat.GeneratingStatus"] = "✅ 收到确认，正在整理需求并生成代码...",
        ["Str.Chat.GeneratedHeader"] = "✅ 已生成 Python 脚本",
        ["Str.Chat.Deps"] = "依赖",
        ["Str.Chat.NoDeps"] = "（无第三方依赖）",
        ["Str.Chat.IterateHint"] = "你可以回复修改意见，我会更新代码；或点击下方「📦 导出项目 ZIP」按钮下载完整项目",
        ["Str.Chat.NeedPythonHint"] = "需安装 Python 3.8+ 后运行",
        ["Str.Chat.GenerateFailed"] = "生成失败",
        ["Str.Welcome.Title"] = "欢迎使用 GuideCraft",
        ["Str.Welcome.Subtitle"] = "4 步快速上手：把你的想法变成可运行的自动化脚本",
        ["Str.Welcome.Step1Title"] = "配置 API Key",
        ["Str.Welcome.Step1Text"] = "点击右上角 ⚙ 设置，前往阿里云百炼或 DeepSeek 平台免费申请 Key",
        ["Str.Welcome.Step2Title"] = "描述你的想法",
        ["Str.Welcome.Step2Text"] = "用一句话说你想自动化什么，AI 会像顾问一样引导你逐步澄清细节",
        ["Str.Welcome.Step3Title"] = "确认需求摘要",
        ["Str.Welcome.Step3Text"] = "AI 生成结构化需求文档，确认后自动为你编写 Python 代码",
        ["Str.Welcome.Step4Title"] = "导出运行",
        ["Str.Welcome.Step4Text"] = "一键导出项目 ZIP，安装依赖后即可运行你的自动化脚本",
        ["Str.Welcome.Start"] = "开始使用",
        ["Str.Welcome.Skip"] = "跳过引导",
        ["Str.CodeBlock.Copy"] = "📋 复制",
        ["Str.CodeBlock.Copied"] = "✓ 已复制",
        ["Str.CodeBlock.CopyFailed"] = "复制失败",
        ["Str.Provider.Qwen"] = "千问 Qwen（推荐 · 免费额度）",
        ["Str.Provider.DeepSeek"] = "DeepSeek",
        ["Str.SettingsKeyHint.Qwen"] = "前往阿里云百炼控制台创建 DashScope API Key（新用户有免费额度）",
        ["Str.SettingsKeyHint.DeepSeek"] = "前往 DeepSeek 开放平台创建 API Key（免费额度即可日常使用）",
    };
}
