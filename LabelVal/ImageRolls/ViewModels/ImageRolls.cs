using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.GS1;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using LabelVal.ImageRolls.Databases;
using LabelVal.Main.Messages;
using LabelVal.Main.ViewModels;
using LabelVal.Results.ViewModels;
using MahApps.Metro.Controls.Dialogs;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace LabelVal.ImageRolls.ViewModels;

/// <summary>
/// ViewModel for managing both fixed (asset-based) and user-created image rolls.
/// This class handles loading, creating, editing, and deleting image rolls and their associated databases.
/// </summary>
public partial class ImageRolls : ObservableRecipient, IDisposable
{
    #region Properties

    /// <summary>
    /// Gets the singleton instance of the global application settings.
    /// </summary>
    public GlobalAppSettings AppSettings => GlobalAppSettings.Instance;

    /// <summary>
    /// Gets or sets the root entry for the user's image roll databases.
    /// </summary>
    [ObservableProperty] private FileFolderEntry fileRoot = App.Settings.GetValue("UserImageRollsDatabases_FileRoot", new FileFolderEntry(App.UserImageRollsRoot), true);
    partial void OnFileRootChanged(FileFolderEntry value) => App.Settings.SetValue("UserImageRollsDatabases_FileRoot", value);

    /// <summary>
    /// User databases are loaded from the <see cref="FileRoot"/>.
    /// </summary>
    private ObservableCollection<Databases.ImageRollsDatabase> UserDatabases { get; } = [];

    /// <summary>
    /// The currently selected User Image Rolls database.
    /// <see cref="SelectedUserDatabase"/>
    /// </summary>
    [ObservableProperty][NotifyPropertyChangedRecipients] private Databases.ImageRollsDatabase selectedUserDatabase;

    /// <summary>
    /// A temporary image roll used for adding or editing.
    /// <see cref="NewImageRoll"/>
    /// </summary>
    [ObservableProperty] private ImageRoll newImageRoll = null;

    /// <summary>
    /// Fixed image rolls are loaded from the Assets folder.
    /// </summary>
    public ObservableCollection<ImageRoll> FixedImageRolls { get; } = [];

    /// <summary>
    /// The currently selected fixed image roll.
    /// <see cref="SelectedFixedImageRoll"/>
    /// </summary>
    [ObservableProperty] private ImageRoll selectedFixedImageRoll;
    partial void OnSelectedFixedImageRollChanged(ImageRoll value)
    {
        if (value != null)
            SelectedUserImageRoll = null;
        else
            return;

        SelectedImageRoll = value;
    }

    /// <summary>
    /// User image rolls are loaded from the <see cref="SelectedUserDatabase"/>.
    /// </summary>
    public ObservableCollection<ImageRoll> UserImageRolls { get; } = [];

    /// <summary>
    /// The currently selected user image roll.
    /// <see cref="SelectedUserImageRoll"/>
    /// </summary>
    [ObservableProperty]
    private ImageRoll selectedUserImageRoll;
    partial void OnSelectedUserImageRollChanged(ImageRoll value)
    {
        if (value != null)
            SelectedFixedImageRoll = null;
        else
            return;

        SelectedImageRoll = value;
    }

    /// <summary>
    /// Gets or sets the currently selected image roll, which can be either a fixed or a user image roll.
    /// <see cref="SelectedImageRoll"/>
    /// </summary>
    [ObservableProperty][NotifyPropertyChangedRecipients] private ImageRoll selectedImageRoll;
    partial void OnSelectedImageRollChanged(ImageRoll value)
    {
        App.Settings.SetValue(nameof(SelectedImageRoll), value);
    }

    /// <summary>
    /// Gets or sets a flag to trigger a view refresh.
    /// </summary>
    [ObservableProperty] private bool refreshView;

    /// <summary>
    /// Gets or sets a value indicating whether to right-align overflow content in the UI.
    /// </summary>
    public bool RightAlignOverflow
    {
        get => App.Settings.GetValue(nameof(RightAlignOverflow), false);
        set => App.Settings.SetValue(nameof(RightAlignOverflow), value);
    }

    #endregion

