using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using GuideCraft.ViewModels;
using GuideCraft.Views;
using Microsoft.Extensions.DependencyInjection;

namespace GuideCraft;

/// <summary>主窗口：对话为主 + 独立设置窗口 + 屏幕边缘磁吸 + 首次引导教程动画</summary>
public partial class MainWindow : Window
{
    private const int SnapThreshold = 14;

    private readonly MainViewModel _vm;

    public MainWindow(MainViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;

        LocationChanged += OnWindowLocationChanged;
        PreviewKeyDown += OnPreviewKeyDown;
        Loaded += OnLoaded;

        // 首次引导教程：订阅步骤切换事件播放卡片动画 + 圆点高亮
        _vm.WelcomeStepChanged += OnWelcomeStepChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // 打开时如有引导教程，播放入场动画
        if (_vm.IsWelcomeVisible)
            PlayWelcomeCardAnimation();
    }

    // ---------- 首次引导教程动画 ----------

    /// <summary>圆点指示器数组（与 WelcomeStepData 顺序一致）</summary>
    private System.Windows.Shapes.Ellipse[] Dots => new[] { Dot0, Dot1, Dot2, Dot3 };

    private void OnWelcomeStepChanged(int step)
    {
        // 圆点高亮随步骤切换
        for (int i = 0; i < Dots.Length; i++)
        {
            Dots[i].Fill = i == step
                ? GetThemeBrush("AccentBrush")
                : GetThemeBrush("SecondaryTextBrush");
        }
        PlayWelcomeCardAnimation();
    }

    /// <summary>卡片动画：淡入 + 轻微缩放 + 上浮（随步骤轮播带来"流动"感）</summary>
    private void PlayWelcomeCardAnimation()
    {
        var card = WelcomeCard;

        // 淡入
        card.BeginAnimation(OpacityProperty, new DoubleAnimation(0.35, 1, TimeSpan.FromMilliseconds(320))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        });

        // 缩放 + 位移（TransformGroup 挂载，避免依赖单一变换类型）
        if (card.RenderTransform is not TransformGroup group || group.Children.Count < 2)
        {
            group = new TransformGroup();
            group.Children.Add(new ScaleTransform(0.96, 0.96));   // [0] 缩放
            group.Children.Add(new TranslateTransform(6, 0));     // [1] 上浮
            card.RenderTransform = group;
            card.RenderTransformOrigin = new Point(0.5, 0.5);
        }

        var scale = (ScaleTransform)group.Children[0];
        var translate = (TranslateTransform)group.Children[1];
        var duration = TimeSpan.FromMilliseconds(360);
        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };

        scale.BeginAnimation(ScaleTransform.ScaleXProperty,
            new DoubleAnimation(0.96, 1, duration) { EasingFunction = easing });
        scale.BeginAnimation(ScaleTransform.ScaleYProperty,
            new DoubleAnimation(0.96, 1, duration) { EasingFunction = easing });
        translate.BeginAnimation(TranslateTransform.YProperty,
            new DoubleAnimation(6, 0, duration) { EasingFunction = easing });
    }

    /// <summary>从当前主题资源中取画刷（AccentBrush / SecondaryTextBrush 等）</summary>
    private Brush GetThemeBrush(string key)
        => TryFindResource(key) as Brush ?? Brushes.Gray;

    /// <summary>Esc 关闭独立设置窗口（若打开）</summary>
    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            var settingsWin = Application.Current.Windows
                .OfType<SettingsWindow>().FirstOrDefault();
            if (settingsWin is { IsVisible: true })
            {
                settingsWin.Close();
                e.Handled = true;
            }
        }
    }

    /// <summary>窗口拖动到屏幕工作区边缘（阈值内）自动磁吸</summary>
    private void OnWindowLocationChanged(object? sender, EventArgs e)
    {
        if (WindowState != WindowState.Normal) return;

        var wa = SystemParameters.WorkArea;
        double left = Left;
        double top = Top;
        double right = wa.Right - (Left + Width);
        double bottom = wa.Bottom - (Top + Height);

        double newLeft = Left;
        double newTop = Top;

        if (Math.Abs(left) < SnapThreshold) newLeft = wa.Left;
        else if (Math.Abs(right) < SnapThreshold) newLeft = wa.Right - Width;

        if (Math.Abs(top) < SnapThreshold) newTop = wa.Top;
        else if (Math.Abs(bottom) < SnapThreshold) newTop = wa.Bottom - Height;

        if (Math.Abs(newLeft - Left) > 0.1 || Math.Abs(newTop - Top) > 0.1)
        {
            Left = newLeft;
            Top = newTop;
        }
    }
}
