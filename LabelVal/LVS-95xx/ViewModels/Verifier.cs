using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using LabelVal.Messages;
using LabelVal.Sectors.ViewModels;
using System;
using System.Collections.ObjectModel;

namespace LabelVal.LVS_95xx.ViewModels;
public partial class Verifier : ObservableRecipient
{  
    private SerialPort PortController = new();


    [ObservableProperty] private bool isConnected;
    partial void OnIsConnectedChanged(bool value) => OnPropertyChanged(nameof(IsNotConnected));
    public bool IsNotConnected => !IsConnected;


    [ObservableProperty] private string readData;
    public ObservableCollection<string> ComNameList { get; } = [];

    public string SelectedComName { get => App.Settings.GetValue("95xx_COM_Name", ""); set => App.Settings.SetValue("95xx_COM_Name", value); }

    public Verifier() => LoadComList();

    private void LoadComList()
    {
        ComNameList.Clear();
        PortController.Controller.GetCOMList();

        foreach (var com in PortController.Controller.COMPortsAvailable)
            ComNameList.Add(com);
    }

    [RelayCommand]
    private void Connect()
    {
        if (!string.IsNullOrEmpty(SelectedComName) && !PortController.IsConnected)
        {
            _ = PortController.Init(SelectedComName);

            PortController.SectorAvailable -= PortController_SectorAvailable;
            PortController.SectorAvailable += PortController_SectorAvailable;

            PortController.PacketAvailable -= PortController_PacketAvailable;
            PortController.PacketAvailable += PortController_PacketAvailable;

            PortController.Exception -= PortController_Exception;
            PortController.Exception += PortController_Exception;

            if (PortController.Start())
                IsConnected = true;
        }
        else
            ClosePort();
    }

    private void PortController_Exception(object sender, EventArgs e) => ClosePort();
    private void PortController_SectorAvailable(object sector) => WeakReferenceMessenger.Default.Send(new VerifierMessages.NewPacket(sector));
    private void PortController_PacketAvailable(string packet) => ReadData = packet;

    [RelayCommand]
    private void ClosePort()
    {
        PortController.Exception -= PortController_Exception;
        PortController.SectorAvailable -= PortController_SectorAvailable;
        PortController.PacketAvailable -= PortController_PacketAvailable;

        PortController.Stop();
        IsConnected = false;
    }

}
