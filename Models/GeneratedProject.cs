namespace GuideCraft.Models;

/// <summary>AI 生成的代码产物</summary>
public class GeneratedProject
{
    public ProjectType Type { get; set; } = ProjectType.PythonScript;

    /// <summary>完整代码文本</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>中文说明</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>依赖包列表</summary>
    public List<string> Dependencies { get; set; } = new();

    /// <summary>需求文档（Markdown）</summary>
    public string RequirementDocument { get; set; } = string.Empty;
}
