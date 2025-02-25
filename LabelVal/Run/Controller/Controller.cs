using CommunityToolkit.Mvvm.ComponentModel;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.Results.Databases;
using LabelVal.Run.Databases;
using LabelVal.V275.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace LabelVal.Run.Controller;

public partial class Controller : ObservableObject
{
    [ObservableProperty] private RunStates state;
    [ObservableProperty] private RunStates requestedState;
    [ObservableProperty] private bool updateUI = true;
    public ObservableCollection<Results.ViewModels.ImageResultEntry> ImageResultEntries { get; private set; }

    private ResultsDatabase ResultsDatabase { get; set; }
    public RunEntry RunEntry { get; private set; }
    private string RunUID => RunEntry.UID;

    public ImageRollEntry ImageRollEntry { get; private set; }

    [ObservableProperty] private Node v275;
    partial void OnV275Changed(Node value)
    {
        HasV275 = value != null;
        UseV275 = false;
    }
    [ObservableProperty] private bool hasV275;
    [ObservableProperty] private bool useV275;

    [ObservableProperty] private V5_REST_Lib.Controller v5;
    partial void OnV5Changed(V5_REST_Lib.Controller value)
    {
        HasV5 = value != null;
        UseV5 = false;
    }
    [ObservableProperty] private bool hasV5;
    [ObservableProperty] private bool useV5;

    [ObservableProperty] private Lvs95xx.lib.Core.Controllers.Controller l95;
    partial void OnL95Changed(Lvs95xx.lib.Core.Controllers.Controller value)
    {
        HasL95 = value != null;
        UseL95 = false;
    }
    [ObservableProperty] private bool hasL95;
    [ObservableProperty] private bool useL95;


    public int LoopCount { get; private set; }
    public int CurrentLoopCount { get; private set; }
    public int CurrentLabelCount { get; private set; }

    public bool StartAsync(ObservableCollection<Results.ViewModels.ImageResultEntry> imageResultEntries, ImageRollEntry imageRollEntry,
        Node v275,
        V5_REST_Lib.Controller v5,
Lvs95xx.lib.Core.Controllers.Controller l95,
        int loopCount)
    {
        ImageResultEntries = imageResultEntries;
        ImageRollEntry = imageRollEntry;

        V275 = v275;
        V5 = v5;
        L95 = l95;

        if (!HasV275 && !HasV5 && !HasL95)
        {
            LogError("Run: No device selected for run.");
            return false;
        }

        if (HasV275 && !V275.Controller.IsLoggedIn_Control)
        {
            LogError("Run: V275, Not logged in.");
            return false;
        }

        if (HasV5 && !V5.IsConnected)
        {
            LogError("Run: V5, Not connected.");
            return false;
        }

        if ((HasL95 && !L95.IsConnected) || L95.ProcessState != Watchers.lib.Process.Win32_ProcessWatcherProcessState.Running)
        {
            LogError("Run: Lvs95xx, Not connected.");
            return false;
        }

        LoopCount = loopCount;
        CurrentLoopCount = 0;

        RunEntry = new RunEntry();

        if (!OpenDatabase() || !UpdateRunEntry())
            return false;

        _ = Task.Run(Start);

        return true;
    }

    private bool OpenDatabase() => (ResultsDatabase = new ResultsDatabase().Open($"{App.RunsRoot}\\RunResults.sqlite")) != null;
    private bool UpdateRunEntry()
    {
        RunEntry.GradingStandard = ImageRollEntry.SelectedStandard;
        RunEntry.Gs1TableName = ImageRollEntry.SelectedGS1Table;
        RunEntry.DesiredLoops = LoopCount;

        RunEntry.CompletedLoops = CurrentLoopCount;
        RunEntry.State = State;
        
        if(State == RunStates.Complete)
            RunEntry.EndTime = DateTime.Now.Ticks;

        return ResultsDatabase.InsertOrReplace(RunEntry) > 0;
    }
    private bool RemoveRunEntry() => ResultsDatabase.DeleteLedgerEntry(RunEntry.UID) > 0;
    private bool ExistRunEntry() => ResultsDatabase.ExistsLedgerEntry(RunEntry.UID);

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

        if (HasL95)
        {
            LogInfo("Run: Lvs95xx, Pre-Run");

            if (await PreRunL95() != RunStates.Running)
                return State;
        }

        LogInfo($"Run: Loop Count {LoopCount}");

