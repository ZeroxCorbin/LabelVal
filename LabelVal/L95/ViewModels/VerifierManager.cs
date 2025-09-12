using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.Main.ViewModels;
using System.Collections.ObjectModel;
using System.Linq;

namespace LabelVal.L95.ViewModels;
public partial class VerifierManager : ObservableRecipient
{
    public GlobalAppSettings AppSettings => GlobalAppSettings.Instance;

    public ObservableCollection<Verifier> Devices { get; } = [];

    [ObservableProperty][NotifyPropertyChangedRecipients] public Verifier selectedDevice;
    partial void OnSelectedDeviceChanged(Verifier value) => App.Settings.SetValue($"L95_IsSelected", value != null);

    public VerifierManager()
    {
        var sel = App.Settings.GetValue<Verifier>($"L95_Verifier");
        if (sel == null)
        {
            sel = new Verifier();
            App.Settings.SetValue($"L95_Verifier", sel);
        }
        sel.Manager = this;
        Devices.Add(sel);

        if(App.Settings.GetValue<bool>($"L95_IsSelected"))
            SelectedDevice = sel;

        WeakReferenceMessenger.Default.Register<RequestMessage<Verifier>>(
            this,
            (recipient, message) =>
            {
                message.Reply(SelectedDevice);
            });
    }

    //[RelayCommand] private void Add() { Devices.Add(new Verifier() { Manager = this }); Save(); }
    //[RelayCommand]
    //private void Delete(Verifier device)
    //{
    //    Devices.Remove(device);
    //    Save();
    //}
    [RelayCommand] private void Save() => App.Settings.SetValue($"L95_Verifier", Devices.FirstOrDefault());

}
