using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.Logging;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using V275_REST_lib.Models;
using V275_REST_Lib.Models;

namespace LabelVal.V275.ViewModels;

[JsonObject(MemberSerialization.OptIn)]
public partial class NodeManager : ObservableRecipient, IRecipient<PropertyChangedMessage<ImageRollEntry>>
{
    public V275Manager Manager { get; set; }

    [ObservableProperty][property: JsonProperty] private string host = "127.0.0.1"; //App.Settings.GetValue($"{NodeManager.ClassName}{nameof(NodeManager.Host)}", "127.0.0.1", true);
    partial void OnHostChanged(string value)
    {
        foreach (var nd in Nodes)
            nd.Host = value;
    }

    [ObservableProperty][property: JsonProperty] private uint systemPort = GetPortNumber(); //App.Settings.GetValue($"{NodeManager.ClassName}{nameof(NodeManager.SystemPort)}", GetPortNumber(), true);
    partial void OnSystemPortChanged(uint value)
    {
        if (!LibStaticUtilities_IPHostPort.Ports.IsPortValid(value))
            _ = App.Current.Dispatcher.BeginInvoke(() => SystemPort = GetPortNumber());
        else
        {
            foreach (var nd in Nodes)
                nd.SystemPort = value;
        }
    }
    public string SystemPortString
    {
        get => SystemPort.ToString();
        set
        {
            if (value == null || !LibStaticUtilities_IPHostPort.Ports.IsPortValid(value))
                _ = App.Current.Dispatcher.BeginInvoke(() => { SystemPort = GetPortNumber(); OnPropertyChanged(nameof(SystemPortString)); });
            else
                SystemPort = uint.Parse(value);
        }
    }

    [ObservableProperty][property: JsonProperty] private string userName = "admin";// App.Settings.GetValue($"{NodeManager.ClassName}{nameof(NodeManager.UserName)}", "admin", true);
    partial void OnUserNameChanged(string value)
    {
        foreach (var nd in Nodes)
            nd.UserName = value;
    }

    [ObservableProperty][property: JsonProperty] private string password = "admin"; // App.Settings.GetValue($"{NodeManager.ClassName}{nameof(Password)}", "admin", true);
    partial void OnPasswordChanged(string value)
    {
        foreach (var nd in Nodes)
            nd.Password = value;
    }

    [ObservableProperty][property: JsonProperty] private string simulatorImageDirectory = GetSimulationDirectory();
    partial void OnSimulatorImageDirectoryChanged(string value)
    {
        if (string.IsNullOrEmpty(value))
            _ = App.Current.Dispatcher.BeginInvoke(() => SimulatorImageDirectory = GetSimulationDirectory());
        else
        {
            foreach (var nd in Nodes)
                nd.SimulatorImageDirectory = value;
        }
    }

    [ObservableProperty] private ObservableCollection<Node> nodes = [];

    [ObservableProperty] private bool isGetDevices = false;

    [ObservableProperty] private ImageRollEntry selectedImageRoll;

    [RelayCommand]
    private async Task GetDevices()
    {
        LogInfo("Loading V275 devices.");

        //Reset();

        Node system = new(Host, SystemPort, 0, SelectedImageRoll);

        Devices dev;
        if ((dev = await system.Controller.Commands.GetDevices()) != null)
        {
            List<Node> lst = new();
            foreach (Devices.Node node in dev.nodes)
            {
                if (lst.Any(n => n.Details.cameraMAC == node.cameraMAC))
                {
                    LogWarning($"Duplicate device MAC: {node.cameraMAC}");
                    continue;
                }

                LogDebug($"Adding Device MAC: {node.cameraMAC}");

                Devices.Camera camera = dev.cameras.FirstOrDefault(c => c.mac == node.cameraMAC);

                Node newNode = new(Host, SystemPort, (uint)node.enumeration, SelectedImageRoll)
                { Manager = this, Details = node, Camera = camera, SimulatorImageDirectory = SimulatorImageDirectory, UserName = UserName, Password = Password };

                Inspection insp;
                if ((insp = await newNode.Controller.Commands.GetInspection()) != null)
                    newNode.Inspection = insp;

                lst.Add(newNode);
            }

            List<Node> srt = lst.OrderBy(n => n.Controller.Commands.NodeNumber).ToList();

            Nodes.Clear();
            if (srt.Count == 0)
            {
                LogWarning("No devices found.");
                return;
            }

            foreach (Node node in srt)
                Nodes.Add(node);

            Product product = await new V275_REST_lib.Controller(Host, SystemPort, 0).Commands.GetProduct();
            if (product != null)
            {
                string curVer = product.part.Remove(0, product.part.LastIndexOf("-") + 1);

                bool res = false;
                if (Version.TryParse(curVer, out Version result))
                {
                    Version baseVer = Version.Parse("1.2.0.0000");
                    res = result.CompareTo(baseVer) < 0;
                }

                foreach (Node node in Nodes)
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

        Node sel = App.Settings.GetValue<Node>($"V275_{nameof(Manager.SelectedDevice)}");
        foreach (Node node in Nodes)
        {
            if (sel != null && (node.Host == sel.Host && node.SystemPort == sel.SystemPort && node.NodeNumber == sel.NodeNumber))
                Manager.SelectedDevice = node;
        }
    }

    private static uint GetPortNumber()
    {
        object res = Registry.GetValue("HKEY_LOCAL_MACHINE\\Software\\OMRON\\V275Service", "SystemServerPort", 8080);

        return res == null ? 8080 : Convert.ToUInt32(res);
    }
    private static string GetSimulationDirectory()
    {
        object res = Registry.GetValue("HKEY_LOCAL_MACHINE\\Software\\OMRON\\V275Service", "DataDirectory", "");

        if (string.IsNullOrEmpty((string)res))
            return @"C:\Program Files\V275\data\images\simulation";
        else
            res += @"\images\simulation";

        return res.ToString();
    }

    #region Recieve Messages
    //There is no point in reuesting the SeletedImageRoll at init, the user has not selected anything yet.
    public void Receive(PropertyChangedMessage<ImageRollEntry> message) => SelectedImageRoll = message.NewValue;
    #endregion

    #region Dialogs
    public static IDialogCoordinator DialogCoordinator => MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance;
    public async Task OkDialog(string title, string message) => _ = await DialogCoordinator.ShowMessageAsync(this, title, message, MessageDialogStyle.Affirmative);
    #endregion

    #region Logging
    private readonly Logger logger = new();
    public void LogInfo(string message) => logger.LogInfo(this.GetType(), message);
    public void LogDebug(string message) => logger.LogDebug(this.GetType(), message);
    public void LogWarning(string message) => logger.LogInfo(this.GetType(), message);
    public void LogError(string message) => logger.LogError(this.GetType(), message);
    public void LogError(Exception ex) => logger.LogError(this.GetType(), ex);
    public void LogError(string message, Exception ex) => logger.LogError(this.GetType(), message, ex);
    #endregion
}
