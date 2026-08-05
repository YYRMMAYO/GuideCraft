using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace GuideCraft.Services;

/// <summary>DeepSeek API 客户端接口（OpenAI 兼容）</summary>
public interface IDeepSeekApiClient
{
    /// <summary>非流式对话，返回完整回复文本</summary>
    Task<string> ChatAsync(
        IReadOnlyList<ChatApiMessage> messages,
        string? apiKey = null,
        string? model = null,
        CancellationToken ct = default);

    /// <summary>流式对话，增量文本通过 onDelta 回调输出</summary>
    Task StreamChatAsync(
        IReadOnlyList<ChatApiMessage> messages,
        Func<string, Task> onDelta,
        string? apiKey = null,
        string? model = null,
        CancellationToken ct = default);
}

/// <summary>API 调用异常（含状态码，便于 UI 映射中文提示）</summary>
public sealed class DeepSeekApiException : Exception
{
    public int StatusCode { get; }

    public DeepSeekApiException(int statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }
}

/// <summary>DeepSeek API 客户端：HttpClient + System.Text.Json 直连，零第三方 AI SDK</summary>
public sealed class DeepSeekApiClient : IDeepSeekApiClient
{
    private const string BaseUrl = "https://api.deepseek.com/chat/completions";
    private const string DefaultModel = "deepseek-v4-flash";

    private readonly HttpClient _http;

    public DeepSeekApiClient(HttpClient http)
    {
        _http = http;
        // 流式场景需要较长的整体超时（服务端排队等待可达数分钟）
        _http.Timeout = TimeSpan.FromMinutes(5);
    }

    public async Task<string> ChatAsync(
        IReadOnlyList<ChatApiMessage> messages,
        string? apiKey = null,
        string? model = null,
        CancellationToken ct = default)
    {
        var sb = new StringBuilder();
        await StreamChatAsync(messages, delta =>
        {
            sb.Append(delta);
            return Task.CompletedTask;
        }, apiKey, model, ct);
        return sb.ToString();
    }

    public async Task StreamChatAsync(
        IReadOnlyList<ChatApiMessage> messages,
        Func<string, Task> onDelta,
        string? apiKey = null,
        string? model = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new DeepSeekApiException(401, "缺少 API Key");

        var payload = BuildPayload(messages, model ?? DefaultModel);
        using var req = new HttpRequestMessage(HttpMethod.Post, BaseUrl);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        req.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        HttpResponseMessage resp;
        try
        {
            // ResponseHeadersRead：响应头就绪即开始读流，不等待整个 body
            resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new DeepSeekApiException(408, "请求超时");
        }
        catch (HttpRequestException)
        {
            throw new DeepSeekApiException(0, "网络错误");
        }

        using (resp)
        {
            if (!resp.IsSuccessStatusCode)
                throw new DeepSeekApiException((int)resp.StatusCode, $"HTTP {(int)resp.StatusCode}");

            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream, Encoding.UTF8);

            while (!ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(ct);
                if (line is null) break;                      // 流正常结束
                if (string.IsNullOrWhiteSpace(line)) continue; // 心跳空行
                if (line.StartsWith(':')) continue;           // keep-alive 注释行（排队等待时必现，跳过！）
                if (!line.StartsWith("data:")) continue;      // 忽略其它行

                var data = line.AsSpan(5).Trim().ToString();
                if (data == "[DONE]") break;                  // 终止标记

                // 逐行解析 JSON；单行 data 完整，按行边界解析安全
                using var doc = JsonDocument.Parse(data);
                var root = doc.RootElement;
                if (!root.TryGetProperty("choices", out var choices)
                    || choices.GetArrayLength() == 0)
                    continue;                                 // usage chunk 或空 choices

                var delta = choices[0].GetProperty("delta");
                if (delta.TryGetProperty("content", out var c)
                    && c.ValueKind == JsonValueKind.String)
                {
                    var text = c.GetString() ?? string.Empty;
                    if (text.Length > 0)
                        await onDelta(text);
                }
            }
        }
    }

    private static object BuildPayload(IReadOnlyList<ChatApiMessage> messages, string model)
    {
        var msgs = messages
            .Where(m => !string.IsNullOrWhiteSpace(m.Content))
            .Select(m => new
            {
                role = m.RoleName,
                content = m.Content
            })
            .ToList();

        return new
        {
            model,
            messages = msgs,
            stream = true,
            temperature = 0.7,
            // v4 系列默认思考模式，引导对话不需要思考链，显式关闭以提速并避免 reasoning_content
            thinking = new { type = "disabled" }
        };
    }
}
