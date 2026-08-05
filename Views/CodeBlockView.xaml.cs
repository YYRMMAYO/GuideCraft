using System.Windows;
using System.Windows.Controls;
using GuideCraft.Controls;

namespace GuideCraft.Views;

/// <summary>代码块视图：AvalonEdit 只读编辑器 + 复制按钮 + 语言标签</summary>
public partial class CodeBlockView : UserControl
{
    public static readonly DependencyProperty CodeProperty =
        DependencyProperty.Register(nameof(Code), typeof(string), typeof(CodeBlockView),
            new PropertyMetadata(string.Empty, OnCodeChanged));

    public static readonly DependencyProperty CodeLanguageProperty =
        DependencyProperty.Register(nameof(CodeLanguage), typeof(string), typeof(CodeBlockView),
            new PropertyMetadata(string.Empty, OnLanguageChanged));

    public string Code
    {
        get => (string)GetValue(CodeProperty);
        set => SetValue(CodeProperty, value);
    }

    public string CodeLanguage
    {
        get => (string)GetValue(CodeLanguageProperty);
        set => SetValue(CodeLanguageProperty, value);
    }

    public CodeBlockView()
    {
        InitializeComponent();
        ApplyLanguageHighlighting();
    }

    private static void OnCodeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((CodeBlockView)d).Editor.Text = (e.NewValue as string) ?? string.Empty;
    }

    private static void OnLanguageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((CodeBlockView)d).ApplyLanguageHighlighting();
    }

    /// <summary>根据语言名应用 AvalonEdit 内置语法高亮（支持的子集：python/json/xml/sql）</summary>
    private void ApplyLanguageHighlighting()
    {
        var lang = (CodeLanguage ?? string.Empty).ToLowerInvariant();
        ICSharpCode.AvalonEdit.Highlighting.IHighlightingDefinition? def = null;
        try
        {
            def = lang switch
            {
                "python" or "py" => ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition("Python"),
                "json" => ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition("Json"),
                "xml" or "html" => ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition("XML"),
                "sql" => ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition("TSQL"),
                "js" or "javascript" or "ts" or "typescript" => ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition("JavaScript"),
                "cs" or "csharp" => ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition("C#"),
                _ => null
            };
        }
        catch { /* 高亮加载失败时降级为纯文本 */ }
        Editor.SyntaxHighlighting = def;
        LangLabel.Text = string.IsNullOrEmpty(CodeLanguage) ? "text" : CodeLanguage.ToLowerInvariant();
    }

    private void CopyButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(Editor.Text ?? string.Empty);
            CopyButton.Content = "✓ 已复制";
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1.4)
            };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                CopyButton.Content = "📋 复制";
            };
            timer.Start();
        }
        catch
        {
            CopyButton.Content = "复制失败";
        }
    }
}