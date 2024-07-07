using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.ImageRolls.ViewModels;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

public partial class Node : ObservableRecipient, IRecipient<PropertyChangedMessage<ImageRollEntry>>
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    public V275_REST_lib.Controller Connection { get; }

    private string V275_Host = App.Settings.GetValue<string>(nameof(V275.V275_Host));

    private uint V275_SystemPort = App.Settings.GetValue<uint>(nameof(V275.V275_SystemPort));

    private static string UserName => App.Settings.GetValue<string>(nameof(V275.UserName));
    private static string Password => App.Settings.GetValue<string>(nameof(V275.Password));

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
    private static string SimulatorImageDirectory => App.Settings.GetValue<string>(nameof(V275.SimulatorImageDirectory));

    [ObservableProperty] private NodeStates state = NodeStates.Offline;
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
        if (await Connection.Commands.UnloadJob())
            if (await Connection.Commands.LoadJob(name))
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

        Connection = new V275_REST_lib.Controller(host, systemPort, nodeNumber);

        Connection.WebSocket.SessionStateChange += WebSocket_SessionStateChange;
        Connection.StateChanged += V275_StateChanged;

        App.Settings.PropertyChanged += Settings_PropertyChanged;

        IsActive = true;
    }

    public async Task OkDialog(string title, string message) => _ = await DialogCoordinator.Instance.ShowMessageAsync(this, title, message, MessageDialogStyle.Affirmative);

    public void Receive(PropertyChangedMessage<ImageRollEntry> message) => SelectedImageRoll = message.NewValue;

    private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(V275.V275_Host))
            Connection.Commands.Host = V275_Host;
        else if (e.PropertyName == nameof(V275.V275_SystemPort))
            Connection.Commands.SystemPort = V275_SystemPort;
        else if (e.PropertyName == "SelectedLanguage")
            OnPropertyChanged(nameof(State));
    }

    [RelayCommand]
    private async Task Login()
    {
        if (IsLoggedIn)
        {
            await Logout();
            return;
        }

        if (!PreLogin())
            return;

        if (await Connection.Commands.Login(UserName, Password, LoginMonitor))
        {
            PostLogin(LoginMonitor);
        }
        else
        {
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
                    //Label_StatusChanged(ex.Message);

                    Logger.Error(ex);
                    return false;
                }
                return true;
            }
            else
            {
                // _ = OkDialog("Invalid Simulation Images Directory", $"Please select a valid simulator images directory.\r\n'{SimulatorImageDirectory}'");
                return false;
            }
        }
        return true;
    }
    private async void PostLogin(bool isLoggedIn_Monitor)
    {
        // Set the login data based on whether the login is for monitoring or control
        LoginData.accessLevel = isLoggedIn_Monitor ? "monitor" : "control";
        LoginData.token = Connection.Commands.Token; // Store the authentication token
        LoginData.id = UserName; // Store the user's ID
        LoginData.state = "0"; // Set the login state to '0' indicating a successful login

        // Update the login status properties based on the login type
        IsLoggedIn_Monitor = isLoggedIn_Monitor;
        IsLoggedIn_Control = !isLoggedIn_Monitor;

        // Fetch and store the camera configuration, symbologies, and calibration data from the server
        ConfigurationCamera = await Connection.Commands.GetCameraConfig();
        Symbologies = await Connection.Commands.GetSymbologies();
        Calibration = await Connection.Commands.GetCalibration();
        Jobs = await Connection.Commands.GetJobs();
        Print = await Connection.Commands.GetPrint();

        //If the system is in simulator mode, adjust the simulation settings
        if (IsSimulator && Connection.Commands.Host != "127.0.0.1")
        {
            Simulation = await Connection.Commands.GetSimulation();

            // If the current simulation mode is 'continuous', change it to 'trigger' with a dwell time of 1ms
            if (Simulation != null && Simulation.mode == "continuous")
            {
                Simulation.mode = "trigger";
                Simulation.dwellMs = 1;
                _ = await Connection.Commands.PutSimulation(Simulation);
            }

            // Fetch the simulation settings again to ensure they have been updated correctly
            Simulation = await Connection.Commands.GetSimulation();
            if (Simulation != null && Simulation.mode != "trigger")
            {
                // If the mode is not 'trigger', additional handling could be implemented here
            }
        }
        else Simulation = IsSimulator ? await Connection.Commands.GetSimulation() : null;

        // Request the server to send extended data
        _ = await Connection.Commands.SetSendExtendedData(true);

        // Attempt to start the WebSocket connection for receiving node events
        if (!await Connection.WebSocket.StartAsync(Connection.Commands.URLs.WS_NodeEvents))
            return; // If the WebSocket connection cannot be started, exit the method
    }

    [RelayCommand]
    private async Task Logout()
    {

        PreLogout();

        _ = await Connection.Commands.Logout();

        try
        {
            await Connection.WebSocket.StopAsync();
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
            Simulation = await Connection.Commands.GetSimulation();

            // If the current simulation mode is 'continuous', change it to 'trigger' with a dwell time of 1ms
            if (Simulation != null && Simulation.mode != "continuous")
            {
                Simulation.mode = "continuous";
                Simulation.dwellMs = 1000;
                _ = await Connection.Commands.PutSimulation(Simulation);
            }

            // Fetch the simulation settings again to ensure they have been updated correctly
            Simulation = await Connection.Commands.GetSimulation();
            if (Simulation != null && Simulation.mode != "continuous")
            {
                // If the mode is not 'continuous', additional handling could be implemented here
            }
        }

        _ = await Connection.Commands.SetSendExtendedData(false);
    }

    [RelayCommand]
    public async Task EnablePrint(object parameter)
    {
        if (!IsSimulator)
        {
            if (IsBackupVoid && (string)parameter == "1")
            {
                if (!await Connection.Commands.Print(false))
                    return;

                Thread.Sleep(50);
            }

            _ = await Connection.Commands.Print((string)parameter == "1");
        }
        else
            _ = await Connection.SimulatorTogglePrint();

        Print = await Connection.Commands.GetPrint();

    }

    [RelayCommand]
    private async Task RemoveRepeat()
    {
        int repeat;

        repeat = await Connection.GetLatestRepeat();
        if (repeat == -9999)
            return;

        if (!await Connection.Commands.RemoveRepeat(repeat))
        {
            return;
        }

        if (!await Connection.Commands.ResumeJob())
        {
            return;
        }
    }
    //[RelayCommand] private void TriggerSim() => _ = Connection.Commands.TriggerSimulator();
    [RelayCommand] private async Task SwitchRun() => await Connection.SwitchToRun();
    [RelayCommand] private async Task SwitchEdit() => await Connection.SwitchToEdit();

    private void WebSocket_SessionStateChange(Events_System ev)
    {
        //if (ev.data.id == LoginData.id)
        if (ev.data.state == "0")
            if (ev.data.accessLevel == "control")
                if (LoginData.accessLevel == "control")
                    if (ev.data.token != LoginData.token)
                        _ = Logout();
    }
    private void V275_StateChanged(string state, string jobName)
    {
        State = Enum.Parse<NodeStates>(state);
        JobName = jobName;

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

        if (SelectedImageRoll.SelectedStandard != Sectors.ViewModels.StandardsTypes.GS1)
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
}
