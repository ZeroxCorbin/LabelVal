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
        public enum RunStates
        {
            LOADED,
            RUNNING,
            PAUSED,
            COMPLETE,
            STOPPED
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

        private int LoopCount { get; set; }
        private ObservableCollection<LabelControlViewModel> Labels { get; set; }
        private string GradingStandard { get; }
        private StandardsDatabase StandardsDatabase { get; }

        public RunController(ObservableCollection<LabelControlViewModel> labels, int loopCount, StandardsDatabase standardsDatabase, string productPart, string cameraMAC)
        {
            Labels = labels;
            LoopCount = loopCount;
            StandardsDatabase = standardsDatabase;
            GradingStandard = Labels[0].GradingStandard;
            TimeDate = DateTime.UtcNow.Ticks;

            RunEntry = new RunLedgerDatabase.RunEntry() { GradingStandard = GradingStandard, TimeDate = TimeDate, Completed = 0, ProductPart = productPart, CameraMAC = cameraMAC };

            //OpenDatabases();
            //CreateJobEntries();
            //InitializeRunDatabase();
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

            //if (!InitializeRunDatabase())
            //    return null;

            State = RunStates.LOADED;
            RunStateChange?.Invoke(State);

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

        public bool Start()
        {
            for (int i = 0; i < LoopCount; i++)
                foreach (var label in Labels)
                {
                    if (label.StoredSectors.Count == 0)
                        continue;

                    while (RequestedState == RunStates.PAUSED)
                    {
                        if (State == RunStates.RUNNING)
                            RunStateChange?.Invoke(State = RunStates.PAUSED);

                        Thread.Sleep(10);
                    }

                    if (RequestedState == RunStates.STOPPED)
                    {
                        RunStateChange?.Invoke(State = RunStates.STOPPED);
                        return false;
                    }

                    if (RequestedState == RunStates.RUNNING && State != RunStates.RUNNING)
                        RunStateChange?.Invoke(State = RunStates.RUNNING);

                    label.PrintAction(null);

                    var sRow = StandardsDatabase.GetRow(GradingStandard, label.LabelImageUID);

                    if (sRow == null || string.IsNullOrEmpty(sRow.LabelReport))
                        continue;

                    var row = new RunDatabase.Run()
                    {
                        LabelTemplate = sRow.LabelTemplate,
                        LabelReport = sRow.LabelReport,
                        LabelImageUID = label.LabelImageUID,
                        LabelImage = label.LabelImageBytes
                    };

                    RunLabels.Add(row);
                    RunDatabase.InsertOrReplace(row);

                    while (label.IsWorking)
                    {
                        if (RequestedState == RunStates.STOPPED)
                        {
                            RunStateChange?.Invoke(State = RunStates.STOPPED);
                            return false;
                        }
                    };

                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    using (var ms = new System.IO.MemoryStream(label.RepeatImageData))
                    {
                        //img.BeginInit();
                        //img.CacheOption = BitmapCacheOption.OnLoad; // here
                        //img.StreamSource = ms;
                        //img.EndInit();
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
                    //}
                }

            RunStateChange?.Invoke(State = RunStates.COMPLETE);

            RunDatabase.Close();
            RunLedgerDatabase.Close();

            return true;
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
