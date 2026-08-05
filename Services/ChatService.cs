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

    /// <summary>判定用户输入是否为「修改/调整」类回复（用于 Confirm → Clarify 回到澄清阶段）</summary>
    bool IsModifyReply(string userMessage);
}

/// <summary>引导式对话编排实现</summary>
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
        var t = userMessage.Trim();
        var keys = new[] { "确认", "没问题", "可以", "好的", "开始生成", "生成代码", "继续", "是的", "对", "OK", "ok" };
        return keys.Any(k => t.Equals(k) || t.StartsWith(k));
    }

    public bool IsModifyReply(string userMessage)
    {
        if (string.IsNullOrWhiteSpace(userMessage)) return false;
        var t = userMessage.Trim();
        // 包含修改/调整/不对/错了/需要等关键词，且不是纯确认
        if (IsConfirmReply(t)) return false;
        var keys = new[] { "修改", "调整", "改", "补充", "遗漏", "不对", "错了", "需要", "应该", "再加", "改为" };
        return keys.Any(k => t.Contains(k));
    }
}