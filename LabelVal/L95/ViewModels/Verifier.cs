using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Lvs95xx.lib.Core.Controllers;
using Lvs95xx.lib.Shared.Watchers;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Watchers.lib.Process;

namespace LabelVal.L95.ViewModels;

[JsonObject(MemberSerialization.OptIn)]
public partial class Verifier : ObservableRecipient, IRecipient<RegistryMessage>
{
    [JsonProperty] public long ID { get; set; } = DateTime.Now.Ticks;

    public VerifierManager Manager { get; set; }
    public Controller Controller { get; } = new();

    public ObservableCollection<string> AvailablePorts { get; } = [];

    [ObservableProperty][property: JsonProperty] private string selectedComName;
    [ObservableProperty][property: JsonProperty] private string selectedComBaudRate = "9600";
    [ObservableProperty] private string databasePath = string.Empty;
    [ObservableProperty] private string passwordOfTheDay;


    partial void OnDatabasePathChanged(string value)
    {
        //if(string.IsNullOrEmpty(value))
        //    App.Current.Dispatcher.BeginInvoke(() => DatabasePath = @"C:\Users\Public\LVS-95XX\LVS-95XX.mdb");
    }

    public Verifier()
    {
        RequestMessages();
        IsActive = true;
    }
    private void RequestMessages()
    {
        var ret2 = WeakReferenceMessenger.Default.Send(new RequestMessage<PasswordOfTheDayMessage>());
        if (ret2.HasReceivedResponse)
            PasswordOfTheDay = ret2.Response.Value;

        var ret3 = WeakReferenceMessenger.Default.Send(new RequestMessage<RegistryMessage>());
        if (ret3.HasReceivedResponse)
            DatabasePath = ExtractDatabasePath(ret3.Response.RegistryValue);
    }

    private string ExtractDatabasePath(string registry) => !string.IsNullOrWhiteSpace(registry) ? registry[(registry.IndexOf("Data Source=") + "Data Source=".Length)..].Trim('\"') : DatabasePath;

    [RelayCommand]
    private void Connect()
    {
        if (Controller.IsConnected)
            Controller.Disconnect();
        else
            _ = Controller.Connect(DatabasePath);
    }

    [RelayCommand]
    private void RefreshComList()
    {
        var names = System.IO.Ports.SerialPort.GetPortNames();
        foreach (var name in names)
        {
            if (!AvailablePorts.Contains(name))
                AvailablePorts.Add(name);
        }
        var toRemove = AvailablePorts.Where(name => !names.Contains(name)).ToList();
        foreach (var name in toRemove)
            _ = AvailablePorts.Remove(name);
    }

    private void PostLogin()
    {
        var cur = Controller.Database.GetSetting("Report", "ReportImageReduction");
        _ = Controller.Database.GetSetting("GS1", "Table");
        //Update database setting to allow storing full resolution images to the report.
        if (cur != "1")
            Controller.Database.SetSetting("Report", "ReportImageReduction", "1");
    }

    public void Receive(RegistryMessage message)
    {
        Controller.Disconnect();
        DatabasePath = ExtractDatabasePath(message.RegistryValue);
    }

}
