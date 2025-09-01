using BarcodeVerification.lib.Extensions;
using BarcodeVerification.lib.GS1;
using CommunityToolkit.Mvvm.ComponentModel;
using LabelVal.ImageRolls.Databases;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.Results.Databases;
using LabelVal.Results.ViewModels;
using LabelVal.Run.Databases;
using LabelVal.Sectors.Classes;
using LabelVal.V275.ViewModels;
using Lvs95xx.lib.Core.Controllers;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

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

    public ImageRoll ImageRollEntry { get; private set; }

    [ObservableProperty] private Node v275;
    partial void OnV275Changed(Node value)
    {
        HasV275 = value != null;
        UseV275 = false;
    }
    [ObservableProperty] private bool hasV275;
    [ObservableProperty] private bool useV275;

    [ObservableProperty] private V5_REST_Lib.Controllers.Controller v5;
    partial void OnV5Changed(V5_REST_Lib.Controllers.Controller value)
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

    public bool StartAsync(ObservableCollection<Results.ViewModels.ImageResultEntry> imageResultEntries, ImageRoll imageRollEntry,
        Node v275,
        V5_REST_Lib.Controllers.Controller v5,
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
            Logger.LogError("Run: No device selected for run.");
            return false;
        }

        if (HasV275 && !V275.Controller.IsLoggedIn_Control)
        {
            Logger.LogError("Run: V275, Not logged in.");
            return false;
        }

        if (HasV5 && !V5.IsConnected)
        {
            Logger.LogError("Run: V5, Not connected.");
            return false;
        }

        if (HasL95 && !L95.IsConnected)
        {
            Logger.LogError("Run: Lvs95xx, Not connected.");
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
        RunEntry.GradingStandard = ImageRollEntry.SelectedGradingStandard;
        RunEntry.ApplicationStandard = ImageRollEntry.SelectedApplicationStandard;
        RunEntry.Gs1TableName = ImageRollEntry.SelectedGS1Table;
        RunEntry.DesiredLoops = LoopCount;
        RunEntry.ImageRollName = ImageRollEntry.Name;
        RunEntry.ImageRollUID = ImageRollEntry.UID;
        RunEntry.HasV275 = HasV275;
        RunEntry.V275Version = V275?.Controller?.Version;
        RunEntry.HasV5 = HasV5;
        RunEntry.V5Version = V5?.Version;
        RunEntry.HasL95 = HasL95;
        RunEntry.L95Version = L95?.Version;

        RunEntry.CompletedLoops = CurrentLoopCount;
        RunEntry.State = State;

        if (State == RunStates.Complete)
            RunEntry.EndTime = DateTime.Now.Ticks;

        return ResultsDatabase.InsertOrReplace(RunEntry) > 0;
    }
    private bool RemoveRunEntry() => ResultsDatabase.DeleteLedgerEntry(RunEntry.UID) > 0;
    private bool ExistRunEntry() => ResultsDatabase.ExistsLedgerEntry(RunEntry.UID);

    private async Task<RunStates> Start()
    {
        try
        {
            CurrentLabelCount = 0;

            RequestedState = UpdateRunState(RunStates.Running);

            if (HasV275)
            {
                Logger.LogInfo("Run: V275, Pre-Run");

                if (await PreRunV275() != RunStates.Running)
                    return State;
            }

            if (HasV5)
            {
                Logger.LogInfo("Run: V5, Pre-Run");

                if (await PreRunV5() != RunStates.Running)
                    return State;
            }

            if (HasL95)
            {
                Logger.LogInfo("Run: Lvs95xx, Pre-Run");

                if (await PreRunL95() != RunStates.Running)
                    return State;
            }

            Logger.LogInfo($"Run: Loop Count {LoopCount}");

            var wasLoop = 0;
            for (var i = 0; i < LoopCount; i++)
            {

                if (UpdateUI)
                {
                    if (HasV275)
                        foreach (var ire in ImageResultEntries)
                            ire.ClearCurrentCommand.Execute(Results.ViewModels.ImageResultEntryDevices.V275);

                    if (HasV5)
                        foreach (var ire in ImageResultEntries)
                            ire.ClearCurrentCommand.Execute(Results.ViewModels.ImageResultEntryDevices.V5);

                    if (HasL95)
                        foreach (var ire in ImageResultEntries)
                            ire.ClearCurrentCommand.Execute(Results.ViewModels.ImageResultEntryDevices.L95);
                }

                foreach (Results.ViewModels.IImageResultDeviceEntry ire in ImageResultEntries)
                {
                    //UseV275 = HasV275 && ire.V275StoredSectors.Count > 0;
                    //if (HasV275 && ire.V275StoredSectors.Count == 0)
                    //    Logger.LogInfo("Run: V275, No sectors to process.");

                    //UseV5 = HasV5 && ire.V5StoredSectors.Count > 0;
                    //if (HasV5 && ire.V5StoredSectors.Count == 0)
                    //    Logger.LogInfo("Run: V5, No sectors to process.");

                    //UseL95 = HasL95 && ire.L95StoredSectors.Count > 0;
                    //if (HasL95 && ire.L95StoredSectors.Count == 0)
                    //    Logger.LogInfo("Run: Lvs95xx, No sectors to process.");

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
                        Logger.LogInfo($"Run: Starting Loop {CurrentLoopCount}");
                    }

                    if (RequestedState != RunStates.Running)
                        return UpdateRunState(RequestedState);

                    //This must occur before the print so it is added to the V275 image
                    CurrentLabelCount++;

                    Result v275Res = null;
                    if (UseV275)
                        if ((v275Res = await Application.Current.Dispatcher.Invoke(() => ProcessV275(ire))) == null)
                            return UpdateRunState(RunStates.Error); ;

                    Result v5Res = null;
                    if (UseV5)
                        if ((v5Res = await ProcessV5(ire)) == null)
                            return UpdateRunState(RunStates.Error);

                    Result l95Res = null;
                    if (UseL95)
                        if ((l95Res = await ProcessL95(ire)) == null)
                            return UpdateRunState(RunStates.Error);

                    if (v275Res != null)
                    {
                        Run.Databases.ResultEntry current = new(v275Res, ImageResultTypes.Current)
                        {
                            RunUID = RunUID,
                            SourceImageUID = ire.ImageResultEntry.SourceImageUID,
                            ImageRollUID = ire.ImageResultEntry.ImageRollUID,
                            Order = CurrentLabelCount,
                            TotalLoops = LoopCount,
                            CompletedLoops = CurrentLoopCount
                        };

                        _ = ResultsDatabase.Insert(current);
                    }

                    if (v5Res != null)
                    {
                        Run.Databases.ResultEntry current = new(v5Res, ImageResultTypes.Current)
                        {
                            RunUID = RunUID,
                            SourceImageUID = ire.ImageResultEntry.SourceImageUID,
                            ImageRollUID = ire.ImageResultEntry.ImageRollUID,
                            Order = CurrentLabelCount,
                            TotalLoops = LoopCount,
                            CompletedLoops = CurrentLoopCount
                        };
                        _ = ResultsDatabase.Insert(current);
                    }

                    if (l95Res != null)
                    {
                        Run.Databases.ResultEntry current = new(l95Res, ImageResultTypes.Current)
                        {
                            RunUID = RunUID,
                            SourceImageUID = ire.ImageResultEntry.SourceImageUID,
                            ImageRollUID = ire.ImageResultEntry.ImageRollUID,
                            Order = CurrentLabelCount,
                            TotalLoops = LoopCount,
                            CompletedLoops = CurrentLoopCount
                        };
                        _ = ResultsDatabase.Insert(current);
                    }

                    _ = UpdateRunEntry();

                    Thread.Sleep(100);
                }
            }

            RunEntry.EndTime = DateTime.Now.Ticks;

            return UpdateRunState(RunStates.Complete);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex.Message);
            State = RunStates.Error;
            return State;
        }
}

    private async Task<RunStates> PreRunV275()
    {
        if (!await V275.Controller.SwitchToEdit())
        {
            Logger.LogError("Run: V275, Failed to switch to edit mode.");
            return UpdateRunState(RunStates.Error);
        }

        return State;
    }
    private async Task<RunStates> PreLoopV275(Results.ViewModels.IImageResultDeviceEntry ire)
    {
        //If running a non-GS1 label then this will reset the match to file and sequences.
        //If running a GS1 label then edit mode is required.
        if (HasSequencing(ire))
        {
            //Switch to edit to allow the Match files and Sequencing to reset.
            if (!await V275.Controller.SwitchToEdit())
            {
                Logger.LogError("Run: V275, Failed to switch to edit mode.");
                return UpdateRunState(RunStates.Error);
            }
        }

        if (ImageRollEntry.SectorType == ImageRollSectorTypes.Fixed)
        {
            if (!await V275.Controller.SwitchToRun())
            {
                Logger.LogError("Run: V275, Failed to switch to run mode.");
                return UpdateRunState(RunStates.Error);
            }
        }
        return State;
    }
    private async Task<Result> ProcessV275(Results.ViewModels.IImageResultDeviceEntry ire)
    {
        Result v275 = new()
        {
            RunUID = RunUID,
            ImageRollUID = ire.ImageResultEntry.ImageRollUID,

            Source = ire.ImageResultEntry.SourceImage,
        };

        //Start the V275 processing the image.
        if (ire.ImageResultsManager.SelectedImageRoll.ImageType == ImageRollImageTypes.Stored)
           ((ImageResultDeviceEntry_V275) ire).ProcessCommand.Execute(Results.ViewModels.ImageResultEntryImageTypes.V275Stored);
        else
            ((ImageResultDeviceEntry_V275)ire).ProcessCommand.Execute(Results.ViewModels.ImageResultEntryImageTypes.Source);

        //Wait for the V275 to finish processing the image or fault.

        await Task.Run(() =>
        {
            var start = DateTime.Now;
            while (ire.IsWorking)
            {
                if (RequestedState != RunStates.Running)
                {
                    _ = UpdateRunState(RequestedState);
                }

                if (DateTime.Now - start > TimeSpan.FromMilliseconds(10000))
                {
                    Logger.LogError("Run: Timeout waiting for results.");
                    _ = UpdateRunState(RunStates.Error);
                }

                if (ire.IsFaulted)
                {
                    Logger.LogError("Run: Error when interacting with V275.");
                    _ = UpdateRunState(RunStates.Error);
                }

                Thread.Sleep(1);
            }
            ;
        });

        if (State != RunStates.Running)
            return null;

        if (ire.Result == null)
        {
            Logger.LogError("Run: V275, No results returned.");
            _ = UpdateRunState(RunStates.Error);
            return null;
        }

        v275.Report = ire.Result.Report;
        v275.Template = ire.Result.Template;
        v275.StoredImage = ire.Result.StoredImage;

        return v275;
    }

    private async Task<RunStates> PreRunV5()
    {
        if (!await V5.SwitchToEdit())
        {
            Logger.LogError("Run: V5, Failed to switch to edit mode.");
            return UpdateRunState(RunStates.Error);
        }
        return State;
    }
    private async Task<RunStates> PreLoopV5(Results.ViewModels.IImageResultDeviceEntry ire) => State;
    private async Task<Result> ProcessV5(Results.ViewModels.IImageResultDeviceEntry ire)
    {
        Results.Databases.Result v5 = new()
        {
            RunUID = RunUID,
            ImageRollUID = ire.ImageResultEntry.ImageRollUID,
            Source = ire.ImageResultEntry.SourceImage,
        };

        if (V5.IsSimulator)
        {
           if( ire.ImageResultsManager.SelectedImageRoll.ImageType == ImageRollImageTypes.Stored)
            {
                if (!await V5.ChangeImage(ire.StoredImage.ImageBytes, false))
                {
                    Logger.LogError("Could not change the image.");
                    _ = UpdateRunState(RunStates.Error);
                    return null;
                }
            }
            else
            {
                if (!await V5.ChangeImage(ire.ImageResultEntry.SourceImage.ImageBytes, false))
                {
                    Logger.LogError("Could not change the image.");
                    _ = UpdateRunState(RunStates.Error);
                    return null;
                }
            }
        }

        var res = await V5.Trigger_Wait_Return(true);

        if (!res.OK)
        {
            Logger.LogError("Could not trigger the scanner.");
            _ = UpdateRunState(RunStates.Error);
            return null;
        }

        v5.Template = V5.Config;
        v5.Report = res.Report;
        v5.StoredImage = JsonConvert.SerializeObject(new ImageEntry(ire.ImageResultEntry.ImageRollUID, res.Image, 0));

        if (UpdateUI)
            _ = Application.Current.Dispatcher.BeginInvoke(() => ((ImageResultDeviceEntry_V5)ire).ProcessFullReport(res));

        return v5;
    }

    private async Task<RunStates> PreRunL95() => State;
    private async Task<RunStates> PreLoopL95(Results.ViewModels.IImageResultDeviceEntry ire) => State;
    private async Task<Result> ProcessL95(Results.ViewModels.IImageResultDeviceEntry ire)
    {
        Results.Databases.Result l95 = new()
        {
            RunUID = RunUID,
            ImageRollUID = ire.ImageResultEntry.ImageRollUID,
            Source = ire.ImageResultEntry.SourceImage,
        };

        Lvs95xx.lib.Core.Controllers.Label lab = new()
        {
            Config = new Lvs95xx.lib.Core.Controllers.Config()
            {
                ApplicationStandard = ire.ImageResultsManager.SelectedImageRoll.SelectedApplicationStandard.GetDescription(),
            },

            Image = ire.ImageResultsManager.SelectedImageRoll.ImageType == ImageRollImageTypes.Stored ? ire.ImageResultEntry.SourceImage.BitmapBytes : ire.StoredImage.BitmapBytes
        };

        if (ire.ImageResultsManager.SelectedImageRoll.SelectedGS1Table != GS1Tables.Unknown)
            lab.Config.Table = ((GS1Tables)ire.ImageResultsManager.SelectedImageRoll.SelectedGS1Table).GetTableName();

        var res = await L95.ProcessLabelAsync(lab);
        if (res == null)
        {
            Logger.LogError("Run: Lvs95xx, No results returned.");
            _ = UpdateRunState(RunStates.Error);
            return null;
        }

        l95.Report = res.Report;
        l95.StoredImage = JsonConvert.SerializeObject(new ImageEntry(ire.ImageResultEntry.ImageRollUID, res.Template.GetParameter<byte[]>("Report.Thumbnail"), 0));

        if (UpdateUI)
            _ = Application.Current.Dispatcher.BeginInvoke(() => ((ImageResultDeviceEntry_L95) ire).ProcessFullReport(res, true));

        return l95;
    }

    private static bool HasSequencing(Results.ViewModels.IImageResultDeviceEntry label)
    {
        var template = JsonConvert.DeserializeObject<V275_REST_Lib.Models.Job>(label.Result.TemplateString);

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

        Logger.LogInfo($"Run: State Changed to {state}");
        State = state;
        return state;
    }
}
