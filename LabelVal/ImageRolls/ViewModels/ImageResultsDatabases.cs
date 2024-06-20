using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using LabelVal.Messages;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace LabelVal.ImageRolls.ViewModels;
public partial class ImageResultsDatabases : ObservableRecipient
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    public ObservableCollection<Databases.ImageResults> Databases { get; } = [];

    [ObservableProperty][NotifyPropertyChangedRecipients] private Databases.ImageResults selectedDatabase;
    partial void OnSelectedDatabaseChanged(Databases.ImageResults value) => SelectedDatabaseFilePath = value != null ? value.FilePath : "";


    [ObservableProperty] private string selectedDatabaseFilePath = App.Settings.GetValue(nameof(SelectedDatabaseFilePath), "");
    partial void OnSelectedDatabaseFilePathChanged(string value) => App.Settings.SetValue(nameof(SelectedDatabaseFilePath), value);

    private ObservableCollection<string> OrphandStandards { get; } = [];

    public static IDialogCoordinator DialogCoordinator => MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance;

    public ImageResultsDatabases()
    {
        LoadImageResultsDatabasesList();
        SelectImageResultsDatabase();
    }

    public async Task OkDialog(string title, string message) => _ = await DialogCoordinator.ShowMessageAsync(this, title, message, MessageDialogStyle.Affirmative);
    public async Task<string> GetStringDialog(string title, string message) => await DialogCoordinator.ShowInputAsync(this, title, message);

    private void LoadImageResultsDatabasesList()
    {
        Logger.Info("Loading grading standards databases from file system. {path}", App.ImageResultsDatabaseRoot);

        foreach (var file in Directory.EnumerateFiles(App.AssetsImageResultsDatabasesRoot))
        {
            Logger.Debug("Found: {name}", Path.GetFileName(file));

            if (file.EndsWith(App.DatabaseExtension))
            {
                if (Databases.Any((a) => a.FilePath == file))
                    continue;

                Databases.Add(new Databases.ImageResults(file));
            }
        }

        foreach (var file in Directory.EnumerateFiles(App.ImageResultsDatabaseRoot))
        {
            Logger.Debug("Found: {name}", Path.GetFileName(file));

            if (file.EndsWith(App.DatabaseExtension))
            {
                if (Databases.Any((a) => a.FilePath == file))
                    continue;

                Databases.Add(new Databases.ImageResults(file));
            }
        }

        foreach (var db in Databases)
        {
            _ = db.Open();

        }
    }
    private void SelectImageResultsDatabase()
    {
        var res = Databases.Where((a) => a.FilePath == SelectedDatabaseFilePath);
        if (res.FirstOrDefault() != null)
            SelectedDatabase = res.FirstOrDefault();
        else if (Databases.Count > 0)
            SelectedDatabase = Databases.First();
    }

    [RelayCommand]
    private async Task CreateImageResultsDatabase()
    {
        var res = await GetStringDialog("New Standards Database", "What is the name of the new database?");
        if (res == null) return;

        if (string.IsNullOrEmpty(res) || res.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0)
        {
            _ = OkDialog("Invalid Name", $"The name '{res}' contains invalid characters.");
            return;
        }

        var file = new Databases.ImageResults(Path.Combine(App.ImageResultsDatabaseRoot, res + App.DatabaseExtension));
        _ = file.Open();
        file.Close();

        LoadImageResultsDatabasesList();
    }

    [RelayCommand]
    private void LockImageResultsDatabase()
    {
        if (SelectedDatabase.IsPermLocked)
            return;

        //if ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) && (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt)))
        //{
        //    ImageResultsDatabase.DeleteLockTable(false);
        //    ImageResultsDatabase.CreateLockTable(true);
        //}
        //else
        //{
        SelectedDatabase.IsLocked = !SelectedDatabase.IsLocked;
        //}

    }

}
