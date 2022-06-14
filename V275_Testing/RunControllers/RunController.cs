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
using V275_Testing.Databases;
using V275_Testing.Utilities;
using V275_Testing.WindowViewModels;

namespace V275_Testing.RunControllers
{
    public class RunController
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
        public RunLedgerDatabase RunLedgerDatabase { get; private set; }

        public RunLedgerDatabase.RunEntry RunEntry { get; private set; }

        public RunDatabase RunDatabase { get; private set; }
        public List<RunDatabase.Run> RunLabels { get; private set; } = new List<RunDatabase.Run>();

        public int LoopCount { get; private set; }
        public int LabelCount { get; private set; }

        private ObservableCollection<LabelControlViewModel> Labels { get; set; }
        private string GradingStandard { get; }
        private StandardsDatabase StandardsDatabase { get; }
        //private string JobName { get; }

        public bool IsGS1Standard { get; set; }

        public RunController(ObservableCollection<LabelControlViewModel> labels, int loopCount, StandardsDatabase standardsDatabase, string productPart, string cameraMAC)
        {
            Labels = labels;
            LoopCount = loopCount;
            StandardsDatabase = standardsDatabase;
            GradingStandard = Labels[0].GradingStandard;
            IsGS1Standard = GradingStandard.StartsWith("GS1") ? true : false;

            TimeDate = DateTime.UtcNow.Ticks;

            RunEntry = new RunLedgerDatabase.RunEntry() { GradingStandard = GradingStandard, TimeDate = TimeDate, Completed = 0, ProductPart = productPart, CameraMAC = cameraMAC };
        }

        public RunController(long timeDate)
        {
            TimeDate = timeDate;

            OpenDatabases();
        }

        public RunController Init()
        {
            if (!OpenDatabases())
                return null;

            if (!CreateRunEntries())
                return null;

            return this;
        }

        private bool OpenDatabases()
        {
            try
            {
                RunLedgerDatabase = new RunLedgerDatabase().Open($"{App.RunsRoot}\\{App.RunLedgerDatabaseName}");
                RunDatabase = new RunDatabase().Open($"{App.RunsRoot}\\{App.RunDatabaseName(TimeDate)}");

                return true;
            }
            catch
            {
                return false;
            }

        }

        private bool CreateRunEntries()
        {
            try
            {
                RunLedgerDatabase.InsertOrReplace(RunEntry);
                RunDatabase.InsertOrReplace(RunEntry);

                return true;
            }
            catch
            {
                return false;
            }

        }

        public void StartAsync()
        {
            RequestedState = RunStates.RUNNING;
            Task.Run(() => Start());
        }

       

        public async Task<bool> Start()
        {
            LabelCount = 1;

            Logger.Info("Job Started: Loop Count {loop}", LoopCount);

            for (int i = 0; i < LoopCount; i++)
            {
                Logger.Info("Job Loop: {loop}", i + 1);

                foreach (var label in Labels)
                {
                    if (label.LabelSectors.Count == 0)
                        continue;

                    if (IsGS1Standard)
                        await label.V275.SwitchToEdit();
                    else
                        await label.V275.SwitchToRun();

                    while (RequestedState == RunStates.PAUSED)
                    {
                        if (State == RunStates.RUNNING)
                        {
                            Logger.Info("Job Pasued");
                            RunStateChange?.Invoke(State = RunStates.PAUSED);
                        }

                        Thread.Sleep(10);
                    }

                    if (RequestedState == RunStates.STOPPED)
                    {
                        RunEntry.Completed = 2;
                        Stopped();
                        return false;
                    }

                    if (RequestedState == RunStates.RUNNING && State != RunStates.RUNNING)
                        RunStateChange?.Invoke(State = RunStates.RUNNING);

                    var sRow = StandardsDatabase.GetRow(GradingStandard, label.LabelImageUID);

                    if (sRow == null || string.IsNullOrEmpty(sRow.LabelReport))
                        continue;

                    var row = new RunDatabase.Run()
                    {
                        LabelTemplate = sRow.LabelTemplate,
                        LabelReport = sRow.LabelReport,
                        LabelImageUID = label.LabelImageUID,
                        LabelImage = label.LabelImageBytes,
                        LabelImageOrder = LabelCount++,
                        LoopCount = i + 1
                    };

                    RunLabels.Add(row);
                    RunDatabase.InsertOrReplace(row);

                    Logger.Info("Job Print");
                    label.PrintAction(null);

                    DateTime start = DateTime.Now;
                    while (label.IsWorking)
                    {
                        if (RequestedState == RunStates.STOPPED)
                        {
                            RunEntry.Completed = 2;
                            Stopped();
                            return false;
                        }
                        if ((DateTime.Now - start) > TimeSpan.FromMilliseconds(10000))
                        {
                            Logger.Error("Job timeout.");
                            RunEntry.Completed = -1;
                            Stopped();
                            return false;
                        }

                        Thread.Sleep(10);
                    };

                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    using (var ms = new System.IO.MemoryStream(label.RepeatImageData))
                    {
                        using (MemoryStream stream = new MemoryStream())
                        {
                            encoder.Frames.Add(BitmapFrame.Create(ms));
                            encoder.Save(stream);

                            row.RepeatImage = stream.ToArray();

                            stream.Close();
                        }
                    }

                    row.RepeatReport = JsonConvert.SerializeObject(label.Report);
                    RunDatabase.InsertOrReplace(row);
                }

            }

            RunEntry.Completed = 1;

            Logger.Info("Job Completed");

            RunLedgerDatabase.InsertOrReplace(RunEntry);
            RunDatabase.InsertOrReplace(RunEntry);

            RunStateChange?.Invoke(State = RunStates.COMPLETE);

            RunDatabase.Close();
            RunLedgerDatabase.Close();

            return true;
        }

        private void Stopped()
        {

            RunLedgerDatabase.InsertOrReplace(RunEntry);
            RunDatabase.InsertOrReplace(RunEntry);

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

            RunDatabase.Close();
            RunLedgerDatabase.Close();
        }

    }
}
