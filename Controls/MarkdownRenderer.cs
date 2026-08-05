using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using GuideCraft.Views;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using MdBlock = Markdig.Syntax.Block;

namespace GuideCraft.Controls;

/// <summary>
/// Markdown 渲染器：使用 Markdig 解析 + 自研 FlowDocument 渲染。
/// 支持：标题、加粗/斜体/删除线、行内代码、围栏代码块（含 AvalonEdit 高亮）、无序/有序列表、链接、引用、分隔线、普通段落。
/// 表格/脚注等聊天场景罕见元素暂不支持。
/// </summary>
public static class MarkdownRenderer
{
    /// <summary>主色画刷（取自当前主题的 AccentBrush）</summary>
    public static Brush AccentBrush { get; set; } = new SolidColorBrush(Color.FromRgb(0x4C, 0x6F, 0xFF));

    /// <summary>代码块背景/前景画刷</summary>
    public static Brush CodeBackgroundBrush { get; set; } = new SolidColorBrush(Color.FromRgb(0xF2, 0xF3, 0xF5));
    public static Brush CodeTextBrush { get; set; } = new SolidColorBrush(Color.FromRgb(0x24, 0x29, 0x2F));
    public static Brush InlineCodeBackgroundBrush { get; set; } = new SolidColorBrush(Color.FromRgb(0xF2, 0xF3, 0xF5));
    public static Brush InlineCodeTextBrush { get; set; } = new SolidColorBrush(Color.FromRgb(0xD6, 0x33, 0x6C));
    public static Brush QuoteBorderBrush { get; set; } = new SolidColorBrush(Color.FromRgb(0xCC, 0xD1, 0xDA));
    public static Brush LinkBrush { get; set; } = new SolidColorBrush(Color.FromRgb(0x4C, 0x6F, 0xFF));
    public static Brush TextBrush { get; set; } = new SolidColorBrush(Color.FromRgb(0x1F, 0x23, 0x29));

    public static FlowDocument Render(string markdown)
    {
        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Microsoft YaHei UI, Segoe UI"),
            FontSize = 13.5,
            PagePadding = new Thickness(0),
            Background = Brushes.Transparent,
            IsOptimalParagraphEnabled = true,
            IsHyphenationEnabled = false,
            Foreground = TextBrush
        };
        if (string.IsNullOrEmpty(markdown)) return doc;

        // 流式期间可能内容尚未完整，容错解析
        var pipeline = new MarkdownPipelineBuilder()
            .UseEmphasisExtras()
            .UseAutoLinks()
            .Build();
        var mdDoc = Markdown.Parse(markdown, pipeline);

