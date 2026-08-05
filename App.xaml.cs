using System.Net.Http;
using System.Windows;
using System.Windows.Threading;
using GuideCraft.Services;
using GuideCraft.ViewModels;
using GuideCraft.Views;
using Microsoft.Extensions.DependencyInjection;

namespace GuideCraft;

/// <summary>应用入口：依赖注入容器、主题初始化、全局异常捕获</summary>
public partial class App : Application
{
    private ServiceProvider? _provider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        // ---- 依赖注入容器 ----
        var services = new ServiceCollection();
        services.AddSingleton<HttpClient>();
        services.AddSingleton<ILlmClient, LlmApiClient>();
        services.AddSingleton<ILocalStorageService, LocalStorageService>();
        services.AddSingleton<ICryptoService, CryptoService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IChatService, ChatService>();
        services.AddSingleton<IRequirementSummarizer, RequirementSummarizer>();
        services.AddSingleton<ICodeGenerator, CodeGenerator>();
        services.AddSingleton<IProjectExporter, ProjectExporter>();
        services.AddSingleton<IUpdateChecker, UpdateChecker>();
        services.AddSingleton<MainViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<SettingsDialog>();
        services.AddTransient<MainWindow>();
        _provider = services.BuildServiceProvider();

        // ---- 应用保存的主题与语言 ----
        var settings = _provider.GetRequiredService<ISettingsService>();
        ThemeManager.Apply(settings.Settings.Theme);
        Localization.LocalizationManager.Apply(settings.Settings.Language);

        // ---- 主窗口 ----
        var window = _provider.GetRequiredService<MainWindow>();
        MainWindow = window;

        // 加载历史会话
        var vm = _provider.GetRequiredService<ViewModels.MainViewModel>();
        vm.LoadConversations();

        window.Show();
#if DEBUG
        // 调试期：模拟一个已生成产物的会话，验证 UI 完整状态（命令行 --no-demo 跳过）
        var isDemo = !e.Args.Contains("--no-demo");
        if (isDemo && vm.Conversations.Count == 0)
        {
            var conv = new Models.Conversation
            {
                Title = "Gmail 自动整理脚本"
            };
            conv.Messages.Add(new Models.ChatMessage { Role = Models.ChatRole.User, Content = "我想做一个自动整理每天邮件的脚本" });
            conv.Messages.Add(new Models.ChatMessage { Role = Models.ChatRole.Assistant,
                Content = "好的，请告诉我**邮件来源**和**整理频率**？" });
            conv.Messages.Add(new Models.ChatMessage { Role = Models.ChatRole.User, Content = "Gmail 邮箱，每天早上 8 点" });
            conv.Messages.Add(new Models.ChatMessage { Role = Models.ChatRole.Assistant,
                Content = "### 需求摘要\n\n**一句话目标**：每天早上 8 点自动整理 Gmail 未读邮件并归类。\n\n**输入数据**：Gmail IMAP 邮箱；**输出形式**：按发件人域名分组保存到本地 Markdown 文件。\n\n请确认以上理解是否正确。" });
            conv.Messages.Add(new Models.ChatMessage { Role = Models.ChatRole.User, Content = "确认" });
            conv.Messages.Add(new Models.ChatMessage { Role = Models.ChatRole.Assistant, Content = "✅ 已生成 Python 脚本\n\n**Gmail 自动整理脚本**\n\n**依赖：** google-api-python-client、google-auth\n\n```python\nfrom google.auth.transport.requests import Request\nfrom google.oauth2.credentials import Credentials\nfrom google_auth_oauthlib.flow import InstalledAppFlow\nfrom googleapiclient.discovery import build\nimport datetime\nimport os\n\nSCOPES = ['https://www.googleapis.com/auth/gmail.readonly']\n\ndef main():\n    \"\"\"每天运行一次，整理昨日未读邮件并按域名分组保存。\"\"\"\n    creds = authenticate()\n    service = build('gmail', 'v1', credentials=creds)\n    yesterday = (datetime.date.today() - datetime.timedelta(days=1)).isoformat()\n    results = service.users().messages().list(userId='me', q=f'after:{yesterday}').execute()\n    # 略：按域名分组、保存到本地……\n    print('整理完成')\n\nif __name__ == '__main__':\n    main()\n```\n\n---\n你可以回复修改意见，我会更新代码；或点击下方「📦 导出项目 ZIP」按钮下载完整项目。" });
            conv.GeneratedProject = new Models.GeneratedProject
            {
                Type = Models.ProjectType.PythonScript,
                Description = "Gmail 自动整理脚本",
                Dependencies = new System.Collections.Generic.List<string> { "google-api-python-client", "google-auth" },
                Code = "from google.auth.transport.requests import Request\nfrom google.oauth2.credentials import Credentials\nfrom google_auth_oauthlib.flow import InstalledAppFlow\nfrom googleapiclient.discovery import build\nimport datetime\nimport os\n\nSCOPES = ['https://www.googleapis.com/auth/gmail.readonly']\n\ndef main():\n    creds = authenticate()\n    service = build('gmail', 'v1', credentials=creds)\n    yesterday = (datetime.date.today() - datetime.timedelta(days=1)).isoformat()\n    results = service.users().messages().list(userId='me', q=f'after:{yesterday}').execute()\n    print('整理完成')\n\nif __name__ == '__main__':\n    main()\n",
                RequirementDocument = "需求：每天早上 8 点自动整理 Gmail 未读邮件并按域名分组。"
            };
            vm.Conversations.Add(conv);
            vm.SelectedConversation = conv;
        }
#endif
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _provider?.Dispose();
        base.OnExit(e);
    }

    /// <summary>XAML 解析异常等 UI 线程异常统一弹窗提示，避免闪退</summary>
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            $"程序遇到问题：\n{e.Exception.Message}",
            UiStrings.AppTitle,
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            MessageBox.Show(
                $"发生未处理的异常：\n{ex.Message}",
                UiStrings.AppTitle,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
