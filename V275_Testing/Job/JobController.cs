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
using V275_Testing.WindowViewModels;

namespace V275_Testing.Job
{
    public class JobController
    {
        public enum JobStates
        {
            LOADED,
            RUNNING,
            PAUSED,
            COMPLETE,
            STOPPED
        }

        public delegate void JobStateChangeDeletgate(JobStates state);
        public event JobStateChangeDeletgate JobStateChange;

        public JobStates State { get; private set; }
        private JobStates RequestedState { get; set; }
        public long TimeDate { get; private set; }
        public JobDatabase JobsDatabase { get; private set; }

        public JobDatabase.Job Job { get; private set; }

        public RunDatabase RunDatabase { get; private set; }
        public List<RunDatabase.Run> RunLabels { get; private set; } = new List<RunDatabase.Run>();

        private int LoopCount { get; set; }
        private ObservableCollection<LabelControlViewModel> Labels { get; set; }
        private string GradingStandard { get; }
        private StandardsDatabase StandardsDatabase { get; }

        public JobController(ObservableCollection<LabelControlViewModel> labels, int loopCount, StandardsDatabase standardsDatabase, string productPart, string cameraMAC)
        {
            Labels = labels;
            LoopCount = loopCount;
            StandardsDatabase = standardsDatabase;
            GradingStandard = Labels[0].GradingStandard;
            TimeDate = DateTime.UtcNow.Ticks;

            Job = new JobDatabase.Job() { GradingStandard = GradingStandard, TimeDate = TimeDate, Completed = 0, ProductPart = productPart, CameraMAC = cameraMAC };

            //OpenDatabases();
            //CreateJobEntries();
            //InitializeRunDatabase();
        }

        public JobController(long timeDate)
        {
            TimeDate = timeDate;

            OpenDatabases();
        }

        public JobController Init()
        {
            if (!OpenDatabases())
                return null;

            if (!CreateJobEntries())
                return null;

            //if (!InitializeRunDatabase())
            //    return null;

            State = JobStates.LOADED;
            JobStateChange?.Invoke(State);

            return this;
        }

        private bool OpenDatabases()
        {
            try
            {
                JobsDatabase = new JobDatabase().Open($"{App.JobsRoot}\\{App.JobsDatabaseName}");
                RunDatabase = new RunDatabase().Open($"{App.JobsRoot}\\{App.RunsDatabaseName(TimeDate)}");

                return true;
            }
            catch
            {
                return false;
            }

        }

        private bool CreateJobEntries()
        {
            try
            {
                JobsDatabase.InsertOrReplace(Job);
                RunDatabase.InsertOrReplace(Job);

                return true;
            }
            catch
            {
                return false;
            }

        }

        public void StartAsync()
        {
            RequestedState = JobStates.RUNNING;
            Task.Run(() => Start());
        }

        public bool Start()
        {
            for (int i = 0; i < LoopCount; i++)
                foreach (var label in Labels)
                {
                    if (label.StoredSectors.Count == 0)
                        continue;

                    while (RequestedState == JobStates.PAUSED)
                    {
                        if (State == JobStates.RUNNING)
                            JobStateChange?.Invoke(State = JobStates.PAUSED);

                        Thread.Sleep(10);
                    }

                    if (RequestedState == JobStates.STOPPED)
                    {
                        JobStateChange?.Invoke(State = JobStates.STOPPED);
                        return false;
                    }

                    if (RequestedState == JobStates.RUNNING && State != JobStates.RUNNING)
                        JobStateChange?.Invoke(State = JobStates.RUNNING);

                    label.PrintAction(null);

                    var sRow = StandardsDatabase.GetRow(GradingStandard, label.LabelNumber);

                    if (sRow == null || string.IsNullOrEmpty(sRow.Report))
                        continue;

                    var row = new RunDatabase.Run()
                    {
                        Job = sRow.Job,
                        StoredReport = sRow.Report,
                        LabelNumber = label.LabelNumber,
                        LabelImage = File.ReadAllBytes(label.LabelImagePath)
                    };
                    row.LabelImageUID = ImageUID(row.LabelImage);

                    RunLabels.Add(row);
                    RunDatabase.InsertOrReplace(row);

                    while (label.IsWorking)
                    {
                        if (RequestedState == JobStates.STOPPED)
                        {
                            JobStateChange?.Invoke(State = JobStates.STOPPED);
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

                    row.Report = JsonConvert.SerializeObject(label.Report);
                    RunDatabase.InsertOrReplace(row);
                    //}
                }

            JobStateChange?.Invoke(State = JobStates.COMPLETE);

            RunDatabase.Close();
            JobsDatabase.Close();

            return true;
        }

        public void Pause()
        {
            RequestedState = JobStates.PAUSED;
        }

        public void Resume()
        {
            RequestedState = JobStates.RUNNING;
        }

        public void Stop()
        {
            RequestedState = JobStates.STOPPED;
        }

        public void Close()
        {
            Stop();

            RunDatabase.Close();
            JobsDatabase.Close();
        }

        private string ImageUID(byte[] image)
        {
            try
            {
                using (SHA256 md5 = SHA256.Create())
                {
                    return BitConverter.ToString(md5.ComputeHash(image)).Replace("-", String.Empty);
                }

            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }
}
