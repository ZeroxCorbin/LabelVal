using BarcodeVerification.lib.Common;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.ImageRolls.ViewModels;
using MahApps.Metro.Controls.Dialogs;
using Newtonsoft.Json;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using V275_REST_Lib;
using V275_REST_Lib.Models;

namespace LabelVal.V275.ViewModels;

/// <summary>
/// Represents a single V275 system node, managing its state and communication.
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
public partial class Node : ObservableRecipient, IRecipient<PropertyChangedMessage<ImageRoll>>
{
    #region Fields

    private bool systemChangedJob;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the manager for this node.
    /// </summary>
    public NodeManager Manager { get; set; }

    /// <summary>
    /// Gets or sets the REST controller for communicating with the V275 device.
    /// </summary>
    [JsonProperty]
    public V275_REST_Lib.Controllers.Controller Controller { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to monitor the login status.
    /// </summary>
    [ObservableProperty]
    private bool loginMonitor;

    /// <summary>
    /// Gets or sets the currently selected job on the node.
    /// </summary>
    [ObservableProperty]
    private Jobs.Job selectedJob;

    /// <summary>
    /// Gets or sets the currently selected image roll.
    /// </summary>
    [ObservableProperty]
    private ImageRoll selectedImageRoll;

    /// <summary>
    /// Gets or sets a value indicating whether the template name is incorrect.
    /// </summary>
    [ObservableProperty]
    private bool isWrongTemplateName = false;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="Node"/> class.
    /// </summary>
    /// <param name="host">The host address of the node.</param>
    /// <param name="systemPort">The system port number.</param>
    /// <param name="nodeNumber">The node number.</param>
    /// <param name="username">The username for authentication.</param>
    /// <param name="password">The password for authentication.</param>
    /// <param name="dir">The directory path for the controller.</param>
    /// <param name="imageRollEntry">The initial image roll entry.</param>
    public Node(string host, uint systemPort, uint nodeNumber, string username, string password, string dir, ImageRoll imageRollEntry)
    {
        SelectedImageRoll = imageRollEntry;

        Controller = new V275_REST_Lib.Controllers.Controller(host, systemPort, nodeNumber, username, password, dir);
        Controller.PropertyChanged += Controller_PropertyChanged;

        IsActive = true;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Displays an affirmative dialog message.
    /// </summary>
    /// <param name="title">The title of the dialog.</param>
    /// <param name="message">The message to display.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task OkDialog(string title, string message) => _ = await DialogCoordinator.Instance.ShowMessageAsync(this, title, message, MessageDialogStyle.Affirmative);

    /// <summary>
    /// Receives property changed messages for <see cref="ImageRoll"/> and updates the selected image roll.
    /// </summary>
    /// <param name="message">The property changed message.</param>
    public void Receive(PropertyChangedMessage<ImageRoll> message) => SelectedImageRoll = message.NewValue;

    /// <summary>
    /// Checks if the controller's job name matches the selected image roll's template name.
    /// </summary>
    public void CheckTemplateName()
    {
        IsWrongTemplateName = false;

        if (!Controller.IsLoggedIn)
            return;

        if (Controller.JobName == "" || SelectedImageRoll == null)
        {
            IsWrongTemplateName = true;
            return;
        }

        if (SelectedImageRoll.SelectedApplicationStandard != ApplicationStandards.GS1)
        {
            if (Controller.JobName.ToLower().Equals(SelectedImageRoll.Name.ToLower()))
                return;
        }
        else
        {
            if (Controller.JobName.ToLower().StartsWith("gs1"))
                return;
        }

        IsWrongTemplateName = true;
    }

    #endregion

    #region Private Methods

    partial void OnSelectedJobChanged(Jobs.Job value)
    {
        if (value == null || systemChangedJob)
        {
            systemChangedJob = false;
            return;
        }

        _ = Application.Current.Dispatcher.BeginInvoke(() => Controller.ChangeJob(value.name));
    }

    partial void OnSelectedImageRollChanged(ImageRoll value) => CheckTemplateName();

    private void Controller_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(V275_REST_Lib.Controllers.Controller.JobName))
        {
            if (string.IsNullOrEmpty(Controller.JobName))
            {
                SelectedJob = null;
                CheckTemplateName();
                return;
            }

            if (Controller.JobName != SelectedJob?.name)
                systemChangedJob = true;

            SelectedJob = Controller.Jobs.jobs.FirstOrDefault((e) => e.name == Controller.JobName);
            CheckTemplateName();
        }
    }

    #endregion

    #region Commands

    /// <summary>
    /// Logs into the V275 device.
    /// </summary>
    [RelayCommand]
    private Task Login() => Controller.Login(LoginMonitor);

    /// <summary>
    /// Logs out of the V275 device.
    /// </summary>
    [RelayCommand]
    private Task Logout() => Controller.Logout();

    /// <summary>
    /// Enables or disables printing on the V275 device.
    /// </summary>
    [RelayCommand]
    public Task EnablePrint(bool enable) => Controller.TogglePrint(enable);

    /// <summary>
    /// Removes a repeat job from the V275 device.
    /// </summary>
    [RelayCommand]
    private Task RemoveRepeat() => Controller.RemoveRepeat();

    /// <summary>
    /// Switches the V275 device to run mode.
    /// </summary>
    [RelayCommand]
    private Task<bool> SwitchRun() => Controller.SwitchToRun();

    /// <summary>
    /// Switches the V275 device to edit mode.
    /// </summary>
    [RelayCommand]
    private Task<bool> SwitchEdit() => Controller.SwitchToEdit();

    #endregion
}