    #region Constructor and Initialization

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageRolls"/> class.
    /// </summary>
    public ImageRolls()
    {
        if (!Directory.Exists(FileRoot.Path))
            FileRoot = new FileFolderEntry(App.UserImageRollsRoot);

        LoadFixedImageRollsList();

        UpdateFileFolderEvents(FileRoot);
        UpdateImageRollsDatabasesList();

        App.Settings.PropertyChanged += Settings_PropertyChanged;

        IsActive = true;
    }

    private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(RightAlignOverflow))
            OnPropertyChanged(nameof(RightAlignOverflow));
    }

    #endregion

    #region File and Database Management

    /// <summary>
    /// Recursively enumerates folders and files to build a file system tree.
    /// </summary>
    /// <param name="root">The root entry to start enumeration from.</param>
    /// <returns>The updated root entry with its children.</returns>
    private FileFolderEntry EnumerateFolders(FileFolderEntry root)
    {
        var currentDirectories = Directory.EnumerateDirectories(root.Path).ToHashSet();
        var currentFiles = Directory.EnumerateFiles(root.Path, "*.sqlite").ToHashSet();

        // Remove directories that no longer exist
        for (var i = root.Children.Count - 1; i >= 0; i--)
        {
            FileFolderEntry child = root.Children[i];
            if (child.IsDirectory && !currentDirectories.Contains(child.Path))
                root.Children.RemoveAt(i);
        }

        // Remove files that no longer exist
        for (var i = root.Children.Count - 1; i >= 0; i--)
        {
            FileFolderEntry child = root.Children[i];
            if (!child.IsDirectory && !currentFiles.Contains(child.Path))
                root.Children.RemoveAt(i);
        }

        // Add new directories
        foreach (var dir in currentDirectories)
            if (!root.Children.Any(child => child.Path == dir))
                root.Children.Add(EnumerateFolders(GetNewFileFolderEntry(dir)));

        // Add new files
        foreach (var file in currentFiles)
            if (!root.Children.Any(child => child.Path == file))
                root.Children.Add(GetNewFileFolderEntry(file));

        return root;
    }

    /// <summary>
    /// Creates a new <see cref="FileFolderEntry"/> and attaches property changed event handlers.
    /// </summary>
    /// <param name="path">The path of the file or folder.</param>
    /// <returns>A new <see cref="FileFolderEntry"/> instance.</returns>
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

    /// <summary>
    /// Retrieves a <see cref="FileFolderEntry"/> by its path from the file tree.
    /// </summary>
    /// <param name="path">The path to search for.</param>
    /// <returns>The matching <see cref="FileFolderEntry"/>, or null if not found.</returns>
    private FileFolderEntry GetFileFolderEntry(string path) =>
        GetAllFiles(FileRoot).FirstOrDefault((e) => e.Path == path);

    /// <summary>
    /// Recursively attaches property changed event handlers to a <see cref="FileFolderEntry"/> and its children.
    /// </summary>
    /// <param name="root">The root entry.</param>
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

    /// <summary>
    /// Recursively gets all selected files from a file system tree.
    /// </summary>
    /// <param name="root">The root entry to start from.</param>
    /// <returns>A list of selected <see cref="FileFolderEntry"/> items.</returns>
    private List<FileFolderEntry> GetSelectedFiles(FileFolderEntry root)
    {
        List<FileFolderEntry> selectedFiles = [];
        foreach (FileFolderEntry child in root.Children)
        {
            if (child.IsDirectory)
                selectedFiles.AddRange(GetSelectedFiles(child));
            else if (child.IsSelected)
                selectedFiles.Add(child);
        }
        return selectedFiles;
    }

    /// <summary>
    /// Recursively gets all files from a file system tree.
    /// </summary>
    /// <param name="root">The root entry to start from.</param>
    /// <returns>A list of all <see cref="FileFolderEntry"/> file items.</returns>
    private List<FileFolderEntry> GetAllFiles(FileFolderEntry root)
    {
        List<FileFolderEntry> files = [];
        foreach (FileFolderEntry child in root.Children)
        {
            if (child.IsDirectory)
                files.AddRange(GetSelectedFiles(child));
            else
                files.Add(child);
        }
        return files;
    }

    /// <summary>
    /// Updates the list of user image roll databases from the file system.
    /// </summary>
    private void UpdateImageRollsDatabasesList()
    {
        Logger.Info($"Loading Image Rolls databases from file system. {App.UserImageRollsRoot}");

        if (!File.Exists(App.UserImageRollDefaultFile))
        {
            var tmp = new Databases.ImageRollsDatabase(new FileFolderEntry(App.UserImageRollDefaultFile));
            tmp.Close();
        }
        FileRoot = EnumerateFolders(FileRoot);
        UpdateDatabases(FileRoot);

        LoadUserImageRollsList();
    }

    /// <summary>
    /// Synchronizes the <see cref="UserDatabases"/> collection with the selected files in the file tree.
    /// </summary>
    /// <param name="root">The root of the file tree.</param>
    private void UpdateDatabases(FileFolderEntry root)
    {
        var selectedFiles = GetSelectedFiles(root).Select(file => file.Path).ToHashSet();

        // Remove databases that no longer exist
        for (var i = UserDatabases.Count - 1; i >= 0; i--)
        {
            ImageRollsDatabase db = UserDatabases[i];
            if (!selectedFiles.Contains(db.File.Path))
            {
                UserDatabases[i].Close();
                UserDatabases.RemoveAt(i);
            }
        }

        // Add new databases
        foreach (var file in selectedFiles)
        {
            if (!UserDatabases.Any(db => db.File.Path == file))
            {
                Databases.ImageRollsDatabase newDatabase = new(new FileFolderEntry(file));
                UserDatabases.Add(newDatabase);
            }
        }

        SetSelectedUserDatabase();
    }

    /// <summary>
    /// Sets the selected user database, defaulting to the standard user image roll file.
    /// </summary>
    private void SetSelectedUserDatabase()
    {
        ImageRollsDatabase def = UserDatabases.FirstOrDefault((e) => e.File.Path == App.UserImageRollDefaultFile);

        if (def == null)
        {
            SelectedUserDatabase?.Close();
            SelectedUserDatabase = new Databases.ImageRollsDatabase(new FileFolderEntry(App.UserImageRollDefaultFile));
        }
        else
        {
            if (SelectedUserDatabase != def)
            {
                SelectedUserDatabase?.Close();
                SelectedUserDatabase = def;
            }
        }
    }

    /// <summary>
    /// Loads image rolls from the currently selected user databases, using a cache to improve performance.
    /// </summary>
    private void LoadUserImageRollsList()
    {
        _ = WeakReferenceMessenger.Default.Send(new SplashScreenMessage("Initializing user image rolls..."));

        var selectedDbFiles = UserDatabases.Where(db => db.File.IsSelected).ToList();
        var currentMetadata = selectedDbFiles.ToDictionary(db => db.File.Path, db => File.GetLastWriteTimeUtc(db.File.Path));
        Dictionary<string, DateTime> cachedMetadata = App.Settings.GetValue("UserImageRolls_CacheMetadata", new Dictionary<string, DateTime>());

        var failed = false;
        // If metadata matches, load from cache
        if (cachedMetadata.Count > 0 && !cachedMetadata.Except(currentMetadata).Any() && !currentMetadata.Except(cachedMetadata).Any())
        {
            Logger.Info("Loading user image rolls from cache.");
            List<ImageRoll> cachedRolls = App.Settings.GetValue("UserImageRolls_Cache", new List<ImageRoll>());
            UserImageRolls.Clear();
            foreach (ImageRoll roll in cachedRolls)
            {
                // Re-associate the database instance, as it's not serialized
                roll.ImageRollsDatabase = UserDatabases.FirstOrDefault(db => db.File.Path == roll.ImageRollsDatabase?.File.Path);
                if (roll.ImageRollsDatabase is null)
                {
                    failed = true;
                    break;
                }

                UserImageRolls.Add(roll);
            }
            if (failed)
            {
                Logger.Warning("Failed to load user image rolls from cache due to missing database references. Reloading from databases.");
            }
            else
            {
                Logger.Info($"Processed {UserImageRolls.Count} user image rolls from cache.");
                return;
            }
        }

        // Otherwise, load from databases and update cache
        Logger.Info("User image roll cache is invalid or missing, loading from databases.");
        var newRolls = new List<ImageRoll>();
        foreach (ImageRollsDatabase db in selectedDbFiles)
        {
            Logger.Info($"Loading user image rolls from database: {db.File.Name}");
            try
            {
                foreach (ImageRoll roll in db.SelectAllImageRolls())
                {
                    Logger.Debug($"Found: {roll.Name}");
                    roll.ImageRollsDatabase = db;
                    newRolls.Add(roll);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error when accessing {db.File.Path}");
            }
        }

        UserImageRolls.Clear();
        foreach (ImageRoll roll in newRolls)
        {
            UserImageRolls.Add(roll);
        }

        App.Settings.SetValue("UserImageRolls_Cache", UserImageRolls.ToList());
        App.Settings.SetValue("UserImageRolls_CacheMetadata", currentMetadata);

        Logger.Info($"Processed {UserImageRolls.Count} user image rolls.");

        _ = Application.Current.Dispatcher.BeginInvoke(() =>
        {
            _ = WeakReferenceMessenger.Default.Send(new SplashScreenMessage("Loading Complete!"));
        });
    }

    /// <summary>
    /// Loads the list of fixed image rolls from the assets folder, using a cache to improve performance.
    /// The cache is invalidated if the file structure or modification times have changed.
    /// </summary>
    private void LoadFixedImageRollsList()
    {
        _ = WeakReferenceMessenger.Default.Send(new SplashScreenMessage("Initializing fixed image rolls..."));

        Logger.Info($"Loading image rolls from file system. {App.AssetsImageRollsRoot}");

        Dictionary<string, DateTime> cachedMetadata = App.Settings.GetValue("FixedImageRolls_CacheMetadata", new Dictionary<string, DateTime>());
        Dictionary<string, DateTime> currentMetadata = GetDirectoryMetadata(App.AssetsImageRollsRoot, "*.imgr");

        var failed = false;
        // If metadata matches, load from cache
        if (cachedMetadata.Count > 0 && !cachedMetadata.Except(currentMetadata).Any() && !currentMetadata.Except(cachedMetadata).Any())
        {
            Logger.Info("Loading fixed image rolls from cache.");
            List<ImageRoll> cachedRolls = App.Settings.GetValue("FixedImageRolls_Cache", new List<ImageRoll>());
            FixedImageRolls.Clear();
            foreach (ImageRoll roll in cachedRolls)
            {
                if(roll.Path == null || !Directory.Exists(roll.Path))
                {
                    failed = true;
                    break;
                }
                FixedImageRolls.Add(roll);
            }
            if (failed)
            {
                Logger.Warning("Failed to load fixed image rolls from cache due to missing paths. Reloading from file system.");
            }
            else
            {
                Logger.Info($"Processed {FixedImageRolls.Count} fixed image rolls from cache.");
                return;
            }
        }

        // Otherwise, load from file system and update cache
        Logger.Info("Cache is invalid or missing, loading from file system.");
        FixedImageRolls.Clear();

        foreach (var dir in Directory.EnumerateDirectories(App.AssetsImageRollsRoot).ToList().OrderBy((e) => Regex.Replace(e, "[0-9]+", match => match.Value.PadLeft(10, '0'))))
        {
            var fnd = dir[(dir.LastIndexOf('\\') + 1)..];
            Logger.Debug($"Found: {fnd}");

            foreach (var subdir in Directory.EnumerateDirectories(dir))
            {
                var files = Directory.EnumerateFiles(subdir, "*.imgr").ToList();
                if (files.Count == 0)
                    continue;

                try
                {
                    ImageRoll imgr = JsonConvert.DeserializeObject<ImageRoll>(File.ReadAllText(files.First()));
                    imgr.Path = subdir;
                    FixedImageRolls.Add(imgr);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Failed to load image roll from {files.First()}");
                    continue;
                }
            }
        }

        App.Settings.SetValue("FixedImageRolls_Cache", FixedImageRolls.ToList());
        App.Settings.SetValue("FixedImageRolls_CacheMetadata", currentMetadata);

        Logger.Info($"Processed {FixedImageRolls.Count} fixed image rolls.");
    }

    /// <summary>
    /// Recursively gets the last write time for all subdirectories and files matching a pattern within a given path.
    /// </summary>
    /// <param name="path">The root directory path.</param>
    /// <param name="searchPattern">The search string to match against the names of files.</param>
    /// <returns>A dictionary mapping file/directory paths to their last write time (UTC).</returns>
    private Dictionary<string, DateTime> GetDirectoryMetadata(string path, string searchPattern)
    {
        var metadata = new Dictionary<string, DateTime>();
        if (!Directory.Exists(path))
            return metadata;

        var directories = new Stack<string>();
        directories.Push(path);

        while (directories.Count > 0)
        {
            var currentDir = directories.Pop();
            metadata[currentDir] = Directory.GetLastWriteTimeUtc(currentDir);

            foreach (var d in Directory.GetDirectories(currentDir))
                directories.Push(d);

            foreach (var f in Directory.GetFiles(currentDir, searchPattern))
                metadata[f] = File.GetLastWriteTimeUtc(f);
        }

        return metadata;
    }

    #endregion

    #region Commands

    /// <summary>
    /// Prepares a new image roll for addition.
    /// </summary>
    [RelayCommand]
    private void Add()
    {
        Logger.Info("Adding image roll.");

        NewImageRoll = new ImageRoll() { ImageRollsDatabase = SelectedUserDatabase };
    }

    /// <summary>
    /// Prepares the selected user image roll for editing.
    /// </summary>
    [RelayCommand]
    private void Edit()
    {
        Logger.Info("Editing image roll.");

        NewImageRoll = SelectedUserImageRoll.CopyLite();
    }

    /// <summary>
    /// Saves the new or edited image roll to the database.
    /// </summary>
    [RelayCommand]
    public void Save()
    {
        if (NewImageRoll == null)
            return;

        if (string.IsNullOrEmpty(NewImageRoll.Name))
        {
            Logger.Warning("Name is required for image rolls.");
            return;
        }

        if (NewImageRoll.SelectedApplicationStandard is ApplicationStandards.GS1 && NewImageRoll.SelectedGS1Table is GS1Tables.Unknown)
        {
            Logger.Warning("GS1 Table is required for GS1 image rolls.");
            return;
        }

        if (SelectedUserDatabase.InsertOrReplaceImageRoll(NewImageRoll) > 0)
        {
            Logger.Info($"Saved image roll: {NewImageRoll.Name}");

            ImageRoll update = UserImageRolls.FirstOrDefault((e) => e.UID == NewImageRoll.UID);
            if (update != null)
            {
                SelectedImageRoll.SelectedApplicationStandard = NewImageRoll.SelectedApplicationStandard;
                SelectedImageRoll.SelectedGradingStandard = NewImageRoll.SelectedGradingStandard;
                SelectedImageRoll.SelectedGS1Table = NewImageRoll.SelectedGS1Table;
                SelectedImageRoll.Name = NewImageRoll.Name;
                SelectedImageRoll.SectorType = NewImageRoll.SectorType;
                SelectedImageRoll.ImageType = NewImageRoll.ImageType;
            }
            else
            {
                LoadUserImageRollsList();
            }

            FileFolderEntry file = GetFileFolderEntry(App.UserImageRollDefaultFile);
            if (file != null)
                file.IsSelected = true;

            RefreshView = !RefreshView;
        }
        else
            Logger.Error($"Failed to save image roll: {NewImageRoll.Name}");

        NewImageRoll = null;
    }

    /// <summary>
    /// Deletes the selected user image roll and its associated images after confirmation.
    /// </summary>
    [RelayCommand]
    public async Task Delete()
    {
        if (UserDatabases == null || SelectedUserImageRoll == null)
            return;

        if (await OkCancelDialog("Delete Image Roll?", $"Are you sure you want to delete image roll {SelectedUserImageRoll.Name} and images and results?") != MessageDialogResult.Affirmative)
            return;

        foreach (ImageEntry img in SelectedUserImageRoll.ImageEntries)
        {

            if (SelectedUserImageRoll.ImageRollsDatabase.DeleteImage(SelectedUserImageRoll.UID, img.UID))
                Logger.Info($"Deleted image: {img.UID}");
            else
                Logger.Error($"Failed to delete image: {img.UID}");
        }

        if (SelectedUserImageRoll.ImageRollsDatabase.DeleteImageRoll(NewImageRoll.UID))
        {
            Logger.Info($"Deleted image roll: {NewImageRoll.UID}");

            LoadUserImageRollsList();
            NewImageRoll = null;
            SelectedUserImageRoll = null;
        }
        else
            Logger.Error($"Failed to delete image roll: {NewImageRoll.UID}");
    }

    /// <summary>
    /// Cancels the add/edit operation.
    /// </summary>
    [RelayCommand]
    public void Cancel() => NewImageRoll = null;

    /// <summary>
    /// Copies a new GUID to the clipboard.
    /// </summary>
    [RelayCommand]
    private void UIDToClipboard() => Clipboard.SetText(Guid.NewGuid().ToString());

    #endregion

    #region Dialogs

    private static IDialogCoordinator DialogCoordinator => MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance;

    /// <summary>
    /// Displays a confirmation dialog with "OK" and "Cancel" buttons.
    /// </summary>
    /// <param name="title">The title of the dialog.</param>
    /// <param name="message">The message to display.</param>
    /// <returns>The result of the user's choice.</returns>
    public async Task<MessageDialogResult> OkCancelDialog(string title, string message) => await DialogCoordinator.ShowMessageAsync(this, title, message, MessageDialogStyle.AffirmativeAndNegative);

    #endregion

    #region IDisposable Implementation

    /// <summary>
    /// Cleans up resources and unsubscribes from events.
    /// </summary>
    public void Dispose()
    {
        App.Settings.PropertyChanged -= Settings_PropertyChanged;
        GC.SuppressFinalize(this);
    }

    #endregion
}

