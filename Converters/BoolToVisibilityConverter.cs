using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GuideCraft.Converters;

/// <summary>bool → Visibility（true 显示）</summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