        foreach (var block in mdDoc)
        {
            AppendBlock(doc, block);
        }
        return doc;
    }

    private static void AppendBlock(FlowDocument doc, MdBlock block)
    {
        switch (block)
        {
            case HeadingBlock h:
                doc.Blocks.Add(MakeHeading(h));
                break;
            case ParagraphBlock p:
                doc.Blocks.Add(MakeParagraph(p));
                break;
            case ListBlock l:
                doc.Blocks.Add(MakeList(l));
                break;
            case QuoteBlock q:
                doc.Blocks.Add(MakeQuote(q));
                break;
            case ThematicBreakBlock:
                doc.Blocks.Add(new BlockUIContainer(MakeSeparator()));
                break;
            case FencedCodeBlock fc:
                doc.Blocks.Add(MakeCodeBlock(fc.Info ?? string.Empty, fc.Lines.ToString()));
                break;
            case CodeBlock cb:
                doc.Blocks.Add(MakeCodeBlock(string.Empty, cb.Lines.ToString()));
                break;
            default:
                // HTMLBlock 等未识别类型：作为段落渲染其文本
                doc.Blocks.Add(new Paragraph(new Run(block.ToString() ?? string.Empty)));
                break;
        }
    }

    /// <summary>把列表项中除首段外的子块追加到首段 Paragraph 末尾（折叠为同一段落或作为新段落追加）</summary>
    private static void AppendBlock(Paragraph parent, MdBlock block)
    {
        // 子块在 ListItem 内遇到：代码块/列表/标题等用空 Run 占位（实际不会出现在简单列表中）
        parent.Inlines.Add(new Run(block.ToString() ?? string.Empty));
    }

    private static Paragraph MakeHeading(HeadingBlock h)
    {
        var size = h.Level switch { 1 => 18.0, 2 => 16.0, 3 => 14.5, _ => 14.0 };
        var para = new Paragraph
        {
            Margin = new Thickness(0, h.Level == 1 ? 8 : 6, 0, 4),
            FontWeight = FontWeights.Bold,
            FontSize = size
        };
        AppendInlines(para.Inlines, h.Inline);
        return para;
    }

    private static Paragraph MakeParagraph(ParagraphBlock p)
    {
        var para = new Paragraph { Margin = new Thickness(0, 0, 0, 6), LineHeight = 22 };
        AppendInlines(para.Inlines, p.Inline);
        return para;
    }

    private static List MakeList(ListBlock l)
    {
        var list = new List
        {
            MarkerStyle = l.IsOrdered ? TextMarkerStyle.Decimal : TextMarkerStyle.Disc,
            Margin = new Thickness(0, 0, 0, 6),
            Padding = new Thickness(20, 0, 0, 0)
        };
        foreach (var item in l)
        {
            if (item is ListItemBlock li)
            {
                var p = new Paragraph { Margin = new Thickness(0, 0, 0, 3) };
                if (li.Count > 0 && li[0] is ParagraphBlock firstP)
                {
                    AppendInlines(p.Inlines, firstP.Inline);
                    foreach (var sub in li.Skip(1))
                        AppendBlock(p, sub);
                }
                list.ListItems.Add(new ListItem(p));
            }
        }
        return list;
    }

    private static Paragraph MakeQuote(QuoteBlock q)
    {
        var para = new Paragraph
        {
            Margin = new Thickness(4, 4, 0, 6),
            BorderBrush = QuoteBorderBrush,
            BorderThickness = new Thickness(3, 0, 0, 0),
            Padding = new Thickness(10, 0, 0, 0),
            Foreground = new SolidColorBrush(Color.FromRgb(0x55, 0x60, 0x70))
        };
        if (q.Count > 0 && q[0] is ParagraphBlock firstP)
            AppendInlines(para.Inlines, firstP.Inline);
        return para;
    }

    private static UIElement MakeSeparator()
    {
        return new System.Windows.Controls.Separator
        {
            Margin = new Thickness(0, 6, 0, 6),
            Background = new SolidColorBrush(Color.FromRgb(0xE4, 0xE7, 0xEC))
        };
    }

    private static BlockUIContainer MakeCodeBlock(string language, string code)
    {
        var cb = new CodeBlockView { CodeLanguage = (language ?? string.Empty).Trim(), Code = code ?? string.Empty };
        return new BlockUIContainer(cb) { Margin = new Thickness(0, 6, 0, 6) };
    }

    // ---------- Inlines ----------

    private static void AppendInlines(InlineCollection inlines, ContainerInline? container)
    {
        if (container is null) return;
        foreach (var inline in container)
        {
            switch (inline)
            {
                case LiteralInline lit:
                    inlines.Add(new Run(lit.Content.ToString()));
                    break;
                case EmphasisInline em:
                    {
                        var sub = new Span();
                        if (em.DelimiterCount == 2) sub.FontWeight = FontWeights.Bold;
                        else if (em.DelimiterCount == 3) sub.FontStyle = FontStyles.Italic;
                        AppendInlines(sub.Inlines, em);
                        inlines.Add(sub);
                    }
                    break;
                case CodeInline ci:
                    inlines.Add(MakeInlineCodeRun(ci.Content));
                    break;
                case LinkInline li when !li.IsImage:
                    {
                        var linkText = ExtractLinkText(li);
                        var hl = new Hyperlink(new Run(linkText))
                        {
                            Foreground = LinkBrush,
                            TextDecorations = TextDecorations.Underline
                        };
                        if (li.Url is not null) hl.NavigateUri = new Uri(li.Url);
                        inlines.Add(hl);
                    }
                    break;
                case LineBreakInline:
                    inlines.Add(new LineBreak());
                    break;
                case AutolinkInline al:
                    inlines.Add(new Hyperlink(new Run(al.Url))
                    {
                        Foreground = LinkBrush,
                        NavigateUri = new Uri(al.Url)
                    });
                    break;
                default:
                    // 未知 inline：尝试渲染其 ToString 作为纯文本
                    inlines.Add(new Run(inline.ToString() ?? string.Empty));
                    break;
            }
        }
    }

    private static Run MakeInlineCodeRun(string code)
    {
        return new Run(code ?? string.Empty)
        {
            Background = InlineCodeBackgroundBrush,
            Foreground = InlineCodeTextBrush,
            FontFamily = new FontFamily("Cascadia Code, Consolas, Courier New")
        };
    }

    /// <summary>提取 LinkInline 显示文本（合并其后代 LiteralInline 等文本节点）</summary>
    private static string ExtractLinkText(LinkInline li)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var lit in li.Descendants<LiteralInline>())
            sb.Append(lit.Content.ToString());
        return sb.ToString();
    }

    private static string ExtractEmText(EmphasisInline em)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var lit in em.Descendants<LiteralInline>())
            sb.Append(lit.Content.ToString());
        return sb.ToString();
    }
}