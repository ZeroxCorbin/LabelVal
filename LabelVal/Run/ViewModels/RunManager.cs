using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.Results.ViewModels;
using LabelVal.V275.ViewModels;
using LabelVal.V5.ViewModels;
using System;
using System.Collections.ObjectModel;

namespace LabelVal.Run.ViewModels;

public partial class RunManager : ObservableRecipient, IRecipient<PropertyChangedMessage<Node>>, IRecipient<PropertyChangedMessage<Scanner>>  ,IRecipient<PropertyChangedMessage<ImageRollEntry>>
{
    private ImageResults ImageResults { get; }
    [ObservableProperty] private Node selectedNode;
    [ObservableProperty] private Scanner selectedScanner;
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

        IsActive = true;
        RecieveAll();
    }

    private void RecieveAll()
    {
        RequestMessage<Node> mes1 = new();
        WeakReferenceMessenger.Default.Send(mes1);
        SelectedNode = mes1.Response;

        RequestMessage<ImageRollEntry> mes3 = new();
        WeakReferenceMessenger.Default.Send(mes3);
        SelectedImageRoll = mes3.Response;

        RequestMessage<Scanner> mes4 = new();
        WeakReferenceMessenger.Default.Send(mes4);
        SelectedScanner = mes4.Response;
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
            QuickRunController.Update(LoopCount, ImageResults.ImageResultsList, SelectedImageRoll, SelectedNode, SelectedScanner);
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
    #endregion


    #region Logging
    private readonly Logging.Logger logger = new();
    public void LogInfo(string message) => logger.LogInfo(this.GetType(), message);
    public void LogDebug(string message) => logger.LogDebug(this.GetType(), message);
    public void LogWarning(string message) => logger.LogInfo(this.GetType(), message);
    public void LogError(string message) => logger.LogError(this.GetType(), message);
    public void LogError(Exception ex) => logger.LogError(this.GetType(), ex);
    public void LogError(string message, Exception ex) => logger.LogError(this.GetType(), message, ex);
    #endregion
}

