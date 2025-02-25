using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.LVS_95xx.ViewModels;
using LabelVal.Results.ViewModels;
using LabelVal.V275.ViewModels;
using LabelVal.V5.ViewModels;
using System;
using System.Collections.ObjectModel;

namespace LabelVal.Run.ViewModels;

public partial class RunManager : ObservableRecipient, IRecipient<PropertyChangedMessage<Node>>, IRecipient<PropertyChangedMessage<Scanner>>, IRecipient<PropertyChangedMessage<Verifier>>, IRecipient<PropertyChangedMessage<ImageRollEntry>>
{
    private ImageResults ImageResults { get; }
    [ObservableProperty] private Node selectedNode;
    [ObservableProperty] private Scanner selectedScanner;
    [ObservableProperty] private Verifier selectedVerifier;
    [ObservableProperty] private ImageRollEntry selectedImageRoll;

    public ObservableCollection<RunControl> RunControllers { get; } = [];
    public RunControl QuickRunController { get; } = new();

    public RunDatabases RunDatabases { get; } = new();
    public RunResults RunResults { get; set; } = new();


    [ObservableProperty] private int loopCount = App.Settings.GetValue(nameof(LoopCount), 1, true);
    partial void OnLoopCountChanged(int value) { App.Settings.SetValue(nameof(LoopCount), value); }

    public RunManager(ImageResults imageResults)
    {
        ImageResults = imageResults;

        RecieveAll();
        IsActive = true;
    }

    private void RecieveAll()
    {
        var ret1 = WeakReferenceMessenger.Default.Send(new RequestMessage<Node>());
        if (ret1.HasReceivedResponse)
            SelectedNode = ret1.Response;

        var ret2 = WeakReferenceMessenger.Default.Send(new RequestMessage<ImageRollEntry>());
        if (ret2.HasReceivedResponse)
            SelectedImageRoll = ret2.Response;

        var ret3 = WeakReferenceMessenger.Default.Send(new RequestMessage<Scanner>());
        if (ret3.HasReceivedResponse)
            SelectedScanner = ret3.Response;

        var ret4 = WeakReferenceMessenger.Default.Send(new RequestMessage<Verifier>());
        if (ret4.HasReceivedResponse)
            SelectedVerifier = ret4.Response;
    }

    [RelayCommand]
    private void StartStop()
    {
        if (QuickRunController == null || SelectedImageRoll == null)
            return;

        if (QuickRunController.RunController.State == RunStates.Running)
        {
            LogInfo($"Stopping Run: {SelectedImageRoll.Name}; {LoopCount}");
            QuickRunController.RunController.Stop();
        }
        else
        {
            LogInfo($"Starting Run: {SelectedImageRoll.Name}; {LoopCount}");
            QuickRunController.Update(LoopCount, ImageResults.ImageResultsList, SelectedImageRoll, SelectedNode, SelectedScanner, SelectedVerifier);
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

    #region Recieve Messages
    public void Receive(PropertyChangedMessage<Node> message) => SelectedNode = message.NewValue;
    public void Receive(PropertyChangedMessage<ImageRollEntry> message) => SelectedImageRoll = message.NewValue;
    public void Receive(PropertyChangedMessage<Scanner> message) => SelectedScanner = message.NewValue;
    public void Receive(PropertyChangedMessage<Verifier> message) => SelectedVerifier = message.NewValue;
    #endregion


    #region Logging
    private void LogInfo(string message) => Logging.lib.Logger.LogInfo(GetType(), message);
#if DEBUG
    private void LogDebug(string message) => Logging.lib.Logger.LogDebug(GetType(), message);
#else
    private void LogDebug(string message) { }
#endif
    private void LogWarning(string message) => Logging.lib.Logger.LogInfo(GetType(), message);
    private void LogError(string message) => Logging.lib.Logger.LogError(GetType(), message);
    private void LogError(Exception ex) => Logging.lib.Logger.LogError(GetType(), ex);
    private void LogError(string message, Exception ex) => Logging.lib.Logger.LogError(GetType(), ex, message);

    #endregion
}

