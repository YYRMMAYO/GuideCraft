using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GuideCraft.Localization;
using GuideCraft.Services;

namespace GuideCraft.ViewModels;

/// <summary>设置窗口视图模型：API Key 引导、模型提供方/模型、语言、主题、导航栏位置、检查更新</summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IUpdateChecker _updateChecker;

    public SettingsViewModel(ISettingsService settings, IUpdateChecker updateChecker)
    {
        _settings = settings;
        _updateChecker = updateChecker;
        _apiKey = settings.Settings.ApiKey;
        _selectedModel = settings.Settings.PreferredModel;
        _selectedTheme = settings.Settings.Theme;
        _selectedLanguage = settings.Settings.Language;
        _selectedSidebar = settings.Settings.SidebarPosition;

        var modelInfo = LlmCatalog.Find(_selectedModel);
        _selectedProvider = modelInfo?.Provider ?? LlmProvider.Qwen;
        _keyHintText = ProviderKeyHint(_selectedProvider);
        _apiKeyPageLabel = _selectedProvider == LlmProvider.Qwen ? "阿里云百炼" : "DeepSeek";
        RefreshModels();
    }

    // ---------- 提供方 / 模型 ----------

    [ObservableProperty]
    private LlmProvider _selectedProvider;

    [ObservableProperty]
    private string _selectedModel;

    [ObservableProperty]
    private IReadOnlyList<LlmModelInfo> _models = new List<LlmModelInfo>();

    /// <summary>当前提供方 Key 获取提示（本地化）</summary>
    [ObservableProperty]
    private string _keyHintText = string.Empty;

    /// <summary>申请页按钮文案（本地化）</summary>
    [ObservableProperty]
    private string _apiKeyPageLabel = string.Empty;

    /// <summary>支持的提供方（千问优先）</summary>
    public IReadOnlyList<LlmProvider> Providers => LlmCatalog.Providers;

    /// <summary>提供方显示名（本地化）</summary>
    public static string ProviderName(LlmProvider p) => LocalizationManager.Get(LlmCatalog.ProviderNameKey(p));

    /// <summary>提供方 Key 获取提示（本地化）</summary>
    public static string ProviderKeyHint(LlmProvider p) => LocalizationManager.Get(LlmCatalog.ProviderKeyHintKey(p));

    /// <summary>提供方变化 → 刷新模型列表并选择该组首个模型</summary>
    partial void OnSelectedProviderChanged(LlmProvider value)
    {
        RefreshModels();
        KeyHintText = ProviderKeyHint(value);
        ApiKeyPageLabel = value == LlmProvider.Qwen
            ? "阿里云百炼"
            : "DeepSeek";
        if (Models.Count > 0)
        {
            SelectedModel = Models[0].Id;
        }
    }

    partial void OnSelectedModelChanged(string value)
    {
        // 模型变化时同步提供方（如用户在模型下拉直接切换）
        var info = LlmCatalog.Find(value);
        if (info is not null && info.Provider != SelectedProvider)
        {
            SelectedProvider = info.Provider;
        }
    }

    private void RefreshModels()
    {
        Models = LlmCatalog.ByProvider(SelectedProvider);
        OnPropertyChanged(nameof(Models));
    }

    // ---------- API Key ----------

    [ObservableProperty]
    private string _apiKey;

    [ObservableProperty]
    private bool _hasApiKey;

    [ObservableProperty]
    private bool _isTesting;

    [ObservableProperty]
    private string _testResult = string.Empty;

    partial void OnApiKeyChanged(string value)
    {
        HasApiKey = !string.IsNullOrWhiteSpace(value);
    }

    /// <summary>打开当前提供方申请页</summary>
    [RelayCommand]
    private void OpenApiKeyPage()
    {
        var info = LlmCatalog.Find(SelectedModel);
        var url = info?.KeyUrl ?? LlmCatalog.Default.KeyUrl;
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    /// <summary>测试 API Key 是否可用</summary>
    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            TestResult = LocalizationManager.Get("Str.SettingsTestNoKey");
            return;
        }

        IsTesting = true;
        TestResult = LocalizationManager.Get("Str.SettingsTesting");
        try
        {
            var ok = await _settings.TestConnectionAsync(ApiKey.Trim(), SelectedModel);
            TestResult = ok
                ? LocalizationManager.Get("Str.SettingsTestOk")
                : LocalizationManager.Get("Str.SettingsTestFail");
        }
        catch (Exception)
        {
            TestResult = LocalizationManager.Get("Str.SettingsTestNetworkError");
        }
        finally
        {
            IsTesting = false;
        }
    }

    // ---------- 主题 / 语言 / 导航栏 ----------

    public IReadOnlyList<string> Themes { get; } = new[] { ThemeManager.Light, ThemeManager.Dark };

    [ObservableProperty]
    private string _selectedTheme;

    partial void OnSelectedThemeChanged(string value)
    {
        ThemeManager.Apply(value);
        _settings.SaveTheme(value);
    }

    public IReadOnlyList<string> Languages { get; } = new[] { LocalizationManager.Zh, LocalizationManager.En };

    [ObservableProperty]
    private string _selectedLanguage;

    /// <summary>语言名称（本地化显示）</summary>
    public static string LanguageName(string lang)
        => lang == LocalizationManager.En ? "English" : "简体中文";

    partial void OnSelectedLanguageChanged(string value)
    {
        LocalizationManager.Apply(value);
        _settings.SaveLanguage(value);
        // 通知界面刷新（提供方/模型名称等动态文案）
        OnPropertyChanged(nameof(Providers));
    }

    public IReadOnlyList<string> SidebarPositions { get; } = new[] { "Right", "Left" };

    [ObservableProperty]
    private string _selectedSidebar;

    public static string SidebarName(string pos)
        => pos == "Left"
            ? LocalizationManager.Get("Str.SettingsSidebarLeft")
            : LocalizationManager.Get("Str.SettingsSidebarRight");

    partial void OnSelectedSidebarChanged(string value)
    {
        _settings.SaveSidebarPosition(value);
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

    /// <summary>打开 Release 下载页</summary>
    [RelayCommand]
    private void OpenRelease()
    {
        if (!string.IsNullOrEmpty(ReleaseUrl))
            Process.Start(new ProcessStartInfo(ReleaseUrl) { UseShellExecute = true });
    }

    // ---------- 版本 / 保存 ----------

    /// <summary>应用版本号</summary>
    public string VersionText
    {
        get
        {
            var v = typeof(SettingsViewModel).Assembly.GetName().Version;
            return v?.ToString(3) ?? "1.0.0";
        }
    }

    public void Save()
    {
        _settings.SaveApiKey(ApiKey.Trim());
        _settings.SaveModel(SelectedModel);
        _settings.SaveTheme(SelectedTheme);
        _settings.SaveLanguage(SelectedLanguage);
        _settings.SaveSidebarPosition(SelectedSidebar);
    }
}