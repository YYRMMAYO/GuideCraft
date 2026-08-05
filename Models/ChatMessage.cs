namespace GuideCraft.Models;

/// <summary>单条对话消息</summary>
public class ChatMessage
{
    public int Id { get; set; }

    public string ConversationId { get; set; } = string.Empty;

    public ChatRole Role { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.Now;
}
