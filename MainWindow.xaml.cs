using System.Windows;
using System.Windows.Input;
using GuideCraft.ViewModels;

namespace GuideCraft;

/// <summary>主窗口：对话为主 + 设置抽屉 + 屏幕边缘磁吸 + Esc 关闭抽屉</summary>
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
    }

    /// <summary>Esc 关闭设置抽屉</summary>
    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && _vm.IsSettingsOpen)
        {
            _vm.CloseSettingsCommand.Execute(null);
            e.Handled = true;
        }
    }

    /// <summary>点击遮罩关闭设置抽屉</summary>
    private void Overlay_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _vm.CloseSettingsCommand.Execute(null);
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
