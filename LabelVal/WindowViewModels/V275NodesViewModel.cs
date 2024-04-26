using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using V275_REST_lib.Models;

namespace LabelVal.WindowViewModels;
public partial class V275NodesViewModel : ObservableObject
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    private MainWindowViewModel MainWindowViewModel => App.Current.MainWindow.DataContext as MainWindowViewModel;

    public static V275_REST_lib.Controller V275 { get; } = new V275_REST_lib.Controller();

    //private V275_API_WebSocketEvents WebSocket { get; } = new V275_API_WebSocketEvents();
    //private V275_API_WebSocketEvents SysWebSocket { get; } = new V275_API_WebSocketEvents();

    [ObservableProperty] private string v275_Host = V275.Host = App.Settings.GetValue(nameof(V275_Host), "127.0.0.1", true);
    partial void OnV275_HostChanged(string value) { App.Settings.SetValue(nameof(V275_Host), value); V275.Host = value; }

    [ObservableProperty] private uint v275_SystemPort = V275.SystemPort = App.Settings.GetValue(nameof(V275_SystemPort), GetV275PortNumber(), true);
    partial void OnV275_SystemPortChanged(uint value)
    {
        if (value != 0)
        {
            App.Settings.SetValue(nameof(V275_SystemPort), value);
            V275.SystemPort = value;
        }
        else
        {
            _ = App.Settings.DeleteSetting(nameof(V275_SystemPort));
            V275.SystemPort = V275_SystemPort;
        }

        OnPropertyChanged();
    }

    [ObservableProperty] private uint v275_NodeNumber = V275.NodeNumber = App.Settings.GetValue<uint>(nameof(V275_NodeNumber), 1, true);
    partial void OnV275_NodeNumberChanged(uint value) { App.Settings.SetValue(nameof(V275_NodeNumber), value); V275.NodeNumber = value; }



    [ObservableProperty] private string v275_MAC;
    public static string V275_Version => V275.Commands.Product?.part;

    [ObservableProperty] private string v275_State;
    [ObservableProperty] private string v275_JobName;
    public static bool V275_IsBackupVoid => V275.Commands.ConfigurationCamera.backupVoidMode != null && V275.Commands.ConfigurationCamera.backupVoidMode.value == "ON";

    [ObservableProperty] private ObservableCollection<Devices.Node> nodes = [];
    [ObservableProperty] private Devices.Node selectedNode;
    partial void OnSelectedNodeChanged(Devices.Node value)
    {
        if (value != null)
        {
            V275_NodeNumber = (uint)value.enumeration;
            V275_MAC = value.cameraMAC;

            IsDeviceSelected = true;
        }
        else
        {
            V275_NodeNumber = 0;
            V275_MAC = "";

            IsDeviceSelected = false;
        }
    }

    public bool IsWrongTemplateName
    {
        get => isWrongTemplateName;
        set { _ = SetProperty(ref isWrongTemplateName, value); OnPropertyChanged("IsNotWrongTemplateName"); }
    }
    public bool IsNotWrongTemplateName => !isWrongTemplateName;
    private bool isWrongTemplateName = false;

    [ObservableProperty] private string userName = App.Settings.GetValue(nameof(UserName), "admin", true);
    partial void OnUserNameChanged(string value) { App.Settings.SetValue(nameof(UserName), value); }

    [ObservableProperty] private string password = App.Settings.GetValue(nameof(Password), "admin", true);
    partial void OnPasswordChanged(string value) { App.Settings.SetValue(nameof(Password), value); }

    private Events_System.Data LoginData { get; } = new Events_System.Data();

    public bool ShowTemplateNameMismatchDialog { get => App.Settings.GetValue("ShowTemplateNameMismatchDialog", true); set => App.Settings.SetValue("ShowTemplateNameMismatchDialog", value); }
    private Task TemplateNameMismatchDialog;
    private Task TemplateNotLoadedDialog;



    [ObservableProperty] private bool isGetDevices = false;
    [ObservableProperty] private bool isDeviceSelected = false;

    [ObservableProperty] private string simulatorImageDirectory = App.Settings.GetValue(nameof(SimulatorImageDirectory), GetV275SimulationDirectory(), true);

    [ObservableProperty] private bool isLoggedIn_Monitor = false;
    partial void OnIsLoggedIn_MonitorChanged(bool value) { OnPropertyChanged(nameof(IsLoggedIn)); OnPropertyChanged(nameof(IsNotLoggedIn)); OnPropertyChanged(nameof(IsDeviceSimulator)); }

    [ObservableProperty] private bool isLoggedIn_Control = false;
    partial void OnIsLoggedIn_ControlChanged(bool value) { OnPropertyChanged(nameof(IsLoggedIn)); OnPropertyChanged(nameof(IsNotLoggedIn)); OnPropertyChanged(nameof(IsDeviceSimulator)); }
    public bool IsLoggedIn => IsLoggedIn_Monitor || IsLoggedIn_Control;
    public bool IsNotLoggedIn => !(IsLoggedIn_Monitor || IsLoggedIn_Control);

    public bool IsDeviceSimulator => V275_MAC != null && V275_MAC.Equals("00:00:00:00:00:00") && (IsLoggedIn_Control || IsLoggedIn_Monitor);

    [ObservableProperty] private bool isOldISO;

    public static IDialogCoordinator DialogCoordinator => MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance;

    public V275NodesViewModel()
    {
        V275.StateChanged += V275_StateChanged;
        V275.WebSocket.SessionStateChange += WebSocket_SessionStateChange;
    }

    public async Task OkDialog(string title, string message) => _ = await DialogCoordinator.ShowMessageAsync(this, title, message, MessageDialogStyle.Affirmative);

    [RelayCommand] private static void TriggerSim() => _ = V275.Commands.TriggerSimulator();
    [RelayCommand] private static async Task V275_SwitchRun() => await V275.SwitchToRun();
    [RelayCommand] private static async Task V275_SwitchEdit() => await V275.SwitchToEdit();


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
        V275_State = state;
        V275_JobName = jobName;

        if (V275_JobName != "")
            _ = CheckTemplateName();
        else if (V275_State == "Idle")
            _ = CheckTemplateName();
        else
        {

        }
    }

    [RelayCommand]
    private async Task GetDevices()
    {
        Logger.Info("Loading V275 devices.");

        //Reset();

        IsDeviceSelected = false;
        V275_MAC = "";
        Nodes.Clear();
        SelectedNode = null;

        if (await V275.Commands.GetDevices())
        {
            foreach (var node in V275.Commands.Devices.nodes)
            {
                Logger.Debug("Device MAC: {dev}", node.cameraMAC);

                Nodes.Add(node);

                if (node.enumeration == V275_NodeNumber)
                    SelectedNode = node;
            }

            Logger.Info("Processed {count} devices.", Nodes.Count);

            if (SelectedNode == null && Nodes.Count > 0)
                SelectedNode = Nodes.First();

            IsGetDevices = true;

            _ = await V275.Commands.GetProduct();
            if (V275_Version != null)
            {
                var curVer = V275_Version.Remove(0, V275_Version.LastIndexOf("-") + 1);

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
            //Label_StatusChanged(V275.Status);
            IsGetDevices = false;
        }
    }

    [RelayCommand]
    public async Task<bool> EnablePrint(object parameter)
    {
        if (!IsDeviceSimulator)
        {
            if (V275_IsBackupVoid)
            {
                if (!await V275.Commands.Print(false))
                    return false;

                Thread.Sleep(50);
            }

            return await V275.Commands.Print((string)parameter == "1");
        }
        else
        {
            return await V275.SimulatorTogglePrint();
        }
    }

    [RelayCommand]
    private async Task LoginMonitor()
    {
        //Reset();

        if (!PreLogin()) return;

        if (await V275.Commands.Login(UserName, Password, true))
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

        if (await V275.Commands.Login(UserName, Password, false))
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

        if (!await V275.Commands.Logout())
            //Label_StatusChanged(V275.Status);

        LoginData.accessLevel = "";
        LoginData.token = "";
        LoginData.id = "";
        LoginData.state = "1";

        IsLoggedIn_Control = false;
        IsLoggedIn_Monitor = false;

        try
        {
            await V275.WebSocket.StopAsync();

            //V275.V275_State = "";
            //V275.V275_JobName = "";

            //V275_State = "";
            //V275_JobName = "";
        }
        catch { }
    }
    private bool PreLogin()
    {
        if (IsDeviceSimulator)
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
                _ = OkDialog("Invalid Simulation Images Directory", $"Please select a valid simulator images directory.\r\n'{SimulatorImageDirectory}'");
                return false;
            }
        }
        return true;
    }
    private async Task PostLogin(bool isLoggedIn_Monitor)
    {
        LoginData.accessLevel = isLoggedIn_Monitor ? "monitor" : "control";
        LoginData.token = V275.Commands.Token;
        LoginData.id = UserName;
        LoginData.state = "0";

        IsLoggedIn_Monitor = isLoggedIn_Monitor;
        IsLoggedIn_Control = !isLoggedIn_Monitor;

        _ = await V275.Commands.GetCameraConfig();
        _ = await V275.Commands.GetSymbologies();
        _ = await V275.Commands.GetCalibration();
        _ = await V275.Commands.SetSendExtendedData(true);

        if (!await V275.WebSocket.StartAsync(V275.Commands.URLs.WS_NodeEvents))
            return;

        MainWindowViewModel.Repeats.Clear();
    }


    public int CheckTemplateName()
    {
        IsWrongTemplateName = false;

        if (!IsLoggedIn)
            return 0;

        if (V275_JobName == "")
        {
            IsWrongTemplateName = true;

            if (TemplateNotLoadedDialog != null)
                if (TemplateNotLoadedDialog.Status != TaskStatus.RanToCompletion)
                    return -1;

            TemplateNotLoadedDialog = OkDialog("Template Not Loaded!", "There is no template loaded in the V275 software.");
            return -1;
        }

        if (!MainWindowViewModel.StandardsDatabaseViewModel.SelectedStandard.IsGS1)
        {
            if (V275_JobName.ToLower().Equals(MainWindowViewModel.StandardsDatabaseViewModel.SelectedStandard.Name.ToLower()))
                return 1;
        }
        else
        {
            if (V275_JobName.ToLower().StartsWith("gs1"))
                return 1;
        }

        if (!ShowTemplateNameMismatchDialog)
            return 1;

        IsWrongTemplateName = true;

        if (TemplateNameMismatchDialog != null)
            if (TemplateNameMismatchDialog.Status != TaskStatus.RanToCompletion)
                return -2;

        TemplateNameMismatchDialog = OkDialog("Template Name Mismatch!", $"The template name loaded in the V275 software '{V275_JobName}' does not match the selected standard. '{MainWindowViewModel.StandardsDatabaseViewModel.SelectedStandard.Name.ToLower()}'");
        return -2;
    }

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
}
