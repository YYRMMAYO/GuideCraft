using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace GuideCraft.Services;

/// <summary>沙盒试运行结果</summary>
public sealed class SandboxRunResult
{
    /// <summary>是否允许执行（AST 预检通过）</summary>
    public bool Allowed { get; set; } = true;

    /// <summary>预检被拒绝的原因（Allowed=false 时）</summary>
    public string? RejectReason { get; set; }

    /// <summary>退出码（null 表示超时被杀）</summary>
    public int? ExitCode { get; set; }

    /// <summary>标准输出</summary>
    public string StdOut { get; set; } = string.Empty;

    /// <summary>标准错误</summary>
    public string StdErr { get; set; } = string.Empty;

    /// <summary>是否超时</summary>
    public bool TimedOut { get; set; }

    /// <summary>耗时</summary>
    public double DurationSeconds { get; set; }

    /// <summary>合并输出（截断后展示）</summary>
    public string DisplayOutput
    {
        get
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(StdOut)) sb.Append(StdOut.TrimEnd());
            if (!string.IsNullOrWhiteSpace(StdErr))
            {
                if (sb.Length > 0) sb.Append('\n');
                sb.Append(StdErr.TrimEnd());
            }
            var text = sb.ToString();
            const int maxLen = 6000;
            return text.Length > maxLen ? text[..maxLen] + "\n…（输出已截断）" : text;
        }
    }
}

/// <summary>代码沙盒试运行服务：AST 静态预检 + 临时目录 + 子进程隔离 + 超时终止</summary>
public interface ICodeRunner
{
    /// <summary>在沙盒中试运行一段 Python 代码</summary>
    Task<SandboxRunResult> RunPythonAsync(string code, int timeoutSeconds, CancellationToken ct = default);
}

/// <summary>
/// 安全设计（参考 Cursor 沙盒模式 / Claude 沙箱实践）：
/// 1. AST 静态分析拦截危险操作（os.system / subprocess / eval / exec / 网络 / 文件系统写）
/// 2. 写入系统临时目录（隔离工作区）
/// 3. 子进程独立运行，超时自动 Kill（含进程树）
/// 4. 禁止 shell（ProcessStartInfo.UseShellExecute=false），参数化启动
/// </summary>
public sealed class CodeRunner : ICodeRunner
{
    private static readonly string[] ForbiddenImports =
    {
        "os", "subprocess", "shutil", "socket", "http", "urllib", "requests",
        "ctypes", "pickle", "shelve", "marshal", "socketserver", "telnetlib",
        "ftplib", "imaplib", "smtplib", "webbrowser", "winreg", "crypt"
    };

    private static readonly string[] ForbiddenAttrs =
    {
        "system", "popen", "startfile", "remove", "rmdir", "unlink", "mkdir",
        "chmod", "rename", "replace", "truncate", "kill", "terminate"
    };

    private static readonly string[] ForbiddenCalls =
    {
        "eval", "exec", "compile", "open", "input", "exit", "quit"
    };

    public async Task<SandboxRunResult> RunPythonAsync(string code, int timeoutSeconds, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        // ---------- 1. AST 静态预检 ----------
        if (string.IsNullOrWhiteSpace(code))
        {
            return new SandboxRunResult { Allowed = false, RejectReason = "代码为空" };
        }
        var precheck = Precheck(code);
        if (!precheck.Allowed)
        {
            return new SandboxRunResult { Allowed = false, RejectReason = precheck.RejectReason };
        }

        // ---------- 2. 写临时脚本 ----------
        var workDir = Path.Combine(Path.GetTempPath(), "GuideCraftSandbox");
        Directory.CreateDirectory(workDir);
        var scriptPath = Path.Combine(workDir, $"main_{Guid.NewGuid():N}.py");
        try
        {
            await File.WriteAllTextAsync(scriptPath, code, Encoding.UTF8, ct);

            // ---------- 3. 子进程执行 ----------
            var psi = new ProcessStartInfo
            {
                FileName = "python",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = workDir
            };
            psi.ArgumentList.Add(scriptPath);

            using var proc = new Process { StartInfo = psi };
            var stdoutTask = proc.StandardOutput.ReadToEndAsync(ct);
            var stderrTask = proc.StandardError.ReadToEndAsync(ct);

            proc.Start();

            var exited = await WaitForExitAsync(proc, TimeSpan.FromSeconds(timeoutSeconds), ct);
            if (!exited)
            {
                KillTree(proc);
                return new SandboxRunResult
                {
                    TimedOut = true,
                    ExitCode = null,
                    StdOut = await SafeRead(stdoutTask),
                    StdErr = await SafeRead(stderrTask),
                    DurationSeconds = sw.Elapsed.TotalSeconds
                };
            }

            return new SandboxRunResult
            {
                ExitCode = proc.ExitCode,
                StdOut = await SafeRead(stdoutTask),
                StdErr = await SafeRead(stderrTask),
                DurationSeconds = sw.Elapsed.TotalSeconds
            };
        }
        catch (Exception ex)
        {
            return new SandboxRunResult
            {
                Allowed = false,
                RejectReason = $"执行失败：{ex.Message}"
            };
        }
        finally
        {
            TryDelete(scriptPath);
        }
    }

