using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using GuideCraft.ViewModels;

namespace GuideCraft;

/// <summary>主窗口：三区布局，Enter 发送，滚动跟随，引导教程动画</summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;
    private readonly Ellipse[] _dots;

    public MainWindow(MainViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;

        vm.ScrollToBottomRequested += () =>
        {
            Dispatcher.BeginInvoke(() => MessagesScroller.ScrollToEnd());
        };

        vm.WelcomeStepChanged += step =>
        {
            Dispatcher.BeginInvoke(() => PlayWelcomeAnimation(step));
        };

        _dots = new[] { Dot0, Dot1, Dot2, Dot3 };
        Loaded += (_, _) =>
        {
            if (_vm.IsWelcomeVisible) PlayWelcomeAnimation(_vm.CurrentStep);
        };
    }

    private void InputBox_OnKeyDown(object sender, KeyEventArgs e)
    {
        // Enter 发送（Shift+Enter 换行）
        if (e.Key == Key.Enter && Keyboard.Modifiers != ModifierKeys.Shift)
        {
            if (_vm.SendCommand.CanExecute(null))
            {
                _vm.SendCommand.Execute(null);
                e.Handled = true;
            }
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
