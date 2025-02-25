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

namespace LabelVal.LVS_95xx.ViewModels;

[JsonObject(MemberSerialization.OptIn)]
public partial class Verifier : ObservableRecipient, IRecipient<RegistryMessage>, IRecipient<Win32_ProcessWatcherMessage>
{
    [JsonProperty] public long ID { get; set; } = DateTime.Now.Ticks;

    public VerifierManager Manager { get; set; }
    public Controller Controller { get; } = new();

    public ObservableCollection<string> AvailablePorts { get; } = [];

    [ObservableProperty][property: JsonProperty] private string selectedComName;
    [ObservableProperty][property: JsonProperty] private string selectedComBaudRate = "9600";
    [ObservableProperty] private string databasePath = @"C:\Users\Public\LVS-95XX\LVS-95XX.mdb";
    [ObservableProperty] private string passwordOfTheDay;

    [ObservableProperty] private System.Diagnostics.Process process;
    [ObservableProperty] private Win32_ProcessWatcherProcessState processState;

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
        RequestMessage<PasswordOfTheDayMessage> ret2 = WeakReferenceMessenger.Default.Send(new RequestMessage<PasswordOfTheDayMessage>());
        if (ret2.HasReceivedResponse)
            PasswordOfTheDay = ret2.Response.Value;

        RequestMessage<RegistryMessage> ret3 = WeakReferenceMessenger.Default.Send(new RequestMessage<RegistryMessage>());
        if (ret3.HasReceivedResponse)
            DatabasePath = ExtractDatabasePath(ret3.Response.RegistryValue);

        RequestMessage<Win32_ProcessWatcherMessage> ret4 = WeakReferenceMessenger.Default.Send(new RequestMessage<Win32_ProcessWatcherMessage>());
        if (ret4.HasReceivedResponse)
        {
            Process = ret4.Response.Process;
            ProcessState = ret4.Response.State;
        }
    }

    private string ExtractDatabasePath(string registry) => !string.IsNullOrWhiteSpace(registry) ? registry[(registry.IndexOf("Data Source=") + "Data Source=".Length)..].Trim('\"') : DatabasePath;

    [RelayCommand]
    private void Connect()
    {
        if (Controller.SerialPort.IsListening)
            Controller.Disconnect();
        else
            _ = Controller.Connect(SelectedComName, DatabasePath);
    }

    [RelayCommand]
    private void RefreshComList()
    {
        string[] names = System.IO.Ports.SerialPort.GetPortNames();
        foreach (string name in names)
        {
            if (!AvailablePorts.Contains(name))
                AvailablePorts.Add(name);
        }
        System.Collections.Generic.List<string> toRemove = AvailablePorts.Where(name => !names.Contains(name)).ToList();
        foreach (string name in toRemove)
            _ = AvailablePorts.Remove(name);
    }

    private void PostLogin()
    {
        string cur = Controller.Database.ReadSetting("Report", "ReportImageReduction");
        _ = Controller.Database.ReadSetting("GS1", "Table");
        //Update database setting to allow storing full resolution images to the report.
        if (cur != "1")
            Controller.Database.WriteSetting("Report", "ReportImageReduction", "1");
    }

    public void Receive(RegistryMessage message) => DatabasePath = ExtractDatabasePath(message.RegistryValue);

    public void Receive(Win32_ProcessWatcherMessage message)
    {
        if (message.AppName != "LVS-95XX.exe")
            return;

        Process = message.Process;
        ProcessState = message.State;

    }
}
