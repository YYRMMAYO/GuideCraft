using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GuideCraft.Services;

namespace GuideCraft.ViewModels;

/// <summary>设置窗口视图模型：API Key 引导、模型/主题选择、连接测试</summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;

    public SettingsViewModel(ISettingsService settings)
    {
        _settings = settings;
        _apiKey = settings.Settings.ApiKey;
        _selectedModel = settings.Settings.PreferredModel;
        _selectedTheme = settings.Settings.Theme;
    }

    /// <summary>可选模型列表</summary>
    public IReadOnlyList<string> Models { get; } = new[] { "deepseek-v4-flash", "deepseek-v4-pro" };

    /// <summary>可选主题列表</summary>
    public IReadOnlyList<string> Themes { get; } = new[] { ThemeManager.Light, ThemeManager.Dark };

    [ObservableProperty]
    private string _apiKey;

    [ObservableProperty]
    private string _selectedModel;

    [ObservableProperty]
    private string _selectedTheme;

    [ObservableProperty]
    private bool _isTesting;

    [ObservableProperty]
    private bool _hasApiKey;

    [ObservableProperty]
    private string _testResult = string.Empty;

    /// <summary>API Key 输入变化即判定</summary>
    partial void OnApiKeyChanged(string value)
    {
        HasApiKey = !string.IsNullOrWhiteSpace(value);
    }

    /// <summary>主题切换：立即生效并持久化</summary>
    partial void OnSelectedThemeChanged(string value)
    {
        ThemeManager.Apply(value);
        _settings.SaveTheme(value);
    }

    /// <summary>打开 DeepSeek 开放平台申请页</summary>
    [RelayCommand]
    private void OpenApiKeyPage()
    {
        Process.Start(new ProcessStartInfo("https://platform.deepseek.com")
        {
            UseShellExecute = true
        });
    }

    /// <summary>测试 API Key 是否可用</summary>
    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            TestResult = "请先填写 API Key";
            return;
        }

        IsTesting = true;
        TestResult = "正在测试连接...";
        try
        {
            var ok = await _settings.TestConnectionAsync(ApiKey.Trim(), SelectedModel);
            TestResult = ok ? "✅ 连接成功，API Key 可用" : "❌ 连接失败，请检查 Key 是否正确";
        }
        catch (Exception)
        {
            TestResult = "❌ 网络异常，请稍后重试";
        }
        finally
        {
            IsTesting = false;
        }
    }

    /// <summary>保存设置</summary>
    public void Save()
    {
        _settings.SaveApiKey(ApiKey.Trim());
        _settings.SaveModel(SelectedModel);
        _settings.SaveTheme(SelectedTheme);
    }
}
