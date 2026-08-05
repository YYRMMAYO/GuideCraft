namespace GuideCraft;

/// <summary>界面中文提示文案统一管理</summary>
public static class UiStrings
{
    public const string NoApiKeyHint =
        "还没有配置 API Key 哦 😊\n\n请点击右上角「⚙ 设置」→「获取 API Key」，按引导到 DeepSeek 开放平台申请你自己的 Key 并填入，即可开始引导式对话。";

    public const string AppTitle = "GuideCraft · 引导式AI助手";

    public const string NewConversation = "新对话";

    public const string Settings = "设置";

    public const string GetApiKey = "获取 API Key";

    public const string TestConnection = "测试连接";

    public const string Send = "发送";

    public const string Stop = "停止";

    public const string Copy = "复制";

    public const string Copied = "已复制";

    public const string ExportZip = "导出项目 ZIP";

    public const string Exporting = "正在导出...";

    public const string Thinking = "正在思考...";

    public const string ApiKeyInvalid =
        "API Key 无效或请求被拒绝（401）。请到设置中检查 Key 是否正确。";

    public const string RateLimited =
        "请求过于频繁（429），请稍候片刻再试。";

    public const string ServerError =
        "服务端暂时不可用（5xx），请稍后重试。";

    public const string NetworkError =
        "网络连接中断，请检查网络后重试。已生成的内容已为你保留。";

    public const string StreamTimeout =
        "等待响应超时，请重试。";

    public const string NeedPythonHint =
        "生成的代码需要本机安装 Python 环境及依赖后即可运行。\n运行方式：`pip install -r requirements.txt` 然后 `python main.py`";

    public const string ConfirmGenerateHint =
        "以上是需求摘要。如果没有问题，直接回复「确认」即可为你生成代码；如有遗漏或错误，请直接告诉我需要修改的地方。";
}
