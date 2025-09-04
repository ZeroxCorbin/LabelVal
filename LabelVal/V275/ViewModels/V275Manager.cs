using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Org.BouncyCastle.Asn1.GM;
using System.Collections.ObjectModel;

namespace LabelVal.V275.ViewModels;

public partial class V275Manager : ObservableRecipient
{
    public ObservableCollection<NodeManager> Devices { get; } = App.Settings.GetValue($"V275_{nameof(Devices)}", new ObservableCollection<NodeManager>(), true);

    [ObservableProperty][NotifyPropertyChangedRecipients] private Node selectedDevice;
    partial void OnSelectedDeviceChanged(Node value) { if (value != null) App.Settings.SetValue($"V275_{nameof(SelectedDevice)}", value); }

    public V275Manager()
    {
        //Node sel = App.Settings.GetValue<Node>($"V275_{nameof(SelectedDevice)}");

        foreach (var dev in Devices)
        {
            dev.Manager = this;
            dev.GetDevicesCommand.Execute(null);
        }

        WeakReferenceMessenger.Default.Register<RequestMessage<Node>>( this,
            (recipient, message) =>
            {
                message.Reply(SelectedDevice);
            });
    }

    [RelayCommand]
    private void Add()
    {
        var nm = new NodeManager { Manager = this };
        nm.GetDevicesCommand.Execute(null);
        Devices.Add(nm);
        Save();
    }
    [RelayCommand]
    private void Delete(NodeManager nodeMan)
    {
        Devices.Remove(nodeMan);
        Save();
    }
    [RelayCommand] private void Save() => App.Settings.SetValue($"V275_{nameof(Devices)}", Devices);

}
