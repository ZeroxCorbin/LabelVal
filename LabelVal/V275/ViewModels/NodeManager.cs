using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.Main.ViewModels;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using V275_REST_Lib.Models;

namespace LabelVal.V275.ViewModels;

/// <summary>
/// Manages the discovery and state of V275 verifier nodes.
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
public partial class NodeManager : ObservableRecipient, IRecipient<PropertyChangedMessage<ImageRoll>>, IDisposable
{
    #region Properties

    public GlobalAppSettings AppSettings => GlobalAppSettings.Instance;

    /// <summary>
    /// Gets or sets a unique identifier for this NodeManager instance.
    /// </summary>
    [JsonProperty] public long ID { get; set; } = DateTime.Now.Ticks;

    /// <summary>
    /// Gets or sets the parent V275Manager.
    /// </summary>
    public V275Manager Manager { get; set; }

    /// <summary>
    /// Gets or sets the collection of discovered V275 nodes.
    /// </summary>
    [ObservableProperty] private ObservableCollection<Node> nodes = [];

    /// <summary>
    /// Gets or sets the host address for the V275 system.
    /// </summary>
    [ObservableProperty][property: JsonProperty] private string host;
    partial void OnHostChanged(string value)
    {
        foreach (var nd in Nodes)
            nd.Controller.Host = value;
    }

    /// <summary>
    /// Gets or sets the system port for the V275 system.
    /// </summary>
    [ObservableProperty][property: JsonProperty] private uint systemPort;
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

    /// <summary>
    /// Gets or sets the system port as a string for UI binding and validation.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the username for authentication.
    /// </summary>
    [ObservableProperty][property: JsonProperty] private string username;
    partial void OnUsernameChanged(string value)
    {
        foreach (var nd in Nodes)
            nd.Controller.Username = value;
    }

    /// <summary>
    /// Gets or sets the password for authentication.
    /// </summary>
    [ObservableProperty][property: JsonProperty] private string password;
    partial void OnPasswordChanged(string value)
    {
        foreach (var nd in Nodes)
            nd.Controller.Password = value;
    }

    /// <summary>
    /// Gets or sets the directory for simulator images.
    /// </summary>
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

    /// <summary>
    /// Use the simulation directory or use the API for images.
    /// <see cref="UseSimulationDirectory"/>
    /// </summary>
    [ObservableProperty][property: JsonProperty] private bool useSimulationDirectory = false;
    partial void OnUseSimulationDirectoryChanged(bool value)
    {
        foreach (var nd in Nodes)
            nd.Controller.UseSimulationDirectory = value;
    }

    /// <summary>
    /// Gets or sets the currently selected image roll.
    /// </summary>
    [ObservableProperty] private ImageRoll activeImageRoll;

    private readonly Timer _deviceDiscoveryTimer;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="NodeManager"/> class.
    /// Sets default connection properties and starts the device discovery timer.
    /// </summary>
    public NodeManager()
    {
        Host ??= "127.0.0.1";

        if (SystemPort == 0)
            SystemPort = GetPortNumber();

        Username ??= "admin";
        Password ??= "admin";

        SimulatorImageDirectory ??= GetSimulationDirectory();

        _deviceDiscoveryTimer = new Timer(10000);
        _deviceDiscoveryTimer.Elapsed += OnDeviceDiscoveryTimerElapsed;
        _deviceDiscoveryTimer.AutoReset = true;

        if (GlobalAppSettings.Instance.V275AutoRefreshServers)
        {
            _deviceDiscoveryTimer.Start();
        }

        GlobalAppSettings.Instance.PropertyChanged += Instance_PropertyChanged;
    }

