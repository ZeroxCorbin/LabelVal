using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using LabelVal.Messages;
using LabelVal.Sectors.ViewModels;
using System;
using System.Collections.ObjectModel;

namespace LabelVal.LVS_95xx.ViewModels;

public class VerifierPacket(string value)
{
    public string Value { get; set; } = value;
}

public partial class Verifier : ObservableRecipient
{  
    private SerialPortController PortController = new();


    [ObservableProperty] private bool isConnected;
    partial void OnIsConnectedChanged(bool value) => OnPropertyChanged(nameof(IsNotConnected));
    public bool IsNotConnected => !IsConnected;


    [ObservableProperty][NotifyPropertyChangedRecipients] private VerifierPacket readData;

    public ObservableCollection<string> ComNameList { get; } = [];

    public string SelectedComName { get => App.Settings.GetValue("95xx_COM_Name", ""); set => App.Settings.SetValue("95xx_COM_Name", value); }

    public Verifier()
    {
        PortController.PacketAvailable -= PortController_PacketAvailable;
        PortController.PacketAvailable += PortController_PacketAvailable;

        PortController.Exception -= PortController_Exception;
        PortController.Exception += PortController_Exception;

        LoadComList();
    }

    private void LoadComList()
    {
        ComNameList.Clear();
        PortController.GetCOMList();

        foreach (var com in PortController.COMPortsAvailable)
            ComNameList.Add(com);
    }

    [RelayCommand]
    private void Connect()
    {
        if (!string.IsNullOrEmpty(SelectedComName) && !IsConnected)
        {
            PortController.Init(SelectedComName);

            if (PortController.Connect())
                IsConnected = true;
        }
        else
            ClosePort();
    }

    private void PortController_Exception(object sender, EventArgs e) => ClosePort();
    private void PortController_PacketAvailable(string packet) => ReadData = new VerifierPacket(packet);

    [RelayCommand]
    private void ClosePort()
    {
        PortController.Disconnect();
        IsConnected = false;
    }

}
