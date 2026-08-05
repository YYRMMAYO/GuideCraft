using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using GuideCraft.ViewModels;

namespace GuideCraft;

/// <summary>主窗口：双布局 + 屏幕边缘磁吸 + 引导动画</summary>
public partial class MainWindow : Window
{
    private const int SnapThreshold = 14;

    private readonly MainViewModel _vm;
    private readonly Ellipse[] _dots;

    public MainWindow(MainViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;

        _dots = new[] { Dot0, Dot1, Dot2, Dot3 };
        vm.WelcomeStepChanged += step => Dispatcher.BeginInvoke(() => PlayWelcomeAnimation(step));

        // 窗口磁吸：拖动靠近屏幕边缘时自动吸附
        LocationChanged += OnWindowLocationChanged;

        Loaded += (_, _) =>
        {
            if (_vm.IsWelcomeVisible) PlayWelcomeAnimation(_vm.CurrentStep);
        };
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

    /// <summary>引导教程切换动画：卡片淡入 + 上移 + 缩放，圆点高亮当前步骤</summary>
    private void PlayWelcomeAnimation(int step)
    {
        for (int i = 0; i < _dots.Length; i++)
        {
            _dots[i].Fill = i == step
                ? (Brush)FindResource("AccentBrush")
                : (Brush)FindResource("SecondaryTextBrush");
        }

        var duration = TimeSpan.FromMilliseconds(480);
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

        var fade = new DoubleAnimation(0.25, 1.0, duration) { EasingFunction = ease };
        Storyboard.SetTarget(fade, WelcomeCard);
        Storyboard.SetTargetProperty(fade, new PropertyPath(OpacityProperty));

        var slide = new DoubleAnimation(18, 0, duration) { EasingFunction = ease };
        Storyboard.SetTarget(slide, WelcomeCardTranslate);
        Storyboard.SetTargetProperty(slide, new PropertyPath(TranslateTransform.YProperty));

        var scale = new DoubleAnimation(0.96, 1.0, duration) { EasingFunction = ease };
        Storyboard.SetTarget(scale, WelcomeCard);
        Storyboard.SetTargetProperty(scale, new PropertyPath(ScaleTransform.ScaleXProperty));
        var scaleY = new DoubleAnimation(0.96, 1.0, duration) { EasingFunction = ease };
        Storyboard.SetTarget(scaleY, WelcomeCard);
        Storyboard.SetTargetProperty(scaleY, new PropertyPath(ScaleTransform.ScaleYProperty));

        var sb = new Storyboard();
        sb.Children.Add(fade);
        sb.Children.Add(slide);
        sb.Children.Add(scale);
        sb.Children.Add(scaleY);
        sb.Begin(this);
    }
}
