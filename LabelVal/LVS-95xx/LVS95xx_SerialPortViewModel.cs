using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabelVal.V275.ViewModels;
using System;
using System.Collections.ObjectModel;

namespace LabelVal.LVS_95xx;

public partial class LVS95xx_SerialPortViewModel : ObservableObject
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

    private LVS95xx_SerialPort PortController = new();

    public LVS95xx_SerialPortViewModel(object sectorControl)
    {
        StoredSectorControl = (Sectors.ViewModels.Sectors)sectorControl;

        //lVS95Xx_SerialPortSetup.DataAvailable += LVS95Xx_SerialPortSetup_DataAvailable;
        //Task.Run(() => lVS95Xx_SerialPortSetup.StartCancellableProcess("cmd.exe", "", new System.Threading.CancellationToken()));

        LoadComList();
    }

    private void LoadComList()
    {
        ComNameList.Clear();
        PortController.Controller.GetCOMList();

        foreach (var com in PortController.Controller.COMPortsAvailable)
            ComNameList.Add(com);
    }

    [RelayCommand]
    public void OpenPort()
    {
        if (!string.IsNullOrEmpty(SelectedComName))
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
