using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GuideCraft.Localization;
using GuideCraft.Models;
using GuideCraft.Services;

namespace GuideCraft.ViewModels;

/// <summary>
/// 设置页视图模型（模块化页面，非弹窗）：自定义模型配置 CRUD、语言/主题/导航栏位置、更新检查。
/// 所有偏好即时保存，无需"保存"按钮。
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IModelProfileService _profiles;
    private readonly IUpdateChecker _updateChecker;

    public SettingsViewModel(ISettingsService settings, IModelProfileService profiles, IUpdateChecker updateChecker)
    {
        _settings = settings;
        _profiles = profiles;
        _updateChecker = updateChecker;

        _selectedLanguage = settings.Settings.Language;
        _selectedTheme = settings.Settings.Theme;
        _selectedSidebar = settings.Settings.SidebarPosition;
        RefreshProfiles();
    }

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

    /// <summary>可用的提供方预设（新增时选择）</summary>
    public IReadOnlyList<string> ProviderPresets { get; } = new[] { "Qwen", "DeepSeek", "Custom" };

    /// <summary>模型预设提示（随提供方变化）</summary>
    public string ModelPresetHint => EditProvider switch
    {
        "Qwen" => "如 qwen-plus / qwen-turbo / qwen-max / qwen-flash",
        "DeepSeek" => "如 deepseek-v4-flash / deepseek-v4-pro",
        _ => "任意 OpenAI 兼容模型 ID"
    };

    partial void OnEditProviderChanged(string value)
    {
        if (value == "Qwen") EditBaseUrl = "https://dashscope.aliyuncs.com/compatible-mode/v1";
        else if (value == "DeepSeek") EditBaseUrl = "https://api.deepseek.com";
        else EditBaseUrl = "https://";
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
        OnPropertyChanged(nameof(LanguageName));
        OnPropertyChanged(nameof(ThemeName));
        OnPropertyChanged(nameof(SidebarName));
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