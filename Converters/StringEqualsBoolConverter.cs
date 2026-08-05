using System.Globalization;
using System.Windows.Data;

namespace GuideCraft.Converters;

/// <summary>
/// 字符串相等 → bool。用于 RadioButton.IsChecked 双向绑定：
/// Convert:   源值(SelectedString/Enum) == parameter → bool（控制选中态）
/// ConvertBack: IsChecked==true 时把 parameter 写回源（切换选中）；false 时返回
/// Binding.DoNothing，避免 RadioButton 组内"取消选中"的兄弟项把源值覆盖回旧值。
/// </summary>
public class StringEqualsBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value?.ToString() is { } s && parameter is string p && string.Equals(s, p, StringComparison.OrdinalIgnoreCase);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // 只有变为"选中"时才允许写回源；取消选中（false）不参与，防止互相覆盖
        if (value is not true || parameter is not string p)
            return Binding.DoNothing;

        // 目标属性是枚举（如 SettingsTab）时返回对应枚举值，避免依赖 WPF 隐式字符串转换
        if (targetType.IsEnum && Enum.TryParse(targetType, p, ignoreCase: true, out var parsed))
            return parsed;

        return p;
    }
}
