using System.Globalization;
using System.Windows.Data;
using GuideCraft.Services;
using GuideCraft.ViewModels;

namespace GuideCraft.Converters;

/// <summary>LlmProvider → 本地化显示名</summary>
public class ProviderNameConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is LlmProvider p ? SettingsViewModel.ProviderName(p) : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
