using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using System.Collections.ObjectModel;
using System.Linq;

namespace LabelVal.LVS_95xx.ViewModels;
public partial class VerifierManager : ObservableRecipient
{
    public ObservableCollection<Verifier> Devices { get; } = [];

    [ObservableProperty][NotifyPropertyChangedRecipients] public Verifier selectedDevice;
    partial void OnSelectedDeviceChanged(Verifier value) => App.Settings.SetValue($"L95xx_IsSelected", value != null);

    public VerifierManager()
    {
        Verifier sel = App.Settings.GetValue<Verifier>($"L95xx_Verifier");
        if (sel == null)
        {
            sel = new Verifier();
            App.Settings.SetValue($"L95xx_Verifier", sel);
        }
        sel.Manager = this;
        Devices.Add(sel);

        if(App.Settings.GetValue<bool>($"L95xx_IsSelected"))
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
    [RelayCommand] private void Save() => App.Settings.SetValue($"L95xx_Verifier", Devices.FirstOrDefault());

}
