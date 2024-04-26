using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using LabelVal.Databases;
using LabelVal.Utilities;
using LabelVal.WindowViewModels;
using LabelVal.Models;
using LabelVal.ORM_Test;
using MahApps.Metro.Controls;
using LabelVal.Run.Databases;

namespace LabelVal.Run
{
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
        public LedgerDatabase RunLedgerDatabase { get; private set; }

        public LedgerDatabase.LedgerEntry LedgerEntry { get; private set; }

        public ResultDatabase RunEntryDatabase { get; private set; }
        //public List<RunEntryDatabase.Run> RunLabels { get; private set; } = new List<RunEntryDatabase.Run>();

        public int LoopCount { get; private set; }
        public int CurrentLoopCount { get; private set; }
        public int CurrentLabelCount { get; private set; }
        private long RunId { get; set; }

        private ObservableCollection<LabelControlViewModel> Labels { get; set; }
        public StandardEntryModel GradingStandard { get; private set; }
        private StandardsDatabase StandardsDatabase { get; set; }
        //private string JobName { get; }

        public Controller()
        {

        }

        public Controller(long timeDate)
        {
            TimeDate = timeDate;

            _ = OpenDatabases();
        }

        public Controller Init(ObservableCollection<LabelControlViewModel> labels, int loopCount, StandardsDatabase standardsDatabase, string productPart, string cameraMAC)
        {
            Labels = labels;
            LoopCount = loopCount;
            StandardsDatabase = standardsDatabase;
            GradingStandard = Labels[0].GradingStandard;

            TimeDate = DateTime.UtcNow.Ticks;

            LedgerEntry = new LedgerDatabase.LedgerEntry() { GradingStandard = GradingStandard.Name, TimeDate = TimeDate, Completed = 0, ProductPart = MainWindowViewModel.V275.Commands.Product.part, CameraMAC = Labels[0].MainWindow.V275_MAC };

            return !OpenDatabases() ? null : !UpdateRunEntries() ? null : this;
        }

        private bool OpenDatabases()
        {
            try
            {
                RunLedgerDatabase = new LedgerDatabase().Open($"{App.RunsRoot}\\{App.RunLedgerDatabaseName}");
                RunEntryDatabase = new ResultDatabase().Open($"{App.RunsRoot}\\{App.RunResultsDatabaseName(TimeDate)}");

                return true;
            }
            catch
            {
                return false;
            }

        }

        private bool UpdateRunEntries()
        {
            try
            {
                _ = RunLedgerDatabase.InsertOrReplace(LedgerEntry);
                _ = RunEntryDatabase.InsertOrReplace(LedgerEntry);

                return true;
            }
            catch
            {
                return false;
            }

        }

