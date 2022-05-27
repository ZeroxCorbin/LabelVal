using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using V275_Testing.Databases;

namespace V275_Testing.WindowViewModels
{
    public class JobRunViewModel : Core.BaseViewModel
    {
        JobDatabase JobDatabase { get; set; }
        RunDatabase RunDatabase { get; set; }

        public ObservableCollection<JobDatabase.Job> Jobs { get; } = new ObservableCollection<JobDatabase.Job>();
        public JobDatabase.Job SelectedJob 
        { 
            get => selectedJob;
            set 
            { 
                SetProperty(ref selectedJob, value); 

                if (!value.RunDBMissing)
                    LoadRun(); 
                else 
                    Labels.Clear(); 
            }
        }
        private JobDatabase.Job selectedJob;

        public ObservableCollection<JobLabelControlViewModel> Labels { get; } = new ObservableCollection<JobLabelControlViewModel>();

        public string GroupName { get; set; } = "GS1 TABLE 1";

        private IDialogCoordinator dialogCoordinator;
        public JobRunViewModel()
        {
            dialogCoordinator = MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance;

            LoadJobs();
        }

        private void LoadJobs()
        {
            JobDatabase = new JobDatabase().Open($"{App.JobsRoot}\\{App.JobsDatabaseName}");
            var files = Directory.GetFiles(App.JobsRoot);

            if (JobDatabase != null)
            {
                var list = JobDatabase.SelectAllJobs();

                foreach (var job in list)
                {
                    if (string.IsNullOrEmpty(files.FirstOrDefault(e => e.EndsWith($"{App.RunsDatabaseName(job.TimeDate)}"))))
                        job.RunDBMissing = true;

                    Jobs.Add(job);
                }


            }
        }

        private void LoadRun()
        {
            Labels.Clear();

            RunDatabase = new RunDatabase().Open($"{App.JobsRoot}\\{App.RunsDatabaseName(SelectedJob.TimeDate)}");

            var runs = RunDatabase.SelectAllRuns();

            foreach(var run in runs)
            {
                Labels.Add(new JobLabelControlViewModel(MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance, run, SelectedJob));
            }
        }
    }
}
