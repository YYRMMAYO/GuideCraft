using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace GuideCraft.Controls;

/// <summary>轻量 Markdown 渲染控件：内含 FlowDocumentScrollViewer，按 MarkdownText 属性自动重建</summary>
public class MarkdownView : FrameworkElement
{
    private readonly FlowDocumentScrollViewer _viewer;

    public static readonly DependencyProperty MarkdownTextProperty =
        DependencyProperty.Register(
            nameof(MarkdownText),
            typeof(string),
            typeof(MarkdownView),
            new FrameworkPropertyMetadata(
                string.Empty,
                FrameworkPropertyMetadataOptions.AffectsMeasure,
                OnMarkdownTextChanged));

    public string MarkdownText
    {
        get => (string)GetValue(MarkdownTextProperty);
        set => SetValue(MarkdownTextProperty, value);
    }

    public MarkdownView()
    {
        _viewer = new FlowDocumentScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            IsToolBarVisible = false,
            Padding = new Thickness(0),
            Margin = new Thickness(0),
            Background = Brushes.Transparent,
            Focusable = false
        };
        AddVisualChild(_viewer);
        AddLogicalChild(_viewer);
        Rebuild();
    }

    private static void OnMarkdownTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((MarkdownView)d).Rebuild();
    }

    private void Rebuild()
    {
        _viewer.Document = MarkdownRenderer.Render(MarkdownText ?? string.Empty);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        _viewer.Measure(availableSize);
        return _viewer.DesiredSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        _viewer.Arrange(new Rect(finalSize));
        return finalSize;
    }

    protected override int VisualChildrenCount => 1;

    protected override Visual GetVisualChild(int index) => _viewer;
}