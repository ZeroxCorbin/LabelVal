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
using System.Windows.Threading;

namespace LabelVal.ImageRolls.ViewModels;

/// <summary>
/// Central coordinator for all Image Rolls (both fixed/asset-based and user/database-based).
/// Responsibilities:
///  - Load and cache fixed (asset) rolls from the filesystem.
///  - Load, cache, and persist user rolls from selected SQLite roll databases.
///  - Maintain an Active Image Roll and preserve selection across sessions.
///  - Manage splash screen lifecycle logic tied to roll readiness and rendering.
///  - Respond to external rendering completion via <see cref="ResultssRenderedMessage"/>.
///  - Provide commands for adding, editing, saving, deleting, and selecting rolls.
/// Splash Logic Summary:
///  - While determining active roll, splash remains visible.
///  - Splash closes early if:
///     * No active roll can be restored or selected, OR
///     * Active roll exists but contains zero images.
///  - If active roll has images, splash remains until a <see cref="ResultssRenderedMessage"/> arrives.
/// </summary>
public partial class ImageRollsManager : ObservableRecipient, IDisposable, IRecipient<ResultssRenderedMessage>
{
    #region Constants / Keys
    private const string ActiveImageRollUidSettingKey = "ActiveImageRollUID";
    #endregion

    #region Private State Fields
    private bool _activeRollInitialized;
    private bool _splashCloseRequested;
    private bool _awaitingRenderMessage;
    #endregion

    #region External Singletons
    public GlobalAppSettings AppSettings => GlobalAppSettings.Instance;
    #endregion

    #region Observable Properties (Toolkit Generated)
    [ObservableProperty] private bool _isLoading;

    [ObservableProperty]
    private FileFolderEntry fileRoot =
        App.Settings.GetValue("UserImageRollsDatabases_FileRoot",
            new FileFolderEntry(App.UserImageRollsRoot), true);
    partial void OnFileRootChanged(FileFolderEntry value) =>
        App.Settings.SetValue("UserImageRollsDatabases_FileRoot", value);

    /// <summary>
    /// The currently selected user roll database (one of the checked .sqlite files).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    private ImageRollsDatabase selectedUserDatabase;

    /// <summary>
    /// Working editable instance when adding or editing a roll.
    /// </summary>
    [ObservableProperty] private ImageRoll newImageRoll = null;

    /// <summary>
    /// The currently active (in-use) image roll.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    private ImageRoll activeImageRoll;
    partial void OnActiveImageRollChanged(ImageRoll value)
    {
        // Persist selection
        App.Settings.SetValue(ActiveImageRollUidSettingKey, value?.UID);

        if (!_activeRollInitialized)
            return;

        // Re-evaluate splash lifecycle once active roll changes post-initialization
        EvaluateSplashForActiveRoll(allowStart: true);
    }
    #endregion

    #region Collections
    /// <summary>
    /// In-memory list of all user roll databases selected via FileRoot (private).
    /// </summary>
    private ObservableCollection<ImageRollsDatabase> UserDatabases { get; } = [];

    /// <summary>
    /// Aggregate of both fixed and user rolls. Consumers filter by origin.
    /// </summary>
    public ObservableCollection<ImageRoll> AllImageRolls { get; } = [];
    #endregion

    #region Settings-backed Properties
    public bool RightAlignOverflow
    {
        get => App.Settings.GetValue(nameof(RightAlignOverflow), false);
        set => App.Settings.SetValue(nameof(RightAlignOverflow), value);
    }
    #endregion

    #region Constructor
    public ImageRollsManager()
    {
        IsLoading = true;

        if (!Directory.Exists(FileRoot.Path))
            FileRoot = new FileFolderEntry(App.UserImageRollsRoot);

        // Fixed rolls first (assets)
        WeakReferenceMessenger.Default.Send(new SplashScreenMessage("Loading Fixed Image Rolls..."));
        LoadFixedImageRollsList();

        // Setup user DB roots and load user rolls
        UpdateFileFolderEvents(FileRoot);
        UpdateImageRollsDatabasesList();

        WeakReferenceMessenger.Default.Send(new SplashScreenMessage("Loading User Image Rolls..."));
        LoadUserImageRollsList();

        // Defer active selection until dispatcher idle so collections are populated
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ContextIdle, InitializeActiveRollSelection);

