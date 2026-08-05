using System.Globalization;
using System.Windows.Data;

namespace GuideCraft.Converters;

/// <summary>bool 取反（用于发送按钮可用性等）</summary>
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not true;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
