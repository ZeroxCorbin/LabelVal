using BarcodeBuilder.lib.Wpf.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using LabelVal.Main.ViewModels;

namespace LabelVal.LabelBuilder.ViewModels;

public class LabelBuilderViewModel : ObservableObject
{
    public BarcodeBuilder.lib.Wpf.ViewModels.BarcodeBuilderViewModel BarcodeBuilderViewModel { get; } = new BarcodeBuilder.lib.Wpf.ViewModels.BarcodeBuilderViewModel(App.UserDataDirectory, App.Settings);


    public GlobalAppSettings AppSettings => GlobalAppSettings.Instance;
}