    private void Instance_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GlobalAppSettings.V275AutoRefreshServers))
        {
            if (GlobalAppSettings.Instance.V275AutoRefreshServers)
            {
                if (!_deviceDiscoveryTimer.Enabled)
                {
                    _ = GetDevices();
                }
            }
            else
            {
                if (_deviceDiscoveryTimer.Enabled)
                {
                    _deviceDiscoveryTimer.Stop();
                }
            }
        }
    }

    #endregion

    #region Device Discovery

    /// <summary>
    /// Periodically triggers the device discovery process.
    /// </summary>
    private void OnDeviceDiscoveryTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        _deviceDiscoveryTimer.Interval = 30000; // Subsequent checks are less frequent.
        _ = GetDevices();
    }

    /// <summary>
    /// Asynchronously discovers and populates the list of V275 nodes.
    /// </summary>
    [RelayCommand]
    private async Task GetDevices()
    {
        if (!Application.Current.Dispatcher.CheckAccess())
        {
            _ = Application.Current.Dispatcher.BeginInvoke(GetDevices);
            return;
        }

        Logger.Info("Loading V275 devices.");

        Node system = new(Host, SystemPort, 0, Username, Password, SimulatorImageDirectory, UseSimulationDirectory, ActiveImageRoll);

        if ((await system.Controller.Commands.GetDevices()).Object is Devices dev)
        {
            // Stop the timer since we have successfully connected and retrieved the devices.
            _deviceDiscoveryTimer.Stop();

            List<Node> lst = [];
            foreach (Devices.Node node in dev.nodes)
            {
                if (lst.Any(n => n.Controller.Node.cameraMAC == node.cameraMAC))
                {
                    Logger.Warning($"Duplicate device MAC: {node.cameraMAC}");
                    continue;
                }

                Logger.Debug($"Adding Device MAC: {node.cameraMAC}");

                Node newNode = new(Host, SystemPort, (uint)node.enumeration, Username, Password, SimulatorImageDirectory, UseSimulationDirectory, ActiveImageRoll) { Manager = this };
                newNode.Controller.Initialize();
                lst.Add(newNode);
            }

            var srt = lst.OrderBy(n => n.Controller.NodeNumber).ToList();

            Nodes.Clear();
            if (srt.Count == 0)
            {
                Logger.Warning("No devices found.");
                return;
            }

            foreach (Node node in srt)
                Nodes.Add(node);
        }
        else
        {
            // If we fail to get devices, ensure the timer is running to try again.
            if (!_deviceDiscoveryTimer.Enabled && GlobalAppSettings.Instance.V275AutoRefreshServers)
            {
                _deviceDiscoveryTimer.Start();
            }
            Nodes.Clear();
        }

        // Restore previously selected device if it's found in the new list.
        Node sel = App.Settings.GetValue<Node>($"V275_{nameof(Manager.SelectedDevice)}");
        if (sel != null)
        {
            foreach (Node node in Nodes)
            {
                if (node.Controller.Host == sel.Controller.Host && node.Controller.SystemPort == sel.Controller.SystemPort && node.Controller.NodeNumber == sel.Controller.NodeNumber)
                {
                    Manager.SelectedDevice = node;
                    break;
                }
            }
        }
    }

    #endregion

    #region Registry Helpers

    /// <summary>
    /// Retrieves the V275 service port number from the Windows Registry.
    /// </summary>
    /// <returns>The port number, or a default of 8080 if not found.</returns>
    private static uint GetPortNumber()
    {
        var res = Registry.GetValue("HKEY_LOCAL_MACHINE\\Software\\OMRON\\V275Service", "SystemServerPort", 8080);
        return res == null ? 8080 : Convert.ToUInt32(res);
    }

    /// <summary>
    /// Retrieves the V275 simulation image directory from the Windows Registry.
    /// </summary>
    /// <returns>The path to the simulation directory.</returns>
    private static string GetSimulationDirectory()
    {
        var res = Registry.GetValue("HKEY_LOCAL_MACHINE\\Software\\OMRON\\V275Service", "DataDirectory", "");

        if (string.IsNullOrEmpty((string)res))
            return @"C:\Program Files\V275\data\images\simulation";
        else
            res += @"\images\simulation";

        return res.ToString();
    }

    #endregion

    #region Message Handlers

    /// <summary>
    /// Receives property change messages for the selected <see cref="ImageRoll"/>.
    /// </summary>
    /// <param name="message">The message containing the new ImageRoll value.</param>
    public void Receive(PropertyChangedMessage<ImageRoll> message) =>
        // There is no point in requesting the ActiveImageRoll at init, the user has not selected anything yet.
        ActiveImageRoll = message.NewValue;

    #endregion

    #region Dialogs

    /// <summary>
    /// Gets the default dialog coordinator instance.
    /// </summary>
    public static IDialogCoordinator DialogCoordinator => MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance;

    /// <summary>
    /// Shows a simple dialog with a title, message, and an OK button.
    /// </summary>
    /// <param name="title">The title of the dialog.</param>
    /// <param name="message">The message to display.</param>
    public async Task OkDialog(string title, string message) => _ = await DialogCoordinator.ShowMessageAsync(this, title, message, MessageDialogStyle.Affirmative);

    #endregion

    #region IDisposable

    private bool disposedValue;

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                GlobalAppSettings.Instance.PropertyChanged -= Instance_PropertyChanged;

                // Dispose managed state (managed objects).
                _deviceDiscoveryTimer.Stop();
                _deviceDiscoveryTimer.Elapsed -= OnDeviceDiscoveryTimerElapsed;
                _deviceDiscoveryTimer.Dispose();
            }

            // Free unmanaged resources (unmanaged objects) and override finalizer
            // Set large fields to null
            disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    #endregion
}