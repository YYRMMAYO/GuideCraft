namespace GuideCraft.Models;

/// <summary>一次完整的对话会话</summary>
public class Conversation
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Title { get; set; } = "新对话";

    public List<ChatMessage> Messages { get; set; } = new();

    public GeneratedProject? GeneratedProject { get; set; }

    /// <summary>
    /// 对话历史压缩摘要（仅内存，未持久化）：
    /// 当历史超过 ~6000 tokens 时由 LLM 压缩为 200 字短摘要，
    /// 注入 system prompt 末尾，保持前缀稳定，DeepSeek/Qwen 上下文缓存最大化命中。
    /// </summary>
    public string? CompactedSummary { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
