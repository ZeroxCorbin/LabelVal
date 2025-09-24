using ControlzEx.Theming;
using MaterialDesignThemes.Wpf;
using System.Windows;
using System.Windows.Media;

namespace LabelVal.Theme;


public static class ThemeSupport
{
    public const string SystemSyncSentinel = "#SYSTEM#";

    // Keep Wpf.Ui in sync without redundant re-applies
    private static bool _themeChangedHooked;
    //private static ApplicationTheme? _lastUiTheme;
    private static Color? _lastUiAccent;

    // Resource keys affected by color-blind variants
    private static readonly string[] ColorBlindResourceKeys =
    {
        "ISO_GradeA","ISO_GradeB","ISO_GradeC","ISO_GradeD","ISO_GradeF",
        "SectorWarning","SectorError",
        "StatusGreen","StatusYellow","StatusRed",
        "V275","V5","L95","ImageRoll","Results","LabelBuilder",
        "AppPrimary","AppSecondary","AppAccent","AppInfo","AppNeutral"
    };

    public static IReadOnlyList<string> GetAvailableThemeNames()
    {
        var list = ThemeManager.Current.Themes
            .Where(t => t.BaseColorScheme is "Light" or "Dark")
            .Select(t => t.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();

        list.Insert(0, SystemSyncSentinel);
        return list;
    }

    /// <summary>
    /// Apply MahApps/ControlzEx theme or OS sync sentinel. Mirrors base+accent to Wpf.Ui and updates MaterialDesign.
    /// </summary>
    public static bool ApplyTheme(string themeName)
    {
        if (string.IsNullOrWhiteSpace(themeName))
            return false;

        EnsureControlzExHook();

        if (themeName == SystemSyncSentinel)
        {
            ThemeManager.Current.SyncTheme(ThemeSyncMode.SyncAll);
            App.Settings.SetValue("App.Theme", SystemSyncSentinel);

            var detected = ThemeManager.Current.DetectTheme();
            var isDark = detected?.BaseColorScheme == ThemeManager.BaseColorDark;
            var accent = detected?.PrimaryAccentColor ?? Colors.DodgerBlue;

            //UpdateWpfUiBase(isDark, accent);
            UpdateMaterialDesignTheme();
            return true;
        }

        var match = ThemeManager.Current.Themes
            .FirstOrDefault(t => t.Name.Equals(themeName, StringComparison.OrdinalIgnoreCase));

        if (match == null)
            return false;

        // ControlzEx drives the theme/accent
        ThemeManager.Current.ChangeTheme(Application.Current, match.Name);
        App.Settings.SetValue("App.Theme", match.Name);

        var isDarkMode = match.BaseColorScheme == ThemeManager.BaseColorDark
                         || match.Name.Contains("Dark", StringComparison.OrdinalIgnoreCase);

        // Mirror to Wpf.Ui using the effective accent
        var detectedAfter = ThemeManager.Current.DetectTheme();
        var accentAfter = detectedAfter?.PrimaryAccentColor ?? match.PrimaryAccentColor;

        //UpdateWpfUiBase(isDarkMode, accentAfter);
        UpdateMaterialDesignTheme();

        return true;
    }

    private static void EnsureControlzExHook()
    {
        if (_themeChangedHooked)
            return;

        _themeChangedHooked = true;

        // When MahApps/ControlzEx theme changes (including OS sync), mirror to Wpf.Ui + MaterialDesign
        ThemeManager.Current.ThemeChanged += (_, __) =>
        {
            var detected = ThemeManager.Current.DetectTheme();
            if (detected == null)
                return;

            var isDark = detected.BaseColorScheme == ThemeManager.BaseColorDark;
            //UpdateWpfUiBase(isDark, detected.PrimaryAccentColor);
            UpdateMaterialDesignTheme();
        };
    }

    // Align Wpf.Ui base theme and accent with the current MahApps/ControlzEx theme.
    //private static void UpdateWpfUiBase(bool isDark, Color accent)
    //{
    //    var targetTheme = isDark ? ApplicationTheme.Dark : ApplicationTheme.Light;
    //    if (_lastUiTheme != targetTheme)
    //    {
    //        ApplicationThemeManager.Apply(targetTheme);
    //        _lastUiTheme = targetTheme;
    //    }

    //    if (!_lastUiAccent.HasValue || _lastUiAccent.Value != accent)
    //    {
    //        ApplicationAccentColorManager.Apply(accent);
    //        _lastUiAccent = accent;
    //    }
    //}

    /// <summary>
    /// Applies the color-blind palette variant.
    /// </summary>
    public static void ApplyColorBlindTheme(ColorBlindnessType colorBlindnessType)
    {
        App.Settings.SetValue("App.ColorBlindnessType", colorBlindnessType);

        string suffix = colorBlindnessType switch
        {
            ColorBlindnessType.RedGreen => "_RG",
            ColorBlindnessType.BlueYellow => "_BY",
            ColorBlindnessType.Monochrome => "_M",
            _ => ""
        };

        var app = Application.Current;
        if (app == null) return;

        foreach (var key in ColorBlindResourceKeys)
        {
            var baseBrushObj = app.Resources[$"{key}{suffix}_Brush"];
            var baseColorObj = app.Resources[$"{key}{suffix}"];

            if (baseBrushObj is not Brush baseBrush || baseColorObj is not Color baseColor)
                continue;

            app.Resources[$"{key}_Brush_Active"] = baseBrush;
            app.Resources[$"{key}_Brush_Active50"] = new SolidColorBrush(Color.FromArgb(164, baseColor.R, baseColor.G, baseColor.B));
            app.Resources[$"{key}_Color_Active"] = baseColor;
            app.Resources[$"{key}_Color_Active50"] = Color.FromArgb(164, baseColor.R, baseColor.G, baseColor.B);
        }
    }

    /// <summary>
    /// Cycles to the next ColorBlindnessType, applies and returns it.
    /// </summary>
    public static ColorBlindnessType CycleColorBlindTheme()
    {
        var current = App.Settings.GetValue("App.ColorBlindnessType", ColorBlindnessType.None);
        var values = (ColorBlindnessType[])Enum.GetValues(typeof(ColorBlindnessType));
        var idx = Array.IndexOf(values, current);
        var next = values[(idx + 1) % values.Length];
        ApplyColorBlindTheme(next);
        return next;
    }

    private static void UpdateMaterialDesignTheme()
    {
        var detected = ThemeManager.Current.DetectTheme();
        if (detected == null)
            return;

        var paletteHelper = new PaletteHelper();
        var mdTheme = new MaterialDesignThemes.Wpf.Theme();
        mdTheme.SetPrimaryColor(detected.PrimaryAccentColor);
        mdTheme.SetBaseTheme(detected.BaseColorScheme == ThemeManager.BaseColorDark ? BaseTheme.Dark : BaseTheme.Light);
        paletteHelper.SetTheme(mdTheme);
    }
}