using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.Main.ViewModels;
using Org.BouncyCastle.Asn1.GM;
using System.Collections.ObjectModel;

namespace LabelVal.V275.ViewModels;

/// <summary>
/// Manages the collection of V275 verifier systems, represented by <see cref="NodeManager"/> instances.
/// </summary>
public partial class V275Manager : ObservableRecipient
{
    #region Properties

    /// <summary>
    /// Gets the global application settings instance.
    /// </summary>
    public GlobalAppSettings AppSettings => GlobalAppSettings.Instance;

    /// <summary>
    /// Gets the collection of configured V275 device managers.
    /// </summary>
    public ObservableCollection<NodeManager> Devices { get; } = App.Settings.GetValue($"V275_{nameof(Devices)}", new ObservableCollection<NodeManager>(), true);

    /// <summary>
    /// Gets or sets the currently selected V275 device node across all managers.
    /// <see cref="SelectedDevice"/>
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedRecipients] 
    private Node selectedDevice;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="V275Manager"/> class.
    /// Loads saved devices, initializes them, and registers for messages to provide the selected device.
    /// </summary>
    public V275Manager()
    {
        // Restore the last selected device if needed.
        // Node sel = App.Settings.GetValue<Node>($"V275_{nameof(SelectedDevice)}");

        foreach (var dev in Devices)
        {
            dev.Manager = this;
            dev.GetDevicesCommand.Execute(null);
        }

        // Register to reply with the currently selected device when requested.
        WeakReferenceMessenger.Default.Register<RequestMessage<Node>>( this,
            (recipient, message) =>
            {
                message.Reply(SelectedDevice);
            });
    }

    #endregion

    #region Partial Methods

    /// <summary>
    /// Saves the selected device to application settings when it changes.
    /// </summary>
    /// <param name="value">The newly selected device node.</param>
    partial void OnSelectedDeviceChanged(Node value) 
    { 
        if (value != null) App.Settings.SetValue($"V275_{nameof(SelectedDevice)}", value); 
    }

    #endregion

    #region Commands

    /// <summary>
    /// Adds a new V275 device manager to the collection and saves the updated collection.
    /// </summary>
    [RelayCommand]
    private void Add()
    {
        var nm = new NodeManager { Manager = this };
        nm.GetDevicesCommand.Execute(null);
        Devices.Add(nm);
        Save();
    }
    
    /// <summary>
    /// Deletes the specified V275 device manager from the collection and saves the updated collection.
    /// </summary>
    /// <param name="nodeMan">The node manager to remove.</param>
    [RelayCommand]
    private void Delete(NodeManager nodeMan)
    {
        Devices.Remove(nodeMan);
        Save();
    }
    
    /// <summary>
    /// Saves the current collection of device managers to the application settings.
    /// </summary>
    [RelayCommand] 
    private void Save() => App.Settings.SetValue($"V275_{nameof(Devices)}", Devices);

    #endregion
}
