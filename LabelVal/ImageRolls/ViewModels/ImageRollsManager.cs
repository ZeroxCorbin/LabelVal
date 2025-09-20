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
/// Unified manager for fixed (assets) and user (database) image rolls.
/// Splash logic (per requirements):
/// - Keep splash visible while determining active roll.
/// - Close ONLY if:
///     (a) Stored UID is null/invalid (no active roll found), OR
///     (b) Active roll exists but has zero images.
/// - If active roll exists and has images (>0), DO NOT close until ResultssRenderedMessage arrives.
/// </summary>
public partial class ImageRollsManager : ObservableRecipient, IDisposable, IRecipient<ResultssRenderedMessage>
{
    private const string ActiveImageRollUidSettingKey = "ActiveImageRollUID";

    private bool _activeRollInitialized;
    private bool _splashCloseRequested;
    private bool _awaitingRenderMessage;   // True when we must wait for ResultssRenderedMessage before closing

    public GlobalAppSettings AppSettings => GlobalAppSettings.Instance;

    [ObservableProperty] private bool _isLoading;

    [ObservableProperty]
    private FileFolderEntry fileRoot =
        App.Settings.GetValue("UserImageRollsDatabases_FileRoot", new FileFolderEntry(App.UserImageRollsRoot), true);
    partial void OnFileRootChanged(FileFolderEntry value) =>
        App.Settings.SetValue("UserImageRollsDatabases_FileRoot", value);

    private ObservableCollection<ImageRollsDatabase> UserDatabases { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    private ImageRollsDatabase selectedUserDatabase;

    [ObservableProperty] private ImageRoll newImageRoll = null;

    public ObservableCollection<ImageRoll> AllImageRolls { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    private ImageRoll activeImageRoll;
    partial void OnActiveImageRollChanged(ImageRoll value)
    {
        // Persist UID
        App.Settings.SetValue(ActiveImageRollUidSettingKey, value?.UID);

        if (!_activeRollInitialized)
            return;

        // Re-evaluate always on change (now supports re-opening splash when user selects a different roll)
        EvaluateSplashForActiveRoll(allowStart:true);
    }

    [ObservableProperty] private bool refreshView;

    public bool RightAlignOverflow
    {
        get => App.Settings.GetValue(nameof(RightAlignOverflow), false);
        set => App.Settings.SetValue(nameof(RightAlignOverflow), value);
    }

    public ImageRollsManager()
    {
        //StartSplash("Loading Image Rolls...");

        if (!Directory.Exists(FileRoot.Path))
            FileRoot = new FileFolderEntry(App.UserImageRollsRoot);

        WeakReferenceMessenger.Default.Send(new SplashScreenMessage("Loading Fixed Image Rolls..."));
        LoadFixedImageRollsList();

        UpdateFileFolderEvents(FileRoot);
        UpdateImageRollsDatabasesList();

        WeakReferenceMessenger.Default.Send(new SplashScreenMessage("Loading User Image Rolls..."));
        LoadUserImageRollsList();

        // Defer final evaluation to allow UI materialization.
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ContextIdle, InitializeActiveRollSelection);

        App.Settings.PropertyChanged += Settings_PropertyChanged;
        IsActive = true;
    }

    #region Splash Handling

    private void StartSplash(string message)
    {
        IsLoading = true;
        _splashCloseRequested = false;
        _awaitingRenderMessage = false;
        App.ShowSplashScreen = true;
        WeakReferenceMessenger.Default.Send(new SplashScreenMessage(message));
    }

    private void EndSplash()
    {
        if (_splashCloseRequested) return;
        _splashCloseRequested = true;
        IsLoading = false;
        App.ShowSplashScreen = false;
        WeakReferenceMessenger.Default.Send(new CloseSplashScreenMessage(true));
    }

    #endregion

    /// <summary>
    /// Implements splash rules and (optionally) re-opens splash when active roll changes.
    /// Rules:
    ///   - If ActiveImageRoll is null (invalid / missing) => close splash.
    ///   - If ActiveImageRoll has zero images => close splash.
    ///   - If ActiveImageRoll has images (>0) => keep (or show) splash until ResultssRenderedMessage.
    /// </summary>
    /// <param name="allowStart">If true, will (re)start the splash if it is not currently showing.</param>
    private void EvaluateSplashForActiveRoll(bool allowStart = false)
    {
        if (!_activeRollInitialized)
            return;

        // No active roll -> close immediately.
        if (ActiveImageRoll == null)
        {
            EndSplash();
            return;
        }

        // Active roll but zero images -> close immediately.
        if (ActiveImageRoll.ImageCount == 0)
        {
            EndSplash();
            return;
        }

        // Re-open splash if caller allows and we are currently not showing it.
        // Removed the !_splashCloseRequested gate so a new splash session can start
        // after a previous one was closed (e.g., user switches to another roll).
        if (allowStart)
        {
            StartSplash(ActiveImageRoll != null
                ? $"Loading Image Roll: {ActiveImageRoll.Name}..."
                : "Loading Image Roll...");
        }

        // Active roll has images: wait for ResultssRenderedMessage before closing.
        _awaitingRenderMessage = true;
    }

    public void Receive(ResultssRenderedMessage message)
    {
        // Only close if we are in the waiting-for-render state
        if (IsLoading && _awaitingRenderMessage && !_splashCloseRequested)
            EndSplash();
    }

    private void InitializeActiveRollSelection()
    {
        if (_activeRollInitialized)
            return;

        RestoreActiveImageRoll();

        if (ActiveImageRoll == null && AllImageRolls.Count > 0)
            ActiveImageRoll = AllImageRolls[0];

        _activeRollInitialized = true;

        // Initial evaluation (do not auto-open twice; splash is already started from constructor)
        EvaluateSplashForActiveRoll(allowStart:false);
    }

    private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
    if (e.PropertyName == nameof(RightAlignOverflow))
        OnPropertyChanged(nameof(RightAlignOverflow));
    }

