using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.Messages;
using LabelVal.Run;
using LabelVal.V275.ViewModels;
using System;

namespace LabelVal.WindowViewModels;
public partial class RunViewModel : ObservableRecipient, IRecipient<PropertyChangedMessage<Node>>
{
    public Controller RunController { get; set; } = new Controller();

    [ObservableProperty] private Node selectedNode;

    [ObservableProperty] private Controller.RunStates state = Controller.RunStates.IDLE;

    [ObservableProperty] private int loopCount = App.Settings.GetValue(nameof(LoopCount), 1, true);
    partial void OnLoopCountChanged(int value) { App.Settings.SetValue(nameof(LoopCount), value); }

    public RunViewModel()
    {
        RunController.RunStateChange += RunController_RunStateChange;
    }

    public void Receive(PropertyChangedMessage<Node> message)
    {
        SelectedNode = message.NewValue;
    }

    [RelayCommand]
    private void StartRun()
    {
        SendControlMessage("StartRun");
    }

    public void StartRunRequest()
    {
        UpdateStatus($"Starting Run: {RunController.SelectedImageRoll.Name}; {RunController.LoopCount}");

        RunController.StartAsync();
    }

    [RelayCommand]
    private void PauseRun()
    {
        if (RunController == null)
            return;

        if (RunController.State != Controller.RunStates.PAUSED)
            RunController.Pause();
        else
            RunController.Resume();
    }

    [RelayCommand]
    private void StopRun()
    {
        if (RunController == null)
            return;

        RunController.Stop();
    }
    private void RunController_RunStateChange(Controller.RunStates state)
    {
        switch (state)
        {
            case Controller.RunStates.RUNNING:
                State = state;
                break;
            case Controller.RunStates.PAUSED:
                State = state;
                break;
            case Controller.RunStates.STOPPED:
                State = Controller.RunStates.IDLE;
                break;
            default:
                State = Controller.RunStates.IDLE;
                break;
        }
    }

    #region Logging & Status Messages

    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    private void UpdateStatus(string message)
    {
        UpdateStatus(message);
        _ = Messenger.Send(new SystemMessages.StatusMessage(message, SystemMessages.StatusMessageType.Info));
    }
    private void UpdateStatus(string message, SystemMessages.StatusMessageType type)
    {
        switch (type)
        {
            case SystemMessages.StatusMessageType.Info:
                UpdateStatus(message);
                break;
            case SystemMessages.StatusMessageType.Debug:
                Logger.Debug(message);
                break;
            case SystemMessages.StatusMessageType.Warning:
                Logger.Warn(message);
                break;
            case SystemMessages.StatusMessageType.Error:
                Logger.Error(message);
                break;
            default:
                UpdateStatus(message);
                break;
        }
        _ = Messenger.Send(new SystemMessages.StatusMessage(message, type));
    }
    private void UpdateStatus(Exception ex)
    {
        Logger.Error(ex);
        _ = Messenger.Send(new SystemMessages.StatusMessage(ex));
    }
    private void UpdateStatus(string message, Exception ex)
    {
        Logger.Error(ex);
        _ = Messenger.Send(new SystemMessages.StatusMessage(ex));
    }

    private void SendControlMessage(string message)
    {
        _ = Messenger.Send(new SystemMessages.ControlMessage(this, message));
    }

    #endregion

}
