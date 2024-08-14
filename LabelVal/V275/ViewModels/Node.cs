using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.ImageRolls.ViewModels;
using MahApps.Metro.Controls.Dialogs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using V275_REST_lib.Models;
using V275_REST_Lib.Models;

namespace LabelVal.V275.ViewModels;

public enum NodeStates
{
    Offline,
    Idle,
    Editing,
    Running,
    Paused,
    Disconnected
}

[JsonObject(MemberSerialization.OptIn)]
public partial class Node : ObservableRecipient, IRecipient<PropertyChangedMessage<ImageRollEntry>>
{
    public V275_REST_lib.Controller Controller { get; }

    [JsonProperty] private string Host => App.Settings.GetValue<string>($"{NodeManager.ClassName}{nameof(NodeManager.Host)}");
    [JsonProperty] private uint SystemPort => App.Settings.GetValue<uint>($"{NodeManager.ClassName}{nameof(NodeManager.SystemPort)}");

    [JsonProperty] public uint ID { get; set; }

    [JsonProperty] private static string UserName => App.Settings.GetValue<string>($"{NodeManager.ClassName}{nameof(NodeManager.UserName)}");
    private static string Password => App.Settings.GetValue<string>($"{NodeManager.ClassName}{nameof(NodeManager.Password)}");

    [ObservableProperty] private bool loginMonitor;

    private Events_System.Data LoginData { get; } = new Events_System.Data();

    public Devices.Node Details { get; set; }
    public Devices.Camera Camera { get; set; }
    public Inspection Inspection { get; set; }

    [ObservableProperty] private Jobs jobs;

    [ObservableProperty] private Product product;
    [ObservableProperty] private bool isOldISO;

    [ObservableProperty] private Configuration_Camera configurationCamera;
    [ObservableProperty] private List<Symbologies.Symbol> symbologies;
    [ObservableProperty] private Calibration calibration;
    [ObservableProperty] private Simulation simulation;
    [ObservableProperty] private Print print;

    public bool IsSimulator => Inspection != null && Inspection.device.Equals("simulator");
    private static string SimulatorImageDirectory => App.Settings.GetValue<string>($"{NodeManager.ClassName}{nameof(SimulatorImageDirectory)}");

    [ObservableProperty] private NodeStates state = NodeStates.Offline;

    [ObservableProperty] private int dpi = 600;
    partial void OnDpiChanged(int value) => Is600Dpi = value == 600;
    [ObservableProperty] private bool is600Dpi = true;

    [ObservableProperty] private string jobName = "";
    partial void OnJobNameChanged(string value)
    {
        if (Jobs == null)
        {
            SelectedJob = null;
            return;
        }

        var jb = Jobs.jobs.FirstOrDefault((e) => e.name == JobName);

        if (jb != null)
        {
            if (SelectedJob != jb)
            {
                userChange = true;
                SelectedJob = jb;

            }
        }
    }
    private bool userChange;


    [ObservableProperty] private Jobs.Job selectedJob;
    partial void OnSelectedJobChanged(Jobs.Job value)
    {
        if (value == null)
        {
            userChange = false;
            return;
        }

        if (userChange)
        {
            userChange = false;
            return;
        }

        App.Current.Dispatcher.BeginInvoke(() => ChangeJob(value.name));
    }
    private async Task ChangeJob(string name)
    {
        if (await Controller.Commands.UnloadJob())
            if (await Controller.Commands.LoadJob(name))
            {

            }
    }
    public bool IsBackupVoid => ConfigurationCamera != null && ConfigurationCamera.backupVoidMode.value == "ON";

    [ObservableProperty] private bool isLoggedIn_Monitor = false;
    partial void OnIsLoggedIn_MonitorChanged(bool value) { OnPropertyChanged(nameof(IsLoggedIn)); OnPropertyChanged(nameof(IsNotLoggedIn)); }

    [ObservableProperty] private bool isLoggedIn_Control = false;
    public bool IsNotLoggedIn_Control => !IsLoggedIn_Control;
    partial void OnIsLoggedIn_ControlChanged(bool value) { OnPropertyChanged(nameof(IsLoggedIn)); OnPropertyChanged(nameof(IsNotLoggedIn)); OnPropertyChanged(nameof(IsNotLoggedIn_Control)); }
    public bool IsLoggedIn => IsLoggedIn_Monitor || IsLoggedIn_Control;
    public bool IsNotLoggedIn => !(IsLoggedIn_Monitor || IsLoggedIn_Control);

    [ObservableProperty] private ImageRollEntry selectedImageRoll;
    partial void OnSelectedImageRollChanged(ImageRollEntry value) => CheckTemplateName();

