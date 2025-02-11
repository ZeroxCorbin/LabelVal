using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.Results.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

namespace LabelVal.ImageRolls.ViewModels;
public partial class ImageRolls : ObservableRecipient
{
    [ObservableProperty] private FileFolderEntry fileRoot = App.Settings.GetValue("UserImageRollsDatabases_FileRoot", new FileFolderEntry(App.UserImageRollsRoot), true);
    partial void OnFileRootChanged(FileFolderEntry value) => App.Settings.SetValue("UserImageRollsDatabases_FileRoot", value);

    [ObservableProperty][NotifyPropertyChangedRecipients] private Databases.ImageRollsDatabase defaultDatabase;
    private ObservableCollection<Databases.ImageRollsDatabase> UserDatabases { get; } = [];
    public ObservableCollection<ImageRollEntry> UserImageRolls { get; } = [];

    [ObservableProperty] private ImageRollEntry newImageRoll = null;

    public ObservableCollection<ImageRollEntry> FixedImageRolls { get; } = [];

    [ObservableProperty][NotifyPropertyChangedRecipients] private ImageRollEntry selectedImageRoll;// = App.Settings.GetValue<ImageRollEntry>(nameof(SelectedImageRoll), null);
    partial void OnSelectedImageRollChanged(ImageRollEntry value) { App.Settings.SetValue(nameof(SelectedImageRoll), value); if (value != null) SelectedUserImageRoll = null; }

    [ObservableProperty][NotifyPropertyChangedRecipients] private ImageRollEntry selectedUserImageRoll;// = App.Settings.GetValue<ImageRollEntry>(nameof(SelectedUserImageRoll), null);
    partial void OnSelectedUserImageRollChanged(ImageRollEntry value) { if (value != null) App.Settings.SetValue(nameof(SelectedUserImageRoll), value); if (value != null) SelectedImageRoll = null; }


    [ObservableProperty] private bool rightAlignOverflow = App.Settings.GetValue(nameof(RightAlignOverflow), false);
    partial void OnRightAlignOverflowChanged(bool value) => App.Settings.SetValue(nameof(RightAlignOverflow), value);

