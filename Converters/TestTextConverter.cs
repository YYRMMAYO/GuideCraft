using System.Globalization;
using System.Windows.Data;

namespace GuideCraft.Converters;

/// <summary>测试按钮文字：IsTesting → "测试中..." / "测试连接"</summary>
public class TestTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? "测试中..." : "测试连接";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
