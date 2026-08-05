using System.Windows;
using System.Windows.Input;
using GuideCraft.ViewModels;

namespace GuideCraft;

/// <summary>主窗口：三区布局，Enter 发送，滚动跟随</summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    public MainWindow(MainViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        vm.ScrollToBottomRequested += () =>
        {
            Dispatcher.BeginInvoke(() => MessagesScroller.ScrollToEnd());
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
}