        int wasLoop = 0;
        for (int i = 0; i < LoopCount; i++)
        {
            foreach (Results.ViewModels.ImageResultEntry ire in ImageResultEntries)
            {
                UseV275 = HasV275 && ire.V275StoredSectors.Count > 0;
                if (HasV275 && ire.V275StoredSectors.Count == 0)
                    LogInfo("Run: V275, No sectors to process.");

                UseV5 = HasV5 && ire.V5StoredSectors.Count > 0;
                if (HasV5 && ire.V5StoredSectors.Count == 0)
                    LogInfo("Run: V5, No sectors to process.");

                UseL95 = HasL95 && ire.L95xxStoredSectors.Count > 0;
                if (HasL95 && ire.L95xxStoredSectors.Count == 0)
                    LogInfo("Run: Lvs95xx, No sectors to process.");

                if (!UseV275 && !UseV5 && !UseL95)
                    continue;

                //The loop count is controlled inside the image entry loop so the PreLoop calls can use the ImageResultEntry.
                CurrentLoopCount = i + 1;
                if (CurrentLoopCount != wasLoop)
                {
                    if (UseV275)
                        if (await PreLoopV275(ire) != RunStates.Running)
                            return State;

                    if (UseV5)
                        if (await PreLoopV5(ire) != RunStates.Running)
                            return State;

                    if (UseL95)
                        if (await PreLoopL95(ire) != RunStates.Running)
                            return State;

                    wasLoop = CurrentLoopCount;
                    LogInfo($"Run: Starting Loop {CurrentLoopCount}");
                }

                if (RequestedState != RunStates.Running)
                    return UpdateRunState(RequestedState);

                //This must occur before the print so it is added to the V275 image
                CurrentLabelCount++;

                V275Result v275Res = null;
                if (UseV275)
                    if ((v275Res = await ProcessV275(ire)) == null)
                        return State;

                V5Result v5Res = null;
                if (UseV5)
                    if ((v5Res = await ProcessV5(ire)) == null)
                        return State;

                L95xxResult l95Res = null;
                if (UseL95)
                    if ((l95Res = await ProcessL95(ire)) == null)
                        return State;

                if(v275Res != null)
                {
                    Run.Databases.ResultEntry current = new()
                    {
                        RunUID = RunUID,
                        SourceImageUID = ire.SourceImageUID,
                        ImageRollUID = ire.ImageRollUID,
                        Order = CurrentLabelCount,
                        TotalLoops = LoopCount,
                        CompletedLoops = CurrentLoopCount,
                        ResultType = ImageResultTypes.Current,
                        DeviceType = DeviceTypes.V275,
                        V275Result = v275Res,
                    };

                    _ = ResultsDatabase.InsertOrReplace(current);
                }

                if (v5Res != null)
                {
                    Run.Databases.ResultEntry current = new()
                    {
                        RunUID = RunUID,
                        SourceImageUID = ire.SourceImageUID,
                        ImageRollUID = ire.ImageRollUID,
                        Order = CurrentLabelCount,
                        TotalLoops = LoopCount,
                        CompletedLoops = CurrentLoopCount,
                        ResultType = ImageResultTypes.Current,
                        DeviceType = DeviceTypes.V5,
                        V5Result = v5Res,
                    };
                    _ = ResultsDatabase.InsertOrReplace(current);
                }

                if (l95Res != null)
                {
                    Run.Databases.ResultEntry current = new()
                    {
                        RunUID = RunUID,
                        SourceImageUID = ire.SourceImageUID,
                        ImageRollUID = ire.ImageRollUID,
                        Order = CurrentLabelCount,
                        TotalLoops = LoopCount,
                        CompletedLoops = CurrentLoopCount,
                        ResultType = ImageResultTypes.Current,
                        DeviceType = DeviceTypes.L95xx,
                        L95xxResult = l95Res,
                    };
                    _ = ResultsDatabase.InsertOrReplace(current);
                }

                UpdateRunEntry();
            }
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

    private async Task<V275Result> ProcessV275(Results.ViewModels.ImageResultEntry ire)
    {
        V275Result v275 = new()
        {
            RunUID = RunUID,
            SourceImageUID = ire.SourceImageUID,
            ImageRollUID = ire.ImageRollUID,

            SourceImage = ire.SourceImage?.Serialize,
        };

        //Start the V275 processing the image.
        if (V275.Controller.IsSimulator)
            ire.V275ProcessCommand.Execute("v275Stored");
        else
            ire.V275ProcessCommand.Execute("source");

        //Wait for the V275 to finish processing the image or fault.

        await Task.Run(() =>
        {
            DateTime start = DateTime.Now;
            while (ire.IsV275Working)
            {
                if (RequestedState != RunStates.Running)
                {
                    UpdateRunState(RequestedState);
                }

                if (DateTime.Now - start > TimeSpan.FromMilliseconds(10000))
                {
                    LogError("Run: Timeout waiting for results.");
                    UpdateRunState(RunStates.Error);
                }

                if (ire.IsV275Faulted)
                {
                    LogError("Run: Error when interacting with V275.");
                    UpdateRunState(RunStates.Error);
                }

                Thread.Sleep(1);
            };
        });

        if(State != RunStates.Running)
            return null;


        if (ire.V275ResultRow == null)
        {
            LogError("Run: V275, No results returned.");
            UpdateRunState(RunStates.Error);
            return null;
        }

        v275.Report = ire.V275ResultRow.Report;
        v275.Template = ire.V275ResultRow.Template;
        v275.StoredImage = ire.V275ResultRow.StoredImage;

        return v275;
    }

    private async Task<RunStates> PreRunV5()
    {
        if (!await V5.SwitchToEdit())
        {
            LogError("Run: V5, Failed to switch to edit mode.");
            return UpdateRunState(RunStates.Error);
        }
        return State;
    }
    private async Task<RunStates> PreLoopV5(Results.ViewModels.ImageResultEntry ire) => State;


    private async Task<V5Result> ProcessV5(Results.ViewModels.ImageResultEntry ire)
    {
        Results.Databases.V5Result v5 = new()
        {
            RunUID = RunUID,
            SourceImageUID = ire.SourceImageUID,
            ImageRollUID = ire.ImageRollUID,

            SourceImage = ire.SourceImage?.Serialize,
        };

        if (V5.IsSimulator)
        {
            if (!await V5.ChangeImage(ire.V5ResultRow.Stored.ImageBytes, false))
            {
                LogError("Could not change the image.");
                _ = UpdateRunState(RunStates.Error);
                return null;
            }
        }

        V5_REST_Lib.Controller.FullReport res = await V5.Trigger_Wait_Return(true);

        if (!res.OK)
        {
            LogError("Could not trigger the scanner.");
            _ = UpdateRunState(RunStates.Error);
            return null;
        }

        v5.Template = JsonConvert.SerializeObject(V5.Config);
        v5.Report = res.ReportJSON;
        v5.StoredImage = JsonConvert.SerializeObject(new ImageEntry(ire.ImageRollUID, res.FullImage, 0));

        if (UpdateUI)
            _ = App.Current.Dispatcher.BeginInvoke(() => ire.V5ProcessResults(res));

        return v5;
    }

    private async Task<RunStates> PreRunL95()
    {
        return State;
    }
    private async Task<RunStates> PreLoopL95(Results.ViewModels.ImageResultEntry ire) => State;

    private async Task<L95xxResult> ProcessL95(Results.ViewModels.ImageResultEntry ire)
    {
        Results.Databases.L95xxResult l95 = new()
        {
            RunUID = RunUID,
            SourceImageUID = ire.SourceImageUID,
            ImageRollUID = ire.ImageRollUID,

            SourceImage = ire.SourceImage?.Serialize,
        };



        //v5.Template = JsonConvert.SerializeObject(V5.Config);
        //v5.Report = res.ReportJSON;
        //v5.StoredImage = JsonConvert.SerializeObject(new ImageEntry(ire.ImageRollUID, res.FullImage, 0));

        //if (UpdateUI)
        //    _ = App.Current.Dispatcher.BeginInvoke(() => ire.L95xxProcessResults(res));

        return l95;
    }

    //private async Task<V5_REST_Lib.Controller.TriggerResults> ProcessV5(Results.ViewModels.ImageResultEntry ire)
    //{
    //    if (V5.IsSimulator)
    //    {
    //        if (!await V5.ChangeImage(ire.V5ResultRow.Stored.ImageBytes, true))
    //        {
    //            LogError("Could not change the image.");
    //            return null;
    //        }
    //    }

    //    var res = await V5.Trigger_Wait_Return(true);

    //    if (!res.OK)
    //    {
    //        LogError("Could not trigger the scanner.");
    //        return null;
    //    }

    //    return res;
    //}

    private static bool HasSequencing(Results.ViewModels.ImageResultEntry label)
    {
        V275_REST_Lib.Models.Job template = JsonConvert.DeserializeObject<V275_REST_Lib.Models.Job>(label.V275ResultRow.Template);

        foreach (V275_REST_Lib.Models.Job.Sector sect in template.sectors)
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

        ResultsDatabase?.Close();
    }

    private RunStates UpdateRunState(RunStates state)
    {
        if (state is RunStates.Complete or RunStates.Stopped or RunStates.Error)
        {
            V5?.FTPClient?.Disconnect();

            _ = CurrentLabelCount != 0 ? UpdateRunEntry() : ExistRunEntry() && RemoveRunEntry();
            ResultsDatabase?.Close();
        }

        LogInfo($"Run: State Changed to {state}");
        State = state;
        return state;
    }

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
