using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GuideCraft.ViewModels;

namespace GuideCraft.Views;

/// <summary>内容宿主：对话 / 设置 双面板切换，Enter 发送，滚动跟随</summary>
public partial class ContentHost : UserControl
{
    private MainViewModel? _vm;

    public ContentHost()
    {
        InitializeComponent();
        Loaded += (_, _) => HookViewModel();
        DataContextChanged += (_, _) => HookViewModel();
    }

    private void HookViewModel()
    {
        if (_vm is not null)
        {
            _vm.ScrollToBottomRequested -= ScrollToBottom;
        }
        _vm = DataContext as MainViewModel;
        if (_vm is not null)
        {
            _vm.ScrollToBottomRequested += ScrollToBottom;
        }
    }

    private void ScrollToBottom()
    {
        Dispatcher.BeginInvoke(() => MessagesScroller.ScrollToEnd());
    }

    private void InputBox_OnKeyDown(object sender, KeyEventArgs e)
    {
        // Enter 发送（Shift+Enter 换行）
        if (e.Key == Key.Enter && Keyboard.Modifiers != ModifierKeys.Shift && _vm is not null)
        {
            if (_vm.SendCommand.CanExecute(null))
            {
                _vm.SendCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
