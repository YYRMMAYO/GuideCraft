using System.Windows;

namespace GuideCraft.Services;

/// <summary>主题切换管理器：运行时替换颜色字典，样式字典保持不动</summary>
public static class ThemeManager
{
    public const string Light = "Light";
    public const string Dark = "Dark";

    public static string Current { get; private set; } = Light;

    /// <summary>应用指定主题（Light / Dark），供深色浅色运行时切换</summary>
    public static void Apply(string theme)
    {
        if (theme != Dark) theme = Light;
        Current = theme;

        var merged = Application.Current.Resources.MergedDictionaries;

        // 只移除颜色字典（识别规则：Source 包含 "Colors."），保留 Styles.xaml 等其他字典
        var old = merged.FirstOrDefault(d =>
            d.Source != null && d.Source.OriginalString.Contains("Colors."));
        if (old != null) merged.Remove(old);

        var newDict = new ResourceDictionary
        {
            Source = new Uri($"pack://application:,,,/Themes/Colors.{theme}.xaml", UriKind.Absolute)
        };
        // Insert(0)：MergedDictionaries 后添加者优先查找，插入最前保证颜色资源生效且不覆盖样式字典
        merged.Insert(0, newDict);
    }
}
