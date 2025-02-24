using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LabelVal.Results.ViewModels;
public partial class ImageResultsDatabases : ObservableRecipient
{
    [ObservableProperty] private FileFolderEntry fileRoot = App.Settings.GetValue<FileFolderEntry>("ImageResultsDatabases_FileRoot", new FileFolderEntry(App.ImageResultsDatabaseRoot), true);
    partial void OnFileRootChanged(FileFolderEntry value) => App.Settings.SetValue("ImageResultsDatabases_FileRoot", value);

    public ObservableCollection<Databases.ImageResultsDatabase> Databases { get; } = [];

    [ObservableProperty][NotifyPropertyChangedRecipients] private Databases.ImageResultsDatabase selectedDatabase;
    partial void OnSelectedDatabaseChanged(Databases.ImageResultsDatabase value) => App.Settings.SetValue("SelectedImageResultDatabaseFFE", value?.File);

    [ObservableProperty] private bool rightAlignOverflow = App.Settings.GetValue(nameof(RightAlignOverflow), false);
    partial void OnRightAlignOverflowChanged(bool value) => App.Settings.SetValue(nameof(RightAlignOverflow), value);

    public ImageResultsDatabases()
    {
        if(!Directory.Exists(FileRoot.Path))
            FileRoot = new FileFolderEntry(App.ImageResultsDatabaseRoot);

        UpdateFileFolderEvents(FileRoot);

        UpdateImageResultsDatabasesList();
        SelectImageResultsDatabase();

        WeakReferenceMessenger.Default.Register<RequestMessage<Databases.ImageResultsDatabase>>(
            this,
            (recipient, message) =>
            {
                message.Reply(SelectedDatabase);
            });
    }

