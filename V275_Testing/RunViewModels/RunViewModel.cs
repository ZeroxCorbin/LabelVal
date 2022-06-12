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

namespace V275_Testing.RunViewModels
{
    public class RunViewModel : Core.BaseViewModel
    {
        public string Version => App.Version;

        RunLedgerDatabase RunLedgerDatabase { get; set; }
        RunDatabase RunDatabase { get; set; }

        private ObservableCollection<RunLedgerDatabase.RunEntry> runEntries = new ObservableCollection<RunLedgerDatabase.RunEntry>();
        public ObservableCollection<RunLedgerDatabase.RunEntry> RunEntries { get => runEntries; set => SetProperty(ref runEntries, value); }

        public RunLedgerDatabase.RunEntry SelectedRunEntry
        {
            get => selectedRunEntry;
            set
            {
                SetProperty(ref selectedRunEntry, value);

                if (value != null && !value.RunDBMissing)
                    LoadRun();
                else
                    Labels.Clear();
            }
        }
        private RunLedgerDatabase.RunEntry selectedRunEntry;

        private ObservableCollection<RunLabelControlViewModel> labels = new ObservableCollection<RunLabelControlViewModel>();
        public ObservableCollection<RunLabelControlViewModel> Labels { get => labels; set => SetProperty(ref labels, value); }

        public ICommand DeleteRunCommand { get; }

        private IDialogCoordinator dialogCoordinator;
        public RunViewModel()
        {
            dialogCoordinator = MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance;

            DeleteRunCommand = new Core.RelayCommand(DeleteRunAction, c => true);

            LoadRunEntries();
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

            if (await OkCancelDialog("Delete Run?", $"Are you sure you want to permenatley delete the Run dated {new DateTime(((RunLedgerDatabase.RunEntry)parameter).TimeDate).ToLocalTime()}") == MessageDialogResult.Affirmative)
            {
                RunLedgerDatabase.RunEntry runEntry = (RunLedgerDatabase.RunEntry)parameter;

                RunEntries.Remove(runEntry);

                RunLedgerDatabase = new RunLedgerDatabase().Open($"{App.RunsRoot}\\{App.RunLedgerDatabaseName}");
                RunLedgerDatabase.DeleteRunEntry(runEntry.TimeDate);
                RunLedgerDatabase.Close();

                if (!runEntry.RunDBMissing)
                {
                    if (RunDatabase != null)
                        RunDatabase.Close();

                    try
                    {
                        File.Delete($"{App.RunsRoot}\\{App.RunDatabaseName(runEntry.TimeDate)}");
                    }
                    catch
                    {
                    }

                }
            }
        }

        private void LoadRunEntries()
        {
            RunLedgerDatabase = new RunLedgerDatabase().Open($"{App.RunsRoot}\\{App.RunLedgerDatabaseName}");
            var files = Directory.GetFiles(App.RunsRoot);

            if (RunLedgerDatabase != null)
            {
                var list = RunLedgerDatabase.SelectAllRunEntries();

                foreach (var runEntry in list)
                {
                    if (string.IsNullOrEmpty(files.FirstOrDefault(e => e.EndsWith($"{App.RunDatabaseName(runEntry.TimeDate)}"))))
                        runEntry.RunDBMissing = true;

                    RunEntries.Add(runEntry);
                }

                RunLedgerDatabase.Close();
            }
        }

        private void LoadRun()
        {

            foreach (var label in Labels.ToList())
                label.Clear();

            Labels.Clear();

            RunDatabase = new RunDatabase().Open($"{App.RunsRoot}\\{App.RunDatabaseName(SelectedRunEntry.TimeDate)}");

            var runs = RunDatabase.SelectAllRuns();

            foreach (var run in runs)
            {
                Labels.Add(new RunLabelControlViewModel(MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance, run, SelectedRunEntry));
            }

            RunDatabase.Close();
            RunDatabase = null;
        }
    }
}
