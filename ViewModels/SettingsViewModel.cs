using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GuideCraft.Localization;
using GuideCraft.Models;
using GuideCraft.Services;

namespace GuideCraft.ViewModels;

/// <summary>设置窗口 Tab 分类</summary>
public enum SettingsTab
{
    Models,      // 模型配置
    Appearance,  // 外观
    Language,    // 语言
    Layout,      // 布局
    Agent,       // Agent 行为
    Stats,       // 用量统计
    About        // 关于更新
}

/// <summary>左侧导航项（Key 对应 SettingsTab，Name 为本地化标题）</summary>
public sealed record SettingsNavItem(SettingsTab Tab, string Name);

/// <summary>
/// 设置页视图模型（模块化页面，非弹窗）：自定义模型配置 CRUD、语言/主题/导航栏位置、更新检查。
/// 所有偏好即时保存，无需"保存"按钮。
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IModelProfileService _profiles;
    private readonly IUpdateChecker _updateChecker;
    private readonly IUsageTracker _usage;

    public SettingsViewModel(ISettingsService settings, IModelProfileService profiles, IUpdateChecker updateChecker, IUsageTracker usage)
    {
        _settings = settings;
        _profiles = profiles;
        _updateChecker = updateChecker;
        _usage = usage;

        _selectedLanguage = settings.Settings.Language;
        _selectedTheme = settings.Settings.Theme;
        _selectedSidebar = settings.Settings.SidebarPosition;
        _sandboxEnabled = settings.Settings.SandboxEnabled;
        _sandboxTimeoutSeconds = settings.Settings.SandboxTimeoutSeconds;
        _showUsageStats = settings.Settings.ShowUsageStats;
        _showStatsPanel = settings.Settings.ShowStatsPanel;
        RefreshNavItems();
        RefreshProfiles();
        RefreshStats();
    }

    // ---------- Tab 切换 ----------

    /// <summary>左侧导航项（ListBox 数据驱动，避免 RadioButton+Converter 绑定回环；语言切换时刷新）</summary>
    public ObservableCollection<SettingsNavItem> NavItems { get; } = new();

    /// <summary>当前选中的导航项（ListBox.SelectedItem 双向绑定，天然互斥）</summary>
    [ObservableProperty]
    private SettingsNavItem? _activeNav;

    partial void OnActiveNavChanged(SettingsNavItem? value)
    {
        if (value is not null)
            ActiveTab = value.Tab;
    }

    [ObservableProperty]
    private SettingsTab _activeTab;

    partial void OnActiveTabChanged(SettingsTab value)
    {
        // 同步导航选中项（程序化 OpenAt 跳转时）
        if (ActiveNav?.Tab != value)
            ActiveNav = NavItems.FirstOrDefault(n => n.Tab == value);
        OnPropertyChanged(nameof(SettingsHeaderTitle));
        OnPropertyChanged(nameof(SettingsHeaderDesc));
        if (value == SettingsTab.Stats)
            RefreshStats();
    }

    [RelayCommand]
    private void SwitchTab(SettingsTab tab) => ActiveTab = tab;

    /// <summary>重建导航项（构造与语言切换时调用，保持本地化）</summary>
    private void RefreshNavItems()
    {
        var currentTab = ActiveNav?.Tab ?? ActiveTab;
        NavItems.Clear();
        foreach (var item in new[]
                 {
                     new SettingsNavItem(SettingsTab.Models, LocalizationManager.Get("Str.SettingsNavModels")),
                     new SettingsNavItem(SettingsTab.Appearance, LocalizationManager.Get("Str.SettingsNavAppearance")),
                     new SettingsNavItem(SettingsTab.Language, LocalizationManager.Get("Str.SettingsNavLanguage")),
                     new SettingsNavItem(SettingsTab.Layout, LocalizationManager.Get("Str.SettingsNavLayout")),
                     new SettingsNavItem(SettingsTab.Agent, LocalizationManager.Get("Str.SettingsNavAgent")),
                     new SettingsNavItem(SettingsTab.Stats, LocalizationManager.Get("Str.SettingsNavStats")),
                     new SettingsNavItem(SettingsTab.About, LocalizationManager.Get("Str.SettingsNavAbout"))
                 })
            NavItems.Add(item);
        ActiveNav = NavItems.FirstOrDefault(n => n.Tab == currentTab);
    }

    /// <summary>窗口标题（随 Tab 变化）</summary>
    public string SettingsHeaderTitle => ActiveTab switch
    {
        SettingsTab.Models => LocalizationManager.Get("Str.SettingsHeaderModels"),
        SettingsTab.Appearance => LocalizationManager.Get("Str.SettingsHeaderAppearance"),
        SettingsTab.Language => LocalizationManager.Get("Str.SettingsHeaderLanguage"),
        SettingsTab.Layout => LocalizationManager.Get("Str.SettingsHeaderLayout"),
        SettingsTab.Agent => LocalizationManager.Get("Str.SettingsHeaderAgent"),
        SettingsTab.Stats => LocalizationManager.Get("Str.SettingsHeaderStats"),
        _ => LocalizationManager.Get("Str.SettingsHeaderAbout")
    };

    /// <summary>窗口副标题（随 Tab 变化）</summary>
    public string SettingsHeaderDesc => ActiveTab switch
    {
        SettingsTab.Models => LocalizationManager.Get("Str.SettingsHeaderModelsDesc"),
        SettingsTab.Appearance => LocalizationManager.Get("Str.SettingsHeaderAppearanceDesc"),
        SettingsTab.Language => LocalizationManager.Get("Str.SettingsHeaderLanguageDesc"),
        SettingsTab.Layout => LocalizationManager.Get("Str.SettingsHeaderLayoutDesc"),
        SettingsTab.Agent => LocalizationManager.Get("Str.SettingsHeaderAgentDesc"),
        SettingsTab.Stats => LocalizationManager.Get("Str.SettingsHeaderStatsDesc"),
        _ => LocalizationManager.Get("Str.SettingsHeaderAboutDesc")
    };

    /// <summary>关闭设置窗口（由窗口代码后台执行）</summary>
    [RelayCommand]
    private void CloseWindow() => System.Windows.Application.Current.Dispatcher.Invoke(() =>
    {
        var win = System.Windows.Application.Current.Windows
            .OfType<GuideCraft.Views.SettingsWindow>().FirstOrDefault();
        win?.Close();
    });

    // ---------- 模型配置管理 ----------

    [ObservableProperty]
    private ObservableCollection<ModelProfile> _profilesList = new();

    [ObservableProperty]
    private ModelProfile? _selectedProfile;

    // 编辑表单字段
    [ObservableProperty]
    private string _editName = string.Empty;

    [ObservableProperty]
    private string _editProvider = "Qwen";

    [ObservableProperty]
    private string _editBaseUrl = "https://dashscope.aliyuncs.com/compatible-mode/v1";

    [ObservableProperty]
    private string _editModelId = "qwen-plus";

    [ObservableProperty]
    private string _editApiKey = string.Empty;

    [ObservableProperty]
    private bool _editIsDefault;

    [ObservableProperty]
    private bool _editEnableCache = true;

    [ObservableProperty]
    private string _editNote = string.Empty;

    /// <summary>编辑表单可见（新增/编辑时显示）</summary>
    [ObservableProperty]
    private bool _isEditing;

    /// <summary>可用的提供方预设（新增时选择，来自模型目录）</summary>
    public IReadOnlyList<string> ProviderPresets { get; } =
        LlmCatalog.Providers.Select(p => p.ToString()).ToArray();

    /// <summary>模型预设提示（随提供方变化）</summary>
    public string ModelPresetHint
    {
        get
        {
            if (!Enum.TryParse<LlmProvider>(EditProvider, out var provider)) return "任意 OpenAI 兼容模型 ID";
            var models = LlmCatalog.ByProvider(provider);
            if (models.Count == 0) return "任意 OpenAI 兼容模型 ID";
            return "如 " + string.Join(" / ", models.Take(4).Select(m => m.Id)) + (models.Count > 4 ? " 等" : string.Empty);
        }
    }

    partial void OnEditProviderChanged(string value)
    {
        if (Enum.TryParse<LlmProvider>(value, out var provider))
        {
            var first = LlmCatalog.ByProvider(provider).FirstOrDefault();
            if (first is not null)
            {
                EditBaseUrl = first.BaseUrl;
                if (string.IsNullOrEmpty(EditModelId) || !string.Equals(EditModelId, "qwen-plus", StringComparison.OrdinalIgnoreCase))
                    EditModelId = first.Id;
            }
            else if (provider == LlmProvider.Custom)
            {
                EditBaseUrl = "https://";
            }
        }
        OnPropertyChanged(nameof(ModelPresetHint));
    }

    partial void OnSelectedProfileChanged(ModelProfile? value)
    {
        if (value is null) return;
        EditName = value.Name;
        EditProvider = value.Provider.ToString();
        EditBaseUrl = value.BaseUrl;
        EditModelId = value.ModelId;
        EditApiKey = string.Empty; // 不显示已存 Key，留空表示保留
        EditIsDefault = value.IsDefault;
        EditEnableCache = value.EnableCache;
        EditNote = value.Note;
        OnPropertyChanged(nameof(ModelPresetHint));
    }

    private void RefreshProfiles()
    {
        ProfilesList = new ObservableCollection<ModelProfile>(_profiles.GetAll());
        OnPropertyChanged(nameof(ProfilesList));
        SelectedProfile = ProfilesList.FirstOrDefault();
    }

    /// <summary>新增配置</summary>
    [RelayCommand]
    private void NewProfile()
    {
        SelectedProfile = null;
        EditName = "新配置";
        EditProvider = "Qwen";
        EditBaseUrl = "https://dashscope.aliyuncs.com/compatible-mode/v1";
        EditModelId = "qwen-plus";
        EditApiKey = string.Empty;
        EditIsDefault = ProfilesList.Count == 0;
        EditEnableCache = true;
        EditNote = string.Empty;
        IsEditing = true;
    }

    /// <summary>编辑选中配置</summary>
    [RelayCommand]
    private void EditProfile() => IsEditing = SelectedProfile is not null;

    /// <summary>保存配置（新增或更新；Key 留空时保留原值）</summary>
    [RelayCommand]
    private void SaveProfile()
    {
        if (string.IsNullOrWhiteSpace(EditName) || string.IsNullOrWhiteSpace(EditBaseUrl) || string.IsNullOrWhiteSpace(EditModelId))
            return;

        // 安全校验：Base URL 必须是 http/https
        var url = EditBaseUrl.Trim();
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != "https" && uri.Scheme != "http"))
            return;

        var profile = SelectedProfile ?? new ModelProfile();
        profile.Name = EditName.Trim();
        profile.Provider = Enum.TryParse<LlmProvider>(EditProvider, out var p) ? p : LlmProvider.Custom;
        profile.BaseUrl = url.TrimEnd('/');
        profile.ModelId = EditModelId.Trim();
        profile.IsDefault = EditIsDefault;
        profile.EnableCache = EditEnableCache;
        profile.Note = EditNote.Trim();

        _profiles.Save(profile, EditApiKey.Trim());
        RefreshProfiles();
        IsEditing = false;
    }

    /// <summary>删除选中配置</summary>
    [RelayCommand]
    private void DeleteProfile()
    {
        if (SelectedProfile is null) return;
        _profiles.Delete(SelectedProfile.Id);
        RefreshProfiles();
        IsEditing = false;
    }

    /// <summary>将选中配置设为默认</summary>
    [RelayCommand]
    private void SetDefaultProfile()
    {
        if (SelectedProfile is null) return;
        _profiles.SetDefault(SelectedProfile.Id);
        RefreshProfiles();
    }

    /// <summary>取消编辑</summary>
    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        if (SelectedProfile is not null)
            OnSelectedProfileChanged(SelectedProfile);
    }

    /// <summary>测试选中配置的连接与 Key</summary>
    [RelayCommand]
    private async Task TestProfileAsync()
    {
        var target = SelectedProfile;
        if (target is null) return;
        var key = string.IsNullOrEmpty(EditApiKey) ? _profiles.DecryptApiKey(target) : EditApiKey.Trim();
        if (string.IsNullOrEmpty(key))
        {
            TestResult = LocalizationManager.Get("Str.SettingsTestNoKey");
            return;
        }

        IsTesting = true;
        TestResult = LocalizationManager.Get("Str.SettingsTesting");
        try
        {
            var ok = await _settings.TestConnectionAsync(key, target.ModelId, target.BaseUrl);
            TestResult = ok
                ? LocalizationManager.Get("Str.SettingsTestOk")
                : LocalizationManager.Get("Str.SettingsTestFail");
        }
        catch
        {
            TestResult = LocalizationManager.Get("Str.SettingsTestNetworkError");
        }
        finally
        {
            IsTesting = false;
        }
    }

    // ---------- 测试状态 ----------

    [ObservableProperty]
    private bool _isTesting;

    [ObservableProperty]
    private string _testResult = string.Empty;

    // ---------- 偏好设置（即时保存） ----------

    public IReadOnlyList<string> Languages { get; } = new[] { LocalizationManager.Zh, LocalizationManager.En };

    [ObservableProperty]
    private string _selectedLanguage;

    partial void OnSelectedLanguageChanged(string value)
    {
        LocalizationManager.Apply(value);
        _settings.SaveLanguage(value);
        RefreshNavItems();
        OnPropertyChanged(nameof(LanguageName));
        OnPropertyChanged(nameof(ThemeName));
        OnPropertyChanged(nameof(SidebarName));
        OnPropertyChanged(nameof(SettingsHeaderTitle));
        OnPropertyChanged(nameof(SettingsHeaderDesc));
    }

    public string LanguageName => SelectedLanguage == LocalizationManager.En ? "English" : "简体中文";

    public IReadOnlyList<string> Themes { get; } = new[] { ThemeManager.Light, ThemeManager.Dark };

    [ObservableProperty]
    private string _selectedTheme;

    partial void OnSelectedThemeChanged(string value)
    {
        ThemeManager.Apply(value);
        _settings.SaveTheme(value);
        OnPropertyChanged(nameof(ThemeName));
    }

    public string ThemeName => LocalizationManager.Get(SelectedTheme == ThemeManager.Dark
        ? "Str.SettingsThemeDark" : "Str.SettingsThemeLight");

    public IReadOnlyList<string> SidebarPositions { get; } = new[] { "Right", "Left" };

    [ObservableProperty]
    private string _selectedSidebar;

    partial void OnSelectedSidebarChanged(string value)
    {
        _settings.SaveSidebarPosition(value);
        OnPropertyChanged(nameof(SidebarName));
    }

    public string SidebarName => LocalizationManager.Get(SelectedSidebar == "Left"
        ? "Str.SettingsSidebarLeft" : "Str.SettingsSidebarRight");

    // ---------- Agent 行为设置（沙盒 / 用量统计） ----------

    [ObservableProperty]
    private bool _sandboxEnabled;

    [ObservableProperty]
    private int _sandboxTimeoutSeconds = 30;

    [ObservableProperty]
    private bool _showUsageStats;

    [ObservableProperty]
    private bool _showStatsPanel;

    public IReadOnlyList<int> SandboxTimeoutOptions { get; } = new[] { 10, 20, 30, 60, 120 };

    partial void OnSandboxEnabledChanged(bool value) => _settings.SaveSandboxEnabled(value);

    partial void OnSandboxTimeoutSecondsChanged(int value) => _settings.SaveSandboxTimeout(value);

    partial void OnShowUsageStatsChanged(bool value) => _settings.SaveShowUsageStats(value);

    partial void OnShowStatsPanelChanged(bool value) => _settings.SaveShowStatsPanel(value);

    // ---------- 用量统计（来自 UsageTracker） ----------

    public IUsageTracker Usage => _usage;

    /// <summary>累计统计（供统计页绑定）</summary>
    public UsageTotals UsageTotalsView => _usage.GetTotals();

    /// <summary>最近 14 天记录（供统计页绑定）</summary>
    public IReadOnlyList<UsageRecord> UsageRecentView => _usage.GetRecent(14);

    /// <summary>整体缓存命中率显示（"82.5%" 或 "—"）</summary>
    public string UsageCacheRateDisplay => _usage.GetCacheHitRate() is { } r ? $"{r:F1}%" : "—";

    /// <summary>统计汇总文本（缓存命中率与费用）</summary>
    [ObservableProperty]
    private string _statsSummary = string.Empty;

    /// <summary>刷新统计页（切到统计 Tab 或点击刷新按钮时调用）</summary>
    [RelayCommand]
    private void RefreshStats()
    {
        var totals = _usage.GetTotals();
        OnPropertyChanged(nameof(Usage));
        OnPropertyChanged(nameof(UsageTotalsView));
        OnPropertyChanged(nameof(UsageRecentView));
        OnPropertyChanged(nameof(UsageCacheRateDisplay));
        StatsSummary = totals.CacheHitRate is { } rate
            ? $"{totals.RequestCount} 次请求 · {totals.TotalTokens:N0} tokens · 缓存命中 {rate:F1}% · 预估费用 ¥{totals.EstimatedCost:F4}"
            : $"{totals.RequestCount} 次请求 · {totals.TotalTokens:N0} tokens · 预估费用 ¥{totals.EstimatedCost:F4}";
    }

    // ---------- 更新检查 ----------

    [ObservableProperty]
    private bool _isCheckingUpdate;

    [ObservableProperty]
    private string _updateStatus = string.Empty;

    [ObservableProperty]
    private bool _hasUpdate;

    [ObservableProperty]
    private string _releaseUrl = string.Empty;

    [RelayCommand]
    private async Task CheckUpdateAsync()
    {
        IsCheckingUpdate = true;
        UpdateStatus = LocalizationManager.Get("Str.SettingsCheckingUpdate");
        HasUpdate = false;
        try
        {
            var result = await _updateChecker.CheckAsync();
            if (result.Error is not null)
            {
                UpdateStatus = LocalizationManager.Get("Str.SettingsUpdateFailed");
            }
            else if (result.HasUpdate)
            {
                HasUpdate = true;
                ReleaseUrl = result.ReleaseUrl ?? string.Empty;
                UpdateStatus = LocalizationManager.Get("Str.SettingsNewVersion")
                    .Replace("{version}", result.LatestTag ?? string.Empty);
            }
            else
            {
                UpdateStatus = LocalizationManager.Get("Str.SettingsUpToDate");
            }
        }
        finally
        {
            IsCheckingUpdate = false;
        }
    }

    [RelayCommand]
    private void OpenRelease()
    {
        if (!string.IsNullOrEmpty(ReleaseUrl))
            Process.Start(new ProcessStartInfo(ReleaseUrl) { UseShellExecute = true });
    }

    // ---------- 关于 ----------

    public string VersionText
    {
        get
        {
            var v = typeof(SettingsViewModel).Assembly.GetName().Version;
            return v?.ToString(3) ?? "1.0.0";
        }
    }
}