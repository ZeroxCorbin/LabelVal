using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelVal.LVS_95xx.ViewModels;
public partial class Verifier : ObservableRecipient
{
    [ObservableProperty] private double width;
    [ObservableProperty] private double height;
    [ObservableProperty] private string readData;
    public ObservableCollection<string> ComNameList { get; } = [];

    public string SelectedComName { get => App.Settings.GetValue("95xx_COM_Name", ""); set => App.Settings.SetValue("95xx_COM_Name", value); }

    public Sectors.ViewModels.Sectors StoredSectorControl { get; }

    [ObservableProperty] private Sectors.ViewModels.Sectors lvs95xxSectorControl = new();

    [ObservableProperty] private bool isConnected;
    partial void OnIsConnectedChanged(bool value) => OnPropertyChanged(nameof(IsNotConnected));
    public bool IsNotConnected => !IsConnected;

    private SerialPort PortController = new();

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

    private void PortController_SectorAvailable(object sector)
    {
        _ = App.Current.Dispatcher.BeginInvoke(new Action(() =>
        Lvs95xxSectorControl = new Sectors.ViewModels.Sectors(StoredSectorControl.Template, sector, false, true)));
    }

    private void PortController_PacketAvailable(string packet) => ReadData = packet;

    [RelayCommand]
    public void ClosePort()
    {
        PortController.Exception -= PortController_Exception;
        PortController.SectorAvailable -= PortController_SectorAvailable;
        PortController.PacketAvailable -= PortController_PacketAvailable;

        PortController.Stop();
        IsConnected = false;
    }
    [RelayCommand]
    public void WriteData()
    {
        //lVS95Xx_SerialPortSetup.Write(@"cd D:\OneDrive - OMRON\Omron\OCR\Applications\LabelVal\LabelVal\bin\Debug\LVS-95xx\com0com\x64" + "\r\n");
        //lVS95Xx_SerialPortSetup.Write(".\\setupc.exe");
        //lVS95Xx_SerialPortSetup.Write("\r\n");
    }
}
