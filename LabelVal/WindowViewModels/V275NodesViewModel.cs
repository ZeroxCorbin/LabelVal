using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using LabelVal.Messages;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using V275_REST_lib.Models;
using V275_REST_Lib.Models;

namespace LabelVal.WindowViewModels;

public enum NodeStates
{
    Editing,
    Idle,
    Running,
    Paused,
}

public partial class V275Node : ObservableObject
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    public V275_REST_lib.Controller Connection { get; }
    private static string UserName => App.Settings.GetValue<string>(nameof(V275NodesViewModel.UserName));
    private static string Password => App.Settings.GetValue<string>(nameof(V275NodesViewModel.Password));

    private Events_System.Data LoginData { get; } = new Events_System.Data();

    public Devices.Node Node { get; set; }
    public Devices.Camera Camera { get; set; }
    public Inspection Inspection { get; set; }

    public bool IsSimulator => Inspection != null && Inspection.device.Equals("simulator");
    private static string SimulatorImageDirectory => App.Settings.GetValue<string>(nameof(V275NodesViewModel.SimulatorImageDirectory));

    public string Version => Connection.Commands.Product?.part;
    [ObservableProperty] NodeStates state = NodeStates.Idle;
    [ObservableProperty] private string jobName;
    public bool IsBackupVoid => Connection.Commands.ConfigurationCamera.backupVoidMode != null && Connection.Commands.ConfigurationCamera.backupVoidMode.value == "ON";


    [ObservableProperty] private bool isLoggedIn_Monitor = false;
    partial void OnIsLoggedIn_MonitorChanged(bool value) { OnPropertyChanged(nameof(IsLoggedIn)); OnPropertyChanged(nameof(IsNotLoggedIn)); }

    [ObservableProperty] private bool isLoggedIn_Control = false;
    partial void OnIsLoggedIn_ControlChanged(bool value) { OnPropertyChanged(nameof(IsLoggedIn)); OnPropertyChanged(nameof(IsNotLoggedIn)); }
    public bool IsLoggedIn => IsLoggedIn_Monitor || IsLoggedIn_Control;
    public bool IsNotLoggedIn => !(IsLoggedIn_Monitor || IsLoggedIn_Control);


    public V275Node(string host, uint systemPort, uint nodeNumber)
    {
        Connection = new V275_REST_lib.Controller(host, systemPort, nodeNumber);

        Connection.WebSocket.SessionStateChange += WebSocket_SessionStateChange;
        Connection.StateChanged += V275_StateChanged;
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
        //Reset();

        if (!await Connection.Commands.Logout())
            //Label_StatusChanged(V275.Status);

        //    LoginData.accessLevel = "";
        //LoginData.token = "";
        //LoginData.id = "";
        //LoginData.state = "1";

        IsLoggedIn_Control = false;
        IsLoggedIn_Monitor = false;

        try
        {
            await Connection.WebSocket.StopAsync();

            //V275.V275_State = "";
            //V275.V275_JobName = "";

            //V275_State = "";
            //V275_JobName = "";
        }
        catch { }
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
        //LoginData.accessLevel = isLoggedIn_Monitor ? "monitor" : "control";
        //LoginData.token = Connection.Commands.Token;
        //LoginData.id = UserName;
        //LoginData.state = "0";

        IsLoggedIn_Monitor = isLoggedIn_Monitor;
        IsLoggedIn_Control = !isLoggedIn_Monitor;

        _ = await Connection.Commands.GetCameraConfig();
        _ = await Connection.Commands.GetSymbologies();
        _ = await Connection.Commands.GetCalibration();
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

        //if (JobName != "")
        //    _ = CheckTemplateName();
        //else if (State == NodeStates.Idle)
        //    _ = CheckTemplateName();
        //else
        //{

        //}
    }

}

