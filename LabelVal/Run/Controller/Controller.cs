using CommunityToolkit.Mvvm.ComponentModel;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.Run.Databases;
using LabelVal.V275.ViewModels;
using LabelVal.V5.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace LabelVal.Run;

public partial class Controller : ObservableObject
{
    [ObservableProperty] private RunStates state;
    [ObservableProperty] private RunStates requestedState;

    public ObservableCollection<Results.ViewModels.ImageResultEntry> ImageResultEntries { get; private set; }

    private RunDatabase RunDatabase { get; set; }
    public RunEntry RunEntry { get; private set; }
    private string RunUID => RunEntry.UID;

    public ImageRollEntry ImageRollEntry { get; private set; }

    public Node V275 { get; private set; }
    private bool HasV275 => V275 != null;

    public Scanner V5 { get; private set; }
    private bool HasV5 => V5 != null;

    public int LoopCount { get; private set; }
    public int CurrentLoopCount { get; private set; }
    public int CurrentLabelCount { get; private set; }

    public bool StartAsync(ObservableCollection<Results.ViewModels.ImageResultEntry> imageResultEntries, ImageRollEntry imageRollEntry,
        Node v275,
        Scanner v5,
        int loopCount)
    {
        ImageResultEntries = imageResultEntries;
        ImageRollEntry = imageRollEntry;

        V275 = v275;
        V5 = v5;

        if (!HasV275 && !HasV5)
        {
            LogError("Run: No device selected for run.");
            return false;
        }

        if(HasV275 && !V275.IsLoggedIn_Control)
        {
            LogError("Run: V275, Not logged in.");
            return false;
        }

        if (HasV5 && !V5.IsConnected)
        {
            LogError("Run: V5, Not connected.");
            return false;
        }

        LoopCount = loopCount;

        RunEntry = new RunEntry(RunDatabase, ImageRollEntry, LoopCount);

        if (!OpenDatabase() || !UpdateRunEntry())
            return false;

        Task.Run(Start);

        return true;
    }

    private bool OpenDatabase() => (RunDatabase = new RunDatabase().Open($"{App.RunsRoot}\\RunResults.sqlite")) != null;
    private bool UpdateRunEntry() => RunDatabase.InsertOrReplace(RunEntry) > 0;
    private bool RemoveRunEntry() => RunDatabase.DeleteLedgerEntry(RunEntry.UID) > 0;
    private bool ExistRunEntry() => RunDatabase.ExistsLedgerEntry(RunEntry.UID);

    private async Task<RunStates> Start()
    {
        CurrentLabelCount = 0;

        RequestedState = UpdateRunState(RunStates.Running);

        if (HasV275)
        {
            LogInfo("Run: V275, Pre-Run");

            if (await PreRunV275() != RunStates.Running)
                return State;
        }

        if (HasV5)
        {
            LogInfo("Run: V5, Pre-Run");

            if (await PreRunV5() != RunStates.Running)
                return State;
        }

        LogInfo($"Run: Loop Count {LoopCount.ToString()}");

        int wasLoop = 0;
        for (int i = 0; i < LoopCount; i++)
        {
            foreach (Results.ViewModels.ImageResultEntry ire in ImageResultEntries)
            {
                var useV275 = HasV275 && ire.V275StoredSectors.Count > 0;
                if (ire.V275StoredSectors.Count == 0)
                    LogInfo("Run: V275, No sectors to process.");

                var useV5 = HasV5 && ire.V5StoredSectors.Count > 0;
                if (ire.V5StoredSectors.Count == 0)
                    LogInfo("Run: V5, No sectors to process.");

                if (!useV275 && !useV5)
                    continue;

                CurrentLoopCount = i + 1;
                if (CurrentLoopCount != wasLoop)
                {
                    if (useV275)
                        if (await PreLoopV275(ire) != RunStates.Running)
                            return State;

                    if (useV5)
                        if (await PreLoopV5(ire) != RunStates.Running)
                            return State;

                    wasLoop = CurrentLoopCount;
                    LogInfo($"Run: Starting Loop {CurrentLoopCount.ToString()}");
                }

                if (RequestedState != RunStates.Running)
                    return UpdateRunState(RequestedState);

                //This must occur before the print so it is added to the V275 image
                CurrentLabelCount++;

                if(useV275)
                    if(ProcessV275(ire) != RunStates.Running)
                        return State;

                if (useV5)
                    if (await ProcessV5(ire) != RunStates.Running)
                        return State;

                Results.Databases.StoredImageResultGroup stored = ire.GetStoredImageResultGroup(RunUID);
                Results.Databases.CurrentImageResultGroup current = ire.GetCurrentImageResultGroup(RunUID);

                if (stored == null || current == null)
                {
                    LogError("Run: Failed to get stored or current image result group.");
                    return UpdateRunState(RunStates.Error);
                }

                stored.Order = CurrentLabelCount;
                stored.Loop = CurrentLoopCount;
                stored.LoopCount = LoopCount;

                current.Order = CurrentLabelCount;
                current.Loop = CurrentLoopCount;
                current.LoopCount = LoopCount;

                RunDatabase.InsertOrReplace(stored);
                RunDatabase.InsertOrReplace(current);
            }

            RunEntry.CompletedLoops = CurrentLoopCount;
        }

        RunEntry.EndTime = DateTime.Now.Ticks;

        return UpdateRunState(RunStates.Complete);
    }

