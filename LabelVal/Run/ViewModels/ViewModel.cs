using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using LabelVal.Run.Databases;
using CommunityToolkit.Mvvm.ComponentModel;
using LabelVal.result.ViewModels;
using CommunityToolkit.Mvvm.Input;

namespace LabelVal.Run.ViewModels
{
    public partial class ViewModel : ObservableObject
    {
        public static string Version => App.Version;

        public ObservableCollection<LedgerDatabase.LedgerEntry> LedgerEntries { get; } = [];
        private LedgerDatabase RunLedgerDatabase { get; set; }

        [ObservableProperty] private LedgerDatabase.LedgerEntry selectedRunEntry;
        partial void OnSelectedRunEntryChanged(LedgerDatabase.LedgerEntry value)
        {
            if (value != null && !value.RunDBMissing)
            {
                Labels.Clear();
                _ = Task.Run(() => LoadRun());
            }
            else
                Labels.Clear();
        }

        public ObservableCollection<LabelViewModel> Labels { get; } = [];


        private IDialogCoordinator dialogCoordinator;
        public ViewModel()
        {
            dialogCoordinator = DialogCoordinator.Instance;

            LoadRunEntries();
        }
        public async Task<MessageDialogResult> OkCancelDialog(string title, string message)
        {
            MessageDialogResult result = await dialogCoordinator.ShowMessageAsync(this, title, message, MessageDialogStyle.AffirmativeAndNegative);

            return result;
        }

        [RelayCommand]
        private async Task DeleteRun(object parameter)
        {
            if (parameter == null)
                return;

            if (await OkCancelDialog("Delete Run?", $"Are you sure you want to permenatley delete the Run dated {new DateTime(((LedgerDatabase.LedgerEntry)parameter).TimeDate).ToLocalTime()}") == MessageDialogResult.Affirmative)
            {
                LedgerDatabase.LedgerEntry runEntry = (LedgerDatabase.LedgerEntry)parameter;

                _ = LedgerEntries.Remove(runEntry);

                RunLedgerDatabase = new LedgerDatabase().Open($"{App.RunsRoot}\\{App.RunLedgerDatabaseName}");
                _ = RunLedgerDatabase.DeleteLedgerEntry(runEntry.TimeDate);
                RunLedgerDatabase.Close();

                if (!runEntry.RunDBMissing)
                {
                    try
                    {
                        File.Delete($"{App.RunsRoot}\\{App.RunResultsDatabaseName(runEntry.TimeDate)}");
                    }
                    catch
                    {
                    }

                }
            }
        }

        private void LoadRunEntries()
        {
            using (RunLedgerDatabase = new LedgerDatabase().Open($"{App.RunsRoot}\\{App.RunLedgerDatabaseName}"))
            {
                var files = Directory.GetFiles(App.RunsRoot);

                if (RunLedgerDatabase != null)
                {
                    var list = RunLedgerDatabase.SelectAllRunEntries();

                    foreach (var runEntry in list)
                    {
                        if (string.IsNullOrEmpty(files.FirstOrDefault(e => e.EndsWith($"{App.RunResultsDatabaseName(runEntry.TimeDate)}"))))
                            runEntry.RunDBMissing = true;

                        LedgerEntries.Add(runEntry);
                    }
                }
            }

        }

        private async void LoadRun()
        {

            List<ResultDatabase.Result> runs;
            using (ResultDatabase db = new ResultDatabase().Open($"{App.RunsRoot}\\{App.RunResultsDatabaseName(SelectedRunEntry.TimeDate)}"))
                runs = db.SelectAllRuns();

            if (runs != null)
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    foreach (var run in runs)
                    {
                        Labels.Add(new LabelViewModel(run, SelectedRunEntry));
                    }
                });
        }
    }
}
