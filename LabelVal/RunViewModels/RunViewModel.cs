using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using LabelVal.Databases;

namespace LabelVal.RunViewModels
{
    public class RunViewModel : Core.BaseViewModel
    {
        public string Version => App.Version;

        RunLedgerDatabase RunLedgerDatabase { get; set; }

        private ObservableCollection<RunLedgerDatabase.RunEntry> runEntries = new ObservableCollection<RunLedgerDatabase.RunEntry>();
        public ObservableCollection<RunLedgerDatabase.RunEntry> RunEntries { get => runEntries; set => SetProperty(ref runEntries, value); }

        public RunLedgerDatabase.RunEntry SelectedRunEntry
        {
            get => selectedRunEntry;
            set
            {
                SetProperty(ref selectedRunEntry, value);

                if (value != null && !value.RunDBMissing)
                    Task.Run(() => LoadRun());
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
            using (RunLedgerDatabase = new RunLedgerDatabase().Open($"{App.RunsRoot}\\{App.RunLedgerDatabaseName}"))
            {
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
                }
            }

        }

        private async void LoadRun()
        {
            await App.Current.Dispatcher.InvokeAsync(() =>
            {
                foreach (var lab in Labels)
                    lab.Clear();
                Labels.Clear();
            });


            List<RunDatabase.Run> runs;
            using (RunDatabase db = new RunDatabase().Open($"{App.RunsRoot}\\{App.RunDatabaseName(SelectedRunEntry.TimeDate)}"))
                    runs = db.SelectAllRuns();

            if(runs != null)
                await App.Current.Dispatcher.InvokeAsync(() =>
                {
                    foreach (var run in runs)
                    {
                        Labels.Add(new RunLabelControlViewModel(MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance, run, SelectedRunEntry));
                    }
                });
        }
    }
}