    private async Task<RunStates> PreRunV275()
    {
        if (!await V275.Controller.SwitchToEdit())
        {
            LogError("Run: V275, Failed to switch to edit mode.");
            return UpdateRunState(RunStates.Error);
        }
        return State;
    }

    private async Task<RunStates> PreLoopV275(Results.ViewModels.ImageResultEntry ire)
    {
        //If running a non-GS1 label then this will reset the match to file and sequences.
        //If running a GS1 label then edit mode is required.
        if (HasSequencing(ire))
        {
            //Switch to edit to allow the Match files and Sequencing to reset.
            if (!await V275.Controller.SwitchToEdit())
            {
                LogError("Run: V275, Failed to switch to edit mode.");
                return UpdateRunState(RunStates.Error);
            }
        }

        if (!ImageRollEntry.WriteSectorsBeforeProcess)
        {
            if (!await V275.Controller.SwitchToRun())
            {
                LogError("Run: V275, Failed to switch to run mode.");
                return UpdateRunState(RunStates.Error);
            }
        }
        return State;
    }

    private RunStates ProcessV275(Results.ViewModels.ImageResultEntry ire)
    {
        //Start the V275 processing the image.
        if (V275.IsSimulator)
            ire.V275ProcessCommand.Execute("v275Stored");
        else
            ire.V275ProcessCommand.Execute("source");

        //Wait for the V275 to finish processing the image or fault.
        //DateTime start = DateTime.Now;
        //while (ire.IsV275Working)
        //{
        //    if (RequestedState != RunStates.Running)
        //        return UpdateRunState(RequestedState);

        //    if (DateTime.Now - start > TimeSpan.FromMilliseconds(10000))
        //    {
        //        LogError("Run: Timeout waiting for results.");
        //        return UpdateRunState(RunStates.Error);
        //    }

        //    if (ire.IsV275Faulted)
        //    {
        //        LogError("Run: Error when interacting with V275.");
        //        return UpdateRunState(RunStates.Error);
        //    }

        //    Thread.Sleep(1);
        //};

        return State;
    }

    private async Task<RunStates> PreRunV5()
    {
        if (!await V5.Controller.SwitchToEdit())
        {
            LogError("Run: V5, Failed to switch to edit mode.");
            return UpdateRunState(RunStates.Error);
        }
        return State;
    }
    private async Task<RunStates> PreLoopV5(Results.ViewModels.ImageResultEntry ire)
    {
        return State;
    }
    private async Task<RunStates> ProcessV5(Results.ViewModels.ImageResultEntry ire)
    {
        V5_REST_Lib.Models.Config config = null;
        if (V5.IsSimulator)
        {
            
            config = await V5.Controller.ChangeImage(ire.V5ResultRow.Stored.ImageBytes, true);

            if (config == null)
            {
                LogError("Could not change the image.");
                return UpdateRunState(RunStates.Error);
            }
        }
        else
        {
            var cfgRes = await V5.Controller.GetConfig();

            if (!cfgRes.OK)
            {
                LogError("Could not get the configuration.");
                return UpdateRunState(RunStates.Error);
            }

            config = (V5_REST_Lib.Models.Config)cfgRes.Object;
        }

        var res = await V5.Controller.Trigger_Wait_Return(true);

        if(!res.OK)
        {
            LogError("Could not trigger the scanner.");
            return UpdateRunState(RunStates.Error);
        }

        App.Current.Dispatcher.Invoke(() => ire.V5ProcessResults(res, config));

        return State;
    }

    private static bool HasSequencing(Results.ViewModels.ImageResultEntry label)
    {
        V275_REST_lib.Models.Job template = JsonConvert.DeserializeObject<V275_REST_lib.Models.Job>(label.V275ResultRow.Template);

        foreach (V275_REST_lib.Models.Job.Sector sect in template.sectors)
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
        if (state is RunStates.Complete or RunStates.Stopped or RunStates.Error)
        {
            V5?.Controller?.FTPClient?.Disconnect();

            _ = CurrentLabelCount != 0 ? UpdateRunEntry() : ExistRunEntry() && RemoveRunEntry();
            RunDatabase?.Close();
        }

        LogInfo($"Run: State Changed to {state.ToString()}");
        State = state;
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
