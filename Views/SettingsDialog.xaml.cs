using System.Windows;
using GuideCraft.ViewModels;

namespace GuideCraft.Views;

/// <summary>设置对话框</summary>
public partial class SettingsDialog : Window
{
    private readonly SettingsViewModel _vm;

    public SettingsDialog(SettingsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
    }

    private void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        _vm.Save();
        DialogResult = true;
    }
}