        App.Settings.PropertyChanged += Settings_PropertyChanged;
        IsActive = true;
    }
    #endregion

    #region Splash Handling
    /// <summary>
    /// Starts (or restarts) the splash screen with a message.
    /// </summary>
    private void StartSplash(string message)
    {
        IsLoading = true;
        _splashCloseRequested = false;
        _awaitingRenderMessage = false;
        App.ShowSplashScreen = true;
        WeakReferenceMessenger.Default.Send(new SplashScreenMessage(message));
    }

    /// <summary>
    /// Idempotently ends splash display.
    /// </summary>
    private void EndSplash()
    {
        if (_splashCloseRequested)
            return;

        _splashCloseRequested = true;
        IsLoading = false;
        App.ShowSplashScreen = false;
        WeakReferenceMessenger.Default.Send(new CloseSplashScreenMessage(true));
    }

    /// <summary>
    /// Evaluates whether the splash should remain visible based on the active roll's state.
    /// </summary>
    private void EvaluateSplashForActiveRoll(bool allowStart = false)
    {
        if (!_activeRollInitialized)
            return;

        // No active roll -> close splash
        if (ActiveImageRoll == null)
        {
            EndSplash();
            return;
        }

        // Active but empty -> nothing to await
        if (ActiveImageRoll.ImageCount == 0)
        {
            EndSplash();
            return;
        }

        // Active and has images -> only start splash if flagged; await render completion
        if (allowStart)
        {
            StartSplash(ActiveImageRoll != null
                ? $"Loading Image Roll: {ActiveImageRoll.Name}..."
                : "Loading Image Roll...");
        }

        _awaitingRenderMessage = true;
    }
    #endregion

    #region Messenger Receivers
    /// <summary>
    /// Called when downstream processing/rendering finishes; may close splash.
    /// </summary>
    public void Receive(ResultssRenderedMessage message)
    {
        if (IsLoading && _awaitingRenderMessage && !_splashCloseRequested)
            EndSplash();
    }
    #endregion

    #region Active Roll Initialization
    /// <summary>
    /// Restores previously active roll or selects first available.
    /// </summary>
    private void InitializeActiveRollSelection()
    {
        if (_activeRollInitialized)
            return;

        RestoreActiveImageRoll();

        if (ActiveImageRoll == null && AllImageRolls.Count > 0)
            ActiveImageRoll = AllImageRolls[0];

        _activeRollInitialized = true;

        // Now decide if splash can end (do not start a new one here)
        EvaluateSplashForActiveRoll(allowStart: false);
    }

    private void RestoreActiveImageRoll()
    {
        string uid = App.Settings.GetValue(ActiveImageRollUidSettingKey, (string)null, true);
        if (string.IsNullOrWhiteSpace(uid))
            return;

        var match = AllImageRolls.FirstOrDefault(r => r.UID == uid);
        if (match != null)
            ActiveImageRoll = match;
    }
    #endregion

    #region Settings Change Handling
    private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(RightAlignOverflow))
            OnPropertyChanged(nameof(RightAlignOverflow));
    }
    #endregion

    #region Roll Enumerators (Helpers)
    private IEnumerable<ImageRoll> EnumerateUserRolls() =>
        AllImageRolls.Where(r => r.ImageRollsDatabase != null);

    private IEnumerable<ImageRoll> EnumerateFixedRolls() =>
        AllImageRolls.Where(r => r.ImageRollsDatabase == null && r.Path != null);
    #endregion

    #region File System Enumeration (User Database Selection Tree)
    /// <summary>
    /// Recursively synchronizes a FileFolderEntry tree with current filesystem state.
    /// Adds new directories/files and removes deleted ones.
    /// </summary>
    private FileFolderEntry EnumerateFolders(FileFolderEntry root)
    {
        var currentDirectories = Directory.EnumerateDirectories(root.Path).ToHashSet();
        var currentFiles = Directory.EnumerateFiles(root.Path, "*.sqlite").ToHashSet();

        // Remove missing directories
        for (int i = root.Children.Count - 1; i >= 0; i--)
        {
            var child = root.Children[i];
            if (child.IsDirectory && !currentDirectories.Contains(child.Path))
                root.Children.RemoveAt(i);
        }

        // Remove missing files
        for (int i = root.Children.Count - 1; i >= 0; i--)
        {
            var child = root.Children[i];
            if (!child.IsDirectory && !currentFiles.Contains(child.Path))
                root.Children.RemoveAt(i);
        }

        // Add new directories
        foreach (var dir in currentDirectories)
            if (!root.Children.Any(c => c.Path == dir))
                root.Children.Add(EnumerateFolders(GetNewFileFolderEntry(dir)));

        // Add new files
        foreach (var file in currentFiles)
            if (!root.Children.Any(c => c.Path == file))
                root.Children.Add(GetNewFileFolderEntry(file));

        return root;
    }

    /// <summary>
    /// Creates a FileFolderEntry and wires selection change to trigger DB reload of user rolls.
    /// </summary>
    private FileFolderEntry GetNewFileFolderEntry(string path)
    {
        FileFolderEntry ffe = new(path);
        ffe.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == "IsSelected")
            {
                UpdateDatabases(FileRoot);
                App.Settings.SetValue("UserImageRollsDatabases_FileRoot", FileRoot);
                WeakReferenceMessenger.Default.Send(new SplashScreenMessage("Refreshing User Image Rolls..."));
                LoadUserImageRollsList();
                if (_activeRollInitialized && IsLoading)
                    EvaluateSplashForActiveRoll();
            }
        };
        return ffe;
    }

    private FileFolderEntry GetFileFolderEntry(string path) =>
        GetAllFiles(FileRoot).FirstOrDefault(e => e.Path == path);

    private void UpdateFileFolderEvents(FileFolderEntry root)
    {
        root.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == "IsSelected")
            {
                UpdateDatabases(FileRoot);
                App.Settings.SetValue("UserImageRollsDatabases_FileRoot", FileRoot);
                WeakReferenceMessenger.Default.Send(new SplashScreenMessage("Refreshing User Image Rolls..."));
                LoadUserImageRollsList();
                if (_activeRollInitialized && IsLoading)
                    EvaluateSplashForActiveRoll();
            }
        };

        foreach (var child in root.Children)
            UpdateFileFolderEvents(child);
    }

    /// <summary>
    /// Returns only selected .sqlite file entries in subtree.
    /// </summary>
    private List<FileFolderEntry> GetSelectedFiles(FileFolderEntry root)
    {
        List<FileFolderEntry> selected = [];
        foreach (var child in root.Children)
        {
            if (child.IsDirectory)
                selected.AddRange(GetSelectedFiles(child));
            else if (child.IsSelected)
                selected.Add(child);
        }
        return selected;
    }

    /// <summary>
    /// Returns all file entries (NOTE: current implementation uses GetSelectedFiles for directories;
    /// if intention was to gather all *including unselected* files, this may be a logic bug.)
    /// </summary>
    private List<FileFolderEntry> GetAllFiles(FileFolderEntry root)
    {
        List<FileFolderEntry> files = [];
        foreach (var child in root.Children)
        {
            if (child.IsDirectory)
                files.AddRange(GetSelectedFiles(child)); // Potential limitation; left intact intentionally.
            else
                files.Add(child);
        }
        return files;
    }
    #endregion

    #region User Roll Database Management
    /// <summary>
    /// Discovers .sqlite user roll DB files and ensures default file exists.
    /// </summary>
    private void UpdateImageRollsDatabasesList()
    {
        Logger.Info($"Loading Image Rolls databases from file system. {App.UserImageRollsRoot}");

        // Ensure default DB file exists
        if (!File.Exists(App.UserImageRollDefaultFile))
        {
            var tmp = new ImageRollsDatabase(new FileFolderEntry(App.UserImageRollDefaultFile));
            tmp.Close();
        }

        FileRoot = EnumerateFolders(FileRoot);
        UpdateDatabases(FileRoot);
    }

    /// <summary>
    /// Synchronizes open ImageRollsDatabase instances with current selection state.
    /// </summary>
    private void UpdateDatabases(FileFolderEntry root)
    {
        var selectedFiles = GetSelectedFiles(root).Select(f => f.Path).ToHashSet();

        // Remove deselected
        for (int i = UserDatabases.Count - 1; i >= 0; i--)
        {
            var db = UserDatabases[i];
            if (!selectedFiles.Contains(db.File.Path))
            {
                db.Close();
                UserDatabases.RemoveAt(i);
            }
        }

        // Add newly selected
        foreach (var file in selectedFiles)
        {
            if (!UserDatabases.Any(db => db.File.Path == file))
                UserDatabases.Add(new ImageRollsDatabase(new FileFolderEntry(file)));
        }

        SetSelectedUserDatabase();
    }

    /// <summary>
    /// Ensures a selected database is always defined (default prioritized).
    /// </summary>
    private void SetSelectedUserDatabase()
    {
        var def = UserDatabases.FirstOrDefault(e => e.File.Path == App.UserImageRollDefaultFile);

        if (def == null)
        {
            SelectedUserDatabase?.Close();
            SelectedUserDatabase = new ImageRollsDatabase(new FileFolderEntry(App.UserImageRollDefaultFile));
        }
        else if (SelectedUserDatabase != def)
        {
            SelectedUserDatabase?.Close();
            SelectedUserDatabase = def;
        }
    }

    /// <summary>
    /// Loads user rolls either from cache (if metadata matches) or from underlying databases.
    /// </summary>
    private void LoadUserImageRollsList()
    {
        var selectedDbFiles = UserDatabases.Where(db => db.File.IsSelected).ToList();
        var currentMetadata = selectedDbFiles.ToDictionary(db => db.File.Path, db => File.GetLastWriteTimeUtc(db.File.Path));
        var cachedMetadata = App.Settings.GetValue("UserImageRolls_CacheMetadata", new Dictionary<string, DateTime>());

        bool failed = false;

        // Cache validity check (metadata equality)
        if (cachedMetadata.Count > 0 &&
            !cachedMetadata.Except(currentMetadata).Any() &&
            !currentMetadata.Except(cachedMetadata).Any())
        {
            Logger.Info("Loading user image rolls from cache.");
            _ = Application.Current.Dispatcher.Invoke(DispatcherPriority.Render,
                () => WeakReferenceMessenger.Default.Send(new SplashScreenMessage("Loading User Image Rolls (Cache)...")));

            var cachedRolls = App.Settings.GetValue("UserImageRolls_Cache", new List<ImageRoll>());

            // Clear existing user rolls
            foreach (var ur in EnumerateUserRolls().ToList())
                AllImageRolls.Remove(ur);

            // Rehydrate and inject references
            foreach (var roll in cachedRolls)
            {
                roll.ImageRollsDatabase = SelectedUserDatabase;
                if (roll.ImageRollsDatabase is null)
                {
                    failed = true;
                    break;
                }
                roll.ImageRollsManager = this;
                AllImageRolls.Add(roll);
            }
            if (!failed)
            {
                Logger.Info($"Processed {EnumerateUserRolls().Count()} user image rolls from cache.");
                if (_activeRollInitialized && IsLoading)
                    EvaluateSplashForActiveRoll(allowStart: false);
                return;
            }
            Logger.Warning("Failed to load cached user rolls due to missing DB references. Reloading.");
        }

        // Cache invalid -> reload from databases
        _ = Application.Current.Dispatcher.Invoke(DispatcherPriority.Render,
            () => WeakReferenceMessenger.Default.Send(new SplashScreenMessage("Indexing User Image Rolls...")));
        Logger.Info("User image roll cache invalid/missing. Loading from databases.");

        var newRolls = new List<ImageRoll>();
        foreach (var db in selectedDbFiles)
        {
            Logger.Info($"Loading user image rolls from database: {db.File.Name}");
            try
            {
                foreach (var roll in db.SelectAllImageRolls())
                {
                    Logger.Debug($"Found: {roll.Name}");
                    roll.ImageRollsDatabase = db;
                    roll.ImageRollsManager = this;
                    newRolls.Add(roll);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error accessing {db.File.Path}");
            }
        }

        // Replace existing user rolls with fresh list
        foreach (var existing in EnumerateUserRolls().ToList())
            AllImageRolls.Remove(existing);

        foreach (var roll in newRolls)
            AllImageRolls.Add(roll);

        App.Settings.SetValue("UserImageRolls_Cache", EnumerateUserRolls().ToList());
        App.Settings.SetValue("UserImageRolls_CacheMetadata", currentMetadata);

        Logger.Info($"Processed {EnumerateUserRolls().Count()} user image rolls.");
        if (_activeRollInitialized && IsLoading)
            EvaluateSplashForActiveRoll(allowStart: false);
    }

    /// <summary>
    /// Adds or updates a single roll in cache after persistence.
    /// </summary>
    private void UpdateUserImageRollCache(ImageRoll roll)
    {
        if (roll?.ImageRollsDatabase?.File?.Path is null)
        {
            Logger.Warning("Cannot update user cache: no database path.");
            return;
        }

        Logger.Info($"Updating user image roll cache: {roll.Name}");

        if (AllImageRolls.FirstOrDefault(r => r.UID == roll.UID) == null)
        {
            roll.ImageRollsManager = this;
            AllImageRolls.Add(roll);
        }

        App.Settings.SetValue("UserImageRolls_Cache", EnumerateUserRolls().ToList());
        UpdateUserImageRollCacheMetadata();

        if (_activeRollInitialized && IsLoading)
            EvaluateSplashForActiveRoll(allowStart: false);
    }

    /// <summary>
    /// Synchronizes cached DB modification timestamps for invalidation logic.
    /// </summary>
    private void UpdateUserImageRollCacheMetadata()
    {
        var cachedMetadata = App.Settings.GetValue("UserImageRolls_CacheMetadata", new Dictionary<string, DateTime>());
        var dbPath = SelectedUserDatabase.File.Path;
        if (File.Exists(dbPath))
        {
            cachedMetadata[dbPath] = File.GetLastWriteTimeUtc(dbPath);
            App.Settings.SetValue("UserImageRolls_CacheMetadata", cachedMetadata);
        }
    }
    #endregion

    #region Fixed (Asset) Roll Loading
    /// <summary>
    /// Loads fixed rolls (immutable asset-based) either from cache or by scanning the assets directory.
    /// </summary>
    private void LoadFixedImageRollsList()
    {
        Logger.Info($"Loading fixed image rolls from assets: {App.AssetsImageRollsRoot}");

        var cachedMetadata = App.Settings.GetValue("FixedImageRolls_CacheMetadata", new Dictionary<string, DateTime>());
        var currentMetadata = GetDirectoryMetadata(App.AssetsImageRollsRoot, "*.imgr");

        bool failed = false;

        // Attempt cache load
        if (cachedMetadata.Count > 0 &&
            !cachedMetadata.Except(currentMetadata).Any() &&
            !currentMetadata.Except(cachedMetadata).Any())
        {
            Logger.Info("Loading fixed image rolls from cache.");
            _ = Application.Current.Dispatcher.Invoke(DispatcherPriority.Render,
                () => WeakReferenceMessenger.Default.Send(new SplashScreenMessage("Loading Fixed Image Rolls (Cache)...")));

            var cachedRolls = App.Settings.GetValue("FixedImageRolls_Cache", new List<ImageRoll>());

            foreach (var fr in EnumerateFixedRolls().ToList())
                AllImageRolls.Remove(fr);

            foreach (var roll in cachedRolls)
            {
                if (roll.Path == null || !Directory.Exists(roll.Path))
                {
                    failed = true;
                    break;
                }
                AllImageRolls.Add(roll);
            }
            if (!failed)
            {
                Logger.Info($"Processed {EnumerateFixedRolls().Count()} fixed rolls from cache.");
                return;
            }
            Logger.Warning("Failed to load fixed rolls from cache due to missing paths; reindexing.");
        }

        // Rebuild from filesystem
        _ = Application.Current.Dispatcher.Invoke(DispatcherPriority.Render,
            () => _ = WeakReferenceMessenger.Default.Send(new SplashScreenMessage("Indexing Fixed Image Rolls...")));

        Logger.Info("Fixed rolls cache invalid/missing. Loading from filesystem.");

        foreach (var existing in EnumerateFixedRolls().ToList())
            AllImageRolls.Remove(existing);

        foreach (var dir in Directory.EnumerateDirectories(App.AssetsImageRollsRoot)
                     .ToList()
                     .OrderBy(e => Regex.Replace(e, "[0-9]+", m => m.Value.PadLeft(10, '0'))))
        {
            foreach (var subdir in Directory.EnumerateDirectories(dir))
            {
                var files = Directory.EnumerateFiles(subdir, "*.imgr").ToList();
                if (files.Count == 0)
                    continue;

                try
                {
                    var imgr = JsonConvert.DeserializeObject<ImageRoll>(File.ReadAllText(files.First()));
                    imgr.Path = subdir;
                    imgr.ImageRollsManager = this;
                    AllImageRolls.Add(imgr);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Failed to load image roll from {files.First()}");
                }
            }
        }

        App.Settings.SetValue("FixedImageRolls_Cache", EnumerateFixedRolls().ToList());
        App.Settings.SetValue("FixedImageRolls_CacheMetadata", currentMetadata);
    }
    #endregion

    #region Metadata Helpers
    /// <summary>
    /// Recursively gathers directory timestamps + matching file timestamps for invalidation checks.
    /// </summary>
    private Dictionary<string, DateTime> GetDirectoryMetadata(string path, string searchPattern)
    {
        var metadata = new Dictionary<string, DateTime>();
        if (!Directory.Exists(path))
            return metadata;

        var stack = new Stack<string>();
        stack.Push(path);

        while (stack.Count > 0)
        {
            var currentDir = stack.Pop();
            metadata[currentDir] = Directory.GetLastWriteTimeUtc(currentDir);

            foreach (var d in Directory.GetDirectories(currentDir))
                stack.Push(d);

            foreach (var f in Directory.GetFiles(currentDir, searchPattern))
                metadata[f] = File.GetLastWriteTimeUtc(f);
        }

        return metadata;
    }
    #endregion

    #region Persistence (Save / Update Image Rolls & Images)
    public void SaveUserImageRoll(ImageRoll roll)
    {
        if (SelectedUserDatabase.InsertOrReplaceImageRoll(roll) > 0)
        {
            Logger.Info($"Saved image roll: {roll.Name}");
            UpdateUserImageRollCache(roll);
        }
        else
            Logger.Error($"Failed to save image roll: {roll.Name}");
    }

    public void SaveImageEntry(ImageEntry img)
    {
        if (SelectedUserDatabase.InsertOrReplaceImage(img) > 0)
        {
            Logger.Info($"Saved image: {img.Name}");
            UpdateUserImageRollCacheMetadata();
        }
        else
            Logger.Error($"Failed to save image: {img.Name}");
    }
    #endregion

    #region Commands (Add / Edit / Save / Delete / Cancel / Utilities)
    [RelayCommand]
    private void Add()
    {
        Logger.Info("Adding image roll.");
        NewImageRoll = new ImageRoll(this)
        {
            IsSaved = false,
            ImageRollsDatabase = SelectedUserDatabase
        };
    }

    [RelayCommand]
    private void Edit()
    {
        if (ActiveImageRoll == null || ActiveImageRoll.ImageRollsDatabase == null)
            return;

        Logger.Info("Editing image roll.");
        NewImageRoll = ActiveImageRoll.CopyLite();
        NewImageRoll.ImageRollsDatabase = SelectedUserDatabase;
        NewImageRoll.ImageRollsManager = this;
    }

    [RelayCommand]
    public void Save()
    {
        if (NewImageRoll == null)
            return;

        if (NewImageRoll.HasErrors)
        {
            foreach (var err in NewImageRoll.GetErrors())
                Logger.Warning(err.ErrorMessage);
            return;
        }

        if (SelectedUserDatabase.InsertOrReplaceImageRoll(NewImageRoll) > 0)
        {
            Logger.Info($"Saved image roll: {NewImageRoll.Name}");

            var existing = AllImageRolls.FirstOrDefault(r => r.UID == NewImageRoll.UID);
            if (existing != null)
            {
                // Update properties in-place (avoid replacing reference used in bindings)
                existing.Name = NewImageRoll.Name;
                existing.SelectedApplicationStandard = NewImageRoll.SelectedApplicationStandard;
                existing.SelectedGS1Table = NewImageRoll.SelectedGS1Table;
                existing.SelectedGradingStandard = NewImageRoll.SelectedGradingStandard;
                existing.TargetDPI = NewImageRoll.TargetDPI;
                existing.IsLocked = NewImageRoll.IsLocked;
                existing.SectorType = NewImageRoll.SectorType;
                existing.ImageType = NewImageRoll.ImageType;
            }
            else
            {
                var savedRoll = NewImageRoll.CopyLite();
                savedRoll.ImageRollsDatabase = SelectedUserDatabase;
                savedRoll.ImageRollsManager = this;
                savedRoll.IsSaved = true;
                AllImageRolls.Add(savedRoll);
                ActiveImageRoll = savedRoll;
            }

            UpdateUserImageRollCache(NewImageRoll);

            // Ensure default DB file stays selected
            var file = GetFileFolderEntry(App.UserImageRollDefaultFile);
            if (file != null)
                file.IsSelected = true;

            if (_activeRollInitialized && IsLoading)
                EvaluateSplashForActiveRoll();
        }
        else
            Logger.Error($"Failed to save image roll: {NewImageRoll.Name}");

        NewImageRoll = null;
    }

    [RelayCommand]
    public async Task Delete()
    {
        if (NewImageRoll == null || NewImageRoll.ImageRollsDatabase == null)
            return;

        if (await OkCancelDialog("Delete Image Roll and all Stored Images and related Result Entries?",
                $"Are you sure you want to delete image roll {NewImageRoll.Name}?") != MessageDialogResult.Affirmative)
            return;

        if (NewImageRoll.ImageRollsDatabase.DeleteAllImages(NewImageRoll.UID))
            Logger.Info($"Deleted all images for roll {NewImageRoll.UID}");
        else
            Logger.Error($"Failed to delete images for roll {NewImageRoll.UID}");

        if (NewImageRoll.ImageRollsDatabase.DeleteImageRoll(NewImageRoll.UID))
            Logger.Info($"Deleted image roll {NewImageRoll.UID}");
        else
            Logger.Error($"Failed to delete image roll {NewImageRoll.UID}");

        WeakReferenceMessenger.Default.Send(new DeleteResultsForRollMessage(NewImageRoll.UID));

        // Refresh list (does NOT change ActiveImageRoll)
        LoadUserImageRollsList();

        // If the deleted roll was the active one, DO NOT auto-select another; clear selection.
        if (ActiveImageRoll?.UID == NewImageRoll.UID)
            ActiveImageRoll = null;   // Persisted setting is cleared in OnActiveImageRollChanged

        NewImageRoll = null;

        if (_activeRollInitialized && IsLoading)
            EvaluateSplashForActiveRoll();
    }

    [RelayCommand]
    public void Cancel() => NewImageRoll = null;

    [RelayCommand]
    private void UIDToClipboard() => Clipboard.SetText(Guid.NewGuid().ToString());
    #endregion

    #region Dialog Helpers
    private static IDialogCoordinator DialogCoordinator => MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance;

    public async Task<MessageDialogResult> OkCancelDialog(string title, string message) =>
        await DialogCoordinator.ShowMessageAsync(this, title, message, MessageDialogStyle.AffirmativeAndNegative);
    #endregion

    #region IDisposable
    public void Dispose()
    {
        App.Settings.PropertyChanged -= Settings_PropertyChanged;
        GC.SuppressFinalize(this);
    }
    #endregion
}