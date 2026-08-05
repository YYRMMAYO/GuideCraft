using System.Windows;
using System.Windows.Controls;

namespace GuideCraft.Utils;

/// <summary>PasswordBox 密码绑定辅助（Password 属性不可绑定，附加属性解决）</summary>
public static class PasswordBoxHelper
{
    public static readonly DependencyProperty BoundPasswordProperty =
        DependencyProperty.RegisterAttached(
            "BoundPassword",
            typeof(string),
            typeof(PasswordBoxHelper),
            new FrameworkPropertyMetadata(string.Empty, OnBoundPasswordChanged));

    public static readonly DependencyProperty BindPasswordProperty =
        DependencyProperty.RegisterAttached(
            "BindPassword",
            typeof(bool),
            typeof(PasswordBoxHelper),
            new PropertyMetadata(false, OnBindPasswordChanged));

    private static readonly DependencyProperty UpdatingPasswordProperty =
        DependencyProperty.RegisterAttached(
            "UpdatingPassword",
            typeof(bool),
            typeof(PasswordBoxHelper),
            new PropertyMetadata(false));

    public static string GetBoundPassword(DependencyObject d) => (string)d.GetValue(BoundPasswordProperty);

    public static void SetBoundPassword(DependencyObject d, string value) => d.SetValue(BoundPasswordProperty, value);

    public static bool GetBindPassword(DependencyObject d) => (bool)d.GetValue(BindPasswordProperty);

    public static void SetBindPassword(DependencyObject d, bool value) => d.SetValue(BindPasswordProperty, value);

    private static bool GetUpdatingPassword(DependencyObject d) => (bool)d.GetValue(UpdatingPasswordProperty);

    private static void SetUpdatingPassword(DependencyObject d, bool value) => d.SetValue(UpdatingPasswordProperty, value);

    private static void OnBindPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not PasswordBox box) return;

        if ((bool)e.NewValue)
        {
            box.PasswordChanged += OnPasswordChanged;
        }
        else
        {
            box.PasswordChanged -= OnPasswordChanged;
        }
    }

    private static void OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not PasswordBox box) return;
        if (GetUpdatingPassword(box)) return;

        SetBoundPassword(box, box.Password);
    }

    private static void OnBoundPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not PasswordBox box) return;

        SetUpdatingPassword(box, true);
        box.Password = e.NewValue as string ?? string.Empty;
        SetUpdatingPassword(box, false);
    }
}
