using GuideCraft.Services;

namespace GuideCraft.Models;

/// <summary>
/// 用户自定义模型配置：每个配置 = 名称 + 提供方 + BaseURL + 模型 ID + 独立 API Key。
/// 用户可自由创建多个配置，每个配置对应自己的 API 密钥，不硬性绑定某个模型。
/// </summary>
public class ModelProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>显示名称（用户自定义）</summary>
    public string Name { get; set; } = "默认配置";

    public LlmProvider Provider { get; set; } = LlmProvider.Qwen;

    /// <summary>OpenAI 兼容 Base URL（用户可填写任意兼容端点）</summary>
    public string BaseUrl { get; set; } = "https://dashscope.aliyuncs.com/compatible-mode/v1";

    /// <summary>模型 ID（如 qwen-plus / deepseek-v4-flash / 任意模型名）</summary>
    public string ModelId { get; set; } = "qwen-plus";

    /// <summary>API Key 密文（经 CryptoService AES-GCM 加密，绝不明文落盘）</summary>
    public string ApiKeyCipher { get; set; } = string.Empty;

    /// <summary>是否默认配置（新建对话时使用）</summary>
    public bool IsDefault { get; set; }

    // ---------- 计费参数（用于费用估算显示，默认按 DeepSeek/Qwen 官方价格） ----------

    /// <summary>输入 tokens 单价（元 / 百万，未命中缓存）</summary>
    public double InputPricePerM { get; set; } = 1.0;

    /// <summary>输出 tokens 单价（元 / 百万）</summary>
    public double OutputPricePerM { get; set; } = 2.0;

    /// <summary>输入 tokens 缓存命中单价（元 / 百万）</summary>
    public double CacheHitPricePerM { get; set; } = 0.02;

    /// <summary>是否启用缓存（启用则请求携带 include_usage 并尽量复用前缀）</summary>
    public bool EnableCache { get; set; } = true;

    /// <summary>备注</summary>
    public string Note { get; set; } = string.Empty;
}
