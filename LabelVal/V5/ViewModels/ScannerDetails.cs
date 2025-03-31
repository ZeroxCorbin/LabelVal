using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace LabelVal.V5.ViewModels;

public partial class ScannerDetails : ObservableRecipient, IRecipient<PropertyChangedMessage<Scanner>>
{
    [ObservableProperty] Scanner selectedV5;
    public ScannerDetails() => IsActive = true;
    public void Receive(PropertyChangedMessage<Scanner> message)
    {
        //if (SelectedV5 != null)
        //    SelectedV5.PropertyChanged -= SelectedV5_PropertyChanged;

        SelectedV5 = message.NewValue;

        //if (SelectedV5 != null)
        //    SelectedV5.PropertyChanged += SelectedV5_PropertyChanged;
    }

    //private void SelectedV5_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    //{

    //    if (e.PropertyName == "SelectedCamera")
    //    {
    //        var tmp = SelectedV5 as Scanner;
    //        SelectedV5 = null;
    //        SelectedV5 = tmp;
    //    }

    //}
}
