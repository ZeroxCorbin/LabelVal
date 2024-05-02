using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.Messages;
using LabelVal.Models;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;
using V275_REST_lib.Models;
using V275_REST_Lib.Models;
using static System.ComponentModel.Design.ObjectSelectorEditor;

namespace LabelVal.V275.ViewModels;

public enum NodeStates
{
    Editing,
    Idle,
    Running,
    Paused,
}

public partial class Node : ObservableRecipient, IRecipient<Messages.ImageRollMessages.SelectedImageRollChanged>
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    public V275_REST_lib.Controller Connection { get; }


    private string V275_Host = App.Settings.GetValue<string>(nameof(V275.V275_Host));

    private uint V275_SystemPort = App.Settings.GetValue<uint>(nameof(V275.V275_SystemPort));

    private static string UserName => App.Settings.GetValue<string>(nameof(V275.UserName));
    private static string Password => App.Settings.GetValue<string>(nameof(V275.Password));

    private Events_System.Data LoginData { get; } = new Events_System.Data();

    public Devices.Node Details { get; set; }
    public Devices.Camera Camera { get; set; }
    public Inspection Inspection { get; set; }

    [ObservableProperty] private Product product;


    [ObservableProperty] private Configuration_Camera configurationCamera;
    [ObservableProperty] private List<Symbologies.Symbol> symbologies;
    [ObservableProperty] private Calibration calibration;

    public bool IsSimulator => Inspection != null && Inspection.device.Equals("simulator");
    private static string SimulatorImageDirectory => App.Settings.GetValue<string>(nameof(V275.SimulatorImageDirectory));

    [ObservableProperty] NodeStates state = NodeStates.Idle;
    [ObservableProperty] private string jobName = "";
    public bool IsBackupVoid => ConfigurationCamera != null && ConfigurationCamera.backupVoidMode.value == "ON";


    [ObservableProperty] private bool isLoggedIn_Monitor = false;
    partial void OnIsLoggedIn_MonitorChanged(bool value) { OnPropertyChanged(nameof(IsLoggedIn)); OnPropertyChanged(nameof(IsNotLoggedIn)); }

    [ObservableProperty] private bool isLoggedIn_Control = false;
    public bool IsNotLoggedIn_Control => !IsLoggedIn_Control;
    partial void OnIsLoggedIn_ControlChanged(bool value) { OnPropertyChanged(nameof(IsLoggedIn)); OnPropertyChanged(nameof(IsNotLoggedIn)); OnPropertyChanged(nameof(IsNotLoggedIn_Control)); }
    public bool IsLoggedIn => IsLoggedIn_Monitor || IsLoggedIn_Control;
    public bool IsNotLoggedIn => !(IsLoggedIn_Monitor || IsLoggedIn_Control);


    [ObservableProperty] private ImageRoll selectedImageRoll;
    partial void OnSelectedImageRollChanged(ImageRoll value) => CheckTemplateName();

    [ObservableProperty] private bool isWrongTemplateName = false;

    public Node(string host, uint systemPort, uint nodeNumber)
    {
        Connection = new V275_REST_lib.Controller(host, systemPort, nodeNumber);

        Connection.WebSocket.SessionStateChange += WebSocket_SessionStateChange;
        Connection.StateChanged += V275_StateChanged;

        App.Settings.PropertyChanged += Settings_PropertyChanged;

        IsActive = true;
    }

    public async Task OkDialog(string title, string message) => _ = await DialogCoordinator.Instance.ShowMessageAsync(this, title, message, MessageDialogStyle.Affirmative);

    public void Receive(ImageRollMessages.SelectedImageRollChanged message) => SelectedImageRoll = message.Value;

    private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {

        if (e.PropertyName == nameof(V275.V275_Host))
            Connection.Commands.Host = V275_Host;
        else if (e.PropertyName == nameof(V275.V275_SystemPort))
            Connection.Commands.SystemPort = V275_SystemPort;
    }

    [RelayCommand]
    private async Task LoginMonitor()
    {
        //Reset();

        if (!PreLogin()) return;

        if (await Connection.Commands.Login(UserName, Password, true))
        {
            _ = PostLogin(true);
        }
        else
        {
            //Label_StatusChanged(V275.Status);
            IsLoggedIn_Monitor = false;
        }
    }
    [RelayCommand]
    private async Task LoginControl()
    {
        //Reset();

        if (!PreLogin()) return;

        if (await Connection.Commands.Login(UserName, Password, false))
        {
            _ = PostLogin(false);
        }
        else
        {
            //Label_StatusChanged(V275.Status);
            IsLoggedIn_Control = false;
        }
    }
    [RelayCommand]
    private async Task Logout()
    {
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

        JobName = "";
        State = NodeStates.Idle;

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
    private async Task PostLogin(bool isLoggedIn_Monitor)
    {
        LoginData.accessLevel = isLoggedIn_Monitor ? "monitor" : "control";
        LoginData.token = Connection.Commands.Token;
        LoginData.id = UserName;
        LoginData.state = "0";

        IsLoggedIn_Monitor = isLoggedIn_Monitor;
        IsLoggedIn_Control = !isLoggedIn_Monitor;

        ConfigurationCamera = await Connection.Commands.GetCameraConfig();
        Symbologies = await Connection.Commands.GetSymbologies();
        Calibration = await Connection.Commands.GetCalibration();

        _ = await Connection.Commands.SetSendExtendedData(true);

        if (!await Connection.WebSocket.StartAsync(Connection.Commands.URLs.WS_NodeEvents))
            return;

        //MainWindow.Repeats.Clear();
    }

    [RelayCommand]
    public async Task<bool> EnablePrint(object parameter)
    {
        if (!IsSimulator)
        {
            if (IsBackupVoid)
            {
                if (!await Connection.Commands.Print(false))
                    return false;

                Thread.Sleep(50);
            }

            return await Connection.Commands.Print((string)parameter == "1");
        }
        else
        {
            return await Connection.SimulatorTogglePrint();
        }
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
    [RelayCommand] private void TriggerSim() => _ = Connection.Commands.TriggerSimulator();
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

        if (!SelectedImageRoll.IsGS1)
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
