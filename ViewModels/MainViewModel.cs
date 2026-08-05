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

    // ---------- 设置抽屉 ----------

    /// <summary>设置抽屉是否打开（覆盖在对话上方，用完关闭）</summary>
    [ObservableProperty]
    private bool _isSettingsOpen;

    /// <summary>设置抽屉宽度（可拖拽调整）</summary>
    [ObservableProperty]
    private System.Windows.GridLength _settingsWidth = new(480);

    [RelayCommand]
    private void OpenSettings()
    {
        IsSettingsOpen = true;
        RefreshLocalized();
    }

    [RelayCommand]
    private void CloseSettings() => IsSettingsOpen = false;

    // ---------- 布局与引导 ----------

    [ObservableProperty]
    private bool _isSidebarRight = true;

    [ObservableProperty]
    private System.Windows.GridLength _sidebarWidth = new(260);

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

    // ---------- 服务 ----------

    private ILlmClient _api => _services.GetRequiredService<ILlmClient>();
    private IChatService _chat => _services.GetRequiredService<IChatService>();
    private IRequirementSummarizer _summarizer => _services.GetRequiredService<IRequirementSummarizer>();
    private ICodeGenerator _codeGen => _services.GetRequiredService<ICodeGenerator>();
    private IProjectExporter _exporter => _services.GetRequiredService<IProjectExporter>();
    private ISettingsService _settings => _services.GetRequiredService<ISettingsService>();
    private ILocalStorageService _storage => _services.GetRequiredService<ILocalStorageService>();
    private IModelProfileService _profiles => _services.GetRequiredService<IModelProfileService>();

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
        foreach (var m in value.Messages)
            Messages.Add(new ChatMessageViewModel(m.Role, m.Content));
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
    }

    // ---------- 命令 ----------

    [RelayCommand]
    private async Task SendAsync()
    {
        var text = InputText;
        if (string.IsNullOrWhiteSpace(text) || IsBusy) return;

        InputText = string.Empty;
        AddMessage(ChatRole.User, text);
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
        _lastGeneratedCode = null;
        OnPropertyChanged(nameof(CanExport));
        // 新对话时收起设置抽屉，回到对话主界面
        IsSettingsOpen = false;
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

    // ---------- 阶段实现 ----------

    private async Task DoChatStreamAsync()
    {
        var assistant = new ChatMessageViewModel(ChatRole.Assistant);
        Messages.Add(assistant);
        _cts = new CancellationTokenSource();
        IsStreaming = true;

        var profile = CurrentProfile;
        var apiKey = profile is null ? string.Empty : _profiles.DecryptApiKey(profile);
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
                _cts.Token);

            if (lastUsage is not null && profile is not null)
            {
                var usageLine = BuildUsageLine(lastUsage, profile);
                if (usageLine is not null)
                    assistant.AppendContent($"\n\n---\n*{usageLine}*");
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
        401 => UiStrings.ApiKeyInvalid,
        408 or 504 => UiStrings.StreamTimeout,
        429 => UiStrings.RateLimited,
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
    /// 关键优化点（v1.4.0）：
    /// 1. System Prompt 字节级稳定 → DeepSeek/Qwen 上下文缓存最大化命中
    /// 2. 历史消息超出 ~4000 估算 tokens 时，先用 LLM 摘要压缩为 200 字短摘要，
    ///    摘要持久化在 _conversation.CompactedSummary → 始终作为 system prompt 一部分发送。
    ///    这样系统 prompt 几乎不变（仅摘要变化），首条消息（用户最近）也稳定 → 高缓存命中
    /// 3. 缓存命中率从 usage 读取实时显示给用户
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

        int acc = 0;
        var kept = new List<ChatMessageViewModel>();
        for (int i = history.Count - 1; i >= 0; i--)
        {
            var m = history[i];
            var est = Math.Max(1, m.Content.Length / 2);
            if (acc + est > maxHistoryTokens && kept.Count > 0) break;
            acc += est;
            kept.Insert(0, m);
        }

        // 总历史超阈值 → 用 LLM 压缩 + 缓存
        int totalEst = history.Sum(m => m.Content.Length / 2);
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
                        apiKey, profile.BaseUrl, profile.ModelId);
                    if (!string.IsNullOrWhiteSpace(summary))
                    {
                        Conversation.CompactedSummary = summary.Trim();
                        // 不持久化到 DB（避免大文件），仅本会话有效
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
