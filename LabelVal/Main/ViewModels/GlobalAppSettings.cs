namespace LabelVal.Main.ViewModels;

public class GlobalAppSettings
{
    public static GlobalAppSettings Instance { get; } = new();

    public bool ShowButtonText { get => App.Settings.GetValue("GlobalAppSettings_ShowButtonText", true, true); set => App.Settings.SetValue("GlobalAppSettings_ShowButtonText", value); }

    public bool ShowMainMenu { get => App.Settings.GetValue("GlobalAppSettings_ShowMainMenu", true, true); set => App.Settings.SetValue("GlobalAppSettings_ShowMainMenu", value); }

    public bool LaunchLvsOnConnect { get => App.Settings.GetValue("GlobalAppSettings_LaunchLvsOnConnect", false, true); set => App.Settings.SetValue("GlobalAppSettings_LaunchLvsOnConnect", value); }

    public bool IgnoreLvsNoResults { get => App.Settings.GetValue("GlobalAppSettings_IgnoreLvsNoResults", false, true); set => App.Settings.SetValue("GlobalAppSettings_IgnoreLvsNoResults", value); }
}
