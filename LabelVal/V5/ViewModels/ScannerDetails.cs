using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace LabelVal.V5.ViewModels;

public partial class ScannerDetails : ObservableRecipient, IRecipient<PropertyChangedMessage<Scanner>>
{
    [ObservableProperty] Scanner selectedScanner;
    public ScannerDetails() => IsActive = true;
    public void Receive(PropertyChangedMessage<Scanner> message)
    {
        //if (SelectedScanner != null)
        //    SelectedScanner.PropertyChanged -= SelectedScanner_PropertyChanged;

        SelectedScanner = message.NewValue;

        //if (SelectedScanner != null)
        //    SelectedScanner.PropertyChanged += SelectedScanner_PropertyChanged;
    }

    //private void SelectedScanner_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    //{

    //    if (e.PropertyName == "SelectedCamera")
    //    {
    //        var tmp = SelectedScanner as Scanner;
    //        SelectedScanner = null;
    //        SelectedScanner = tmp;
    //    }

    //}
}
