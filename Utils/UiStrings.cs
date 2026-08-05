using GuideCraft.Localization;

namespace GuideCraft;

/// <summary>界面文案统一入口：走 LocalizationManager（运行时随语言切换）</summary>
public static class UiStrings
{
    public static string AppTitle => LocalizationManager.Get("Str.AppTitle");

    public static string NoApiKeyHint => LocalizationManager.Get("Str.Chat.NoApiKeyHint");

    public static string ApiKeyInvalid => LocalizationManager.Get("Str.Chat.ApiKeyInvalid");

    public static string ApiBadRequest => LocalizationManager.Get("Str.Chat.ApiBadRequest");

    public static string ApiForbidden => LocalizationManager.Get("Str.Chat.ApiForbidden");

    public static string ApiModelNotFound => LocalizationManager.Get("Str.Chat.ApiModelNotFound");

    public static string ApiTooLarge => LocalizationManager.Get("Str.Chat.ApiTooLarge");

    public static string ServiceUnavailable => LocalizationManager.Get("Str.Chat.ServiceUnavailable");

    public static string RateLimited => LocalizationManager.Get("Str.Chat.RateLimited");

    public static string ServerError => LocalizationManager.Get("Str.Chat.ServerError");

    public static string NetworkError => LocalizationManager.Get("Str.Chat.NetworkError");

    public static string StreamTimeout => LocalizationManager.Get("Str.Chat.StreamTimeout");

    public static string NeedPythonHint => LocalizationManager.Get("Str.Chat.NeedPythonHint");

    public static string ConfirmGenerateHint => LocalizationManager.Get("Str.Chat.ConfirmGenerateHint");

    public static string StopGenerated => LocalizationManager.Get("Str.Chat.StopGenerated");

    public static string GeneratingStatus => LocalizationManager.Get("Str.Chat.GeneratingStatus");

    public static string GeneratedHeader => LocalizationManager.Get("Str.Chat.GeneratedHeader");

    public static string Deps => LocalizationManager.Get("Str.Chat.Deps");

    public static string NoDeps => LocalizationManager.Get("Str.Chat.NoDeps");

    public static string IterateHint => LocalizationManager.Get("Str.Chat.IterateHint");

    public static string GenerateFailed => LocalizationManager.Get("Str.Chat.GenerateFailed");

    public static string ExportSuccess => LocalizationManager.Get("Str.ExportSuccess");

    public static string ExportFailed => LocalizationManager.Get("Str.ExportFailed");

    // ---------- 沙盒试运行（v2.0） ----------

    public static string SandboxRun => LocalizationManager.Get("Str.Sandbox.Run");

    public static string SandboxRunning => LocalizationManager.Get("Str.Sandbox.Running");

    public static string SandboxDone => LocalizationManager.Get("Str.Sandbox.Done");

    public static string SandboxTimeout => LocalizationManager.Get("Str.Sandbox.Timeout");

    public static string SandboxRejected => LocalizationManager.Get("Str.Sandbox.Rejected");

    public static string SandboxNoOutput => LocalizationManager.Get("Str.Sandbox.NoOutput");

    public static string SandboxFailed => LocalizationManager.Get("Str.Sandbox.Failed");

    public static string SandboxDisabled => LocalizationManager.Get("Str.Sandbox.Disabled");

    public static string SandboxClear => LocalizationManager.Get("Str.Sandbox.Clear");
}
