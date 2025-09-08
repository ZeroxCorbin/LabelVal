using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.Main.ViewModels;
using Lvs95xx.lib.Core.Controllers;
using Lvs95xx.lib.Shared.Watchers;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using Watchers.lib.Process;

namespace LabelVal.L95.ViewModels;

/// <summary>
/// ViewModel for managing an LVS-95XX verifier, its connection, and related settings.
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
public partial class Verifier : ObservableRecipient, IRecipient<RegistryMessage>
{
    #region Properties

    public GlobalAppSettings AppSettings { get; } = GlobalAppSettings.Instance;

    /// <summary>
    /// Gets or sets the unique identifier for the verifier instance.
    /// </summary>
    [JsonProperty] public long ID { get; set; } = DateTime.Now.Ticks;

    /// <summary>
    /// Gets or sets the manager associated with this verifier.
    /// </summary>
    public VerifierManager Manager { get; set; }

    /// <summary>
    /// Gets the core controller for interacting with the LVS-95XX device.
    /// </summary>
    public Controller Controller { get; } = new();

    /// <summary>
    /// Gets the collection of available COM ports for connection.
    /// </summary>
    public ObservableCollection<string> AvailablePorts { get; } = [];

    /// <summary>
    /// Gets or sets the name of the selected COM port.
    /// </summary>
    [ObservableProperty][property: JsonProperty] private string selectedComName;

    /// <summary>
    /// Gets or sets the baud rate for the selected COM port.
    /// </summary>
    [ObservableProperty][property: JsonProperty] private string selectedComBaudRate = "9600";

    /// <summary>
    /// Gets or sets the file path to the LVS-95XX database.
    /// </summary>
    [ObservableProperty] private string databasePath = string.Empty;

    /// <summary>
    /// Gets or sets the password of the day for LVS credentials.
    /// </summary>
    [ObservableProperty] private string passwordOfTheDay;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="Verifier"/> class.
    /// </summary>
    public Verifier()
    {
        RequestMessages();
        IsActive = true;
    }

    #endregion

    #region Commands

    /// <summary>
    /// Connects to or disconnects from the verifier controller.
    /// </summary>
    [RelayCommand]
    private void Connect()
    {
        if (Controller.IsConnected)
            Controller.Disconnect();
        else
        {
            _ = Controller.Connect(DatabasePath);
            if (Controller.ProcessState == Win32_ProcessWatcherProcessState.Exited && AppSettings.LvsLaunchOnConnect)
                LaunchLvs();
        }
    }

    private void LaunchLvs()
    {
        if (Controller.Process == null || Controller.Process.HasExited)
        {
            if (Controller.ProcessState == Win32_ProcessWatcherProcessState.Exited)
            {
                //Check if (the exe esists at C:\Program Files (x86)\Microscan\LVS-95XX\LVS-95XX.exe
                //Get the Program Files from the environment variables
                var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                var path = System.IO.Path.Combine(programFiles, "Microscan", "LVS-95XX", "LVS-95XX.exe");

                if (System.IO.File.Exists(path))
                {
                    //Launch the application
                    System.Diagnostics.ProcessStartInfo startInfo = new()
                    {
                        FileName = path,
                        WorkingDirectory = System.IO.Path.GetDirectoryName(path)
                    };
                    _ = System.Diagnostics.Process.Start(startInfo);

                }
            }
        }
    }

    /// <summary>
    /// Refreshes the list of available COM ports.
    /// </summary>
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

    /// <summary>
    /// Sends the standard LVS user credentials to the running LVS-95XX process.
    /// </summary>
    [RelayCommand]
    private void EnterLvsCredentials()
    {
        if (Controller.Process != null)
        {
            WinAPI.lib.WinAPI.SendString($"lvs\t{Lvs95xx.lib.Core.Controllers.Controller.GetTodaysPassword()}\n", Controller.Process);
            // WinAPI.SetFocus(_process);
        }
    }

    /// <summary>
    /// Sends administrator credentials to the running LVS-95XX process.
    /// </summary>
    [RelayCommand]
    private void EnterAdminCredentials()
    {
        if (Controller.Process != null)
        {
            WinAPI.lib.WinAPI.SendString($"admin\tadmin\n", Controller.Process);
            // WinAPI.SetFocus(_process);
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Receives a <see cref="RegistryMessage"/> to update the database path.
    /// </summary>
    /// <param name="message">The message containing the new registry value.</param>
    public void Receive(RegistryMessage message)
    {
        Controller.Disconnect();
        DatabasePath = ExtractDatabasePath(message.RegistryValue);
    }

    #endregion

    #region Private Methods

    partial void OnDatabasePathChanged(string value)
    {
        //if(string.IsNullOrEmpty(value))
        //    App.Current.Dispatcher.BeginInvoke(() => DatabasePath = @"C:\Users\Public\LVS-95XX\LVS-95XX.mdb");
    }

    /// <summary>
    /// Requests initial state information, such as passwords and registry settings, via messaging.
    /// </summary>
    private void RequestMessages()
    {
        RequestMessage<PasswordOfTheDayMessage> ret2 = WeakReferenceMessenger.Default.Send(new RequestMessage<PasswordOfTheDayMessage>());
        if (ret2.HasReceivedResponse)
            PasswordOfTheDay = ret2.Response.Value;

        RequestMessage<RegistryMessage> ret3 = WeakReferenceMessenger.Default.Send(new RequestMessage<RegistryMessage>());
        if (ret3.HasReceivedResponse)
            DatabasePath = ExtractDatabasePath(ret3.Response.RegistryValue);
    }

    /// <summary>
    /// Extracts the database path from a full registry connection string.
    /// </summary>
    /// <param name="registry">The registry value containing the connection string.</param>
    /// <returns>The extracted database path.</returns>
    private string ExtractDatabasePath(string registry) => !string.IsNullOrWhiteSpace(registry) ? registry[(registry.IndexOf("Data Source=") + "Data Source=".Length)..].Trim('\"') : DatabasePath;

    /// <summary>
    /// Performs post-login configuration, such as updating database settings.
    /// </summary>
    private void PostLogin()
    {
        var cur = Controller.Database.GetSetting("Report", "ReportImageReduction");
        _ = Controller.Database.GetSetting("GS1", "Table");
        //Update database setting to allow storing full resolution images to the report.
        if (cur != "1")
            Controller.Database.SetSetting("Report", "ReportImageReduction", "1");
    }

    #endregion
}