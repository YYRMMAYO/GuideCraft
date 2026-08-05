using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GuideCraft.Models;
using GuideCraft.Services;
using GuideCraft.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace GuideCraft.ViewModels;

/// <summary>主窗口视图模型：引导式对话状态机、Markdown 渲染输入、API 流式调用、阶段切换</summary>
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
        _phase = ChatPhase.Idle;
        _conversations = new ObservableCollection<Conversation>();
        _selectedConversation = null;
        _messages.CollectionChanged += (_, _) => HasMessages = _messages.Count > 0;

        var settings = services.GetService<ISettingsService>();
        _hasApiKey = settings is not null && !string.IsNullOrWhiteSpace(settings.Settings.ApiKey);
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

    /// <summary>是否有消息（用于空状态卡片显隐）</summary>
    [ObservableProperty]
    private bool _hasMessages;

    /// <summary>是否可导出 ZIP（仅生成产物后可用）</summary>
    public bool CanExport => _lastGeneratedCode is not null;

    // ---------- 服务懒解析 ----------

    private IDeepSeekApiClient _api => _services.GetRequiredService<IDeepSeekApiClient>();
    private IChatService _chat => _services.GetRequiredService<IChatService>();
    private IRequirementSummarizer _summarizer => _services.GetRequiredService<IRequirementSummarizer>();
    private ICodeGenerator _codeGen => _services.GetRequiredService<ICodeGenerator>();
    private IProjectExporter _exporter => _services.GetRequiredService<IProjectExporter>();
    private ISettingsService _settings => _services.GetRequiredService<ISettingsService>();
    private ILocalStorageService _storage => _services.GetRequiredService<ILocalStorageService>();

    /// <summary>启动时加载会话列表</summary>
    public void LoadConversations()
    {
        Conversations.Clear();
        foreach (var c in _storage.GetConversations())
            Conversations.Add(c);
    }

    /// <summary>新建对话并保存当前（如果非空）</summary>
    public void PersistCurrent()
    {
        if (Messages.Count == 0) return;
        Conversation.Messages = Messages
            .Select(m => new ChatMessage { Role = m.Role, Content = m.Content, Timestamp = DateTime.Now })
            .ToList();
        _storage.SaveConversation(Conversation);
    }

    /// <summary>选中会话切换</summary>
    partial void OnSelectedConversationChanged(Conversation? value)
    {
        if (value is null) return;
        // 持久化当前（如果有）
        PersistCurrent();
        // 直接使用传入的会话（已在内存中，ListBox ItemSource 提供）
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

        // 阶段切换判定
        var oldPhase = Phase;
        if (Phase == ChatPhase.Idle) Phase = ChatPhase.Clarify;
        else if (Phase == ChatPhase.Confirm)
        {
            if (_chat.IsConfirmReply(text)) Phase = ChatPhase.Generate;
            else if (_chat.IsModifyReply(text)) Phase = ChatPhase.Clarify;
        }

        // 首次有内容时更新标题
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
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        ThemeManager.Apply(ThemeManager.Current == ThemeManager.Light ? ThemeManager.Dark : ThemeManager.Light);
        _settings.SaveTheme(ThemeManager.Current);
    }

    [RelayCommand]
    private void OpenSettings()
    {
        var dialog = _services.GetRequiredService<SettingsDialog>();
        dialog.Owner = Application.Current.MainWindow;
        if (dialog.ShowDialog() == true)
        {
            HasApiKey = !string.IsNullOrWhiteSpace(_settings.Settings.ApiKey);
        }
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
            Title = "导出项目 ZIP"
        };
        if (dlg.ShowDialog() != true) return;
        try
        {
            _exporter.SaveTo(dlg.FileName, Conversation.Title, _lastGeneratedCode,
                Conversation.GeneratedProject?.RequirementDocument ?? string.Empty);
            MessageBox.Show($"已导出到：\n{dlg.FileName}", UiStrings.AppTitle, MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"导出失败：{ex.Message}", UiStrings.AppTitle, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ---------- 阶段实现 ----------

    /// <summary>通用流式对话（Clarify/Confirm/Iterate 阶段）</summary>
    private async Task DoChatStreamAsync()
    {
        var assistant = new ChatMessageViewModel(ChatRole.Assistant);
        Messages.Add(assistant);
        _cts = new CancellationTokenSource();
        IsStreaming = true;
        try
        {
            var apiMessages = BuildApiMessages();
            await _api.StreamChatAsync(apiMessages, delta =>
                _ui.InvokeAsync(() => assistant.AppendContent(delta)).Task,
                _settings.Settings.ApiKey,
                _settings.Settings.PreferredModel,
                _cts.Token);

            // 流式完成后：Clarify → Confirm 自动切换
            if (Phase == ChatPhase.Clarify && _chat.IsRequirementSummary(assistant.Content))
            {
                Phase = ChatPhase.Confirm;
                AddMessage(ChatRole.Assistant, UiStrings.ConfirmGenerateHint);
            }
        }
        catch (OperationCanceledException)
        {
            if (string.IsNullOrEmpty(assistant.Content))
                assistant.Content = "（已停止生成）";
            else
                assistant.AppendContent("\n\n（已停止生成）");
        }
        catch (DeepSeekApiException ex)
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

    /// <summary>需求摘要 + 代码生成（Confirm 阶段用户回复"确认"后触发）</summary>
    private async Task DoGenerateAsync(string userConfirmMessage)
    {
        var statusVm = new ChatMessageViewModel(ChatRole.Assistant)
        {
            Content = "✅ 收到确认，正在整理需求并生成代码..."
        };
        Messages.Add(statusVm);

        try
        {
            // 1. 汇总对话历史
            var history = Messages
                .Where(m => !string.IsNullOrWhiteSpace(m.Content))
                .Select(m => new ChatApiMessage(m.Role, m.Content))
                .ToList();

            // 2. 生成需求摘要
            var reqDoc = await _summarizer.SummarizeAsync(
                history, _settings.Settings.ApiKey, _settings.Settings.PreferredModel);

            // 3. 生成代码
            var code = await _codeGen.GenerateAsync(
                reqDoc, _settings.Settings.ApiKey, _settings.Settings.PreferredModel);
            _lastGeneratedCode = code;

            // 4. 持久化到 Conversation
            Conversation.GeneratedProject = new GeneratedProject
            {
                Type = ProjectType.PythonScript,
                Code = code.Code,
                Description = code.Description,
                Dependencies = code.Dependencies,
                RequirementDocument = reqDoc
            };

            // 5. 用 Markdown 渲染展示产物
            var depsLine = code.Dependencies.Count == 0 ? "（无第三方依赖）" : string.Join("、", code.Dependencies);
            var body = $$"""
✅ 已生成 Python 脚本

**{{code.Description}}**

**依赖：** {{depsLine}}

```python
{{code.Code}}
```

---
你可以回复修改意见，我会更新代码；或点击下方「📦 导出项目 ZIP」按钮下载完整项目（{{UiStrings.NeedPythonHint}}）
""";
            statusVm.Content = body;
            Phase = ChatPhase.Iterate;
            OnPropertyChanged(nameof(CanExport));
        }
        catch (OperationCanceledException)
        {
            statusVm.Content = "（已停止生成）";
        }
        catch (Exception ex)
        {
            statusVm.Content = $"❌ 生成失败：{ex.Message}\n\n请检查网络或 API Key 后重试。";
        }
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

    // ---------- 辅助 ----------

    private void AddMessage(ChatRole role, string content)
    {
        Messages.Add(new ChatMessageViewModel(role, content));
    }

    /// <summary>构建 API 请求：阶段对应 System Prompt + 截至当前 assistant 空消息之前的所有对话</summary>
    private IReadOnlyList<ChatApiMessage> BuildApiMessages()
    {
        var sysPrompt = _chat.GetSystemPrompt(Phase);
        var msgs = new List<ChatApiMessage>
        {
            new(ChatRole.System, sysPrompt)
        };
        // 排除最后一条（正在流式的 assistant 空消息）
        for (int i = 0; i < Messages.Count - 1; i++)
        {
            var m = Messages[i];
            msgs.Add(new ChatApiMessage(m.Role, m.Content));
        }
        return msgs;
    }
}