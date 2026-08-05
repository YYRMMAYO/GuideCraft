using CommunityToolkit.Mvvm.ComponentModel;
using GuideCraft.Models;

namespace GuideCraft.ViewModels;

/// <summary>单条消息的视图模型，支持流式增量追加</summary>
public partial class ChatMessageViewModel : ObservableObject
{
    public ChatMessageViewModel(ChatRole role, string content = "")
    {
        Role = role;
        _content = content;
    }

    public ChatRole Role { get; }

    /// <summary>是否为用户消息（决定对齐方向与气泡样式）</summary>
    public bool IsUser => Role == ChatRole.User;

    public bool IsAssistant => Role == ChatRole.Assistant;

    public bool IsSystem => Role == ChatRole.System;

    /// <summary>是否为占位"正在思考"消息</summary>
    public bool IsTypingPlaceholder { get; set; }

    [ObservableProperty]
    private string _content;

    /// <summary>流式期间增量追加内容</summary>
    public void AppendContent(string delta)
    {
        Content += delta;
    }
}
