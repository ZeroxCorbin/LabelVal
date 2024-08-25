using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using L95xx_Lib.Controllers;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace LabelVal.LVS_95xx.ViewModels;

[JsonObject(MemberSerialization.OptIn)]
public partial class Verifier : ObservableRecipient
{   
    [JsonProperty] public long ID { get; set; } = DateTime.Now.Ticks;

    public VerifierManager Manager { get; set; }
    public Controller Controller { get; } = new();

    public ObservableCollection<string> AvailablePorts { get; } = [];

    [ObservableProperty][property: JsonProperty] private string selectedComName;
    [ObservableProperty][property: JsonProperty] private string selectedComBaudRate = "9600";
    [ObservableProperty][property: JsonProperty] private string databasePath = @"C:\Users\Public\LVS-95XX\LVS-95XX.mdb";

    [RelayCommand]
    private void Connect()
    {
        if(Controller.SerialPort.IsListening)
            Controller.Disconnect();
        else
            Controller.Connect(SelectedComName, DatabasePath);
    }

    [RelayCommand]
    private void RefreshComList()
    {
        AvailablePorts.Clear();

        foreach (var name in System.IO.Ports.SerialPort.GetPortNames())
            AvailablePorts.Add(name);
    }

    private void PostLogin()
    {
        var cur = Controller.Database.ReadSetting("Report", "ReportImageReduction");
        var table = Controller.Database.ReadSetting("GS1", "Table");
        //Update database setting to allow storing full resolution images to the report.
        if (cur != "1")
            Controller.Database.WriteSetting("Report", "ReportImageReduction", "1");
    }



}
