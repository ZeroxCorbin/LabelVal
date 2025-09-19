using ControlzEx.Theming;
using MaterialDesignThemes.Wpf;
using System.Windows;
using System.Windows.Media;

namespace LabelVal.Theme;

public static class ThemeSupport
{
    public const string SystemSyncSentinel = "#SYSTEM#";
    private static bool _themeChangedHooked;

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
            .Select(t => t.Name) // Name = Base.Color
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();

        list.Insert(0, SystemSyncSentinel);
        return list;
    }

    /// <summary>
    /// Apply MahApps/ControlzEx theme or OS sync sentinel. Also updates MaterialDesign theme.
    /// </summary>
    public static bool ApplyTheme(string themeName)
    {
        if (string.IsNullOrWhiteSpace(themeName))
            return false;

        if (themeName == SystemSyncSentinel)
        {
            ThemeManager.Current.SyncTheme(ThemeSyncMode.SyncAll);
            App.Settings.SetValue("App.Theme", SystemSyncSentinel);
            UpdateMaterialDesignTheme();
            return true;
        }

        var match = ThemeManager.Current.Themes
            .FirstOrDefault(t => t.Name.Equals(themeName, StringComparison.OrdinalIgnoreCase));

        if (match == null)
            return false;

        ThemeManager.Current.ChangeTheme(Application.Current, match.Name);
        App.Settings.SetValue("App.Theme", match.Name);
        UpdateMaterialDesignTheme();
        return true;
    }

    /// <summary>
    /// Attach a single ThemeChanged handler to persist theme & keep MaterialDesign synchronized.
    /// </summary>
    public static void RegisterThemeChangedHandler()
    {
        if (_themeChangedHooked)
            return;

        ThemeManager.Current.ThemeChanged += (_, e) =>
        {
            // Preserve sentinel if user chose system sync
            if (App.Settings.GetValue("App.Theme", SystemSyncSentinel, true) != SystemSyncSentinel)
                App.Settings.SetValue("App.Theme", e.NewTheme.Name);

            UpdateMaterialDesignTheme();
        };
        _themeChangedHooked = true;
    }

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
                continue; // Skip if resources missing (defensive)

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

        if (detected.BaseColorScheme == ThemeManager.BaseColorDark)
            mdTheme.SetBaseTheme(BaseTheme.Dark);
        else
            mdTheme.SetBaseTheme(BaseTheme.Light);

        paletteHelper.SetTheme(mdTheme);
    }
}