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
using CommunityToolkit.Mvvm.Input;

namespace LabelVal.Run.ViewModels
{
    public partial class Run : ObservableObject
    {
        public static string Version => App.Version;

        private RunDatabase RunDatabase { get; set; }

        public ObservableCollection<RunEntry> RunEntries { get; } = [];
        [ObservableProperty] private RunEntry selectedRunEntry;
        partial void OnSelectedRunEntryChanged(RunEntry value)
        {
            if (value != null && !value.RunDBMissing)
            {
                ImageResultsList.Clear();
                _ = Task.Run(() => LoadRun());
            }
            else
                ImageResultsList.Clear();
        }

        public ObservableCollection<Result> ImageResultsList { get; } = [];


        private IDialogCoordinator dialogCoordinator;
        public Run()
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

            if (await OkCancelDialog("Delete Run?", $"Are you sure you want to permenatley delete the Run dated {new DateTime(((RunEntry)parameter).TimeDate).ToLocalTime()}") == MessageDialogResult.Affirmative)
            {
                RunEntry runEntry = (RunEntry)parameter;

                _ = RunEntries.Remove(runEntry);

                RunDatabase = new RunDatabase().Open($"{App.RunsRoot}\\{App.RunLedgerDatabaseName}");
                _ = RunDatabase.DeleteLedgerEntry(runEntry.TimeDate);
                RunDatabase.Close();

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
            using (RunDatabase = new RunDatabase().Open($"{App.RunsRoot}\\{App.RunLedgerDatabaseName}"))
            {
                var files = Directory.GetFiles(App.RunsRoot);

                if (RunDatabase != null)
                {
                    var list = RunDatabase.SelectAllRunEntries();

                    foreach (var runEntry in list)
                    {
                        if (string.IsNullOrEmpty(files.FirstOrDefault(e => e.EndsWith($"{App.RunResultsDatabaseName(runEntry.TimeDate)}"))))
                            runEntry.RunDBMissing = true;

                        RunEntries.Add(runEntry);
                    }
                }
            }

        }

        private async void LoadRun()
        {

            //List<ResultEntry> runs;
            //using (RunDatabase db = new RunDatabase().Open($"{App.RunsRoot}\\{App.RunResultsDatabaseName(SelectedRunEntry.TimeDate)}"))
            //    runs = db.SelectAllRuns();

            //if (runs != null)
            //    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            //    {
            //        foreach (var run in runs)
            //        {
            //            ImageResultsList.Add(new ViewModels.Result(run, SelectedRunEntry));
            //        }
            //    });
        }
    }
}
