using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using System.Collections.ObjectModel;

namespace LabelVal.LVS_95xx.ViewModels;
public partial class VerifierManager : ObservableRecipient
{
    public ObservableCollection<Verifier> Devices { get; } = App.Settings.GetValue($"L95xx_{nameof(Devices)}", new ObservableCollection<Verifier>(), true);

    [ObservableProperty][NotifyPropertyChangedRecipients] public Verifier selectedDevice;
    partial void OnSelectedDeviceChanged(Verifier value) => App.Settings.SetValue($"L95xx_{nameof(SelectedDevice)}", SelectedDevice);

    public VerifierManager()
    {
        Verifier sel = App.Settings.GetValue<Verifier>($"L95xx_{nameof(SelectedDevice)}");

        foreach (Verifier dev in Devices)
        {
            dev.Manager = this;

            if (sel != null)
                if (dev.ID == sel.ID)
                    SelectedDevice = dev;
        }

        WeakReferenceMessenger.Default.Register<RequestMessage<Verifier>>(
            this,
            (recipient, message) =>
            {
                message.Reply(SelectedDevice);
            });
    }

    [RelayCommand] private void Add() { Devices.Add(new Verifier() { Manager = this }); Save(); }
    [RelayCommand]
    private void Delete(Verifier device)
    {
        Devices.Remove(device);
        Save();
    }
    [RelayCommand] private void Save() => App.Settings.SetValue($"L95xx_{nameof(Devices)}", Devices);

}