    private IEnumerable<ImageRoll> EnumerateUserRolls() =>
        AllImageRolls.Where(r => r.ImageRollsDatabase != null);

    private IEnumerable<ImageRoll> EnumerateFixedRolls() =>
        AllImageRolls.Where(r => r.ImageRollsDatabase == null && r.Path != null);

    private void RestoreActiveImageRoll()
    {
        string uid = App.Settings.GetValue(ActiveImageRollUidSettingKey, (string)null, true);
        if (string.IsNullOrWhiteSpace(uid))
            return;

        var match = AllImageRolls.FirstOrDefault(r => r.UID == uid);
        if (match != null)
            ActiveImageRoll = match;
    }

    private FileFolderEntry EnumerateFolders(FileFolderEntry root)
    {
        var currentDirectories = Directory.EnumerateDirectories(root.Path).ToHashSet();
        var currentFiles = Directory.EnumerateFiles(root.Path, "*.sqlite").ToHashSet();

        for (int i = root.Children.Count - 1; i >= 0; i--)
        {
            var child = root.Children[i];
            if (child.IsDirectory && !currentDirectories.Contains(child.Path))
                root.Children.RemoveAt(i);
        }

        for (int i = root.Children.Count - 1; i >= 0; i--)
        {
            var child = root.Children[i];
            if (!child.IsDirectory && !currentFiles.Contains(child.Path))
                root.Children.RemoveAt(i);
        }

        foreach (var dir in currentDirectories)
            if (!root.Children.Any(c => c.Path == dir))
                root.Children.Add(EnumerateFolders(GetNewFileFolderEntry(dir)));

        foreach (var file in currentFiles)
            if (!root.Children.Any(c => c.Path == file))
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
                WeakReferenceMessenger.Default.Send(new SplashScreenMessage("Refreshing User Image Rolls..."));
                LoadUserImageRollsList();

                // Re-evaluate only if still loading & initialized (could impact early close conditions)
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

    private List<FileFolderEntry> GetAllFiles(FileFolderEntry root)
    {
        List<FileFolderEntry> files = [];
        foreach (var child in root.Children)
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
            var tmp = new ImageRollsDatabase(new FileFolderEntry(App.UserImageRollDefaultFile));
            tmp.Close();
        }

        FileRoot = EnumerateFolders(FileRoot);
        UpdateDatabases(FileRoot);
    }

