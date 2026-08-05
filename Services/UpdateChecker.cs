using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;

namespace GuideCraft.Services;

/// <summary>更新检查结果</summary>
public record UpdateCheckResult(bool HasUpdate, string? LatestTag, string? ReleaseUrl, string? Error);

/// <summary>更新检查服务：查询 GitHub Releases 最新版本并与本地版本比较</summary>
public interface IUpdateChecker
{
    Task<UpdateCheckResult> CheckAsync(CancellationToken ct = default);
}

public sealed class UpdateChecker : IUpdateChecker
{
    private const string Repo = "YYRMMAYO/GuideCraft";

    private readonly HttpClient _http;

    public UpdateChecker(HttpClient http)
    {
        _http = http;
        // 注意：不修改共享 HttpClient 的 Timeout（LlmApiClient 依赖其 5 分钟长超时做流式对话），
        // 改用请求级 CancellationTokenSource 实现 20 秒检查超时。
    }

    public async Task<UpdateCheckResult> CheckAsync(CancellationToken ct = default)
    {
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(20));

            using var req = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{Repo}/releases/latest");
            req.Headers.UserAgent.ParseAdd("GuideCraft");
            req.Headers.Accept.ParseAdd("application/vnd.github+json");

            using var resp = await _http.SendAsync(req, timeoutCts.Token);
            if (!resp.IsSuccessStatusCode)
                return new UpdateCheckResult(false, null, null, $"HTTP {(int)resp.StatusCode}");

            var json = await resp.Content.ReadAsStringAsync(timeoutCts.Token);
            using var doc = JsonDocument.Parse(json);
            var tag = doc.RootElement.TryGetProperty("tag_name", out var t) ? t.GetString() : null;
            var url = doc.RootElement.TryGetProperty("html_url", out var h) ? h.GetString() : null;
            if (string.IsNullOrEmpty(tag))
                return new UpdateCheckResult(false, null, url, null);

            var current = Assembly.GetExecutingAssembly().GetName().Version
                           ?? new Version(1, 0, 0);
            var hasUpdate = Version.TryParse(tag.TrimStart('v', 'V'), out var latest) && latest > current;
            return new UpdateCheckResult(hasUpdate, tag, url, null);
        }
        catch (Exception ex)
        {
            return new UpdateCheckResult(false, null, null, ex.Message);
        }
    }
}
