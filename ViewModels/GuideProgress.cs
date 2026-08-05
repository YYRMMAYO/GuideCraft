namespace GuideCraft.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;

/// <summary>需求澄清维度（8 项，与引导 prompt 一一对应）</summary>
public enum RequirementDimension
{
    /// <summary>目标：要解决什么问题</summary>
    Goal,

    /// <summary>输入来源：数据从哪里来</summary>
    InputSource,

    /// <summary>输出形式：期望的结果</summary>
    Output,

    /// <summary>触发方式与频率：何时运行</summary>
    Trigger,

    /// <summary>环境约束：运行环境要求</summary>
    Environment,

    /// <summary>用户技术背景</summary>
    SkillLevel,

    /// <summary>数据规模</summary>
    DataScale,

    /// <summary>失败处理</summary>
    FailureHandling
}

/// <summary>
/// 引导进度跟踪：8 个需求维度的收集状态 + 当前阶段。
/// 根据用户回答关键词启发式判定维度是否已明确，供 UI 进度指示器展示。
/// </summary>
public sealed class GuideProgress
{
    private readonly HashSet<RequirementDimension> _captured = new();

    /// <summary>已完成维度数量（0-8）</summary>
    public int CapturedCount => _captured.Count;

    /// <summary>是否全部完成（进入确认阶段）</summary>
    public bool IsComplete => _captured.Count >= 8;

    /// <summary>标记某维度已收集</summary>
    public void MarkCaptured(RequirementDimension d) => _captured.Add(d);

    /// <summary>重置进度（新对话）</summary>
    public void Reset() => _captured.Clear();

    /// <summary>判断维度是否已收集</summary>
    public bool IsCaptured(RequirementDimension d) => _captured.Contains(d);

    /// <summary>当前阶段标签（本地化 key）</summary>
    public string PhaseKey => Phase switch
    {
        ChatPhase.Idle => "Str.Guide.PhaseIdle",
        ChatPhase.Clarify => "Str.Guide.PhaseClarify",
        ChatPhase.Confirm => "Str.Guide.PhaseConfirm",
        ChatPhase.Generate => "Str.Guide.PhaseGenerate",
        _ => "Str.Guide.PhaseIterate"
    };

    /// <summary>当前阶段</summary>
    public ChatPhase Phase { get; set; } = ChatPhase.Idle;
}

/// <summary>单维度 UI 展示数据（随收集状态变化）</summary>
public partial class GuideDimensionView : ObservableObject
{
    public required RequirementDimension Dimension { get; init; }

    public required string Label { get; init; }

    public required string Icon { get; init; }

    /// <summary>是否已收集（驱动 UI 高亮）</summary>
    [ObservableProperty]
    private bool _isCaptured;
}
