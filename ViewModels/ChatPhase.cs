namespace GuideCraft.ViewModels;

/// <summary>对话阶段状态机：Clarify 需求澄清 → Confirm 摘要确认 → Generate 生成产物 → Iterate 迭代修改</summary>
public enum ChatPhase
{
    /// <summary>尚未开始</summary>
    Idle,

    /// <summary>引导式提问澄清需求</summary>
    Clarify,

    /// <summary>已输出需求摘要，等待用户确认</summary>
    Confirm,

    /// <summary>生成代码产物</summary>
    Generate,

    /// <summary>用户对产物提出修改意见</summary>
    Iterate
}