public partial class V275NodesViewModel : ObservableRecipient
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    public MainWindowViewModel MainWindow => App.Current.MainWindow.DataContext as MainWindowViewModel;

    //public static V275_REST_lib.Controller V275 { get; } = new V275_REST_lib.Controller();

    //private V275_API_WebSocketEvents WebSocket { get; } = new V275_API_WebSocketEvents();
    //private V275_API_WebSocketEvents SysWebSocket { get; } = new V275_API_WebSocketEvents();

    [ObservableProperty] private string v275_Host = App.Settings.GetValue(nameof(V275_Host), "127.0.0.1", true);
    partial void OnV275_HostChanged(string value) { App.Settings.SetValue(nameof(V275_Host), value); }

    [ObservableProperty] private uint v275_SystemPort = App.Settings.GetValue(nameof(V275_SystemPort), GetV275PortNumber(), true);
    partial void OnV275_SystemPortChanged(uint value)
    {
        if (value != 0)
        {
            App.Settings.SetValue(nameof(V275_SystemPort), value);
        }
        else
        {
            _ = App.Settings.DeleteSetting(nameof(V275_SystemPort));
        }

        OnPropertyChanged();
    }


    [ObservableProperty] private string userName = App.Settings.GetValue(nameof(UserName), "admin", true);
    partial void OnUserNameChanged(string value) => App.Settings.SetValue(nameof(UserName), value);

    [ObservableProperty] private string password = App.Settings.GetValue(nameof(Password), "admin", true);
    partial void OnPasswordChanged(string value) => App.Settings.SetValue(nameof(Password), value);


    [ObservableProperty] private string simulatorImageDirectory = App.Settings.GetValue(nameof(SimulatorImageDirectory), GetV275SimulationDirectory(), true);
    partial void OnSimulatorImageDirectoryChanged(string value) { if (string.IsNullOrEmpty(value)) { _ = App.Settings.DeleteSetting(nameof(SimulatorImageDirectory)); OnPropertyChanged(nameof(SimulatorImageDirectory)); } }


    [ObservableProperty] private ObservableCollection<V275Node> nodes = [];
    [ObservableProperty] private V275Node selectedNode;
    partial void OnSelectedNodeChanged(V275Node oldValue, V275Node newValue)
    {
        _ = WeakReferenceMessenger.Default.Send(new NodeMessages.SelectedNodeChanged(newValue, oldValue));
    }


    public bool IsWrongTemplateName
    {
        get => isWrongTemplateName;
        set { _ = SetProperty(ref isWrongTemplateName, value); OnPropertyChanged("IsNotWrongTemplateName"); }
    }
    public bool IsNotWrongTemplateName => !isWrongTemplateName;
    private bool isWrongTemplateName = false;

    public bool ShowTemplateNameMismatchDialog { get => App.Settings.GetValue("ShowTemplateNameMismatchDialog", true); set => App.Settings.SetValue("ShowTemplateNameMismatchDialog", value); }
    private Task TemplateNameMismatchDialog;
    private Task TemplateNotLoadedDialog;



    [ObservableProperty] private bool isGetDevices = false;

    [ObservableProperty] private bool isOldISO;

    public static IDialogCoordinator DialogCoordinator => MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance;

    public V275NodesViewModel()
    {


        IsActive = true;
    }

    public async Task OkDialog(string title, string message) => _ = await DialogCoordinator.ShowMessageAsync(this, title, message, MessageDialogStyle.Affirmative);

    [RelayCommand] private void TriggerSim() => _ = SelectedNode.Connection.Commands.TriggerSimulator();
    [RelayCommand] private async Task V275_SwitchRun() => await SelectedNode.Connection.SwitchToRun();
    [RelayCommand] private async Task V275_SwitchEdit() => await SelectedNode.Connection.SwitchToEdit();

    [RelayCommand]
    private async Task GetDevices()
    {
        Logger.Info("Loading V275 devices.");

        //Reset();

        var system = new V275Node(V275_Host, V275_SystemPort, 0);

        if (await system.Connection.Commands.GetDevices())
        {
            foreach (var node in system.Connection.Commands.Devices.nodes)
            {
                if (Nodes.Any(n => n.Node.cameraMAC == node.cameraMAC))
                {
                    Logger.Warn("Duplicate device MAC: {dev}", node.cameraMAC);
                    continue;
                }

                Logger.Debug("Adding Device MAC: {dev}", node.cameraMAC);

                Devices.Camera camera = system.Connection.Commands.Devices.cameras.FirstOrDefault(c => c.mac == node.cameraMAC);

                var newNode = new V275Node(V275_Host, V275_SystemPort, (uint)node.enumeration) { Node = node, Camera = camera };

                if (await newNode.Connection.Commands.GetInspection())
                    newNode.Inspection = newNode.Connection.Commands.Inspection;


                Nodes.Add(newNode);
            }

            if (SelectedNode == null && Nodes.Count > 0)
                SelectedNode = Nodes.First();

            _ = await SelectedNode.Connection.Commands.GetProduct();
            if (SelectedNode.Version != null)
            {
                var curVer = SelectedNode.Version.Remove(0, SelectedNode.Version.LastIndexOf("-") + 1);

                if (System.Version.TryParse(curVer, out var result))
                {
                    var baseVer = System.Version.Parse("1.2.0.0000");
                    IsOldISO = result.CompareTo(baseVer) < 0;
                }
            }
            OnPropertyChanged("V275_Version");
        }
        else
        {
            Nodes.Clear();
        }
    }



    [RelayCommand]
    private async Task V275_RemoveRepeat()
    {
        int repeat;

        repeat = await SelectedNode.Connection.GetLatestRepeat();
        if (repeat == -9999)
            return;

        if (!await SelectedNode.Connection.Commands.RemoveRepeat(repeat))
        {
            return;
        }

        if (!await SelectedNode.Connection.Commands.ResumeJob())
        {
            return;
        }
    }

    //public int CheckTemplateName()
    //{
    //    IsWrongTemplateName = false;

    //    if (!SelectedNode.IsLoggedIn)
    //        return 0;

    //    if (V275_JobName == "")
    //    {
    //        IsWrongTemplateName = true;

    //        if (TemplateNotLoadedDialog != null)
    //            if (TemplateNotLoadedDialog.Status != TaskStatus.RanToCompletion)
    //                return -1;

    //        TemplateNotLoadedDialog = OkDialog("Template Not Loaded!", "There is no template loaded in the V275 software.");
    //        return -1;
    //    }

    //    if (!MainWindow.StandardsDatabaseViewModel.SelectedStandard.IsGS1)
    //    {
    //        if (V275_JobName.ToLower().Equals(MainWindow.StandardsDatabaseViewModel.SelectedStandard.Name.ToLower()))
    //            return 1;
    //    }
    //    else
    //    {
    //        if (V275_JobName.ToLower().StartsWith("gs1"))
    //            return 1;
    //    }

    //    if (!ShowTemplateNameMismatchDialog)
    //        return 1;

    //    IsWrongTemplateName = true;

    //    if (TemplateNameMismatchDialog != null)
    //        if (TemplateNameMismatchDialog.Status != TaskStatus.RanToCompletion)
    //            return -2;

    //    TemplateNameMismatchDialog = OkDialog("Template Name Mismatch!", $"The template name loaded in the V275 software '{V275_JobName}' does not match the selected standard. '{MainWindow.StandardsDatabaseViewModel.SelectedStandard.Name.ToLower()}'");
    //    return -2;
    //}

    private static uint GetV275PortNumber()
    {
        var res = Registry.GetValue("HKEY_LOCAL_MACHINE\\Software\\OMRON\\V275Service", "SystemServerPort", 8080);

        return res == null ? 8080 : Convert.ToUInt32(res);
    }

    private static string GetV275SimulationDirectory()
    {
        var res = Registry.GetValue("HKEY_LOCAL_MACHINE\\Software\\OMRON\\V275Service", "DataDirectory", "");

        if (string.IsNullOrEmpty((string)res))
            return @"C:\Program Files\V275\data\images\simulation";
        else
            res += @"\images\simulation";

        return res.ToString();
    }

    public void Receive(NodeMessages.SelectedNodeChanged message) => throw new NotImplementedException();
}
