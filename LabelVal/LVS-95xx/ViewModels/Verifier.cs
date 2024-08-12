using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabelVal.LVS_95xx.Controllers;
using LabelVal.LVS_95xx.Models;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace LabelVal.LVS_95xx.ViewModels;

[JsonObject(MemberSerialization.OptIn)]
public partial class Verifier : ObservableRecipient
{
    public string ID => SelectedComName;

    private SerialPortController PortController = new();
    private L95xxDatabaseConnection DatabaseConnection = new();

    [JsonIgnore] public VerifierManager Manager { get; set; }

    [ObservableProperty] private bool isConnected;
    partial void OnIsConnectedChanged(bool value) => OnPropertyChanged(nameof(IsNotConnected));
    public bool IsNotConnected => !IsConnected;


    [ObservableProperty][NotifyPropertyChangedRecipients] private Models.FullReport readData;

    public ObservableCollection<string> ComNameList { get; } = [];

    [ObservableProperty][property: JsonProperty] private string selectedComName;

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
            {
                if (!DatabaseConnection.Connect())
                {
                    ClosePort();
                    return;
                }
                PostLogin();
                IsConnected = true;

            }
        }
        else
            ClosePort();
    }

    private void PostLogin()
    {
        var cur = DatabaseConnection.ReadSetting("Report", "ReportImageReduction");
        var table = DatabaseConnection.ReadSetting("GS1", "Table");
        //Update database setting to allow storing full resolution images to the report.
        if (cur != "1")
            DatabaseConnection.WriteSetting("Report", "ReportImageReduction", "1");
    }

    private void PortController_Exception(object sender, EventArgs e) => ClosePort();
    private void PortController_PacketAvailable(string packet)
    {
        var full = new FullReport();

        var reportID = GetReportID(packet);

        full.Report = DatabaseConnection.GetReport(reportID);
        full.ReportData = DatabaseConnection.GetReportData(reportID);
        full.Packet = packet;

        ReadData = full;
    }

    private string GetReportID(string packet)
    {
        var pt = packet.IndexOf("ReportID,");
        if (pt == -1)
            return null;

        return GetInt(packet.Substring(pt + 9));
    }

    [RelayCommand]
    private void ClosePort()
    {
        PortController.Disconnect();
        IsConnected = false;
        DatabaseConnection.Disconnect();
    }

    private static string GetInt(string value)
    {
        var digits = new string(value.Trim().TakeWhile(c =>
                                ("0123456789").Contains(c)
                                ).ToArray());

        return digits;
    }

}
