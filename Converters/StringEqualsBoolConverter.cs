using System.Globalization;
using System.Windows.Data;

namespace GuideCraft.Converters;

/// <summary>字符串相等 → bool。RadioButton.IsChecked 双向绑定 SelectedString + ConverterParameter="xxx"。</summary>
public class StringEqualsBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value?.ToString() is { } s && parameter is string p && string.Equals(s, p, StringComparison.OrdinalIgnoreCase);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => parameter as string ?? string.Empty;
}