namespace GuideCraft.Models;

/// <summary>一次完整的对话会话</summary>
public class Conversation
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Title { get; set; } = "新对话";

    public List<ChatMessage> Messages { get; set; } = new();

    public GeneratedProject? GeneratedProject { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
