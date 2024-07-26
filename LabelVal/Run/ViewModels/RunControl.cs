using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.Results.ViewModels;
using LabelVal.V275.ViewModels;
using Mysqlx.Crud;
using System;
using System.Collections.ObjectModel;

namespace LabelVal.Run.ViewModels;
public partial class RunControl : ObservableObject
{
    public Controller RunController { get;  } = new();

    public ObservableCollection<ImageResultEntry> ImageResultsList { get; private set; }

    public Node SelectedNode { get; private set; }
    public ImageRollEntry SelectedImageRoll { get; private set; }

    private int LoopCount { get; set; }

    /// <summary>
    /// If using this constructor, you must call Update before StartStop.
    /// </summary>
    public RunControl() { }
    
    public RunControl(int loopCount, ObservableCollection<ImageResultEntry> imageResults, ImageRollEntry imageRollEntry, Node node)
    {
        LoopCount = loopCount;
        ImageResultsList = imageResults;
        SelectedNode = node;
        SelectedImageRoll = imageRollEntry;
    }

    public void Update(int loopCount, ObservableCollection<ImageResultEntry> imageResults, ImageRollEntry imageRollEntry, Node node)
    {
        if(RunController.State == RunStates.Running)
        {
            LogDebug("Cannot update RunControl while running");
            return;
        }

        LoopCount = loopCount;
        ImageResultsList = imageResults;
        SelectedNode = node;
        SelectedImageRoll = imageRollEntry;
    }

    [RelayCommand]
    private void StartStop()
    {
        if (RunController == null || SelectedImageRoll == null || SelectedNode == null)
            return;

        if (RunController.State == RunStates.Running)
        {
            LogInfo($"Stopping Run: {SelectedImageRoll.Name}; {LoopCount}");
            RunController.Stop();
        }
        else
        {
            LogInfo($"Starting Run: {SelectedImageRoll.Name}; {LoopCount}");
            RunController.StartAsync(ImageResultsList, SelectedImageRoll, SelectedNode, LoopCount);
        }
    }

    [RelayCommand]
    private void Reset()
    {
        if (RunController == null)
            return;

        RunController.Reset();
    }

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
