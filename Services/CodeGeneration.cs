using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using GuideCraft.Models;

namespace GuideCraft.Services;

/// <summary>需求摘要服务：将对话记录整理为结构化需求文档</summary>
public interface IRequirementSummarizer
{
    /// <summary>从对话历史生成需求文档（非流式）</summary>
    Task<string> SummarizeAsync(
        IReadOnlyList<ChatApiMessage> conversation,
        string apiKey,
        LlmModelInfo model,
        CancellationToken ct = default);
}

public sealed class RequirementSummarizer : IRequirementSummarizer
{
    private readonly ILlmClient _api;

    public RequirementSummarizer(ILlmClient api)
    {
        _api = api;
    }

    public async Task<string> SummarizeAsync(
        IReadOnlyList<ChatApiMessage> conversation,
        string apiKey,
        LlmModelInfo model,
        CancellationToken ct = default)
    {
        var transcript = new StringBuilder();
        foreach (var m in conversation)
        {
            if (m.Role == ChatRole.System) continue;
            transcript.Append($"[{m.RoleName}] {m.Content}\n\n");
        }

        var prompt = PromptTemplates.RequirementSummaryPrompt.Replace("{conversation}", transcript.ToString());
        var messages = new List<ChatApiMessage>
        {
            new(ChatRole.System, "你是专业的中文需求分析师。"),
            new(ChatRole.User, prompt)
        };
        var text = await _api.ChatAsync(messages, apiKey, model.BaseUrl, model.Id, ct);
        return text.Trim();
    }
}

/// <summary>代码生成服务：把需求文档生成为单文件 Python 脚本</summary>
public interface ICodeGenerator
{
    /// <summary>从需求文档生成代码</summary>
    Task<GeneratedCode> GenerateAsync(
        string requirementDocument,
        string apiKey,
        LlmModelInfo model,
        CancellationToken ct = default);
}

public sealed class CodeGenerator : ICodeGenerator
{
    private readonly ILlmClient _api;

    public CodeGenerator(ILlmClient api)
    {
        _api = api;
    }

    public async Task<GeneratedCode> GenerateAsync(
        string requirementDocument,
        string apiKey,
        LlmModelInfo model,
        CancellationToken ct = default)
    {
        var prompt = PromptTemplates.CodeGenerationPrompt.Replace("{requirementDocument}", requirementDocument);
        var messages = new List<ChatApiMessage>
        {
            new(ChatRole.System, "你是资深 Python 工程师。"),
            new(ChatRole.User, prompt)
        };
        var raw = await _api.ChatAsync(messages, apiKey, model.BaseUrl, model.Id, ct);
        return ParseGeneratedCode(raw);
    }

    /// <summary>解析 AI 输出：```json 依赖 + ```python 代码</summary>
    public static GeneratedCode ParseGeneratedCode(string raw)
    {
        var code = new GeneratedCode();

        // 提取 JSON 依赖
        var jsonMatch = Regex.Match(raw, @"```(?:json)?\s*(\{[^}]*""dependencies""[^}]*\})\s*```",
            RegexOptions.Singleline);
        if (jsonMatch.Success)
        {
            try
            {
                using var doc = JsonDocument.Parse(jsonMatch.Groups[1].Value);
                var arr = doc.RootElement.GetProperty("dependencies");
                foreach (var dep in arr.EnumerateArray())
                {
                    var v = dep.GetString();
                    if (!string.IsNullOrWhiteSpace(v)) code.Dependencies.Add(v);
                }
            }
            catch { /* 解析失败忽略，使用空列表 */ }
        }

        // 提取 Python 代码
        var pyMatch = Regex.Match(raw, @"```python\s*\n([\s\S]*?)```", RegexOptions.Singleline);
        if (pyMatch.Success)
        {
            code.Code = pyMatch.Groups[1].Value.TrimEnd();
        }
        else
        {
            // 退化：取整段作为代码
            code.Code = raw;
        }

        // 简单中文描述：从第一段非代码、非 JSON 的文字中提取（最多 80 字）
        code.Description = ExtractDescription(raw);

        return code;
    }

    private static string ExtractDescription(string raw)
    {
        // 去掉代码块与 JSON 块
        var stripped = Regex.Replace(raw, @"```[\s\S]*?```", string.Empty).Trim();
        // 截取首行
        var firstLine = stripped.Split('\n').FirstOrDefault()?.Trim() ?? string.Empty;
        if (firstLine.Length > 80) firstLine = firstLine[..80] + "…";
        return firstLine;
    }
}

/// <summary>代码生成结果</summary>
public class GeneratedCode
{
    public string Code { get; set; } = string.Empty;
    public List<string> Dependencies { get; set; } = new();
    public string Description { get; set; } = string.Empty;
}