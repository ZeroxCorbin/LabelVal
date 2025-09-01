using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.ImageRolls.ViewModels;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using V275_REST_Lib.Models;

namespace LabelVal.V275.ViewModels;

[JsonObject(MemberSerialization.OptIn)]
public partial class NodeManager : ObservableRecipient, IRecipient<PropertyChangedMessage<ImageRoll>>
{
    [JsonProperty] public long ID { get; set; } = DateTime.Now.Ticks;

    public V275Manager Manager { get; set; }

    [ObservableProperty] private ObservableCollection<Node> nodes = [];

    [ObservableProperty][property: JsonProperty] private string host; //App.Settings.GetValue($"{NodeManager.ClassName}{nameof(NodeManager.Host)}", "127.0.0.1", true);
    partial void OnHostChanged(string value)
    {
        foreach (var nd in Nodes)
            nd.Controller.Host = value;
    }

    [ObservableProperty][property: JsonProperty] private uint systemPort; //App.Settings.GetValue($"{NodeManager.ClassName}{nameof(NodeManager.SystemPort)}", GetPortNumber(), true);
    partial void OnSystemPortChanged(uint value)
    {
        if (!LibStaticUtilities_IPHostPort.Ports.IsPortValid(value))
            _ = Application.Current.Dispatcher.BeginInvoke(() => SystemPort = GetPortNumber());
        else
        {
            foreach (var nd in Nodes)
                nd.Controller.SystemPort = value;
        }
    }
    public string SystemPortString
    {
        get => SystemPort.ToString();
        set
        {
            if (value == null || !LibStaticUtilities_IPHostPort.Ports.IsPortValid(value))
                _ = Application.Current.Dispatcher.BeginInvoke(() => { SystemPort = GetPortNumber(); OnPropertyChanged(nameof(SystemPortString)); });
            else
                SystemPort = uint.Parse(value);
        }
    }

    [ObservableProperty][property: JsonProperty] private string username;// App.Settings.GetValue($"{NodeManager.ClassName}{nameof(NodeManager.Username)}", "admin", true);
    partial void OnUsernameChanged(string value)
    {
        foreach (var nd in Nodes)
            nd.Controller.Username = value;
    }

    [ObservableProperty][property: JsonProperty] private string password; // App.Settings.GetValue($"{NodeManager.ClassName}{nameof(Password)}", "admin", true);
    partial void OnPasswordChanged(string value)
    {
        foreach (var nd in Nodes)
            nd.Controller.Password = value;
    }

    [ObservableProperty][property: JsonProperty] private string simulatorImageDirectory;
    partial void OnSimulatorImageDirectoryChanged(string value)
    {
        if (string.IsNullOrEmpty(value))
            _ = Application.Current.Dispatcher.BeginInvoke(() => SimulatorImageDirectory = GetSimulationDirectory());
        else
        {
            foreach (var nd in Nodes)
                nd.Controller.SimulatorImageDirectory = value;
        }
    }

    [ObservableProperty] private ImageRoll selectedImageRoll;

    public NodeManager()
    {
        Host ??= "127.0.0.1";

        if (SystemPort == 0)
            SystemPort = GetPortNumber();

        Username ??= "admin";
        Password ??= "admin";

        SimulatorImageDirectory ??= GetSimulationDirectory();
    }

    [RelayCommand]
    private async Task GetDevices()
    {
        if(!Application.Current.Dispatcher.CheckAccess())
        {
            _ = Application.Current.Dispatcher.BeginInvoke(() => GetDevices());
            return;
        }

        Logger.LogInfo("Loading V275 devices.");

        Node system = new(Host, SystemPort, 0, Username, Password, SimulatorImageDirectory, SelectedImageRoll);

        if ((await system.Controller.Commands.GetDevices()).Object is Devices dev)
        {
            List<Node> lst = [];
            foreach (var node in dev.nodes)
            {
                if (lst.Any(n => n.Controller.Node.cameraMAC == node.cameraMAC))
                {
                    Logger.LogWarning($"Duplicate device MAC: {node.cameraMAC}");
                    continue;
                }

                Logger.LogDebug($"Adding Device MAC: {node.cameraMAC}");

                Node newNode = new(Host, SystemPort, (uint)node.enumeration, Username, Password, SimulatorImageDirectory, SelectedImageRoll) { Manager = this };
                newNode.Controller.Initialize();
                lst.Add(newNode);
            }

            var srt = lst.OrderBy(n => n.Controller.NodeNumber).ToList();

            Nodes.Clear();
            if (srt.Count == 0)
            {
                Logger.LogWarning("No devices found.");
                return;
            }

            foreach (var node in srt)
                Nodes.Add(node);

        }
        else
        {
            Nodes.Clear();
        }

        var sel = App.Settings.GetValue<Node>($"V275_{nameof(Manager.SelectedDevice)}");
        foreach (var node in Nodes)
        {
            if (sel != null && node.Controller.Host == sel.Controller.Host && node.Controller.SystemPort == sel.Controller.SystemPort && node.Controller.NodeNumber == sel.Controller.NodeNumber)
                Manager.SelectedDevice = node;
        }
    }
    private static uint GetPortNumber()
    {
        var res = Registry.GetValue("HKEY_LOCAL_MACHINE\\Software\\OMRON\\V275Service", "SystemServerPort", 8080);

        return res == null ? 8080 : Convert.ToUInt32(res);
    }
    private static string GetSimulationDirectory()
    {
        var res = Registry.GetValue("HKEY_LOCAL_MACHINE\\Software\\OMRON\\V275Service", "DataDirectory", "");

        if (string.IsNullOrEmpty((string)res))
            return @"C:\Program Files\V275\data\images\simulation";
        else
            res += @"\images\simulation";

        return res.ToString();
    }

    #region Recieve Messages
    //There is no point in reuesting the SeletedImageRoll at init, the user has not selected anything yet.
    public void Receive(PropertyChangedMessage<ImageRoll> message) => SelectedImageRoll = message.NewValue;
    #endregion

    #region Dialogs
    public static IDialogCoordinator DialogCoordinator => MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance;
    public async Task OkDialog(string title, string message) => _ = await DialogCoordinator.ShowMessageAsync(this, title, message, MessageDialogStyle.Affirmative);
    #endregion

}
