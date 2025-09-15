using CommunityToolkit.Mvvm.ComponentModel;

namespace LabelVal.Main.ViewModels;

public partial class GlobalAppSettings : ObservableObject
{
    public static GlobalAppSettings Instance { get; } = new();

    public bool ShowButtonText { get => App.Settings.GetValue("GlobalAppSettings_ShowButtonText", true, true); set { App.Settings.SetValue("GlobalAppSettings_ShowButtonText", value); OnPropertyChanged(nameof(ShowButtonText)); } }

    public bool ShowMainMenu { get => App.Settings.GetValue("GlobalAppSettings_ShowMainMenu", true, true); set { App.Settings.SetValue("GlobalAppSettings_ShowMainMenu", value); OnPropertyChanged(nameof(ShowMainMenu)); } }

    public bool LvsLaunchOnConnect { get => App.Settings.GetValue("GlobalAppSettings_LaunchLvsOnConnect", false, true); set { App.Settings.SetValue("GlobalAppSettings_LaunchLvsOnConnect", value); OnPropertyChanged(nameof(LvsLaunchOnConnect)); } }

    public bool LvsIgnoreNoResults { get => App.Settings.GetValue("GlobalAppSettings_LvsIgnoreNoResults", false, true); set { App.Settings.SetValue("GlobalAppSettings_LvsIgnoreNoResults", value); OnPropertyChanged(nameof(LvsIgnoreNoResults)); } }

    public bool V275AutoRefreshServers { get => App.Settings.GetValue("GlobalAppSettings_V275AutoRefreshServers", true, true); set { App.Settings.SetValue("GlobalAppSettings_V275AutoRefreshServers", value); OnPropertyChanged(nameof(V275AutoRefreshServers)); } }

    public bool PreseveImageFormat { get => App.Settings.GetValue("GlobalAppSettings_PreserveImageFormat", false, true); set { App.Settings.SetValue("GlobalAppSettings_PreserveImageFormat", value); OnPropertyChanged(nameof(PreseveImageFormat)); } }
}