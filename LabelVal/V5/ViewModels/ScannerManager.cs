using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.Main.ViewModels;
using System.Collections.ObjectModel;

namespace LabelVal.V5.ViewModels;

public partial class ScannerManager : ObservableRecipient
{
    public GlobalAppSettings AppSettings => GlobalAppSettings.Instance;
    public ObservableCollection<Scanner> Devices { get; } = App.Settings.GetValue($"V5_{nameof(Devices)}", new ObservableCollection<Scanner>(), true);

    [ObservableProperty][NotifyPropertyChangedRecipients] private Scanner selectedDevice;
    partial void OnSelectedDeviceChanged(Scanner value) => App.Settings.SetValue($"V5_{nameof(SelectedDevice)}", value);

    public ScannerManager()
    {
        var sel = App.Settings.GetValue<Scanner>($"V5_{nameof(SelectedDevice)}");

        foreach (var dev in Devices)
        {
            dev.Manager = this;

            if (sel != null)
                if (dev.ID == sel.ID)
                    SelectedDevice = dev;
        }

        WeakReferenceMessenger.Default.Register<RequestMessage<Scanner>>(
            this,
            (recipient, message) =>
            {
                message.Reply(SelectedDevice);
            });
    }

    [RelayCommand] private void Add() { Devices.Add(new Scanner() { Manager = this }); Save(); }
    [RelayCommand]
    private void Delete(Scanner scanner)
    {
        Devices.Remove(scanner);
        Save();
    }
    [RelayCommand] private void Save() => App.Settings.SetValue($"V5_{nameof(Devices)}", Devices);
}