public partial class ImageRolls1 : ObservableRecipient, IDisposable
{
    public GlobalAppSettings AppSettings => GlobalAppSettings.Instance;

    [ObservableProperty] private FileFolderEntry fileRoot = App.Settings.GetValue("UserImageRollsDatabases_FileRoot", new FileFolderEntry(App.UserImageRollsRoot), true);
    partial void OnFileRootChanged(FileFolderEntry value) => App.Settings.SetValue("UserImageRollsDatabases_FileRoot", value);

    /// <summary>
    /// User databases are loaded from the <see cref="FileRoot"/>/>
    /// </summary>
    private ObservableCollection<Databases.ImageRollsDatabase> UserDatabases { get; } = [];
    /// <summary>
    /// The currently selected User Image Rolls database.
    /// <see cref="SelectedUserDatabase"/>/>"
    /// </summary>
    [ObservableProperty][NotifyPropertyChangedRecipients] private Databases.ImageRollsDatabase selectedUserDatabase;

    /// <summary>
    /// A temporary image roll used for adding or editing.
    /// <see cref="NewImageRoll"/>"/>
    /// </summary>
    [ObservableProperty] private ImageRoll newImageRoll = null;

    /// <summary>
    /// Fixed image rolls are loaded from the Assets folder.
    /// </summary>
    public ObservableCollection<ImageRoll> FixedImageRolls { get; } = [];
    /// <summary>
    /// The currently selected fixed image roll.
    /// <see cref="SelectedFixedImageRoll"/>"/>
    /// </summary>
    [ObservableProperty] private ImageRoll selectedFixedImageRoll;
    partial void OnSelectedFixedImageRollChanged(ImageRoll value)
    {
        if (value != null)
            SelectedUserImageRoll = null;
        else
            return;

        SelectedImageRoll = value;
    }

