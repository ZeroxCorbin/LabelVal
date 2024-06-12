using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.Messages;
using LabelVal.WindowViewModels;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using V275_REST_lib.Models;
using V275_REST_Lib.Models;

namespace LabelVal.V275.ViewModels;

public partial class V275 : ObservableRecipient, IRecipient<ImageRollMessages.SelectedImageRollChanged>
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
    partial void OnSimulatorImageDirectoryChanged(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            _ = App.Settings.DeleteSetting(nameof(SimulatorImageDirectory)); OnPropertyChanged(nameof(SimulatorImageDirectory));
        }
        else
        {
            App.Settings.SetValue(nameof(SimulatorImageDirectory), value);
        }
    }

    [ObservableProperty] private ObservableCollection<Node> nodes = [];
    [ObservableProperty] private Node selectedNode;
    partial void OnSelectedNodeChanged(Node oldValue, Node newValue) => _ = WeakReferenceMessenger.Default.Send(new NodeMessages.SelectedNodeChanged(newValue, oldValue));

    public bool ShowTemplateNameMismatchDialog { get => App.Settings.GetValue("ShowTemplateNameMismatchDialog", true, true); set => App.Settings.SetValue("ShowTemplateNameMismatchDialog", value); }

    [ObservableProperty] private bool isGetDevices = false;

    [ObservableProperty] private ImageRollEntry selectedImageRoll;

    public static IDialogCoordinator DialogCoordinator => MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance;

    public V275()
    {
        IsActive = true;
    }

    public void Receive(ImageRollMessages.SelectedImageRollChanged message) => SelectedImageRoll = message.Value;

    public async Task OkDialog(string title, string message) => _ = await DialogCoordinator.ShowMessageAsync(this, title, message, MessageDialogStyle.Affirmative);

    [RelayCommand]
    private async Task GetDevices()
    {
        Logger.Info("Loading V275 devices.");

        //Reset();

        var system = new Node(V275_Host, V275_SystemPort, 0, SelectedImageRoll);

        Devices dev;
        if ((dev = await system.Connection.Commands.GetDevices()) != null)
        {
            foreach (var node in dev.nodes)
            {
                if (Nodes.Any(n => n.Details.cameraMAC == node.cameraMAC))
                {
                    Logger.Warn("Duplicate device MAC: {dev}", node.cameraMAC);
                    continue;
                }

                Logger.Debug("Adding Device MAC: {dev}", node.cameraMAC);

                var camera = dev.cameras.FirstOrDefault(c => c.mac == node.cameraMAC);

                var newNode = new Node(V275_Host, V275_SystemPort, (uint)node.enumeration, SelectedImageRoll) { Details = node, Camera = camera };

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

                bool res = false;
                if (Version.TryParse(curVer, out var result))
                {
                    var baseVer = Version.Parse("1.2.0.0000");
                    res = result.CompareTo(baseVer) < 0;
                }

                foreach (var node in Nodes)
                {
                    node.SelectedImageRoll = SelectedImageRoll;
                    node.Product = product;
                    node.IsOldISO = res;
                }
            }
        }
        else
        {
            Nodes.Clear();
        }
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