        private bool RemoveRunEntries()
        {
            try
            {
                _ = RunLedgerDatabase.DeleteLedgerEntry(LedgerEntry.TimeDate);
                _ = RunEntryDatabase.DeleteLedgerEntry(LedgerEntry.TimeDate);

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
                    var run = new RunLedger(JsonConvert.SerializeObject(Labels[0].LabelTemplate), Labels[0].MainWindow.V275_MAC, MainWindowViewModel.V275.Commands.Product.part);

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

            _ = await Labels[0].V275.SwitchToEdit();

            int wasLoop = 0;
            for (int i = 0; i < LoopCount; i++)
            {
                foreach (var label in Labels)
                {
                    if (label.LabelSectors.Count == 0)
                        continue;

                    CurrentLoopCount = i + 1;
                    if (CurrentLoopCount != wasLoop)
                    {
                        //If running a non-GS1 label then this will reset the match to file and sequences.
                        //If running a GS1 label label then edit mode is required.
                        if (HasSequencing(label))
                            _ = await Labels[0].V275.SwitchToEdit();
                        else if (Labels.Count == 1)
                            CurrentLoopCount = 1;

                        wasLoop = CurrentLoopCount;
                        Logger.Info("Job Loop: {loop}", CurrentLoopCount);
                    }

                    if (!GradingStandard.IsGS1)
                        _ = await label.V275.SwitchToRun();

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
                        LedgerEntry.Completed = 2;
                        Stopped();
                        return false;
                    }

                    var sRow = StandardsDatabase.GetRow(GradingStandard.Name, label.LabelImageUID);

                    if (sRow == null || string.IsNullOrEmpty(sRow.LabelReport))
                        continue;

                    Logger.Info("Job Print");

                    //this must occur before the print
                    CurrentLabelCount++;

                    label.PrintCommand.Execute(null);

                    DateTime start = DateTime.Now;
                    while (label.IsWorking)
                    {
                        if (RequestedState == RunStates.STOPPED)
                        {
                            LedgerEntry.Completed = 2;
                            Stopped();
                            return false;
                        }
                        if (DateTime.Now - start > TimeSpan.FromMilliseconds(10000))
                        {
                            Logger.Error("Job timeout.");
                            LedgerEntry.Completed = -1;
                            Stopped();
                            return false;
                        }

                        Thread.Sleep(1);
                    };

                    if (label.IsFaulted)
                    {
                        Logger.Error("Label action faulted.");
                        LedgerEntry.Completed = -1;
                        Stopped();
                        return false;
                    }

                    var row = new ResultDatabase.Result()
                    {
                        LabelTemplate = sRow.LabelTemplate,
                        LabelReport = sRow.LabelReport,
                        RepeatGoldenImage = sRow.RepeatImage,
                        LabelImageUID = label.LabelImageUID,
                        LabelImage = label.MainWindow.IsDeviceSimulator ? null : label.LabelImage,
                        LabelImageOrder = CurrentLabelCount,
                        LoopCount = CurrentLoopCount
                    };

                    if (!label.MainWindow.IsDeviceSimulator)
                        if (label.RepeatImage != null)
                        {
                            //Compress the image to PNG
                            PngBitmapEncoder encoder = new PngBitmapEncoder();
                            using var ms = new MemoryStream(label.RepeatImage);
                            using MemoryStream stream = new MemoryStream();
                            encoder.Frames.Add(BitmapFrame.Create(ms));
                            encoder.Save(stream);

                            row.RepeatImage = stream.ToArray();

                            stream.Close();
                        }

                    row.RepeatReport = JsonConvert.SerializeObject(label.RepeatReport);
                    _ = RunEntryDatabase.InsertOrReplace(row);

                    using var session = new NHibernateHelper().OpenSession();
                    if (session != null)
                    {
                        using var transaction = session.BeginTransaction();
                        var rep = new Report(label.RepeatReport);
                        rep.repeatImage = label.RepeatImage;
                        rep.voidRepeat = rep.repeat;
                        rep.runId = RunId;
                        //var run = new ORM_Test.RunLedger(JsonConvert.SerializeObject(sRow.LabelTemplate), label.MainWindow.V275_MAC, label.MainWindow.V275_NodeNumber.ToString());


                        _ = session.Save(rep);
                        transaction.Commit();
                    }
                }

            }

            LedgerEntry.Completed = 1;

            Logger.Info("Job Completed");

            _ = UpdateRunEntries();

            RunStateChange?.Invoke(State = RunStates.COMPLETE);

            RunEntryDatabase.Close();
            RunLedgerDatabase.Close();

            return true;
        }

        private static bool HasSequencing(LabelControlViewModel label)
        {
            foreach (var sect in label.LabelTemplate.sectors)
            {
                if (sect.matchSettings != null)
                    if (sect.matchSettings.matchMode >= 3 && sect.matchSettings.matchMode <= 6)
                        return true;
            }
            return false;
        }

        private void Stopped()
        {
            if (CurrentLabelCount != 0)
                _ = UpdateRunEntries();
            else
                _ = RemoveRunEntries();

            Logger.Info("Job Stopped");

            RunStateChange?.Invoke(State = RunStates.STOPPED);
        }

        public void Pause()
        {
            RequestedState = RunStates.PAUSED;
        }

        public void Resume()
        {
            RequestedState = RunStates.RUNNING;
        }

        public void Stop()
        {
            RequestedState = RunStates.STOPPED;
        }

        public void Close()
        {
            Stop();

            RunEntryDatabase.Close();
            RunLedgerDatabase.Close();
        }

    }
}