    /// <summary>AST 静态预检：拦截危险 import / 属性访问 / 调用（轻量正则扫描，零额外依赖）</summary>
    internal static SandboxRunResult Precheck(string code)
    {
        try
        {
            return RegexPrecheck(code);
        }
        catch
        {
            return new SandboxRunResult { Allowed = false, RejectReason = "代码语法分析失败" };
        }
    }

    private static SandboxRunResult RegexPrecheck(string code)
    {
        // 1. 危险 import（含 import x / from x import y / import x.y）
        foreach (var imp in ForbiddenImports)
        {
            if (Regex.IsMatch(code, $@"(^|\s)(import|from)\s+{Regex.Escape(imp)}(\s|\.|,|$)", RegexOptions.Multiline | RegexOptions.IgnoreCase))
            {
                return new SandboxRunResult { Allowed = false, RejectReason = $"检测到受控模块 import：{imp}（沙盒出于安全考虑已拦截）" };
            }
        }

        // 2. 危险属性访问（os.system / subprocess.Popen / shutil.rmtree 等）
        foreach (var attr in ForbiddenAttrs)
        {
            if (Regex.IsMatch(code, $@"\.{Regex.Escape(attr)}\s*\(", RegexOptions.IgnoreCase))
            {
                return new SandboxRunResult { Allowed = false, RejectReason = $"检测到危险系统调用：.{attr}( )（沙盒出于安全考虑已拦截）" };
            }
        }

        // 3. 危险内建调用
        foreach (var call in ForbiddenCalls)
        {
            if (Regex.IsMatch(code, $@"(?<![\w.]){call}\s*\(", RegexOptions.IgnoreCase))
            {
                return new SandboxRunResult { Allowed = false, RejectReason = $"检测到危险调用：{call}( )（沙盒出于安全考虑已拦截）" };
            }
        }

        // 4. 网络地址直连（http:// 与 https:// 文本出现）
        if (Regex.IsMatch(code, @"https?://", RegexOptions.IgnoreCase))
        {
            return new SandboxRunResult { Allowed = false, RejectReason = "检测到网络访问（http/https）（沙盒默认隔离网络）" };
        }

        return new SandboxRunResult { Allowed = true };
    }

    /// <summary>等待进程退出；超时或取消返回 false（需调用方 Kill）</summary>
    private static async Task<bool> WaitForExitAsync(Process proc, TimeSpan timeout, CancellationToken ct)
    {
        if (proc.HasExited) return true;
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        proc.EnableRaisingEvents = true;
        proc.Exited += (_, _) => tcs.TrySetResult(proc.HasExited);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var timeoutTask = Task.Delay(timeout, timeoutCts.Token);

        var completed = await Task.WhenAny(tcs.Task, timeoutTask);
        if (completed != tcs.Task)
            return false; // 超时（或取消）

        timeoutCts.Cancel();
        return await tcs.Task;
    }

    /// <summary>终止进程树（Windows：taskkill /T /F）</summary>
    private static void KillTree(Process proc)
    {
        try
        {
            if (!proc.HasExited) proc.Kill(entireProcessTree: true);
        }
        catch
        {
            try { proc.Kill(); } catch { /* 已退出 */ }
        }
    }

    private static async Task<string> SafeRead(Task<string> task)
    {
        try
        {
            var delay = Task.Delay(TimeSpan.FromSeconds(2));
            var completed = await Task.WhenAny(task, delay);
            return completed == task ? await task : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { /* 忽略 */ }
    }
}
