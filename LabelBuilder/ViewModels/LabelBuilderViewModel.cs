using CommunityToolkit.Mvvm.ComponentModel;

namespace LabelVal.LabelBuilder.ViewModels;

public class LabelBuilderViewModel : ObservableObject
{
    public BarcodeBuilder.lib.Wpf.ViewModels.BarcodeBuilderViewModel BarcodeBuilderViewModel { get; } = new BarcodeBuilder.lib.Wpf.ViewModels.BarcodeBuilderViewModel(App.Settings);

}
