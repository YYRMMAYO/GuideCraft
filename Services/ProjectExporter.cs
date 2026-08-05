using System.IO;
using System.IO.Compression;
using System.Text;

namespace GuideCraft.Services;

/// <summary>项目导出服务：把生成的代码打包为 ZIP（main.py + requirements.txt + README.md + 需求文档）</summary>
public interface IProjectExporter
{
    /// <summary>生成 ZIP 字节流</summary>
    byte[] BuildZip(string title, GeneratedCode code, string requirementDocument);

    /// <summary>直接保存到指定路径</summary>
    void SaveTo(string path, string title, GeneratedCode code, string requirementDocument);
}

public sealed class ProjectExporter : IProjectExporter
{
    public byte[] BuildZip(string title, GeneratedCode code, string requirementDocument)
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(zip, "main.py", code.Code);
            WriteEntry(zip, "requirements.txt", string.Join("\n", code.Dependencies ?? new List<string>()));
            WriteEntry(zip, "README.md", BuildReadme(title, code));
            WriteEntry(zip, "REQUIREMENT.md", requirementDocument ?? string.Empty);
        }
        return ms.ToArray();
    }

    public void SaveTo(string path, string title, GeneratedCode code, string requirementDocument)
    {
        File.WriteAllBytes(path, BuildZip(title, code, requirementDocument));
    }

    private static void WriteEntry(ZipArchive zip, string name, string content)
    {
        var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
        using var stream = entry.Open();
        var bytes = Encoding.UTF8.GetBytes(content ?? string.Empty);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static string BuildReadme(string title, GeneratedCode code)
    {
        var deps = (code.Dependencies ?? new List<string>()).Count == 0
            ? "（无第三方依赖）"
            : string.Join("\n", code.Dependencies.Select(d => $"- {d}"));

        return $"""
# {title}

{code.Description}

## 运行步骤

1. 安装 Python（3.8+）
2. 安装依赖：
   ```bash
   pip install -r requirements.txt
   ```
3. 运行脚本：
   ```bash
   python main.py
   ```

## 依赖

{deps}

## 项目结构

- `main.py` — 主程序（按需求文档实现）
- `requirements.txt` — Python 依赖清单
- `REQUIREMENT.md` — 原始需求文档（供二次开发参考）
""";
    }
}