using System.Globalization;
using System.Windows.Data;

namespace GuideCraft.Converters;

/// <summary>值不为 null → true（用于"选中配置后按钮可用"等场景）</summary>
public class NotNullConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
