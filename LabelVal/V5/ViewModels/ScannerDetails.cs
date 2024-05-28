using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Google.Protobuf.WellKnownTypes;
using LabelVal.Messages;

namespace LabelVal.V5.ViewModels;

public partial class ScannerDetails : ObservableRecipient, IRecipient<ScannerMessages.SelectedScannerChanged>
{
    [ObservableProperty] Scanner selectedScanner;
    public ScannerDetails() => IsActive = true;
    public void Receive(ScannerMessages.SelectedScannerChanged message)
    {
        if (SelectedScanner != null)
            SelectedScanner.PropertyChanged -= SelectedScanner_PropertyChanged;

        SelectedScanner = message.Value;

        if (SelectedScanner != null)
            SelectedScanner.PropertyChanged += SelectedScanner_PropertyChanged;
    }

    private void SelectedScanner_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {

        if (e.PropertyName == "SelectedCamera")
        {
            var tmp = SelectedScanner as Scanner;
            SelectedScanner = null;
            SelectedScanner = tmp;
        }

    }
}