    private FileFolderEntry EnumerateFolders(FileFolderEntry root)
    {
        HashSet<string> currentDirectories = Directory.EnumerateDirectories(root.Path).ToHashSet();
        HashSet<string> currentFiles = Directory.EnumerateFiles(root.Path, "*.sqlite").ToHashSet();

        // Remove directories that no longer exist
        for (int i = root.Children.Count - 1; i >= 0; i--)
        {
            FileFolderEntry child = root.Children[i];
            if (child.IsDirectory && !currentDirectories.Contains(child.Path))
                root.Children.RemoveAt(i);
        }

        // Remove files that no longer exist
        for (int i = root.Children.Count - 1; i >= 0; i--)
        {
            FileFolderEntry child = root.Children[i];
            if (!child.IsDirectory && !currentFiles.Contains(child.Path))
                root.Children.RemoveAt(i);
        }

        // Add new directories
        foreach (string dir in currentDirectories)
            if (!root.Children.Any(child => child.Path == dir))
                root.Children.Add(EnumerateFolders(GetFileFolderEntry(dir)));

        // Add new files
        foreach (string file in currentFiles)
            if (!root.Children.Any(child => child.Path == file))
                root.Children.Add(GetFileFolderEntry(file));

        return root;
    }
    private FileFolderEntry GetFileFolderEntry(string path)
    {
        FileFolderEntry ffe = new(path);
        ffe.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == "IsSelected")
            {
                UpdateDatabases(FileRoot);
                App.Settings.SetValue("ImageResultsDatabases_FileRoot", FileRoot);
            }
        };
        return ffe;
    }
    private void UpdateFileFolderEvents(FileFolderEntry root)
    {
        root.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == "IsSelected")
            {
                UpdateDatabases(FileRoot);
                App.Settings.SetValue("ImageResultsDatabases_FileRoot", FileRoot);
            }
        };

        foreach (FileFolderEntry child in root.Children)
            UpdateFileFolderEvents(child);
    }
    private List<FileFolderEntry> CollectSelectedFiles(FileFolderEntry root)
    {
        List<FileFolderEntry> selectedFiles = new();

        foreach (FileFolderEntry child in root.Children)
        {
            if (child.IsDirectory)
            {
                selectedFiles.AddRange(CollectSelectedFiles(child));
            }
            else if (child.IsSelected)
            {
                selectedFiles.Add(child);
            }
        }

        return selectedFiles;
    }

    private void UpdateImageResultsDatabasesList()
    {
        LogInfo($"Loading Image Results databases from file system. {App.ImageResultsDatabaseRoot}");

        FileRoot = EnumerateFolders(FileRoot);
        UpdateDatabases(FileRoot);

        if (Databases.Count == 0)
        {
            Databases.ImageResultsDatabase file = new(new FileFolderEntry(Path.Combine(App.ImageResultsDatabaseRoot, "My First Database" + App.DatabaseExtension)));
            file.Close();

            FileRoot = EnumerateFolders(FileRoot);
            UpdateDatabases(FileRoot);
        }
    }
    private void UpdateDatabases(FileFolderEntry root)
    {
        HashSet<string> selectedFiles = CollectSelectedFiles(root).Select(file => file.Path).ToHashSet();

        // Remove databases that no longer exist
        for (int i = Databases.Count - 1; i >= 0; i--)
        {
            Databases.ImageResultsDatabase db = Databases[i];
            if (!selectedFiles.Contains(db.File.Path))
            {
                Databases[i].Close();
                Databases.RemoveAt(i);
            }
        }

        // Add new databases
        foreach (string file in selectedFiles)
        {
            if (!Databases.Any(db => db.File.Path == file))
            {
                Databases.ImageResultsDatabase newDatabase = new(new FileFolderEntry(file));
                Databases.Add(newDatabase);
            }
        }
    }

    private void SelectImageResultsDatabase()
    {
        FileFolderEntry val = App.Settings.GetValue<FileFolderEntry>("SelectedImageResultDatabaseFFE");

        if (val == null)
        {
            if (Databases.Count > 0)
                SelectedDatabase = Databases.First();
            return;
        }

        IEnumerable<Databases.ImageResultsDatabase> res = Databases.Where((a) => a.File.Path == val.Path);
        if (res.Any())
            SelectedDatabase = res.FirstOrDefault();
        else if (Databases.Count > 0)
            SelectedDatabase = Databases.First();
    }

    [RelayCommand]
    private async Task CreateImageResultsDatabase()
    {
        string res = await GetStringDialog("New Standards Database", "What is the name of the new database?");
        if (res == null) return;

        if (string.IsNullOrEmpty(res) || res.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0)
        {
            _ = OkDialog("Invalid Name", $"The name '{res}' contains invalid characters.");
            return;
        }

        Databases.ImageResultsDatabase file = new(new FileFolderEntry(Path.Combine(App.ImageResultsDatabaseRoot, res + App.DatabaseExtension)));
        file.Close();

        UpdateImageResultsDatabasesList();
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

        if (imageResultsDatabase.IsLocked || imageResultsDatabase.IsPermLocked)
        {
            _ = OkDialog("Database is Locked", "The database is locked. Unlock the database before deleting.");
            return;
        }

        if (await OkCancelDialog("Delete Database", $"Are you sure you want to delete the database '{imageResultsDatabase.File.Name}'?") != MessageDialogResult.Affirmative)
            return;

        imageResultsDatabase.Close();

        if (File.Exists(imageResultsDatabase.File.Path))
        {
            File.Delete(imageResultsDatabase.File.Path);

            if (SelectedDatabase == imageResultsDatabase)
                SelectedDatabase = null;

            FileRoot = EnumerateFolders(FileRoot);
        }
    }

    #region Dialogs
    public static IDialogCoordinator DialogCoordinator => MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance;

    public async Task OkDialog(string title, string message) => _ = await DialogCoordinator.ShowMessageAsync(this, title, message, MessageDialogStyle.Affirmative);
    public async Task<string> GetStringDialog(string title, string message) => await DialogCoordinator.ShowInputAsync(this, title, message);
    public async Task<MessageDialogResult> OkCancelDialog(string title, string message) => await DialogCoordinator.ShowMessageAsync(this, title, message, MessageDialogStyle.AffirmativeAndNegative);
    #endregion

    #region Logging
    private void LogInfo(string message) => Logging.lib.Logger.LogInfo(GetType(), message);
#if DEBUG
    private void LogDebug(string message) => Logging.lib.Logger.LogDebug(GetType(), message);
#else
    private void LogDebug(string message) { }
#endif
    private void LogWarning(string message) => Logging.lib.Logger.LogInfo(GetType(), message);
    private void LogError(string message) => Logging.lib.Logger.LogError(GetType(), message);
    private void LogError(Exception ex) => Logging.lib.Logger.LogError(GetType(), ex);
    private void LogError(string message, Exception ex) => Logging.lib.Logger.LogError(GetType(), ex, message);

    #endregion

}
