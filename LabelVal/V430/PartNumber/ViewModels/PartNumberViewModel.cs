using CommunityToolkit.Mvvm.ComponentModel;

namespace LabelVal.V430.PartNumber.ViewModels;

public partial class PartNumberViewModel : ObservableObject
{
    public PartNumberViewModel()
    {
        MicroHawk = new V430_REST_Lib.PartNumbers.MicroHawk();
        MicroHawk.PropertyChanged += MicroHawk_PropertyChanged;
    }

    [ObservableProperty] private string? cameraPartNumber;
    partial void OnCameraPartNumberChanged(string? value)
    {
        MicroHawk = V430_REST_Lib.PartNumbers.SmartPartNumbers.GetMicroHawk(value);
        MicroHawk.PropertyChanged += MicroHawk_PropertyChanged;
        
    }

    private void MicroHawk_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        CameraPartNumber = V430_REST_Lib.PartNumbers.SmartPartNumbers.GetPartNumber(MicroHawk);

    }

    [ObservableProperty] private V430_REST_Lib.PartNumbers.MicroHawk? microHawk;
}
