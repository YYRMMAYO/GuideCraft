using System.Globalization;
using System.Windows.Data;

namespace GuideCraft.Converters;

/// <summary>将百分比（0-100，double）乘以目标宽度，用于进度条填充宽度</summary>
public class PercentToWidthConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length >= 2
            && values[0] is double percent
            && values[1] is double width
            && width > 0)
        {
            return Math.Max(0, Math.Min(width, width * percent / 100.0));
        }
        return 0.0;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