    public ImageRolls()
    {
        WeakReferenceMessenger.Default.Register<RequestMessage<ImageRollEntry>>(
        this,
        (recipient, message) =>
        {
            message.Reply(SelectedImageRoll ?? SelectedUserImageRoll);
        });

        if (!Directory.Exists(FileRoot.Path))
            FileRoot = new FileFolderEntry(App.UserImageRollsRoot);

        LoadFixedImageRollsList();

        UpdateFileFolderEvents(FileRoot);
        UpdateImageRollsDatabasesList();

        IsActive = true;
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
                root.Children.Add(EnumerateFolders(GetNewFileFolderEntry(dir)));

        // Add new files
        foreach (string file in currentFiles)
            if (!root.Children.Any(child => child.Path == file))
                root.Children.Add(GetNewFileFolderEntry(file));

        return root;
    }
    private FileFolderEntry GetNewFileFolderEntry(string path)
    {
        FileFolderEntry ffe = new(path);
        ffe.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == "IsSelected")
            {
                UpdateDatabases(FileRoot);
                App.Settings.SetValue("UserImageRollsDatabases_FileRoot", FileRoot);
                LoadUserImageRollsList();
            }
        };
        return ffe;
    }
    private FileFolderEntry GetFileFolderEntry(string path) =>
        GetAllFiles(FileRoot).FirstOrDefault((e) => e.Path == path);
    private void UpdateFileFolderEvents(FileFolderEntry root)
    {
        root.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == "IsSelected")
            {
                UpdateDatabases(FileRoot);
                App.Settings.SetValue("UserImageRollsDatabases_FileRoot", FileRoot);
                LoadUserImageRollsList();
            }
        };

        foreach (FileFolderEntry child in root.Children)
            UpdateFileFolderEvents(child);
    }

    private List<FileFolderEntry> GetSelectedFiles(FileFolderEntry root)
    {
        List<FileFolderEntry> selectedFiles = new();
        foreach (FileFolderEntry child in root.Children)
        {
            if (child.IsDirectory)
                selectedFiles.AddRange(GetSelectedFiles(child));
            else if (child.IsSelected)
                selectedFiles.Add(child);
        }
        return selectedFiles;
    }
    private List<FileFolderEntry> GetAllFiles(FileFolderEntry root)
    {
        List<FileFolderEntry> files = new();
        foreach (FileFolderEntry child in root.Children)
        {
            if (child.IsDirectory)
                files.AddRange(GetSelectedFiles(child));
            else
                files.Add(child);
        }
        return files;
    }

    private void UpdateImageRollsDatabasesList()
    {
        LogInfo($"Loading Image Rolls databases from file system. {App.UserImageRollsRoot}");

        if (!File.Exists(App.UserImageRollDefaultFile))
        {
            var tmp = new Databases.ImageRollsDatabase(new FileFolderEntry(App.UserImageRollDefaultFile));
            tmp.Close();
        }
        FileRoot = EnumerateFolders(FileRoot);
        UpdateDatabases(FileRoot);

        LoadUserImageRollsList();
    }
    private void UpdateDatabases(FileFolderEntry root)
    {
        HashSet<string> selectedFiles = GetSelectedFiles(root).Select(file => file.Path).ToHashSet();

        // Remove databases that no longer exist
        for (int i = UserDatabases.Count - 1; i >= 0; i--)
        {
            var db = UserDatabases[i];
            if (!selectedFiles.Contains(db.File.Path))
            {
                UserDatabases[i].Close();
                UserDatabases.RemoveAt(i);
            }
        }

        // Add new databases
        foreach (string file in selectedFiles)
        {
            if (!UserDatabases.Any(db => db.File.Path == file))
            {
                Databases.ImageRollsDatabase newDatabase = new(new FileFolderEntry(file));
                UserDatabases.Add(newDatabase);
            }
        }

        SetDefaultDatabase();
    }

    private void SetDefaultDatabase()
    {
        var def = UserDatabases.FirstOrDefault((e) => e.File.Path == App.UserImageRollDefaultFile);

        if (def == null)
        {
            if (DefaultDatabase != null)
                DefaultDatabase.Close();
            DefaultDatabase = new Databases.ImageRollsDatabase(new FileFolderEntry(App.UserImageRollDefaultFile));
        }
        else
        {
            if (DefaultDatabase != def)
            {
                if (DefaultDatabase != null)
                    DefaultDatabase.Close();
                DefaultDatabase = def;
            }
        }
    }
    private void LoadUserImageRollsList()
    {
        var currentRolls = new HashSet<string>(UserImageRolls.Select(roll => roll.UID));
        var newRolls = new List<ImageRollEntry>();

        foreach (var db in UserDatabases)
        {
            if (!db.File.IsSelected)
                continue;

            LogInfo($"Loading user image rolls from database. {db.File.Name}");

            try
            {
                foreach (var roll in db.SelectAllImageRolls())
                {
                    LogDebug($"Found: {roll.Name}");
                    roll.ImageRollsDatabase = db;

                    if (!currentRolls.Contains(roll.UID))
                        UserImageRolls.Add(roll);

                    newRolls.Add(roll);
                }
            }
            catch (Exception ex)
            {
                LogError($"Error when accessing {db.File.Path}", ex);
            }
        }

        // Remove rolls that are no longer present
        for (int i = UserImageRolls.Count - 1; i >= 0; i--)
            if (!newRolls.Any(newRoll => newRoll.UID == UserImageRolls[i].UID))
                UserImageRolls.RemoveAt(i);

        LogInfo($"Processed {UserImageRolls.Count} user image rolls.");
    }


    private void LoadFixedImageRollsList()
    {
        LogInfo($"Loading image rolls from file system. {App.AssetsImageRollsRoot}");

        FixedImageRolls.Clear();

        foreach (var dir in Directory.EnumerateDirectories(App.AssetsImageRollsRoot).ToList().OrderBy((e) => Regex.Replace(e, "[0-9]+", match => match.Value.PadLeft(10, '0'))))
        {
            var fnd = dir[(dir.LastIndexOf('\\') + 1)..];
            LogDebug($"Found: {fnd}");

            foreach (var subdir in Directory.EnumerateDirectories(dir))
            {
                var files = Directory.EnumerateFiles(subdir, "*.imgr").ToList();
                if (files.Count == 0)
                    continue;

                try
                {
                    var imgr = JsonConvert.DeserializeObject<ImageRollEntry>(File.ReadAllText(files.First()));
                    imgr.Path = subdir;
                    FixedImageRolls.Add(imgr);
                }
                catch (Exception ex)
                {
                    LogError($"Failed to load image roll from {files.First()}", ex);
                    continue;
                }
            }
        }

        LogInfo($"Processed {FixedImageRolls.Count} fixed image rolls.");
    }

    [RelayCommand]
    private void Add()
    {
        LogInfo("Adding image roll.");

        NewImageRoll = new ImageRollEntry() { ImageRollsDatabase = DefaultDatabase };
    }

    [RelayCommand]
    private void Edit()
    {
        LogInfo("Editing image roll.");

        NewImageRoll = SelectedUserImageRoll.CopyLite();
    }

    [RelayCommand]
    public void Save()
    {
        if (NewImageRoll == null)
            return;

        if (string.IsNullOrEmpty(NewImageRoll.Name))
            return;

        if (NewImageRoll.SelectedStandard is Sectors.Interfaces.StandardsTypes.GS1 &&
            NewImageRoll.SelectedGS1Table is Sectors.Interfaces.GS1TableNames.None or Sectors.Interfaces.GS1TableNames.Unsupported)
            return;

        if (DefaultDatabase.InsertOrReplaceImageRoll(NewImageRoll) > 0)
        {
            LogInfo($"Saved image roll: {NewImageRoll.Name}");

            var remove = UserImageRolls.FirstOrDefault((e) => e.UID == NewImageRoll.UID);
            if (remove != null)
                UserImageRolls.Remove(remove);

            LoadUserImageRollsList();

            var file = GetFileFolderEntry(App.UserImageRollDefaultFile);
            if (file != null)
                file.IsSelected = true;
        }
        else
            LogError($"Failed to save image roll: {NewImageRoll.Name}");

        NewImageRoll = null;
    }

    [RelayCommand]
    public void Delete()
    {
        if (UserDatabases == null || SelectedUserImageRoll == null)
            return;

        foreach (var img in SelectedUserImageRoll.Images)
        {
            if (SelectedUserImageRoll.ImageRollsDatabase.DeleteImage(img.UID))
                LogInfo($"Deleted image: {img.UID}");
            else
                LogError($"Failed to delete image: {img.UID}");
        }

        if (SelectedUserImageRoll.ImageRollsDatabase.DeleteImageRoll(NewImageRoll.UID))
        {
            LogInfo($"Deleted image roll: {NewImageRoll.UID}");

            LoadUserImageRollsList();
            NewImageRoll = null;
            SelectedUserImageRoll = null;
        }
        else
            LogError($"Failed to delete image roll: {NewImageRoll.UID}");
    }

    [RelayCommand]
    public void Cancel() => NewImageRoll = null;

    [RelayCommand]
    private void UIDToClipboard()
    {
        Clipboard.SetText(Guid.NewGuid().ToString());
    }

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
