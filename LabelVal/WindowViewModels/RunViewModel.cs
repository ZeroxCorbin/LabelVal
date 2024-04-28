using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using LabelVal.Messages;
using LabelVal.Run;
using MaterialDesignThemes.Wpf.Converters.CircularProgressBar;

namespace LabelVal.WindowViewModels;
public partial class RunViewModel : ObservableRecipient, IRecipient<NodeMessages.SelectedNodeChanged>
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    public Controller RunController { get; set; } = new Controller();

    [ObservableProperty] private V275Node selectedNode;


    [ObservableProperty] private Controller.RunStates state = Controller.RunStates.IDLE;

    [ObservableProperty] private int loopCount = App.Settings.GetValue(nameof(LoopCount), 1, true);
    partial void OnLoopCountChanged(int value) { App.Settings.SetValue(nameof(LoopCount), value); }

    public RunViewModel() => RunController.RunStateChange += RunController_RunStateChange;

    public void Receive(NodeMessages.SelectedNodeChanged message) => SelectedNode = message.Value;

    [RelayCommand]
    private void StartRun()
    {
        SendStatusMessage("StartRun", SystemMessages.StatusMessageType.Control);
    }

    public void StartRunRequest()
    {
        Logger.Info($"Starting Run: {RunController.GradingStandard.Name}; {RunController.LoopCount}");

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

    private void SendStatusMessage(string message, SystemMessages.StatusMessageType type) => WeakReferenceMessenger.Default.Send(new SystemMessages.StatusMessage(this, type, message));

}
