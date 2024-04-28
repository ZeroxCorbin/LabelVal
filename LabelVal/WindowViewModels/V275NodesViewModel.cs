using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using LabelVal.Messages;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using V275_REST_lib.Models;
using V275_REST_Lib.Models;

namespace LabelVal.WindowViewModels;


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

        Devices dev;
        if ((dev = await system.Connection.Commands.GetDevices()) != null)
        {
            foreach (var node in dev.nodes)
            {
                if (Nodes.Any(n => n.Node.cameraMAC == node.cameraMAC))
                {
                    Logger.Warn("Duplicate device MAC: {dev}", node.cameraMAC);
                    continue;
                }

                Logger.Debug("Adding Device MAC: {dev}", node.cameraMAC);

                Devices.Camera camera = dev.cameras.FirstOrDefault(c => c.mac == node.cameraMAC);

                var newNode = new V275Node(V275_Host, V275_SystemPort, (uint)node.enumeration) { Node = node, Camera = camera };

                Inspection insp;
                if ((insp = await newNode.Connection.Commands.GetInspection()) != null)
                    newNode.Inspection = insp;

                Nodes.Add(newNode);
            }

            if (SelectedNode == null && Nodes.Count > 0)
                SelectedNode = Nodes.First();

            var product = await SelectedNode.Connection.Commands.GetProduct();
            if (product != null)
            {
                var curVer = product.part.Remove(0, product.part.LastIndexOf("-") + 1);

                if (Version.TryParse(curVer, out var result))
                {
                    var baseVer = Version.Parse("1.2.0.0000");
                    IsOldISO = result.CompareTo(baseVer) < 0;
                }

                foreach (var node in Nodes)
                    node.Product = product;
            }
        }
        else
        {
            Nodes.Clear();
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