    private void UpdateDatabases(FileFolderEntry root)
    {
        var selectedFiles = GetSelectedFiles(root).Select(f => f.Path).ToHashSet();

        for (int i = UserDatabases.Count - 1; i >= 0; i--)
        {
            var db = UserDatabases[i];
            if (!selectedFiles.Contains(db.File.Path))
            {
                db.Close();
                UserDatabases.RemoveAt(i);
            }
        }

        foreach (var file in selectedFiles)
        {
            if (!UserDatabases.Any(db => db.File.Path == file))
                UserDatabases.Add(new ImageRollsDatabase(new FileFolderEntry(file)));
        }

        SetSelectedUserDatabase();
    }

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

    private void LoadUserImageRollsList()
    {
        var selectedDbFiles = UserDatabases.Where(db => db.File.IsSelected).ToList();
        var currentMetadata = selectedDbFiles.ToDictionary(db => db.File.Path, db => File.GetLastWriteTimeUtc(db.File.Path));
        var cachedMetadata = App.Settings.GetValue("UserImageRolls_CacheMetadata", new Dictionary<string, DateTime>());

        bool failed = false;

        if (cachedMetadata.Count > 0 &&
            !cachedMetadata.Except(currentMetadata).Any() &&
            !currentMetadata.Except(cachedMetadata).Any())
        {
            Logger.Info("Loading user image rolls from cache.");
            _ = Application.Current.Dispatcher.Invoke(DispatcherPriority.Render,
                () => WeakReferenceMessenger.Default.Send(new SplashScreenMessage("Loading User Image Rolls (Cache)...")));

            var cachedRolls = App.Settings.GetValue("UserImageRolls_Cache", new List<ImageRoll>());

            foreach (var ur in EnumerateUserRolls().ToList())
                AllImageRolls.Remove(ur);

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
                    EvaluateSplashForActiveRoll(allowStart:false);
                return;
            }
            Logger.Warning("Failed to load cached user rolls due to missing DB references. Reloading.");
        }

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

        foreach (var existing in EnumerateUserRolls().ToList())
            AllImageRolls.Remove(existing);

        foreach (var roll in newRolls)
            AllImageRolls.Add(roll);

        App.Settings.SetValue("UserImageRolls_Cache", EnumerateUserRolls().ToList());
        App.Settings.SetValue("UserImageRolls_CacheMetadata", currentMetadata);

        Logger.Info($"Processed {EnumerateUserRolls().Count()} user image rolls.");
        if (_activeRollInitialized && IsLoading)
            EvaluateSplashForActiveRoll(allowStart:false);
    }

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

        // If still loading (e.g. initial phase) re-evaluate; do not start new splash here
        if (_activeRollInitialized && IsLoading)
            EvaluateSplashForActiveRoll(allowStart:false);
    }

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

    private void LoadFixedImageRollsList()
    {
        Logger.Info($"Loading fixed image rolls from assets: {App.AssetsImageRollsRoot}");

        var cachedMetadata = App.Settings.GetValue("FixedImageRolls_CacheMetadata", new Dictionary<string, DateTime>());
        var currentMetadata = GetDirectoryMetadata(App.AssetsImageRollsRoot, "*.imgr");

        bool failed = false;

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
                RefreshView = !RefreshView;
                ActiveImageRoll = savedRoll;
            }

            UpdateUserImageRollCache(NewImageRoll);

            var file = GetFileFolderEntry(App.UserImageRollDefaultFile);
            if (file != null)
                file.IsSelected = true;

            // If still loading (unlikely after init) re-evaluate
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

        LoadUserImageRollsList();

        if (ActiveImageRoll?.UID == NewImageRoll.UID)
            ActiveImageRoll = AllImageRolls.FirstOrDefault();

        NewImageRoll = null;

        if (_activeRollInitialized && IsLoading)
            EvaluateSplashForActiveRoll();
    }

    [RelayCommand]
    public void Cancel() => NewImageRoll = null;

    [RelayCommand]
    private void UIDToClipboard() => Clipboard.SetText(Guid.NewGuid().ToString());

    private static IDialogCoordinator DialogCoordinator => MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance;
    public async Task<MessageDialogResult> OkCancelDialog(string title, string message) =>
        await DialogCoordinator.ShowMessageAsync(this, title, message, MessageDialogStyle.AffirmativeAndNegative);

    public void Dispose()
    {
        App.Settings.PropertyChanged -= Settings_PropertyChanged;
        GC.SuppressFinalize(this);
    }
}
