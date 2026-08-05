using System.Globalization;
using System.Windows.Data;
using GuideCraft.Localization;

namespace GuideCraft.Converters;

/// <summary>按钮文字转换：IsBusy → 进行中文案 / 空闲文案。
/// ConverterParameter：test → 测试连接；update → 检查更新</summary>
public class TestTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var kind = parameter as string ?? "test";
        if (value is true)
            return kind == "update"
                ? LocalizationManager.Get("Str.SettingsCheckingUpdate")
                : LocalizationManager.Get("Str.SettingsTesting");
        return kind == "update"
            ? LocalizationManager.Get("Str.SettingsCheckUpdate")
            : LocalizationManager.Get("Str.SettingsTestConnection");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
