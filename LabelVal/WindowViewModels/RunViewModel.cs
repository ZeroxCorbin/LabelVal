using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.Messages;
using LabelVal.Run;
using LabelVal.V275.ViewModels;
using System;

namespace LabelVal.WindowViewModels;
public partial class RunViewModel : ObservableRecipient//, IRecipient<PropertyChangedMessage<Node>>
{
    //public Controller RunController { get; set; } = new Controller();

    //[ObservableProperty] private Node selectedNode;

    //[ObservableProperty] private Controller.RunStates state = Controller.RunStates.IDLE;

    //[ObservableProperty] private int loopCount = App.Settings.GetValue(nameof(LoopCount), 1, true);
    //partial void OnLoopCountChanged(int value) { App.Settings.SetValue(nameof(LoopCount), value); }

    //public RunViewModel()
    //{
    //    RunController.RunStateChange += RunController_RunStateChange;
    //}

    //public void Receive(PropertyChangedMessage<Node> message)
    //{
    //    SelectedNode = message.NewValue;
    //}

    //[RelayCommand]
    //private void StartRun()
    //{
    //    SendControlMessage("StartRun");
    //}

    //public void StartRunRequest()
    //{
    //    UpdateStatus($"Starting Run: {RunController.SelectedImageRoll.Name}; {RunController.LoopCount}");

    //    RunController.StartAsync();
    //}

    //[RelayCommand]
    //private void PauseRun()
    //{
    //    if (RunController == null)
    //        return;

    //    if (RunController.State != Controller.RunStates.PAUSED)
    //        RunController.Pause();
    //    else
    //        RunController.Resume();
    //}

    //[RelayCommand]
    //private void StopRun()
    //{
    //    if (RunController == null)
    //        return;

    //    RunController.Stop();
    //}
    //private void RunController_RunStateChange(Controller.RunStates state)
    //{
    //    switch (state)
    //    {
    //        case Controller.RunStates.RUNNING:
    //            State = state;
    //            break;
    //        case Controller.RunStates.PAUSED:
    //            State = state;
    //            break;
    //        case Controller.RunStates.STOPPED:
    //            State = Controller.RunStates.IDLE;
    //            break;
    //        default:
    //            State = Controller.RunStates.IDLE;
    //            break;
    //    }
    //}

}
