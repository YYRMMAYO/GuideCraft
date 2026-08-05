using System.Windows;
using System.Windows.Media.Animation;
using GuideCraft.Services;
using GuideCraft.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace GuideCraft.Views;

/// <summary>独立设置窗口（v2.0）：左侧分类导航 + 右侧内容区，淡入缩放打开动画</summary>
public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _vm;

    public SettingsWindow(IServiceProvider services)
    {
        InitializeComponent();
        _vm = services.GetRequiredService<SettingsViewModel>();
        DataContext = _vm;
        Owner = Application.Current.MainWindow;
    }

    /// <summary>打开并定位到指定分类</summary>
    public void OpenAt(SettingsTab tab)
    {
        _vm.ActiveTab = tab;
        if (tab == SettingsTab.Stats)
            _vm.RefreshStatsCommand.Execute(null);
        Show();
        Activate();
        PlayOpenAnimation();
    }

    /// <summary>窗口打开动画：淡入 + 缩放</summary>
    private void PlayOpenAnimation()
    {
        Opacity = 0;
        var fade = new DoubleAnimation(0, 1, System.TimeSpan.FromMilliseconds(220))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        BeginAnimation(OpacityProperty, fade);

        var scale = (System.Windows.Media.ScaleTransform)RenderTransform;
        scale.ScaleX = 0.97;
        scale.ScaleY = 0.97;
        var zoom = new DoubleAnimation(0.97, 1, System.TimeSpan.FromMilliseconds(240))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        scale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, zoom);
        scale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, zoom);
    }

    protected override void OnSourceInitialized(System.EventArgs e)
    {
        base.OnSourceInitialized(e);
        // 设置缩放变换原点为窗口中心
        var scale = new System.Windows.Media.ScaleTransform(1, 1, 0, 0)
        {
            CenterX = 0,
            CenterY = 0
        };
        // 通过 RenderTransformOrigin 让缩放从中心展开（依赖布局后尺寸）
        RenderTransformOrigin = new Point(0.5, 0.5);
        RenderTransform = scale;
    }
}