    [ObservableProperty] private bool isWrongTemplateName = false;

    public Node(string host, uint systemPort, uint nodeNumber, ImageRollEntry imageRollEntry)
    {
        SelectedImageRoll = imageRollEntry;

        ID = nodeNumber;
        Controller = new V275_REST_lib.Controller(host, systemPort, nodeNumber);

        Controller.WebSocket.SessionStateChange += WebSocket_SessionStateChange;
        Controller.StateChanged += V275_StateChanged;

        App.Settings.PropertyChanged -= Settings_PropertyChanged;
        App.Settings.PropertyChanged += Settings_PropertyChanged;

        IsActive = true;
    }

    ~Node()
    {
        App.Settings.PropertyChanged -= Settings_PropertyChanged;
    }

    public async Task OkDialog(string title, string message) => _ = await DialogCoordinator.Instance.ShowMessageAsync(this, title, message, MessageDialogStyle.Affirmative);

    public void Receive(PropertyChangedMessage<ImageRollEntry> message) => SelectedImageRoll = message.NewValue;

    private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "SelectedLanguage")
            OnPropertyChanged(nameof(State));
    }

    [RelayCommand]
    private async Task Login()
    {
        if (IsLoggedIn)
        {
            LogDebug($"Logging out. {UserName} @ {Host}:{SystemPort}");
            await Logout();
            return;
        }

        if (!PreLogin())
        {
            LogDebug($"Pre-Log in FAILED. {UserName} @ {Host}:{SystemPort}");
            return;
        }

        Controller.Commands.SystemPort = SystemPort;
        Controller.Commands.Host = Host;

        LogDebug($"Logging in. {UserName} @ {Host}:{SystemPort}");

        if (await Controller.Commands.Login(UserName, Password, LoginMonitor))
        {
            LogDebug($"Logged in. {(LoginMonitor ? "Monitor" : "Control")} {UserName} @ {Host}:{SystemPort}");

            PostLogin(LoginMonitor);
        }
        else
        {
            LogDebug($"Login FAILED. {UserName} @ {Host}:{SystemPort}");

            IsLoggedIn_Control = false;
            IsLoggedIn_Monitor = false;
        }
    }
    private bool PreLogin()
    {
        if (IsSimulator)
        {
            if (Directory.Exists(SimulatorImageDirectory))
            {
                try
                {
                    File.Create(Path.Combine(SimulatorImageDirectory, "file")).Close();
                    File.Delete(Path.Combine(SimulatorImageDirectory, "file"));
                }
                catch (Exception ex)
                {
                    LogError(ex);
                    return false;
                }
                return true;
            }
            else
            {
                LogError($"Invalid Simulation Images Directory: '{SimulatorImageDirectory}'");
                return false;
            }
        }
        return true;
    }
    private async void PostLogin(bool isLoggedIn_Monitor)
    {
        // Set the login data based on whether the login is for monitoring or control
        LoginData.accessLevel = isLoggedIn_Monitor ? "monitor" : "control";
        LoginData.token = Controller.Commands.Token; // Store the authentication token
        LoginData.id = UserName; // Store the user's ID
        LoginData.state = "0"; // Set the login state to '0' indicating a successful login

        // Update the login status properties based on the login type
        IsLoggedIn_Monitor = isLoggedIn_Monitor;
        IsLoggedIn_Control = !isLoggedIn_Monitor;

        // Fetch and store the camera configuration, symbologies, and calibration data from the server
        ConfigurationCamera = await Controller.Commands.GetCameraConfig();
        Symbologies = await Controller.Commands.GetSymbologies();
        Calibration = await Controller.Commands.GetCalibration();
        Jobs = await Controller.Commands.GetJobs();
        Print = await Controller.Commands.GetPrint();

        //If the system is in simulator mode, adjust the simulation settings
        if (IsSimulator)
        {
            Simulation = await Controller.Commands.GetSimulation();

            if (Simulation != null)
                if (!Host.Equals("127.0.0.1"))
                {
                    Simulation.mode = "trigger";
                    Simulation.dwellMs = 1;
                    _ = await Controller.Commands.PutSimulation(Simulation);
                }
                else
                {
                    Simulation.mode = "continuous";
                    Simulation.dwellMs = 1000;
                    _ = await Controller.Commands.PutSimulation(Simulation);
                }

            Simulation = await Controller.Commands.GetSimulation();
        }
        else
            Simulation = null;

        // Request the server to send extended data
        _ = await Controller.Commands.SetSendExtendedData(true);

        // Attempt to start the WebSocket connection for receiving node events
        if (!await Controller.WebSocket.StartAsync(Controller.Commands.URLs.WS_NodeEvents))
            return; // If the WebSocket connection cannot be started, exit the method
    }

    [RelayCommand]
    private async Task Logout()
    {

        PreLogout();

        _ = await Controller.Commands.Logout();

        try
        {
            await Controller.WebSocket.StopAsync();
        }
        catch { }

        LoginData.accessLevel = "";
        LoginData.token = "";
        LoginData.id = "";
        LoginData.state = "1";

        IsLoggedIn_Control = false;
        IsLoggedIn_Monitor = false;

        ConfigurationCamera = null;
        Symbologies = null;
        Calibration = null;
        Jobs = null;
        Print = null;
        Simulation = null;

        JobName = "";
        State = NodeStates.Offline;

    }
    private async void PreLogout()
    {
        //If the system is in simulator mode, adjust the simulation settings
        if (IsSimulator)
        {
            Simulation = await Controller.Commands.GetSimulation();

            // If the current simulation mode is 'continuous', change it to 'trigger' with a dwell time of 1ms
            if (Simulation != null && Simulation.mode != "continuous")
            {
                Simulation.mode = "continuous";
                Simulation.dwellMs = 1000;
                _ = await Controller.Commands.PutSimulation(Simulation);
            }

            // Fetch the simulation settings again to ensure they have been updated correctly
            Simulation = await Controller.Commands.GetSimulation();
            if (Simulation != null && Simulation.mode != "continuous")
            {
                // If the mode is not 'continuous', additional handling could be implemented here
            }
        }

        _ = await Controller.Commands.SetSendExtendedData(false);
    }

    [RelayCommand]
    public async Task EnablePrint(object parameter)
    {
        if (!IsSimulator)
        {
            if (IsBackupVoid && (string)parameter == "1")
            {
                if (!await Controller.Commands.Print(false))
                    return;

                Thread.Sleep(50);
            }

            _ = await Controller.Commands.Print((string)parameter == "1");
        }
        else
            _ = await Controller.SimulatorTogglePrint();

        Print = await Controller.Commands.GetPrint();

    }

    [RelayCommand]
    private async Task RemoveRepeat()
    {
        int repeat;

        repeat = await Controller.GetLatestRepeat();
        if (repeat == -9999)
            return;

        if (!await Controller.Commands.RemoveRepeat(repeat))
        {
            return;
        }

        if (!await Controller.Commands.ResumeJob())
        {
            return;
        }
    }
    //[RelayCommand] private void TriggerSim() => _ = Connection.Commands.TriggerSimulator();
    [RelayCommand] private async Task SwitchRun() => await Controller.SwitchToRun();
    [RelayCommand] private async Task SwitchEdit() => await Controller.SwitchToEdit();

    private void WebSocket_SessionStateChange(Events_System ev)
    {
        //if (ev.data.id == LoginData.id)
        if (ev.data.state == "0")
            if (ev.data.accessLevel == "control")
                if (LoginData.accessLevel == "control")
                    if (ev.data.token != LoginData.token)
                        _ = Logout();
    }
    private void V275_StateChanged(string state, string jobName, int dpi)
    {
        State = Enum.Parse<NodeStates>(state);
        JobName = jobName;
        Dpi = dpi;

        if (JobName != "")
            CheckTemplateName();
        else if (State == NodeStates.Idle)
            CheckTemplateName();
        else
        {

        }
    }

    public void CheckTemplateName()
    {
        IsWrongTemplateName = false;

        if (!IsLoggedIn)
            return;

        if (JobName == "" || SelectedImageRoll == null)
        {
            IsWrongTemplateName = true;
            return;
        }

        if (SelectedImageRoll.SelectedStandard != LabelVal.Sectors.Interfaces.StandardsTypes.GS1)
        {
            if (JobName.ToLower().Equals(SelectedImageRoll.Name.ToLower()))
                return;
        }
        else
        {
            if (JobName.ToLower().StartsWith("gs1"))
                return;
        }

        IsWrongTemplateName = true;
    }

    #region Logging
    private readonly Logging.Logger logger = new();
    public void LogInfo(string message) => logger.LogInfo(this.GetType(), message);
    public void LogDebug(string message) => logger.LogDebug(this.GetType(), message);
    public void LogWarning(string message) => logger.LogInfo(this.GetType(), message);
    public void LogError(string message) => logger.LogError(this.GetType(), message);
    public void LogError(Exception ex) => logger.LogError(this.GetType(), ex);
    public void LogError(string message, Exception ex) => logger.LogError(this.GetType(), message, ex);
    #endregion
}
