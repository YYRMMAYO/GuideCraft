using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GuideCraft.Models;
using GuideCraft.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace GuideCraft.ViewModels;

/// <summary>主窗口视图模型：引导式对话状态机、设置抽屉、API 流式调用、缓存/费用显示</summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IServiceProvider _services;
    private readonly Dispatcher _ui;
    private CancellationTokenSource? _cts;
    private GeneratedCode? _lastGeneratedCode;

    public MainViewModel(IServiceProvider services)
    {
        _services = services;
        _ui = Application.Current.Dispatcher;
        _messages = new ObservableCollection<ChatMessageViewModel>();
        _conversation = new Conversation();
        _conversations = new ObservableCollection<Conversation>();
        _selectedConversation = null;
        _messages.CollectionChanged += (_, _) => HasMessages = _messages.Count > 0;

        var settings = services.GetService<ISettingsService>();
        if (settings is not null)
        {
            _hasApiKey = CurrentProfile is not null
                          && !string.IsNullOrWhiteSpace(_profiles.DecryptApiKey(CurrentProfile));
            _isSidebarRight = settings.Settings.SidebarPosition != "Left";
            _isWelcomeVisible = !settings.Settings.WelcomeShown;
        }
        StartWelcomeLoop();
    }

    // ---------- 设置窗口（独立窗口） ----------

    [RelayCommand]
    private void OpenSettings()
    {
        RefreshLocalized();
        _ui.InvokeAsync(() =>
        {
            var settingsWin = System.Windows.Application.Current.Windows
                .OfType<GuideCraft.Views.SettingsWindow>().FirstOrDefault();
            if (settingsWin is null)
            {
                var window = new GuideCraft.Views.SettingsWindow(_services);
                window.OpenAt(ViewModels.SettingsTab.Models);
            }
            else
            {
                if (settingsWin.WindowState == WindowState.Minimized)
                    settingsWin.WindowState = WindowState.Normal;
                settingsWin.OpenAt(ViewModels.SettingsTab.Models);
            }
        });
    }

    // ---------- 布局与引导 ----------

    [ObservableProperty]
    private bool _isSidebarRight = true;

    [ObservableProperty]
    private System.Windows.GridLength _sidebarWidth = new(300);

    public bool IsLeftLayout => !IsSidebarRight;
    public bool IsRightLayout => IsSidebarRight;

    partial void OnIsSidebarRightChanged(bool value)
    {
        OnPropertyChanged(nameof(IsLeftLayout));
        OnPropertyChanged(nameof(IsRightLayout));
    }

    [ObservableProperty]
    private bool _isWelcomeVisible;

    [ObservableProperty]
    private int _currentStep;

    private static readonly (string Icon, string TitleKey, string TextKey)[] WelcomeStepData =
    {
        ("🔑", "Str.Welcome.Step1Title", "Str.Welcome.Step1Text"),
        ("💡", "Str.Welcome.Step2Title", "Str.Welcome.Step2Text"),
        ("📋", "Str.Welcome.Step3Title", "Str.Welcome.Step3Text"),
        ("🚀", "Str.Welcome.Step4Title", "Str.Welcome.Step4Text")
    };

    public string CurrentStepIcon => WelcomeStepData[CurrentStep].Icon;
    public string CurrentStepTitle => Localization.LocalizationManager.Get(WelcomeStepData[CurrentStep].TitleKey);
    public string CurrentStepText => Localization.LocalizationManager.Get(WelcomeStepData[CurrentStep].TextKey);

    partial void OnCurrentStepChanged(int value)
    {
        OnPropertyChanged(nameof(CurrentStepIcon));
        OnPropertyChanged(nameof(CurrentStepTitle));
        OnPropertyChanged(nameof(CurrentStepText));
        WelcomeStepChanged?.Invoke(value);
    }

    public event Action<int>? WelcomeStepChanged;

    private System.Windows.Threading.DispatcherTimer? _welcomeTimer;

    private void StartWelcomeLoop()
    {
        if (!IsWelcomeVisible) return;
        _welcomeTimer ??= new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3.2)
        };
        _welcomeTimer.Tick += (_, _) => CurrentStep = (CurrentStep + 1) % WelcomeStepData.Length;
        _welcomeTimer.Start();
    }

    public void RefreshLocalized()
    {
        OnPropertyChanged(nameof(CurrentStepTitle));
        OnPropertyChanged(nameof(CurrentStepText));
        OnPropertyChanged(nameof(CurrentStepIcon));
    }

    [RelayCommand]
    private void CloseWelcome()
    {
        _welcomeTimer?.Stop();
        IsWelcomeVisible = false;
        _settings.MarkWelcomeShown();
    }

    // ---------- 消息列表 ----------

    [ObservableProperty]
    private ObservableCollection<ChatMessageViewModel> _messages;

    public event Action? ScrollToBottomRequested;

    // ---------- 会话列表 ----------

    [ObservableProperty]
    private ObservableCollection<Conversation> _conversations;

    [ObservableProperty]
    private Conversation? _selectedConversation;

    // ---------- 输入 ----------

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSend))]
    private string _inputText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSend))]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isStreaming;

    public bool CanSend => !IsBusy && !string.IsNullOrWhiteSpace(InputText);

    // ---------- 会话状态 ----------

    [ObservableProperty]
    private Conversation _conversation;

    [ObservableProperty]
    private ChatPhase _phase;

    [ObservableProperty]
    private bool _hasApiKey;

    [ObservableProperty]
    private bool _hasMessages;

    public bool CanExport => _lastGeneratedCode is not null;

    // ---------- 沙盒试运行 ----------

    [ObservableProperty]
    private bool _isSandboxRunning;

    [ObservableProperty]
    private string _sandboxStatus = string.Empty;

    [ObservableProperty]
    private string _sandboxOutput = string.Empty;

    [ObservableProperty]
    private bool _hasSandboxOutput;

    public bool CanRunSandbox => _lastGeneratedCode is not null && !IsSandboxRunning;

    // ---------- 引导进度（Agent 状态机可视化） ----------

    [ObservableProperty]
    private GuideProgress _guideProgress = new();

    /// <summary>8 个维度的 UI 展示数据</summary>
    public IReadOnlyList<GuideDimensionView> GuideDimensions { get; } = new[]
    {
        new GuideDimensionView { Dimension = RequirementDimension.Goal, Label = "目标", Icon = "🎯" },
        new GuideDimensionView { Dimension = RequirementDimension.InputSource, Label = "输入", Icon = "📥" },
        new GuideDimensionView { Dimension = RequirementDimension.Output, Label = "输出", Icon = "📤" },
        new GuideDimensionView { Dimension = RequirementDimension.Trigger, Label = "触发", Icon = "⏰" },
        new GuideDimensionView { Dimension = RequirementDimension.Environment, Label = "环境", Icon = "🖥️" },
        new GuideDimensionView { Dimension = RequirementDimension.SkillLevel, Label = "背景", Icon = "🧑‍💻" },
        new GuideDimensionView { Dimension = RequirementDimension.DataScale, Label = "规模", Icon = "📊" },
        new GuideDimensionView { Dimension = RequirementDimension.FailureHandling, Label = "容错", Icon = "🛡️" }
    };

    /// <summary>引导进度条百分比（0-100）</summary>
    public double GuideProgressPercent => GuideProgress.CapturedCount * 12.5;

    public string GuidePhaseLabel => Localization.LocalizationManager.Get(GuideProgress.PhaseKey);

    /// <summary>根据用户回答启发式判定已明确的维度并更新进度</summary>
    private void UpdateGuideProgress(string userText)
    {
        if (string.IsNullOrWhiteSpace(userText)) return;
        var t = userText.Trim().ToLowerInvariant();
        var p = GuideProgress;

        // 目标维度：包含"我要/我想/希望/需要/做/自动化/整理/生成/抓取/监控/提取/转换"等目标动词
        if (t.Contains("我要") || t.Contains("我想") || t.Contains("希望") || t.Contains("帮我")
            || t.Contains("做") || t.Contains("自动") || t.Contains("整理") || t.Contains("生成")
            || t.Contains("抓取") || t.Contains("监控") || t.Contains("提取") || t.Contains("转换"))
            p.MarkCaptured(RequirementDimension.Goal);

        // 输入来源：excel/csv/数据库/网页/邮箱/api/文件/目录/爬取等
        if (t.Contains("excel") || t.Contains("csv") || t.Contains("数据库") || t.Contains("网页")
            || t.Contains("邮箱") || t.Contains("邮件") || t.Contains("api") || t.Contains("接口")
            || t.Contains("文件") || t.Contains("目录") || t.Contains("爬") || t.Contains("抓取"))
            p.MarkCaptured(RequirementDimension.InputSource);

        // 输出形式：表格/图表/文件/报告/邮件/推送/消息/网页/控制台等
        if (t.Contains("表格") || t.Contains("图表") || t.Contains("文件") || t.Contains("报告")
            || t.Contains("邮件") || t.Contains("推送") || t.Contains("消息") || t.Contains("通知")
            || t.Contains("网页") || t.Contains("控制台") || t.Contains("打印") || t.Contains("保存"))
            p.MarkCaptured(RequirementDimension.Output);

        // 触发方式：每天/每周/每月/定时/自动/监听/事件/实时/小时/分钟等
        if (t.Contains("每天") || t.Contains("每周") || t.Contains("每月") || t.Contains("定时")
            || t.Contains("每天") || t.Contains("小时") || t.Contains("分钟") || t.Contains("监听")
            || t.Contains("实时") || t.Contains("事件") || t.Contains("启动") || t.Contains("手动"))
            p.MarkCaptured(RequirementDimension.Trigger);

        // 环境：windows/mac/linux/服务器/内网/离线/联网/特定机器等
        if (t.Contains("windows") || t.Contains("mac") || t.Contains("linux") || t.Contains("服务器")
            || t.Contains("内网") || t.Contains("离线") || t.Contains("联网") || t.Contains("机器")
            || t.Contains("系统") || t.Contains("环境"))
            p.MarkCaptured(RequirementDimension.Environment);

        // 技术背景：不懂/没经验/会一点/懂编程/写过/开发等
        if (t.Contains("不懂") || t.Contains("不会") || t.Contains("没经验") || t.Contains("小白")
            || t.Contains("会一点") || t.Contains("会些") || t.Contains("编程") || t.Contains("开发")
            || t.Contains("写过") || t.Contains("经验"))
            p.MarkCaptured(RequirementDimension.SkillLevel);

        // 数据规模：几十/几百/几千/几万/大量/很多/百万/万行等
        if (t.Contains("几十") || t.Contains("几百") || t.Contains("几千") || t.Contains("几万")
            || t.Contains("大量") || t.Contains("很多") || t.Contains("百万") || t.Contains("万行")
            || t.Contains("千行") || t.Contains("条") || t.Contains("数据"))
            p.MarkCaptured(RequirementDimension.DataScale);

        // 失败处理：跳过/继续/日志/提醒/报错/停止/忽略/重试等
        if (t.Contains("跳过") || t.Contains("继续") || t.Contains("日志") || t.Contains("提醒")
            || t.Contains("报错") || t.Contains("停止") || t.Contains("忽略") || t.Contains("重试")
            || t.Contains("失败") || t.Contains("错误"))
            p.MarkCaptured(RequirementDimension.FailureHandling);

        OnPropertyChanged(nameof(GuideProgressPercent));
        OnPropertyChanged(nameof(GuidePhaseLabel));
        SyncGuideDimensions();
    }

    /// <summary>将引导进度同步到维度视图（驱动 UI 高亮）</summary>
    private void SyncGuideDimensions()
    {
        foreach (var d in GuideDimensions)
            d.IsCaptured = GuideProgress.IsCaptured(d.Dimension);
    }

    /// <summary>阶段变化时同步引导进度状态</summary>
    partial void OnPhaseChanged(ChatPhase value)
    {
        GuideProgress.Phase = value;
        OnPropertyChanged(nameof(GuidePhaseLabel));
    }

    // ---------- 服务 ----------

    private ILlmClient _api => _services.GetRequiredService<ILlmClient>();
    private IChatService _chat => _services.GetRequiredService<IChatService>();
    private IRequirementSummarizer _summarizer => _services.GetRequiredService<IRequirementSummarizer>();
    private ICodeGenerator _codeGen => _services.GetRequiredService<ICodeGenerator>();
    private IProjectExporter _exporter => _services.GetRequiredService<IProjectExporter>();
    private ISettingsService _settings => _services.GetRequiredService<ISettingsService>();
    private ILocalStorageService _storage => _services.GetRequiredService<ILocalStorageService>();
    private IModelProfileService _profiles => _services.GetRequiredService<IModelProfileService>();
    private IUsageTracker _usage => _services.GetRequiredService<IUsageTracker>();

    public ModelProfile? CurrentProfile => _profiles.GetDefault();

    public SettingsViewModel SettingsVm => _services.GetRequiredService<SettingsViewModel>();

    // ---------- 会话管理 ----------

    public void LoadConversations()
    {
        Conversations.Clear();
        foreach (var c in _storage.GetConversations())
            Conversations.Add(c);
    }

    public void PersistCurrent()
    {
        if (Messages.Count == 0) return;
        Conversation.Messages = Messages
            .Select(m => new ChatMessage { Role = m.Role, Content = m.Content, Timestamp = DateTime.Now })
            .ToList();
        _storage.SaveConversation(Conversation);
    }

    partial void OnSelectedConversationChanged(Conversation? value)
    {
        if (value is null) return;
        PersistCurrent();
        Conversation = value;
        Messages.Clear();
        _lastGeneratedCode = null;
        GuideProgress.Reset();
        // 读取持久化压缩摘要（跨重启保持 system prompt 前缀稳定 → 缓存命中）
        Conversation.CompactedSummary = _storage.GetConversationSummary(value.Id);
        foreach (var m in value.Messages)
        {
            Messages.Add(new ChatMessageViewModel(m.Role, m.Content));
            if (m.Role == ChatRole.User)
                UpdateGuideProgress(m.Content);
        }
        if (value.GeneratedProject is { } p)
        {
            _lastGeneratedCode = new GeneratedCode
            {
                Code = p.Code,
                Description = p.Description,
                Dependencies = p.Dependencies
            };
            Phase = ChatPhase.Iterate;
        }
        else
        {
            Phase = value.Messages.Count == 0 ? ChatPhase.Idle : ChatPhase.Clarify;
        }
        OnPropertyChanged(nameof(CanExport));
        OnPropertyChanged(nameof(GuideProgressPercent));
        SyncGuideDimensions();
    }

    // ---------- 命令 ----------

    [RelayCommand]
    private async Task SendAsync()
    {
        var text = InputText;
        if (string.IsNullOrWhiteSpace(text) || IsBusy) return;

        InputText = string.Empty;
        AddMessage(ChatRole.User, text);
        UpdateGuideProgress(text);
        ScrollToBottomRequested?.Invoke();

        if (!HasApiKey)
        {
            AddMessage(ChatRole.Assistant, UiStrings.NoApiKeyHint);
            ScrollToBottomRequested?.Invoke();
            return;
        }

        var oldPhase = Phase;
        if (Phase == ChatPhase.Idle) Phase = ChatPhase.Clarify;
        else if (Phase == ChatPhase.Confirm)
        {
            if (_chat.IsConfirmReply(text)) Phase = ChatPhase.Generate;
            else if (_chat.IsModifyReply(text)) Phase = ChatPhase.Clarify;
        }

        if (Conversation.Title == "新对话" && !string.IsNullOrWhiteSpace(text))
            Conversation.Title = text.Length > 20 ? text[..20] + "…" : text;

        IsBusy = true;
        try
        {
            if (Phase == ChatPhase.Generate && oldPhase != ChatPhase.Generate)
                await DoGenerateAsync(text);
            else
                await DoChatStreamAsync();
        }
        catch (Exception ex)
        {
            AddMessage(ChatRole.Assistant, $"\n\n⚠️ 发生错误：{ex.Message}");
        }
        finally
        {
            IsBusy = false;
            _cts?.Dispose();
            _cts = null;
            ScrollToBottomRequested?.Invoke();
            PersistCurrent();
        }
    }

    [RelayCommand]
    private void Stop()
    {
        _cts?.Cancel();
    }

    /// <summary>新建对话：清空当前 + 切回 Chat 页</summary>
    [RelayCommand]
    private void NewConversation()
    {
        _cts?.Cancel();
        PersistCurrent();
        Conversation = new Conversation();
        Messages.Clear();
        Phase = ChatPhase.Idle;
        GuideProgress.Reset();
        OnPropertyChanged(nameof(GuideProgressPercent));
        OnPropertyChanged(nameof(GuidePhaseLabel));
        SyncGuideDimensions();
        _lastGeneratedCode = null;
        OnPropertyChanged(nameof(CanExport));
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        ThemeManager.Apply(ThemeManager.Current == ThemeManager.Light ? ThemeManager.Dark : ThemeManager.Light);
        _settings.SaveTheme(ThemeManager.Current);
    }

    [RelayCommand]
    private void ExportZip()
    {
        if (_lastGeneratedCode is null) return;
        var dlg = new SaveFileDialog
        {
            FileName = SanitizeFileName(Conversation.Title) + ".zip",
            Filter = "ZIP 文件 (*.zip)|*.zip",
            DefaultExt = ".zip",
            Title = Localization.LocalizationManager.Get("Str.ExportZip")
        };
        if (dlg.ShowDialog() != true) return;
        try
        {
            _exporter.SaveTo(dlg.FileName, Conversation.Title, _lastGeneratedCode,
                Conversation.GeneratedProject?.RequirementDocument ?? string.Empty);
            MessageBox.Show(
                UiStrings.ExportSuccess.Replace("{path}", dlg.FileName),
                UiStrings.AppTitle, MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                UiStrings.ExportFailed.Replace("{error}", ex.Message),
                UiStrings.AppTitle, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ---------- 沙盒试运行 ----------

    [RelayCommand]
    private async Task RunSandboxAsync()
    {
        if (_lastGeneratedCode is null || IsSandboxRunning) return;
        if (!_settings.Settings.SandboxEnabled)
        {
            SandboxStatus = UiStrings.SandboxDisabled;
            return;
        }

        IsSandboxRunning = true;
        SandboxOutput = string.Empty;
        HasSandboxOutput = false;
        SandboxStatus = UiStrings.SandboxRunning;
        OnPropertyChanged(nameof(CanRunSandbox));

        try
        {
            var runner = _services.GetRequiredService<ICodeRunner>();
            var result = await runner.RunPythonAsync(
                _lastGeneratedCode.Code,
                _settings.Settings.SandboxTimeoutSeconds);

            if (!result.Allowed)
            {
                SandboxStatus = UiStrings.SandboxRejected;
                SandboxOutput = result.RejectReason ?? string.Empty;
            }
            else if (result.TimedOut)
            {
                SandboxStatus = UiStrings.SandboxTimeout;
                SandboxOutput = result.DisplayOutput;
            }
            else
            {
                SandboxStatus = string.Format(UiStrings.SandboxDone, result.ExitCode ?? -1, result.DurationSeconds);
                SandboxOutput = string.IsNullOrWhiteSpace(result.DisplayOutput)
                    ? UiStrings.SandboxNoOutput
                    : result.DisplayOutput;
            }
            HasSandboxOutput = true;
        }
        catch (Exception ex)
        {
            SandboxStatus = UiStrings.SandboxFailed;
            SandboxOutput = ex.Message;
            HasSandboxOutput = true;
        }
        finally
        {
            IsSandboxRunning = false;
            OnPropertyChanged(nameof(CanRunSandbox));
        }
    }

    [RelayCommand]
    private void ClearSandbox()
    {
        SandboxOutput = string.Empty;
        HasSandboxOutput = false;
        SandboxStatus = string.Empty;
    }

    // ---------- 阶段实现 ----------

    private async Task DoChatStreamAsync()
    {
        var assistant = new ChatMessageViewModel(ChatRole.Assistant);
        Messages.Add(assistant);
        _cts = new CancellationTokenSource();
        IsStreaming = true;

        var profile = CurrentProfile;
        var apiKey = profile is null ? string.Empty : _profiles.DecryptApiKey(profile);
        var maxTokens = ToModelInfo(profile).DefaultMaxTokens;
        UsageInfo? lastUsage = null;

        try
        {
            var apiMessages = await BuildApiMessagesWithSummaryAsync();
            await _api.StreamChatAsync(apiMessages, delta =>
                    _ui.InvokeAsync(() => assistant.AppendContent(delta)).Task,
                apiKey,
                profile?.BaseUrl ?? string.Empty,
                profile?.ModelId ?? string.Empty,
                includeUsage: profile?.EnableCache ?? true,
                usage => { lastUsage = usage; },
                _cts.Token,
                maxTokens);

            if (lastUsage is not null && profile is not null)
            {
                RecordUsage(profile, lastUsage);
                if (_settings.Settings.ShowUsageStats)
                {
                    var usageLine = BuildUsageLine(lastUsage, profile);
                    if (usageLine is not null)
                        assistant.AppendContent($"\n\n---\n*{usageLine}*");
                }
            }

            if (Phase == ChatPhase.Clarify && _chat.IsRequirementSummary(assistant.Content))
            {
                Phase = ChatPhase.Confirm;
                AddMessage(ChatRole.Assistant, UiStrings.ConfirmGenerateHint);
            }
        }
        catch (OperationCanceledException)
        {
            if (string.IsNullOrEmpty(assistant.Content))
                assistant.Content = UiStrings.StopGenerated;
            else
                assistant.AppendContent($"\n\n{UiStrings.StopGenerated}");
        }
        catch (LlmApiException ex)
        {
            assistant.AppendContent($"\n\n⚠️ {MapApiError(ex.StatusCode)}");
        }
        catch (HttpRequestException)
        {
            assistant.AppendContent($"\n\n⚠️ {UiStrings.NetworkError}");
        }
        finally
        {
            IsStreaming = false;
        }
    }

    private static string? BuildUsageLine(UsageInfo u, ModelProfile profile)
    {
        if (u.TotalTokens == 0 && u.CacheHitTokens == 0) return null;

        double cost = 0;
        if (profile.EnableCache)
        {
            cost = (u.CacheHitTokens * profile.CacheHitPricePerM
                    + u.CacheMissTokens * profile.InputPricePerM
                    + u.CompletionTokens * profile.OutputPricePerM) / 1_000_000.0;
        }
        else
        {
            cost = (u.PromptTokens * profile.InputPricePerM
                    + u.CompletionTokens * profile.OutputPricePerM) / 1_000_000.0;
        }

        var hitText = u.CacheHitRate is { } rate
            ? $"缓存命中 {rate:F0}% · "
            : string.Empty;
        return $"{hitText}预计费用 ¥{cost:F4}（{u.PromptTokens}/{u.CompletionTokens} tokens）";
    }

    /// <summary>记录一次 API 用量到本地统计（供统计面板展示）</summary>
    private void RecordUsage(ModelProfile profile, UsageInfo u)
    {
        try
        {
            double cost;
            if (profile.EnableCache)
            {
                cost = (u.CacheHitTokens * profile.CacheHitPricePerM
                        + u.CacheMissTokens * profile.InputPricePerM
                        + u.CompletionTokens * profile.OutputPricePerM) / 1_000_000.0;
            }
            else
            {
                cost = (u.PromptTokens * profile.InputPricePerM
                        + u.CompletionTokens * profile.OutputPricePerM) / 1_000_000.0;
            }

            _usage.Record(new UsageRecord
            {
                ModelId = profile.ModelId,
                PromptTokens = u.PromptTokens,
                CompletionTokens = u.CompletionTokens,
                TotalTokens = u.TotalTokens,
                CacheHitTokens = u.CacheHitTokens,
                CacheMissTokens = u.CacheMissTokens,
                EstimatedCost = cost
            });
        }
        catch
        {
            // 统计失败不影响对话
        }
    }

    private async Task DoGenerateAsync(string userConfirmMessage)
    {
        var statusVm = new ChatMessageViewModel(ChatRole.Assistant)
        {
            Content = UiStrings.GeneratingStatus
        };
        Messages.Add(statusVm);

        var profile = CurrentProfile;
        var apiKey = profile is null ? string.Empty : _profiles.DecryptApiKey(profile);

        try
        {
            var history = Messages
                .Where(m => !string.IsNullOrWhiteSpace(m.Content))
                .Select(m => new ChatApiMessage(m.Role, m.Content))
                .ToList();

            var reqDoc = await _summarizer.SummarizeAsync(history, apiKey, ToModelInfo(profile));
            var code = await _codeGen.GenerateAsync(reqDoc, apiKey, ToModelInfo(profile));
            _lastGeneratedCode = code;

            Conversation.GeneratedProject = new GeneratedProject
            {
                Type = ProjectType.PythonScript,
                Code = code.Code,
                Description = code.Description,
                Dependencies = code.Dependencies,
                RequirementDocument = reqDoc
            };

            var depsLine = code.Dependencies.Count == 0 ? UiStrings.NoDeps : string.Join("、", code.Dependencies);
            var body = $$"""
{{UiStrings.GeneratedHeader}}

**{{code.Description}}**

**{{UiStrings.Deps}}：** {{depsLine}}

```python
{{code.Code}}
```

---
{{UiStrings.IterateHint}}（{{UiStrings.NeedPythonHint}}）
""";
            statusVm.Content = body;
            Phase = ChatPhase.Iterate;
            OnPropertyChanged(nameof(CanExport));
        }
        catch (OperationCanceledException)
        {
            statusVm.Content = UiStrings.StopGenerated;
        }
        catch (Exception ex)
        {
            statusVm.Content = $"❌ {UiStrings.GenerateFailed}：{ex.Message}";
        }
    }

    private static LlmModelInfo ToModelInfo(ModelProfile? profile)
    {
        if (profile is null) return LlmCatalog.Default;
        return new LlmModelInfo
        {
            Id = profile.ModelId,
            Provider = profile.Provider,
            BaseUrl = profile.BaseUrl,
            KeyUrl = LlmCatalog.Default.KeyUrl
        };
    }

    private static string MapApiError(int status) => status switch
    {
        400 => UiStrings.ApiBadRequest,
        401 => UiStrings.ApiKeyInvalid,
        403 => UiStrings.ApiForbidden,
        404 => UiStrings.ApiModelNotFound,
        408 or 504 => UiStrings.StreamTimeout,
        413 => UiStrings.ApiTooLarge,
        429 => UiStrings.RateLimited,
        500 => UiStrings.ServerError,
        502 or 503 => UiStrings.ServiceUnavailable,
        >= 500 => UiStrings.ServerError,
        _ => $"请求失败（{status}）"
    };

    private static string SanitizeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "guidecraft-project";
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return string.IsNullOrWhiteSpace(name) ? "guidecraft-project" : name;
    }

    private void AddMessage(ChatRole role, string content)
    {
        Messages.Add(new ChatMessageViewModel(role, content));
    }

    // ---------- 内核上下文承接（Agent 引导 + 缓存命中率优化） ----------

    /// <summary>
    /// 构建 API 请求消息：固定前缀 + 历史消息。
    /// 缓存命中率优化（v2.0 增强，参考 DeepSeek/Qwen 官方缓存机制）：
    /// 1. System Prompt 字节级稳定（const 模板，绝不拼入动态内容）→ 服务端前缀缓存最大化命中
    /// 2. 历史超阈值时 LLM 压缩为短摘要，注入 system prompt【末尾】——缓存仅匹配前缀，
    ///    摘要变化不会破坏已缓存前缀；摘要持久化 SQLite，跨重启复用，避免重建
    /// 3. 消息顺序保持稳定（旧→新），每轮仅追加末尾 → 前缀天然可命中
    /// 4. 缓存命中率从 usage 读取实时显示（cache hit 计费低至 1/10）
    /// </summary>
    private async Task<IReadOnlyList<ChatApiMessage>> BuildApiMessagesWithSummaryAsync()
    {
        var sysPrompt = _chat.GetSystemPrompt(Phase);
        var msgs = new List<ChatApiMessage> { new(ChatRole.System, sysPrompt) };

        // 排除最后一条（正在流式的 assistant 空消息）
        var history = Messages.Take(Messages.Count - 1).ToList();
        if (history.Count == 0) return msgs;

        const int maxHistoryTokens = 4000;  // 留足 system prompt + 输出的 token 余量
        const int summarizeThreshold = 6000; // 历史超过此值触发摘要

        // 更精确的 token 估算：英文 ~1 token/4字符，中文 ~1 token/1.5字符
        static int EstimateTokens(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            int cjk = s.Count(c => c > 0x2E7F);
            int other = s.Length - cjk;
            return Math.Max(1, cjk / 2 + other / 4);
        }

        int acc = 0;
        var kept = new List<ChatMessageViewModel>();
        for (int i = history.Count - 1; i >= 0; i--)
        {
            var m = history[i];
            var est = EstimateTokens(m.Content);
            if (acc + est > maxHistoryTokens && kept.Count > 0) break;
            acc += est;
            kept.Insert(0, m);
        }

        // 总历史超阈值 → 用 LLM 压缩 + 持久化缓存
        int totalEst = history.Sum(m => EstimateTokens(m.Content));
        if (totalEst > summarizeThreshold && Conversation.CompactedSummary is null)
        {
            try
            {
                var profile = CurrentProfile;
                if (profile is not null)
                {
                    var compactPrompt = BuildCompactPrompt(history);
                    var apiKey = _profiles.DecryptApiKey(profile);
                    var summary = await _api.ChatAsync(
                        new[] { new ChatApiMessage(ChatRole.System, "你是一个对话历史压缩助手，请用 100-200 字中文客观总结以下对话历史，保留关键需求和决定。"), new ChatApiMessage(ChatRole.User, compactPrompt) },
                        apiKey, profile.BaseUrl, profile.ModelId,
                        ct: default, maxTokens: 512);
                    if (!string.IsNullOrWhiteSpace(summary))
                    {
                        Conversation.CompactedSummary = summary.Trim();
                        // 持久化：跨重启复用，保持前缀稳定（缓存命中核心）
                        _storage.SaveConversationSummary(Conversation.Id, Conversation.CompactedSummary);
                    }
                }
            }
            catch { /* 摘要失败则降级为截断模式，不影响对话 */ }
        }

        // 若有摘要，注入到 system prompt 末尾（保持缓存前缀稳定）
        if (!string.IsNullOrWhiteSpace(Conversation.CompactedSummary))
        {
            var compactSuffix = $"\n\n[对话历史摘要] {Conversation.CompactedSummary}";
            msgs[0] = new ChatApiMessage(ChatRole.System, sysPrompt + compactSuffix);
        }

        foreach (var m in kept)
            msgs.Add(new ChatApiMessage(m.Role, m.Content));

        return msgs;
    }

    private static string BuildCompactPrompt(List<ChatMessageViewModel> history)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var m in history)
            sb.AppendLine($"[{m.Role}] {m.Content}");
        return sb.ToString();
    }
}
