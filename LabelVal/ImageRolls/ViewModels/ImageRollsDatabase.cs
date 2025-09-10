using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Logging.lib;
using LabelVal.Results.ViewModels;
using MahApps.Metro.Controls.Dialogs;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System;

namespace LabelVal.ImageRolls.ViewModels;
public partial class ImageRollsDatabases : ObservableRecipient, IDisposable
{

    [ObservableProperty] private FileFolderEntry fileRoot = App.Settings.GetValue<FileFolderEntry>("ImageRollsDatabases_FileRoot", new FileFolderEntry(App.UserImageRollsRoot), true);
    partial void OnFileRootChanged(FileFolderEntry value) => App.Settings.SetValue("ImageRollsDatabases_FileRoot", value);

    public ObservableCollection<Databases.ImageRollsDatabase> Databases { get; } = [];

    [ObservableProperty][NotifyPropertyChangedRecipients] private Databases.ImageRollsDatabase selectedResultsDatabase;
    partial void OnSelectedResultsDatabaseChanged(Databases.ImageRollsDatabase value) => App.Settings.SetValue("SelectedImageRollDatabaseFFE", value?.File);

    public bool RightAlignOverflow
    {
        get => App.Settings.GetValue(nameof(RightAlignOverflow), false);
        set => App.Settings.SetValue(nameof(RightAlignOverflow), value);
    }

    public ImageRollsDatabases()
    {
        if (!Directory.Exists(FileRoot.Path))
            FileRoot = new FileFolderEntry(App.UserImageRollsRoot);

        UpdateFileFolderEvents(FileRoot);

        UpdateImageRollsDatabasesList();
        SelectImageRollsDatabase();

        WeakReferenceMessenger.Default.Register<RequestMessage<Databases.ImageRollsDatabase>>(
            this,
            (recipient, message) =>
            {
                message.Reply(SelectedResultsDatabase);
            });

        App.Settings.PropertyChanged += Settings_PropertyChanged;
    }

    private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(RightAlignOverflow))
            OnPropertyChanged(nameof(RightAlignOverflow));
    }

    private FileFolderEntry EnumerateFolders(FileFolderEntry root)
    {
        var currentDirectories = Directory.EnumerateDirectories(root.Path).ToHashSet();
        var currentFiles = Directory.EnumerateFiles(root.Path, "*.sqlite").ToHashSet();

        // Remove directories that no longer exist
        for (var i = root.Children.Count - 1; i >= 0; i--)
        {
            var child = root.Children[i];
            if (child.IsDirectory && !currentDirectories.Contains(child.Path))
                root.Children.RemoveAt(i);
        }

        // Remove files that no longer exist
        for (var i = root.Children.Count - 1; i >= 0; i--)
        {
            var child = root.Children[i];
            if (!child.IsDirectory && !currentFiles.Contains(child.Path))
                root.Children.RemoveAt(i);
        }

        // Add new directories
        foreach (var dir in currentDirectories)
            if (!root.Children.Any(child => child.Path == dir))
                root.Children.Add(EnumerateFolders(GetFileFolderEntry(dir)));

        // Add new files
        foreach (var file in currentFiles)
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
                App.Settings.SetValue("ImageRollsDatabases_FileRoot", FileRoot);
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
                App.Settings.SetValue("ImageRollsDatabases_FileRoot", FileRoot);
            }
        };

        foreach (var child in root.Children)
            UpdateFileFolderEvents(child);
    }
    private List<FileFolderEntry> CollectSelectedFiles(FileFolderEntry root)
    {
        List<FileFolderEntry> selectedFiles = [];

        foreach (var child in root.Children)
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

    private void UpdateImageRollsDatabasesList()
    {
        Logger.Info($"Loading Image Rolls databases from file system. {App.UserImageRollsRoot}");

        FileRoot = EnumerateFolders(FileRoot);
        UpdateDatabases(FileRoot);

        if (Databases.Count == 0)
        {
            Databases.ImageRollsDatabase file = new(new FileFolderEntry(Path.Combine(App.UserImageRollsRoot, "My First Database" + App.DatabaseExtension)));
            file.Close();

            FileRoot = EnumerateFolders(FileRoot);
            UpdateDatabases(FileRoot);
        }
    }
    private void UpdateDatabases(FileFolderEntry root)
    {
        var selectedFiles = CollectSelectedFiles(root).Select(file => file.Path).ToHashSet();

        // Remove databases that no longer exist
        for (var i = Databases.Count - 1; i >= 0; i--)
        {
            var db = Databases[i];
            if (!selectedFiles.Contains(db.File.Path))
            {
                Databases[i].Close();
                Databases.RemoveAt(i);
            }
        }

        // Add new databases
        foreach (var file in selectedFiles)
        {
            if (!Databases.Any(db => db.File.Path == file))
            {
                Databases.ImageRollsDatabase newDatabase = new(new FileFolderEntry(file));
                Databases.Add(newDatabase);
            }
        }
    }

    private void SelectImageRollsDatabase()
    {
        var val = App.Settings.GetValue<FileFolderEntry>("SelectedImageRollDatabaseFFE");

        if (val == null)
        {
            if (Databases.Count > 0)
                SelectedResultsDatabase = Databases.First();
            return;
        }

        var res = Databases.Where((a) => a.File.Path == val.Path);
        if (res.Any())
            SelectedResultsDatabase = res.FirstOrDefault();
        else if (Databases.Count > 0)
            SelectedResultsDatabase = Databases.First();
    }

    [RelayCommand]
    private async Task CreateImageRollsDatabase()
    {
        var res = await GetStringDialog("New Image Rolls Database", "What is the name of the new database?");
        if (res == null) return;

        if (string.IsNullOrEmpty(res) || res.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0)
        {
            _ = OkDialog("Invalid Name", $"The name '{res}' contains invalid characters.");
            return;
        }

        Databases.ImageRollsDatabase file = new(new FileFolderEntry(Path.Combine(App.UserImageRollsRoot, res + App.DatabaseExtension)));
        file.Close();

        UpdateImageRollsDatabasesList();
    }

    [RelayCommand]
    private async Task Delete(Databases.ImageRollsDatabase imageRollsDatabase)
    {
        if (imageRollsDatabase == null)
            return;

        if (await OkCancelDialog("Delete Database", $"Are you sure you want to delete the database '{imageRollsDatabase.File.Name}'?") != MessageDialogResult.Affirmative)
            return;

        imageRollsDatabase.Close();

        if (File.Exists(imageRollsDatabase.File.Path))
        {
            File.Delete(imageRollsDatabase.File.Path);

            if (SelectedResultsDatabase == imageRollsDatabase)
                SelectedResultsDatabase = null;

            FileRoot = EnumerateFolders(FileRoot);
        }
    }

    #region Dialogs
    public static IDialogCoordinator DialogCoordinator => MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance;

    public async Task OkDialog(string title, string message) => _ = await DialogCoordinator.ShowMessageAsync(this, title, message, MessageDialogStyle.Affirmative);
    public async Task<string> GetStringDialog(string title, string message) => await DialogCoordinator.ShowInputAsync(this, title, message);
    public async Task<MessageDialogResult> OkCancelDialog(string title, string message) => await DialogCoordinator.ShowMessageAsync(this, title, message, MessageDialogStyle.AffirmativeAndNegative);
    #endregion

    public void Dispose()
    {
        App.Settings.PropertyChanged -= Settings_PropertyChanged;
        GC.SuppressFinalize(this);
    }
}