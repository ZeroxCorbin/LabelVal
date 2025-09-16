using CommunityToolkit.Mvvm.ComponentModel;
using LabelVal.Main.ViewModels;

namespace LabelVal.LabelBuilder.ViewModels;

public class LabelBuilderViewModel : ObservableObject
{
    public BarcodeBuilder.lib.Wpf.ViewModels.BarcodeBuilderViewModel BarcodeBuilderViewModel { get; } = new BarcodeBuilder.lib.Wpf.ViewModels.BarcodeBuilderViewModel(App.Settings);
    public DisplayEditorViewModel DisplayEditorViewModel { get; } = new DisplayEditorViewModel();

    public GlobalAppSettings AppSettings => GlobalAppSettings.Instance;
}