    /// <summary>
    /// User image rolls are loaded from the <see cref="SelectedUserDatabase"/>/>
    /// </summary>
    public ObservableCollection<ImageRoll> UserImageRolls { get; } = [];
    /// <summary>
    /// The currently selected user image roll.
    /// <see cref="SelectedUserImageRoll"/>/>
    /// </summary>
    [ObservableProperty]
    private ImageRoll selectedUserImageRoll;
    partial void OnSelectedUserImageRollChanged(ImageRoll value)
    {
        if (value != null)
            SelectedFixedImageRoll = null;
        else
            return;

        SelectedImageRoll = value;
    }

    /// <summary>
    /// <see cref="SelectedImageRoll"/>
    /// </summary>
    [ObservableProperty][NotifyPropertyChangedRecipients] private ImageRoll selectedImageRoll;
    partial void OnSelectedImageRollChanged(ImageRoll value)
    {
        App.Settings.SetValue(nameof(SelectedImageRoll), value);
    }

    [ObservableProperty] private bool refreshView;

    public bool RightAlignOverflow
    {
        get => App.Settings.GetValue(nameof(RightAlignOverflow), false);
        set => App.Settings.SetValue(nameof(RightAlignOverflow), value);
    }

    public ImageRolls1()
    {
        if (!Directory.Exists(FileRoot.Path))
            FileRoot = new FileFolderEntry(App.UserImageRollsRoot);

        LoadFixedImageRollsList();

        UpdateFileFolderEvents(FileRoot);
        UpdateImageRollsDatabasesList();

        App.Settings.PropertyChanged += Settings_PropertyChanged;

        IsActive = true;
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
            FileFolderEntry child = root.Children[i];
            if (child.IsDirectory && !currentDirectories.Contains(child.Path))
                root.Children.RemoveAt(i);
        }

