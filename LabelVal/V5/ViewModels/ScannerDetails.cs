using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using LabelVal.Messages;

namespace LabelVal.V5.ViewModels;

public partial class ScannerDetails : ObservableRecipient, IRecipient<ScannerMessages.SelectedScannerChanged>
{
    [ObservableProperty] Scanner selectedScanner;
    public ScannerDetails() => IsActive = true;
    public void Receive(ScannerMessages.SelectedScannerChanged message) => SelectedScanner = message.Value;

}
