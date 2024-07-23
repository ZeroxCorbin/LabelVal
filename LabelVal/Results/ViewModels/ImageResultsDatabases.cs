using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.Messages;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace LabelVal.Results.ViewModels;
public partial class ImageResultsDatabases : ObservableRecipient
{
    public ObservableCollection<Databases.ImageResultsDatabase> Databases { get; } = [];

    [ObservableProperty][NotifyPropertyChangedRecipients] private Databases.ImageResultsDatabase selectedDatabase;
    partial void OnSelectedDatabaseChanged(Databases.ImageResultsDatabase value) => SelectedDatabaseFilePath = value != null ? value.FilePath : "";


    [ObservableProperty] private string selectedDatabaseFilePath = App.Settings.GetValue(nameof(SelectedDatabaseFilePath), "");
    partial void OnSelectedDatabaseFilePathChanged(string value) => App.Settings.SetValue(nameof(SelectedDatabaseFilePath), value);

    private ObservableCollection<string> OrphandStandards { get; } = [];

  
    public ImageResultsDatabases()
    {
        LoadImageResultsDatabasesList();
        SelectImageResultsDatabase();

        WeakReferenceMessenger.Default.Register<RequestMessage<Databases.ImageResultsDatabase>>(
            this,
            (recipient, message) =>
            {
                message.Reply(SelectedDatabase);
            });
    }


    private void LoadImageResultsDatabasesList()
    {
        LogInfo($"Loading grading standards databases from file system. {App.ImageResultsDatabaseRoot}");

        //foreach (var file in Directory.EnumerateFiles(App.AssetsImageResultsDatabasesRoot))
        //{
        //    LogDebug($"Found: {Path.GetFileName(file)}");

        //    if (file.EndsWith(App.DatabaseExtension))
        //    {
        //        if (Databases.Any((a) => a.FilePath == file))
        //            continue;

        //        Databases.Add(new Databases.ImageResultsDatabase(file));
        //    }
        //}

        foreach (var file in Directory.EnumerateFiles(App.ImageResultsDatabaseRoot))
        {
            LogDebug($"Found: {Path.GetFileName(file)}");

            if (file.EndsWith(App.DatabaseExtension))
            {
                if (Databases.Any((a) => a.FilePath == file))
                    continue;

                Databases.Add(new Databases.ImageResultsDatabase(file));
            }
        }

        if(Databases.Count == 0)
        {
            var file = new Databases.ImageResultsDatabase(Path.Combine(App.ImageResultsDatabaseRoot, "My First Database" + App.DatabaseExtension));
            _ = file.Open();
            file.Close();
        }

        foreach (var db in Databases)
            _ = db.Open();
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

        var file = new Databases.ImageResultsDatabase(Path.Combine(App.ImageResultsDatabaseRoot, res + App.DatabaseExtension));
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
    [RelayCommand]
    private async Task Delete(Databases.ImageResultsDatabase imageResultsDatabase)
    { 
        if (imageResultsDatabase == null)
            return;
        
        if (imageResultsDatabase.IsPermLocked)
            return;

        if (imageResultsDatabase.IsLocked)
        {
            _ = OkDialog("Database is Locked", "The database is locked. Unlock the database before deleting.");
            return;
        }

        if(await OkCancelDialog("Delete Database", $"Are you sure you want to delete the database '{imageResultsDatabase.FileName}'?") != MessageDialogResult.Affirmative)
            return;

        imageResultsDatabase.Close();

        if (File.Exists(imageResultsDatabase.FilePath))
        {
            File.Delete(imageResultsDatabase.FilePath);
            Databases.Remove(imageResultsDatabase);

            if(SelectedDatabase == imageResultsDatabase)
                SelectedDatabase = null;
        }
    }


    #region Dialogs
    public static IDialogCoordinator DialogCoordinator => MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance;

    public async Task OkDialog(string title, string message) => _ = await DialogCoordinator.ShowMessageAsync(this, title, message, MessageDialogStyle.Affirmative);
    public async Task<string> GetStringDialog(string title, string message) => await DialogCoordinator.ShowInputAsync(this, title, message);
    public async Task<MessageDialogResult> OkCancelDialog(string title, string message) => await DialogCoordinator.ShowMessageAsync(this, title, message, MessageDialogStyle.AffirmativeAndNegative);
    #endregion

    #region Logging
    private readonly Logging.Logger logger = new();
    public void LogInfo(string message) => logger.LogInfo(this.GetType(), message);
    public void LogDebug(string message) => logger.LogDebug(this.GetType(), message);
    public void LogWarning(string message) => logger.LogInfo(this.GetType(), message);
    public void LogError(string message) => logger.LogError(this.GetType(), message);
    public void LogError(Exception ex) => logger.LogError(this.GetType(), ex);
    public void LogError(string message, Exception ex) => logger.LogError(this.GetType(), message, ex);
    #endregion

}
