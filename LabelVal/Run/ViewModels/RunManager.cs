using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.L95.ViewModels;
using LabelVal.Results.ViewModels;
using LabelVal.V275.ViewModels;
using LabelVal.V5.ViewModels;
using System;
using System.Collections.ObjectModel;

namespace LabelVal.Run.ViewModels;

public partial class RunManager : ObservableRecipient, IRecipient<PropertyChangedMessage<Node>>, IRecipient<PropertyChangedMessage<Scanner>>, IRecipient<PropertyChangedMessage<Verifier>>, IRecipient<PropertyChangedMessage<ImageRoll>>
{
    private ResultssManager Resultss { get; }
    [ObservableProperty] private Node _selectedV275Node;
    [ObservableProperty] private Scanner _selectedV5;
    [ObservableProperty] private Verifier _selectedL95;
    [ObservableProperty] private ImageRoll _selectedImageRoll;

    public ObservableCollection<RunControl> RunControllers { get; } = [];
    public RunControl QuickRunController { get; } = new();

    public RunDatabases RunDatabases { get; } = new();
    public RunResults RunResults { get; set; } = new();


    [ObservableProperty] private int _loopCount = App.Settings.GetValue(nameof(LoopCount), 1, true);
    partial void OnLoopCountChanged(int value) { App.Settings.SetValue(nameof(LoopCount), value); }

    public RunManager(ResultssManager imageResults)
    {
        Resultss = imageResults;

        ReceiveAll();
        IsActive = true;
    }

    private void ReceiveAll()
    {
        var ret1 = WeakReferenceMessenger.Default.Send(new RequestMessage<Node>());
        if (ret1.HasReceivedResponse)
            SelectedV275Node = ret1.Response;

        var ret2 = WeakReferenceMessenger.Default.Send(new RequestMessage<ImageRoll>());
        if (ret2.HasReceivedResponse)
            SelectedImageRoll = ret2.Response;

        var ret3 = WeakReferenceMessenger.Default.Send(new RequestMessage<Scanner>());
        if (ret3.HasReceivedResponse)
            SelectedV5 = ret3.Response;

        var ret4 = WeakReferenceMessenger.Default.Send(new RequestMessage<Verifier>());
        if (ret4.HasReceivedResponse)
            SelectedL95 = ret4.Response;
    }

    [RelayCommand]
    private void StartStop()
    {
        if (QuickRunController == null || SelectedImageRoll == null)
            return;

        if (QuickRunController.RunController.State == RunStates.Running)
        {
            Logger.Info($"Stopping Run: {SelectedImageRoll.Name}; {LoopCount}");
            QuickRunController.RunController.Stop();
        }
        else
        {
            Logger.Info($"Starting Run: {SelectedImageRoll.Name}; {LoopCount}");
            QuickRunController.Update(LoopCount, Resultss.ResultssEntries, SelectedImageRoll, SelectedV275Node, SelectedV5, SelectedL95);
            QuickRunController.StartStopCommand.Execute(null);
        }
    }

    [RelayCommand]
    private void Reset()
    {
        if (QuickRunController == null)
            return;

        QuickRunController.RunController.Reset();
    }

    #region Receive Messages
    public void Receive(PropertyChangedMessage<Node> message) => SelectedV275Node = message.NewValue;
    public void Receive(PropertyChangedMessage<ImageRoll> message) => SelectedImageRoll = message.NewValue;
    public void Receive(PropertyChangedMessage<Scanner> message) => SelectedV5 = message.NewValue;
    public void Receive(PropertyChangedMessage<Verifier> message) => SelectedL95 = message.NewValue;
    #endregion

}

