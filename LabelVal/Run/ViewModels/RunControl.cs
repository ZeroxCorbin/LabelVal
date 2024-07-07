using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.Mvvm.Messaging;
using LabelVal.V275.ViewModels;
using System;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.Results.Databases;

namespace LabelVal.Run.ViewModels;
public partial class RunControl : ObservableRecipient, IRecipient<PropertyChangedMessage<Node>>, IRecipient<PropertyChangedMessage<ImageRollEntry>>
{
    public Controller RunController { get; } = new();

    private Results.ViewModels.ImageResults ImageResults { get; }

    [ObservableProperty] private Node selectedNode;
    [ObservableProperty] private ImageRollEntry imageRollEntry;

    [ObservableProperty] private int loopCount = App.Settings.GetValue(nameof(LoopCount), 1, true);
    partial void OnLoopCountChanged(int value) { App.Settings.SetValue(nameof(LoopCount), value); }

    public RunControl(Results.ViewModels.ImageResults imageResults)
    {
        ImageResults = imageResults;
        IsActive = true;
    }
    [RelayCommand]
    private void StartStop()
    {
        if (RunController == null || ImageRollEntry == null || SelectedNode == null)
            return;

        if (RunController.State == RunStates.Running)
        {
            LogInfo($"Stopping Run: {ImageRollEntry.Name}; {LoopCount}");
            RunController.Stop();
        }
        else
        {
            LogInfo($"Starting Run: {ImageRollEntry.Name}; {LoopCount}");
            RunController.StartAsync(ImageResults.ImageResultsList, ImageRollEntry, SelectedNode, LoopCount);
        }
    }

    [RelayCommand]
    private void Reset()
    {
        if (RunController == null)
            return;

        RunController.Reset();
    }

    #region Recieve Messages
    public void Receive(PropertyChangedMessage<Node> message) => SelectedNode = message.NewValue;
    public void Receive(PropertyChangedMessage<ImageRollEntry> message) => ImageRollEntry = message.NewValue;
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
