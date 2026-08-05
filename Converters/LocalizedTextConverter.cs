using System.Globalization;
using System.Windows.Data;
using GuideCraft.Localization;
using GuideCraft.Services;

namespace GuideCraft.Converters;

/// <summary>
/// 设置页枚举字符串 → 本地化友好文本。
/// ConverterParameter: language → 简体中文/English；theme → 浅色/深色；sidebar → 左侧/右侧
/// </summary>
public class LocalizedTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var s = value as string ?? string.Empty;
        return (parameter as string) switch
        {
            "language" => s == LocalizationManager.En ? "English" : "简体中文",
            "theme" => LocalizationManager.Get(s == ThemeManager.Dark ? "Str.SettingsThemeDark" : "Str.SettingsThemeLight"),
            "sidebar" => LocalizationManager.Get(s == "Left" ? "Str.SettingsSidebarLeft" : "Str.SettingsSidebarRight"),
            _ => s
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