        // Remove files that no longer exist
        for (var i = root.Children.Count - 1; i >= 0; i--)
        {
            FileFolderEntry child = root.Children[i];
            if (!child.IsDirectory && !currentFiles.Contains(child.Path))
                root.Children.RemoveAt(i);
        }

        // Add new directories
        foreach (var dir in currentDirectories)
            if (!root.Children.Any(child => child.Path == dir))
                root.Children.Add(EnumerateFolders(GetNewFileFolderEntry(dir)));

        // Add new files
        foreach (var file in currentFiles)
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
        List<FileFolderEntry> selectedFiles = [];
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
        List<FileFolderEntry> files = [];
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
        Logger.Info($"Loading Image Rolls databases from file system. {App.UserImageRollsRoot}");

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
        var selectedFiles = GetSelectedFiles(root).Select(file => file.Path).ToHashSet();

        // Remove databases that no longer exist
        for (var i = UserDatabases.Count - 1; i >= 0; i--)
        {
            ImageRollsDatabase db = UserDatabases[i];
            if (!selectedFiles.Contains(db.File.Path))
            {
                UserDatabases[i].Close();
                UserDatabases.RemoveAt(i);
            }
        }

        // Add new databases
        foreach (var file in selectedFiles)
        {
            if (!UserDatabases.Any(db => db.File.Path == file))
            {
                Databases.ImageRollsDatabase newDatabase = new(new FileFolderEntry(file));
                UserDatabases.Add(newDatabase);
            }
        }

