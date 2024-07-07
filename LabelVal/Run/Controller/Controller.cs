using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.ORM_Test;
using LabelVal.Run.Databases;
using LabelVal.V275.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace LabelVal.Run;

public partial class Controller : ObservableObject
{
    public delegate void RunStateChangeDeletgate(RunStates state);
    public event RunStateChangeDeletgate RunStateChange;

    [ObservableProperty] private RunStates state;
    [ObservableProperty] private RunStates requestedState;

    public ObservableCollection<Results.ViewModels.ImageResultEntry> ImageResultEntries { get; private set; }

    private RunDatabase RunDatabase { get; set; }
    public RunEntry RunEntry { get; private set; }
    private string RunUID => RunEntry.UID;

    public Node V275Node { get; private set; }
    public ImageRollEntry ImageRollEntry { get; private set; }

    public int LoopCount { get; private set; }
    public int CurrentLoopCount { get; private set; }
    public int CurrentLabelCount { get; private set; }


    public bool StartAsync(ObservableCollection<Results.ViewModels.ImageResultEntry> imageResultEntries, ImageRollEntry imageRollEntry, Node v275Node, int loopCount)
    {
        ImageRollEntry = imageRollEntry;
        V275Node = v275Node;

        ImageResultEntries = imageResultEntries;

        LoopCount = loopCount;

        RunEntry = new RunEntry(ImageRollEntry.SelectedStandard, v275Node.Product.part, v275Node.Details.cameraMAC, LoopCount);

        if(!OpenDatabase() || !UpdateRunEntry() )
            return false;

        Task.Run(Start);

        return true;
    }

    private bool OpenDatabase() => (RunDatabase = new RunDatabase().Open($"{App.RunsRoot}\\RunResults.sqlite")) != null;
    private bool UpdateRunEntry() => RunDatabase.InsertOrReplace(RunEntry) > 0;
    private bool RemoveRunEntry() => RunDatabase.DeleteLedgerEntry(RunEntry.UID) > 0;

    private async Task<RunStates> Start()
    {
        CurrentLabelCount = 0;

        if (!await V275Node.Connection.SwitchToEdit())
        {
            LogError("Failed to switch to edit mode.");
            return UpdateRunState(RunStates.Error);
        }

        RequestedState = UpdateRunState(RunStates.Running);
        LogInfo($"Run: Loop Count {LoopCount}");

        var wasLoop = 0;
        for (var i = 0; i < LoopCount; i++)
        {
            foreach (var ire in ImageResultEntries)
            {
                if (ire.V275StoredSectors.Count == 0)
                {
                    LogDebug("Run: No sectors to process.");
                    continue;
                }

                CurrentLoopCount = i + 1;
                if (CurrentLoopCount != wasLoop)
                {
                    //If running a non-GS1 label then this will reset the match to file and sequences.
                    //If running a GS1 label then edit mode is required.
                    if (HasSequencing(ire))
                    {
                        //Switch to edit to allow the Match files and Sequencing to reset.
                        if (!await V275Node.Connection.SwitchToEdit())
                        {
                            LogError("Run: Failed to switch to edit mode.");
                            return UpdateRunState(RunStates.Error);
                        }
                    }
                    else if (ImageResultEntries.Count == 1)
                        CurrentLoopCount = 1;

                    wasLoop = CurrentLoopCount;
                    LogInfo($"Run: Starting Loop {CurrentLoopCount}");
                }

                if (!ImageRollEntry.WriteSectorsBeforeProcess)
                {
                    if (!await V275Node.Connection.SwitchToRun())
                    {
                        LogError("Failed to switch to run mode.");
                        return UpdateRunState(RunStates.Error);
                    }
                }

                if (RequestedState != RunStates.Running)
                    return UpdateRunState(RequestedState);

                //This must occur before the print so it is added to the image
                CurrentLabelCount++;

                //Start the V275 processing the image.
                ire.V275ProcessCommand.Execute(null);

                //Wait for the V275 to finish processing the image or fault.
                var start = DateTime.Now;
                while (ire.IsV275Working)
                {
                    if (RequestedState != RunStates.Running)
                        return UpdateRunState(RequestedState);

                    if (DateTime.Now - start > TimeSpan.FromMilliseconds(10000))
                    {
                        LogError("Run: Timeout waiting for results.");
                        return UpdateRunState(RunStates.Error);
                    }

                    if(ire.IsV275Faulted)
                    {
                        LogError("Run: Error when interacting with V275.");
                        return UpdateRunState(RunStates.Error);
                    }

                    Thread.Sleep(1);
                };

                var stored = ire.GetStoredImageResultGroup(RunUID);
                var current = ire.GetCurrentImageResultGroup(RunUID);

                if (stored == null || current == null)
                {
                    LogError("Run: Failed to get stored or current image result group.");
                    return UpdateRunState(RunStates.Error);
                }

                RunDatabase.InsertOrReplace(stored);
                RunDatabase.InsertOrReplace(current);
            }
        }

        RunDatabase.Close();
        return UpdateRunState(RunStates.Complete);
    }

    private static bool HasSequencing(Results.ViewModels.ImageResultEntry label)
    {
        var template = JsonConvert.DeserializeObject<V275_REST_lib.Models.Job>(label.V275ResultRow.Template);

        foreach (var sect in template.sectors)
        {
            if (sect.matchSettings != null)
                if (sect.matchSettings.matchMode is >= 3 and <= 6)
                    return true;
        }
        return false;
    }

    public void Stop() => RequestedState = RunStates.Stopped;
    public void Reset()
    {
        if (State == RunStates.Running)
            return;

        RunDatabase?.Close();
    }

    private RunStates UpdateRunState(RunStates state)
    {
        if (state == RunStates.Error)
            _ = CurrentLabelCount != 0 ? UpdateRunEntry() : RemoveRunEntry();

        LogInfo($"Run: State Changed to {state}");

        RunStateChange?.Invoke(State = state);
        RunEntry.State = state;
        return state;
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
