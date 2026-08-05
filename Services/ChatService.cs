using GuideCraft.ViewModels;

namespace GuideCraft.Services;

/// <summary>对话编排服务：阶段对应的 System Prompt 选择、阶段切换判定</summary>
public interface IChatService
{
    /// <summary>根据当前阶段返回系统提示词</summary>
    string GetSystemPrompt(ChatPhase phase);

    /// <summary>判定 AI 输出是否包含「需求摘要」标记（用于 Clarify → Confirm 自动切换）</summary>
    bool IsRequirementSummary(string aiMessage);

    /// <summary>判定用户输入是否为「确认生成」类回复（用于 Confirm → Generate 自动切换）</summary>
    bool IsConfirmReply(string userMessage);

    /// <summary>判定用户输入是否为「修改/否定」类回复（用于 Confirm → Clarify 回到澄清阶段）</summary>
    bool IsModifyReply(string userMessage);
}

/// <summary>引导式对话编排实现。
/// 阶段切换采用「CONFIRM / DENY / INFO」回复类型分类（多轮澄清最佳实践）：
/// - CONFIRM（确认）→ 进入生成
/// - DENY（否定/修改）→ 回到澄清
/// - INFO（补充信息）→ 回到澄清继续收集
/// 判定基于强/弱两级关键词 + 否定词排除，降低"可以/好的"类模糊回复的误判率。</summary>
public sealed class ChatService : IChatService
{
    public string GetSystemPrompt(ChatPhase phase) => phase switch
    {
        ChatPhase.Clarify or ChatPhase.Confirm => PromptTemplates.GuideClarifyPrompt,
        ChatPhase.Iterate => PromptTemplates.IteratePrompt,
        ChatPhase.Generate => PromptTemplates.CodeGenerationPrompt, // Generate 阶段直接由专用服务调用
        _ => PromptTemplates.GuideClarifyPrompt
    };

    public bool IsRequirementSummary(string aiMessage)
    {
        if (string.IsNullOrEmpty(aiMessage)) return false;
        return aiMessage.Contains("需求摘要")
            || aiMessage.Contains("### 1. 目标")
            || aiMessage.Contains("**一句话目标**");
    }

    public bool IsConfirmReply(string userMessage)
    {
        if (string.IsNullOrWhiteSpace(userMessage)) return false;
        var t = userMessage.Trim().TrimEnd('。', '！', '!', '.', '~', '～');

        // 强确认词：前缀/整句匹配即视为确认
        var strong = new[] { "确认", "没问题", "就按这个", "就这么办", "开始生成", "生成代码", "开始吧", "是的", "对的", "可以生成", "没问题了" };
        if (strong.Any(k => t.Equals(k, StringComparison.OrdinalIgnoreCase)
                            || t.StartsWith(k, StringComparison.OrdinalIgnoreCase)))
            return true;

        // 弱确认词（"可以/好的/OK"等）：仅当整句很短时才视为确认，
        // 避免"可以，但改成..."这类「确认+修改」混合回复被误判为纯确认
        var weak = new[] { "可以", "好的", "好", "嗯", "行", "ok", "同意", "行吧" };
        return t.Length <= 12 && weak.Any(k => t.Equals(k, StringComparison.OrdinalIgnoreCase)
                                               || t.StartsWith(k, StringComparison.OrdinalIgnoreCase));
    }

    public bool IsModifyReply(string userMessage)
    {
        if (string.IsNullOrWhiteSpace(userMessage)) return false;
        var t = userMessage.Trim();
        // 包含修改/否定/补充关键词 → 视为需要回到澄清阶段
        var keys = new[] { "修改", "调整", "改", "补充", "遗漏", "不对", "错了", "不要", "换成",
            "重新", "再加", "改为", "需要", "应该", "还有", "另外", "别忘了", "忘加" };
        return keys.Any(k => t.Contains(k));
    }
}