        SetSelectedUserDatabase();
    }

    private void SetSelectedUserDatabase()
    {
        ImageRollsDatabase def = UserDatabases.FirstOrDefault((e) => e.File.Path == App.UserImageRollDefaultFile);

        if (def == null)
        {
            SelectedUserDatabase?.Close();
            SelectedUserDatabase = new Databases.ImageRollsDatabase(new FileFolderEntry(App.UserImageRollDefaultFile));
        }
        else
        {
            if (SelectedUserDatabase != def)
            {
                SelectedUserDatabase?.Close();
                SelectedUserDatabase = def;
            }
        }
    }
    private void LoadUserImageRollsList()
    {
        var currentRolls = new HashSet<string>(UserImageRolls.Select(roll => roll.UID));
        var newRolls = new List<ImageRoll>();

        foreach (ImageRollsDatabase db in UserDatabases)
        {
            if (!db.File.IsSelected)
                continue;

            Logger.Info($"Loading user image rolls from database. {db.File.Name}");

            try
            {
                foreach (ImageRoll roll in db.SelectAllImageRolls())
                {
                    Logger.Debug($"Found: {roll.Name}");
                    roll.ImageRollsDatabase = db;

                    if (!currentRolls.Contains(roll.UID))
                        UserImageRolls.Add(roll);

                    newRolls.Add(roll);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error when accessing {db.File.Path}");
            }
        }

        // Remove rolls that are no longer present
        for (var i = UserImageRolls.Count - 1; i >= 0; i--)
            if (!newRolls.Any(newRoll => newRoll.UID == UserImageRolls[i].UID))
                UserImageRolls.RemoveAt(i);

        Logger.Info($"Processed {UserImageRolls.Count} user image rolls.");
    }

    private void LoadFixedImageRollsList()
    {
        Logger.Info($"Loading image rolls from file system. {App.AssetsImageRollsRoot}");

        FixedImageRolls.Clear();

        foreach (var dir in Directory.EnumerateDirectories(App.AssetsImageRollsRoot).ToList().OrderBy((e) => Regex.Replace(e, "[0-9]+", match => match.Value.PadLeft(10, '0'))))
        {
            var fnd = dir[(dir.LastIndexOf('\\') + 1)..];
            Logger.Debug($"Found: {fnd}");

            foreach (var subdir in Directory.EnumerateDirectories(dir))
            {
                var files = Directory.EnumerateFiles(subdir, "*.imgr").ToList();
                if (files.Count == 0)
                    continue;

                try
                {
                    ImageRoll imgr = JsonConvert.DeserializeObject<ImageRoll>(File.ReadAllText(files.First()));
                    imgr.Path = subdir;
                    FixedImageRolls.Add(imgr);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Failed to load image roll from {files.First()}");
                    continue;
                }
            }
        }

        Logger.Info($"Processed {FixedImageRolls.Count} fixed image rolls.");
    }

    [RelayCommand]
    private void Add()
    {
        Logger.Info("Adding image roll.");

        NewImageRoll = new ImageRoll() { ImageRollsDatabase = SelectedUserDatabase };
    }

    [RelayCommand]
    private void Edit()
    {
        Logger.Info("Editing image roll.");

        NewImageRoll = SelectedUserImageRoll.CopyLite();
    }

    [RelayCommand]
    public void Save()
    {
        if (NewImageRoll == null)
            return;

        if (string.IsNullOrEmpty(NewImageRoll.Name))
        {
            Logger.Warning("Name is required for image rolls.");
            return;
        }

        if (NewImageRoll.SelectedApplicationStandard is ApplicationStandards.GS1 && NewImageRoll.SelectedGS1Table is GS1Tables.Unknown)
        {
            Logger.Warning("GS1 Table is required for GS1 image rolls.");
            return;
        }

        if (SelectedUserDatabase.InsertOrReplaceImageRoll(NewImageRoll) > 0)
        {
            Logger.Info($"Saved image roll: {NewImageRoll.Name}");

            ImageRoll update = UserImageRolls.FirstOrDefault((e) => e.UID == NewImageRoll.UID);
            if (update != null)
            {
                SelectedImageRoll.SelectedApplicationStandard = NewImageRoll.SelectedApplicationStandard;
                SelectedImageRoll.SelectedGradingStandard = NewImageRoll.SelectedGradingStandard;
                SelectedImageRoll.SelectedGS1Table = NewImageRoll.SelectedGS1Table;
                SelectedImageRoll.Name = NewImageRoll.Name;
                SelectedImageRoll.SectorType = NewImageRoll.SectorType;
                SelectedImageRoll.ImageType = NewImageRoll.ImageType;
            }
            else
            {
                LoadUserImageRollsList();
            }

            FileFolderEntry file = GetFileFolderEntry(App.UserImageRollDefaultFile);
            if (file != null)
                file.IsSelected = true;

            RefreshView = !RefreshView;
        }
        else
            Logger.Error($"Failed to save image roll: {NewImageRoll.Name}");

        NewImageRoll = null;
    }

    [RelayCommand]
    public async Task Delete()
    {
        if (UserDatabases == null || SelectedUserImageRoll == null)
            return;

        if (await OkCancelDialog("Delete Image Roll?", $"Are you sure you want to delete image roll {SelectedUserImageRoll.Name} and images and results?") != MessageDialogResult.Affirmative)
            return;

        foreach (ImageEntry img in SelectedUserImageRoll.ImageEntries)
        {

            if (SelectedUserImageRoll.ImageRollsDatabase.DeleteImage(SelectedUserImageRoll.UID, img.UID))
                Logger.Info($"Deleted image: {img.UID}");
            else
                Logger.Error($"Failed to delete image: {img.UID}");
        }

        if (SelectedUserImageRoll.ImageRollsDatabase.DeleteImageRoll(NewImageRoll.UID))
        {
            Logger.Info($"Deleted image roll: {NewImageRoll.UID}");

            LoadUserImageRollsList();
            NewImageRoll = null;
            SelectedUserImageRoll = null;
        }
        else
            Logger.Error($"Failed to delete image roll: {NewImageRoll.UID}");
    }

    [RelayCommand]
    public void Cancel() => NewImageRoll = null;

    [RelayCommand]
    private void UIDToClipboard() => Clipboard.SetText(Guid.NewGuid().ToString());

    private static IDialogCoordinator DialogCoordinator => MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance;
    public async Task<MessageDialogResult> OkCancelDialog(string title, string message) => await DialogCoordinator.ShowMessageAsync(this, title, message, MessageDialogStyle.AffirmativeAndNegative);

    public void Dispose()
    {
        App.Settings.PropertyChanged -= Settings_PropertyChanged;
        GC.SuppressFinalize(this);
    }
}
