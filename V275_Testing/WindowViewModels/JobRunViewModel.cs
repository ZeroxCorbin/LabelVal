using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
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

                if (value != null && !value.RunDBMissing)
                    LoadRun();
                else
                    Labels.Clear();
            }
        }
        private JobDatabase.Job selectedJob;

        public ObservableCollection<JobLabelControlViewModel> Labels { get; } = new ObservableCollection<JobLabelControlViewModel>();

        public string GroupName { get; set; } = "GS1 TABLE 1";

        public ICommand DeleteRunCommand { get; }

        private IDialogCoordinator dialogCoordinator;
        public JobRunViewModel()
        {
            dialogCoordinator = MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance;

            DeleteRunCommand = new Core.RelayCommand(DeleteRunAction, c => true);

            LoadJobs();
        }
        public async Task<MessageDialogResult> OkCancelDialog(string title, string message)
        {
            MessageDialogResult result = await this.dialogCoordinator.ShowMessageAsync(this, title, message, MessageDialogStyle.AffirmativeAndNegative);

            return result;
        }

        private async void DeleteRunAction(object parameter)
        {
            if (parameter == null)
                return;

            if (await OkCancelDialog("Delete Job Run?", $"Are you sure you want to permenatley delete the Job Run dated {new DateTime(((JobDatabase.Job)parameter).TimeDate).ToLocalTime()}") == MessageDialogResult.Affirmative)
            {
                JobDatabase.Job job = (JobDatabase.Job)parameter;

                Jobs.Remove(job);

                JobDatabase.DeleteJob(job.TimeDate);

                if (!job.RunDBMissing)
                {
                    if (RunDatabase != null)
                        RunDatabase.Close();

                    try
                    {
                        File.Delete($"{App.JobsRoot}\\{App.RunsDatabaseName(job.TimeDate)}");
                    }
                    catch
                    {
                    }

                }


            }


            //var res = Labels.FirstOrDefault(e => e.Job.TimeDate == ((JobDatabase.Job)parameter).TimeDate);

            //if (res != null)
            //    Labels.Remove(res);
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

                JobDatabase.Close();
            }
        }

        private void LoadRun()
        {

            foreach (var label in Labels.ToList())
                label.Clear();

            Labels.Clear();

            //System.GC.Collect();

            RunDatabase = new RunDatabase().Open($"{App.JobsRoot}\\{App.RunsDatabaseName(SelectedJob.TimeDate)}");

            var runs = RunDatabase.SelectAllRuns();

            foreach (var run in runs)
            {
                Labels.Add(new JobLabelControlViewModel(MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance, run, SelectedJob));
            }

            RunDatabase.Close();
            RunDatabase = null;
        }
    }
}
