using BarcodeVerification.lib.Common;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.ImageRolls.ViewModels;
using MahApps.Metro.Controls.Dialogs;
using Newtonsoft.Json;
using System.Threading.Tasks;
using V275_REST_Lib;
using V275_REST_Lib.Models;

namespace LabelVal.V275.ViewModels;

[JsonObject(MemberSerialization.OptIn)]
public partial class Node : ObservableRecipient, IRecipient<PropertyChangedMessage<ImageRoll>>
{
    public NodeManager Manager { get; set; }
    [JsonProperty] public V275_REST_Lib.Controllers.Controller Controller { get; set; }

    [ObservableProperty] private bool loginMonitor;

    private bool systemChangedJob;
    [ObservableProperty] private Jobs.Job selectedJob;
    partial void OnSelectedJobChanged(Jobs.Job value)
    {
        if (value == null || systemChangedJob)
        {
            systemChangedJob = false;
            return;
        }

        _ = App.Current.Dispatcher.BeginInvoke(() => Controller.ChangeJob(value.name));
    }

    [ObservableProperty] private ImageRoll selectedImageRoll;
    partial void OnSelectedImageRollChanged(ImageRoll value) => CheckTemplateName();

    [ObservableProperty] private bool isWrongTemplateName = false;

    public Node(string host, uint systemPort, uint nodeNumber, string username, string password, string dir, ImageRoll imageRollEntry)
    {
        SelectedImageRoll = imageRollEntry;

        Controller = new V275_REST_Lib.Controllers.Controller(host, systemPort, nodeNumber, username, password, dir);
        Controller.PropertyChanged += Controller_PropertyChanged;

        IsActive = true;
    }

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

    public async Task OkDialog(string title, string message) => _ = await DialogCoordinator.Instance.ShowMessageAsync(this, title, message, MessageDialogStyle.Affirmative);

    public void Receive(PropertyChangedMessage<ImageRoll> message) => SelectedImageRoll = message.NewValue;

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

    [RelayCommand] private Task Login() => Controller.Login(LoginMonitor);
    [RelayCommand] private Task Logout() => Controller.Logout();
    [RelayCommand] public Task EnablePrint(bool enable) => Controller.TogglePrint(enable);
    [RelayCommand] private Task RemoveRepeat() => Controller.RemoveRepeat();
    [RelayCommand] private Task<bool> SwitchRun() => Controller.SwitchToRun();
    [RelayCommand] private Task<bool> SwitchEdit() => Controller.SwitchToEdit();

}
