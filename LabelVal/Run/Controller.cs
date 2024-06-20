using LabelVal.ORM_Test;
using LabelVal.Run.Databases;
using LabelVal.WindowViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace LabelVal.Run;

public class Controller
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    public enum RunStates
    {
        IDLE,
        RUNNING,
        PAUSED,
        STOPPED,
        COMPLETE
    }

    public delegate void RunStateChangeDeletgate(RunStates state);
    public event RunStateChangeDeletgate RunStateChange;

    public RunStates State { get; private set; }
    private RunStates RequestedState { get; set; }
    public long TimeDate { get; private set; }

    public RunDatabase RunDatabase { get; private set; }
    public RunEntry RunEntry { get; private set; }
    public ResultEntry ResultEntry { get; private set; }
    //public List<RunEntryDatabase.Run> RunImageResultsList { get; private set; } = new List<RunEntryDatabase.Run>();

    public int LoopCount { get; private set; }
    public int CurrentLoopCount { get; private set; }
    public int CurrentLabelCount { get; private set; }
    private long RunId { get; set; }

    private ObservableCollection<Results.ViewModels.ImageResultEntry> ImageResultsList { get; set; }

    public ImageRolls.ViewModels.ImageRollEntry SelectedImageRoll { get; private set; }
    private Results.Databases.ImageResults ImageResultsDatabase { get; set; }

    public V275.ViewModels.Node Node { get; private set; }
    //private string JobName { get; }

    public Controller()
    {

    }

    public Controller(long timeDate)
    {
        TimeDate = timeDate;

        _ = OpenDatabase();
    }

    public Controller Init(ObservableCollection<Results.ViewModels.ImageResultEntry> imageResultsList, int loopCount, Results.Databases.ImageResults standardsDatabase, V275.ViewModels.Node v275Node)
    {
        ImageResultsList = imageResultsList;
        LoopCount = loopCount;
        ImageResultsDatabase = standardsDatabase;
        Node = v275Node;
        SelectedImageRoll = ImageResultsList[0].SelectedImageRoll;

        TimeDate = DateTime.UtcNow.Ticks;

        RunEntry = new RunEntry() { GradingStandard = SelectedImageRoll.UID, TimeDate = TimeDate, Completed = 0, ProductPart = v275Node.Product.part, CameraMAC = v275Node.Details.cameraMAC };

        return !OpenDatabase() ? null : !UpdateRunEntry() ? null : this;
    }

    private bool OpenDatabase()
    {
        try
        {
            RunDatabase = new RunDatabase().Open($"{App.RunsRoot}\\{App.RunResultsDatabaseName(TimeDate)}");

            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool UpdateRunEntry()
    {
        try
        {
            _ = RunDatabase.InsertOrReplace(RunEntry);

            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool RemoveRunEntry()
    {
        try
        {
            _ = RunDatabase.DeleteLedgerEntry(RunEntry.TimeDate);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public void StartAsync()
    {
        using (var session = new NHibernateHelper().OpenSession())
        {
            if (session != null)
            {
                using var transaction = session.BeginTransaction();
                var run = new RunLedger(ImageResultsList[0].V275ResultRow.Template, Node.Details.cameraMAC, Node.Product.part);

                _ = session.Save(run);
                transaction.Commit();

                RunId = run.Id;
            }
        }

        RequestedState = RunStates.RUNNING;
        _ = Task.Run(Start);
    }

    public async Task<bool> Start()
    {
        CurrentLabelCount = 0;

        Logger.Info("Job Started: Loop Count {loop}", LoopCount);

        if (RequestedState == RunStates.RUNNING && State != RunStates.RUNNING)
            RunStateChange?.Invoke(State = RunStates.RUNNING);

        _ = await Node.Connection.SwitchToEdit();

        var wasLoop = 0;
        for (var i = 0; i < LoopCount; i++)
        {
            foreach (var label in ImageResultsList)
            {
                if (label.V275StoredSectors.Count == 0)
                    continue;

                CurrentLoopCount = i + 1;
                if (CurrentLoopCount != wasLoop)
                {
                    //If running a non-GS1 label then this will reset the match to file and sequences.
                    //If running a GS1 label then edit mode is required.
                    if (HasSequencing(label))
                        _ = await Node.Connection.SwitchToEdit();
                    else if (ImageResultsList.Count == 1)
                        CurrentLoopCount = 1;

                    wasLoop = CurrentLoopCount;
                    Logger.Info("Job Loop: {loop}", CurrentLoopCount);
                }

                if (!SelectedImageRoll.WriteSectorsBeforeProcess)
                    _ = await Node.Connection.SwitchToRun();

                while (RequestedState == RunStates.PAUSED)
                {
                    if (State == RunStates.RUNNING)
                    {
                        Logger.Info("Job Pasued");
                        RunStateChange?.Invoke(State = RunStates.PAUSED);
                    }

                    Thread.Sleep(1);
                }

                if (RequestedState == RunStates.STOPPED)
                {
                    RunEntry.Completed = 2;
                    Stopped();
                    return false;
                }

                var sRow = ImageResultsDatabase.Select_V275Result(SelectedImageRoll.UID, label.SourceImage.UID);

                if (sRow == null || string.IsNullOrEmpty(sRow.Report))
                    continue;

                Logger.Info("Job Print");

                //this must occur before the print
                CurrentLabelCount++;

                label.V275ProcessCommand.Execute(null);

                var start = DateTime.Now;
                while (label.IsV275Working)
                {
                    if (RequestedState == RunStates.STOPPED)
                    {
                        RunEntry.Completed = 2;
                        Stopped();
                        return false;
                    }
                    if (DateTime.Now - start > TimeSpan.FromMilliseconds(10000))
                    {
                        Logger.Error("Job timeout.");
                        RunEntry.Completed = -1;
                        Stopped();
                        return false;
                    }

                    Thread.Sleep(1);
                };

                if (label.IsV275Faulted)
                {
                    Logger.Error("Label action faulted.");
                    RunEntry.Completed = -1;
                    Stopped();
                    return false;
                }

                var row = new ResultEntry()
                {
                    LabelTemplate = sRow.Template,
                    LabelReport = sRow.Report,
                    RepeatGoldenImage = sRow.Stored.GetBitmapBytes(),
                    LabelImageUID = label.SourceImage.UID,
                    LabelImage = Node.IsSimulator ? null : label.SourceImage.GetBitmapBytes(),
                    LabelImageOrder = CurrentLabelCount,
                    LoopCount = CurrentLoopCount
                };

                if (!Node.IsSimulator)
                    if (label.V275Image != null)
                    {
                        //Compress the image to PNG
                        row.RepeatImage = label.V275Image.GetPngBytes();
                    }

                row.RepeatReport = JsonConvert.SerializeObject(label.V275CurrentReport);
                _ = RunDatabase.InsertOrReplace(row);

                using var session = new NHibernateHelper().OpenSession();
                if (session != null)
                {
                    using var transaction = session.BeginTransaction();
                    var rep = new Report(label.V275CurrentReport);
                    rep.repeatImage = label.V275Image.GetPngBytes();
                    rep.voidRepeat = rep.repeat;
                    rep.runId = RunId;
                    //var run = new ORM_Test.RunLedger(JsonConvert.SerializeObject(sRow.LabelTemplate), label.MainWindow.V275_MAC, label.MainWindow.V275_NodeNumber.ToString());

                    _ = session.Save(rep);
                    transaction.Commit();
                }
            }
        }

        RunEntry.Completed = 1;

        Logger.Info("Job Completed");

        _ = UpdateRunEntry();

        RunStateChange?.Invoke(State = RunStates.COMPLETE);

        RunDatabase.Close();

        return true;
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

    private void Stopped()
    {
        _ = CurrentLabelCount != 0 ? UpdateRunEntry() : RemoveRunEntry();

        Logger.Info("Job Stopped");

        RunStateChange?.Invoke(State = RunStates.STOPPED);
    }

    public void Pause() => RequestedState = RunStates.PAUSED;

    public void Resume() => RequestedState = RunStates.RUNNING;

    public void Stop() => RequestedState = RunStates.STOPPED;

    public void Close()
    {
        Stop();

        RunDatabase.Close();
    }
}
