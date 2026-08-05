using System.Globalization;
using System.Windows.Data;

namespace GuideCraft.Converters;

/// <summary>bool → Grid 列索引（true → 1, false → 0；ConverterParameter=inverse 时取反）</summary>
public class BoolToColumnConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var b = value is true;
        if (parameter as string == "inverse") b = !b;
        return b ? 1 : 0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
