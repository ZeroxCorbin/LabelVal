using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.Extensions;
using BarcodeVerification.lib.GS1;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using ImageUtilities.lib.Wpf;
using LabelVal.ImageRolls.Databases;
using LabelVal.Main.ViewModels;
using LabelVal.Results.Helpers;
using LabelVal.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Windows;
using Wpf.lib.Validation;

namespace LabelVal.ImageRolls.ViewModels;

#region Enums

/// <summary>
/// Specifies the source type of an image roll.
/// </summary>
[SQLite.StoreAsText]
[JsonConverter(typeof(StringEnumConverter))]
public enum ImageRollTypes
{
    /// <summary>
    /// The image roll is stored in a database.
    /// </summary>
    [Description("Database")]
    Database,
    /// <summary>
    /// The image roll is sourced from a file system directory.
    /// </summary>
    [Description("Directory")]
    Directory,
}

/// <summary>
/// Specifies the type of image within an image roll.
/// </summary>
[SQLite.StoreAsText]
[JsonConverter(typeof(StringEnumConverter))]
public enum ImageRollImageTypes
{
    /// <summary>
    /// The image is from the original source.
    /// </summary>
    [Description("Source")]
    Source,
    /// <summary>
    /// The image is stored within the application or database.
    /// </summary>
    [Description("Stored")]
    Stored
}

/// <summary>
/// Specifies how sectors are handled for images in the roll.
/// </summary>
[SQLite.StoreAsText]
[JsonConverter(typeof(StringEnumConverter))]
public enum ImageRollSectorTypes
{
    /// <summary>
    /// Sectors are fixed and do not change.
    /// </summary>
    [Description("Fixed")]
    Fixed,
    /// <summary>
    /// Sectors are dynamic and can be updated.
    /// </summary>
    [Description("Dynamic")]
    Dynamic
}

/// <summary>
/// Defines positions for adding new images relative to existing ones.
/// </summary>
[SQLite.StoreAsText]
[JsonConverter(typeof(StringEnumConverter))]
public enum ImageAddPositions
{
    /// <summary>
    /// Add to the top of the list.
    /// </summary>
    Top,
    /// <summary>
    /// Add above a selected item.
    /// </summary>
    Above,
    /// <summary>
    /// Add below a selected item.
    /// </summary>
    Below,
    /// <summary>
    /// Add to the bottom of the list.
    /// </summary>
    Bottom
}

#endregion

/// <summary>
/// Represents the method that will handle the <see cref="ImageRoll.ImageMoved"/> event.
/// </summary>
/// <param name="sender">The source of the event.</param>
/// <param name="imageEntry">The image entry that was moved.</param>
public delegate void ImageMovedEventHandler(object sender, ImageEntry imageEntry);

/// <summary>
/// Represents a roll of images, which can be sourced from a database or a directory.
/// This class manages the collection of images, their properties, and related operations.
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
[ObservableRecipient]
public partial class ImageRoll : ObservableValidator, IRecipient<PropertyChangedMessage<PrinterSettings>>, IDisposable
{
    #region Events

    /// <summary>
    /// Occurs when an image is moved within the roll.
    /// </summary>
    public event ImageMovedEventHandler ImageMoved;

    #endregion

    #region Properties

    /// <summary>
    /// Gets the global application settings instance.
    /// </summary>
    [SQLite.Ignore] public GlobalAppSettings AppSettings => GlobalAppSettings.Instance;

    [SQLite.Ignore] public ImageRollsManager ImageRollsManager { get; set; }

    /// <summary>
    /// The unique identifier for the ImageRoll. This is also called the RollID.
    /// </summary>
    [JsonProperty][SQLite.PrimaryKey] public string UID { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the database associated with this image roll.
    /// </summary>
    [SQLite.Ignore] public Databases.ImageRollsDatabase ImageRollsDatabase { get; set; }

    /// <summary>
    /// Gets or sets the path to the database file for this image roll.
    /// </summary>
    [JsonProperty] public string DatabasePath { get; set; }

    /// <summary>
    /// If this is a directory-based image roll, this is the path to the directory where the images are stored.
    /// </summary>
    [JsonProperty] public string Path { get; set; }

    /// <summary>
    /// Gets the type of the image roll (Directory or Database).
    /// </summary>
    [SQLite.Ignore] public ImageRollTypes RollType => !string.IsNullOrEmpty(Path) ? ImageRollTypes.Directory : ImageRollTypes.Database;

    /// <summary>
    /// Gets or sets the type of images in the roll (e.g., source or stored).
    /// </summary>
    [ObservableProperty][property: JsonProperty] private ImageRollImageTypes imageType = ImageRollImageTypes.Source;

    /// <summary>
    /// Gets or sets the sector handling type. If Dynamic, the system will write template sectors before processing an image.
    /// </summary>
    [ObservableProperty][property: JsonProperty] private ImageRollSectorTypes sectorType = ImageRollSectorTypes.Dynamic;


    [ObservableProperty] private bool isSaved = true;


    /// <summary>
    /// Gets or sets the name of the image roll.
    /// </summary>
    [ObservableProperty]
    [Required]
    [CustomValidation(typeof(ImageRoll), nameof(ValidateName))]
    [property: JsonProperty]
    private string name = "";
    partial void OnNameChanged(string oldValue, string newValue)
    {
        ValidateAllProperties();
    }

    /// <summary>
    /// Gets or sets the number of images in the roll.
    /// </summary>
    [ObservableProperty][property: JsonProperty] private int imageCount;

    /// <summary>
    /// Gets or sets the selected grading standard for the roll.
    /// </summary>
    [ObservableProperty][property: JsonProperty("GradingStandard")][property: SQLite.Column("GradingStandard")] private GradingStandards selectedGradingStandard;
    partial void OnSelectedGradingStandardChanged(GradingStandards value) { OnPropertyChanged(nameof(GradingStandardDescription)); OnPropertyChanged(nameof(StandardGroup)); }

    /// <summary>
    /// Gets the description of the selected grading standard.
    /// </summary>
    public string GradingStandardDescription => SelectedGradingStandard.GetDescription();

    /// <summary>
    /// Gets or sets the selected application standard for the roll.
    /// </summary>
    [ObservableProperty][property: JsonProperty("ApplicationStandard")][property: SQLite.Column("ApplicationStandard")] private ApplicationStandards selectedApplicationStandard;
    partial void OnSelectedApplicationStandardChanged(ApplicationStandards value)
    {
        OnPropertyChanged(nameof(ApplicationStandardDescription));
        OnPropertyChanged(nameof(StandardGroup));
        ValidateAllProperties();
    }

    /// <summary>
    /// Gets the description of the selected application standard.
    /// </summary>
    public string ApplicationStandardDescription => SelectedApplicationStandard.GetDescription();

    /// <summary>
    /// Gets or sets the selected GS1 table for the roll.
    /// </summary>
    [ObservableProperty]
    [property: JsonProperty("GS1Table")]
    [property: SQLite.Column("GS1Table")]
    [CustomValidation(typeof(ImageRoll), nameof(ValidateGS1Table))]
    private GS1Tables selectedGS1Table;
    partial void OnSelectedGS1TableChanged(GS1Tables value)
    {
        OnPropertyChanged(nameof(GS1TableNumber));
        ValidateAllProperties();
    }

    /// <summary>
    /// Gets the numeric value of the selected GS1 table.
    /// </summary>
    public double GS1TableNumber => SelectedGS1Table is GS1Tables.Unknown ? 0 : double.Parse(SelectedGS1Table.GetDescription());

    /// <summary>
    /// Gets a string representation of the standard group, combining application and grading standards.
    /// </summary>
    public string StandardGroup => $"{SelectedApplicationStandard.GetDescription()} : {SelectedGradingStandard.GetDescription()}";

    /// <summary>
    /// Gets or sets the target DPI for images in the roll.
    /// </summary>
    [ObservableProperty]
    [property: JsonProperty]
    [Range(0, 4800)]
    private int targetDPI;

    /// <summary>
    /// The list of images in the roll.
    /// </summary>
    [SQLite.Ignore] public ObservableCollection<ImageEntry> ImageEntries { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the roll is locked. If locked, images cannot be modified.
    /// </summary>
    [ObservableProperty][property: JsonProperty] private bool isLocked = false;

    /// <summary>
    /// Gets or sets the currently selected printer settings.
    /// </summary>
    [ObservableProperty][property: SQLite.Ignore] private PrinterSettings selectedPrinter;

    /// <summary>
    /// Gets or sets the position where new images are added.
    /// </summary>
    [ObservableProperty]
    [property: JsonProperty]
    private ImageAddPositions _imageAddPosition;
    partial void OnImageAddPositionChanged(ImageAddPositions value)
    {
        if (ImageRollsDatabase != null)
        {
            SaveRoll();
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to right-align overflow content.
    /// </summary>
    public bool RightAlignOverflow
    {
        get => App.Settings.GetValue(nameof(RightAlignOverflow), false);
        set => App.Settings.SetValue(nameof(RightAlignOverflow), value);
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageRoll"/> class.
    /// </summary>
    public ImageRoll() { }

    public ImageRoll(ImageRollsManager imageRolls)
    {
        Messenger = WeakReferenceMessenger.Default;
        ImageRollsManager = imageRolls;
        App.Settings.PropertyChanged += Settings_PropertyChanged;

        if (string.IsNullOrEmpty(Name))
        {
            Name = GenerateUniqueName();
        }
        ReceiveAll();
        IsActive = true;


    }

    ~ImageRoll() => Dispose();

    #endregion

    private string GenerateUniqueName()
    {
        var baseName = "New Image Roll";
        var newName = baseName;
        var counter = 1;
        var allRollNames = ImageRollsManager.AllImageRolls
                                            .Select(r => r.Name)
                                            .ToHashSet();
        while (allRollNames.Contains(newName))
        {
            newName = $"{baseName} {counter++}";
        }
        Name = newName;
        return newName;
    }

    #region Validation

    public static bool IsFileNameSafe(string name)
    {
        var invalidChars = System.IO.Path.GetInvalidFileNameChars();
        return !string.IsNullOrEmpty(name) && name.All(c => !invalidChars.Contains(c));
    }
    public static ValidationResult ValidateName(string name, ValidationContext context)
    {
        //Check if (the string is null empty or whitespace.
        //Check the name to make sure it is not a duplicate UserImageRoll only
        //Check if (the name is file safe.

        var imageRoll = (ImageRoll)context.ObjectInstance;

        if(imageRoll.RollType == ImageRollTypes.Directory)
            return ValidationResult.Success;

        var imageRollsManager = imageRoll.ImageRollsManager;
        if (string.IsNullOrWhiteSpace(name))
            return new("A name is required and cannot be empty.");
        if (imageRollsManager != null)
            {
            var allNames = imageRollsManager.AllImageRolls
                                            .Where(r => r.UID != imageRoll.UID) // Exclude the current roll
                                            .Select(r => r.Name)
                                            .ToHashSet();
            if (allNames.Contains(name))
                return new("The name must be unique among user-defined and fixed image rolls.");
        }

        if (!IsFileNameSafe(name))
            return new("The name contains invalid characters. Please avoid using characters such as \\ / : * ? \" < > |");

        return ValidationResult.Success;

    }

    public static ValidationResult ValidateGS1Table(GS1Tables table, ValidationContext context)
    {
        var imageRoll = (ImageRoll)context.ObjectInstance;
        return imageRoll.SelectedApplicationStandard == ApplicationStandards.GS1 && table == GS1Tables.Unknown
            ? new("A GS1 Table must be selected when the Application Standard is GS1.")
            : ValidationResult.Success;
    }

    #endregion

    #region Message Handling

    /// <summary>
    /// Receives all necessary initial messages and data when the view model is activated.
    /// </summary>
    private void ReceiveAll()
    {
        RequestMessage<PrinterSettings> ret1 = WeakReferenceMessenger.Default.Send(new RequestMessage<PrinterSettings>());
        if (ret1.HasReceivedResponse)
            SelectedPrinter = ret1.Response;
    }

    /// <summary>
    /// Receives property changed messages for PrinterSettings.
    /// </summary>
    /// <param name="message">The property changed message.</param>
    public void Receive(PropertyChangedMessage<PrinterSettings> message) => SelectedPrinter = message.NewValue;

    #endregion

    #region Image Loading

    /// <summary>
    /// Loads images into the roll based on its <see cref="RollType"/>.
    /// </summary>
    public Task LoadImages() => RollType == ImageRollTypes.Directory ? LoadImagesFromDirectory() : LoadImagesFromDatabase();

    /// <summary>
    /// Loads images from a file system directory.
    /// Adds support for preserving original container format & DPI (if present) or
    /// normalizing to BGR32 with enforced fallback DPI when GlobalAppSettings.PreseveImageFormat is false.
    /// </summary>
    public async Task LoadImagesFromDirectory()
    {
        if (ImageEntries.Count > 0)
            return;

        Logger.Info($"Loading label images from standards directory: {App.AssetsImageRollsRoot}\\{Name}\\");

        // Stage 1: Efficient file enumeration and extension filtering
        var allowedExt = new HashSet<string>(new[] { ".png", ".bmp", ".jpg", ".jpeg", ".tif", ".tiff", ".gif" }, System.StringComparer.OrdinalIgnoreCase);
        var files = Directory.EnumerateFiles(Path)
            .Where(f => allowedExt.Contains(System.IO.Path.GetExtension(f)))
            .OrderBy(f => f)
            .ToList();

        if (files.Count == 0)
            return;

        int fallback = TargetDPI > 0 ? TargetDPI : 600;
        int maxDegree = Math.Max(1, Environment.ProcessorCount / 2);
        int batchSize = 32;
        int thumbnailMaxEdge = 512;

        // Run all CPU-bound work off the UI thread
        var imageEntries = await Task.Run(() =>
        {
            var result = new List<ImageEntry>(files.Count);
            var lockObj = new object();
            Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = maxDegree }, file =>
            {
                try
                {
                    byte[] buffer = null;
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        int length = (int)Math.Min(fileInfo.Length, int.MaxValue);
                        buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(length);
                        using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan))
                        {
                            int offset = 0;
                            while (offset < length)
                            {
                                int read = fs.Read(buffer, offset, length - offset);
                                if (read == 0) break;
                                offset += read;
                            }
                            // Copy to exact size
                            byte[] exact = new byte[offset];
                            Buffer.BlockCopy(buffer, 0, exact, 0, offset);

                            // Stage 3: DPI/format patching
                            if (GlobalAppSettings.Instance.PreseveImageFormat)
                                exact = ImageFormatHelpers.EnsureDpi(exact, fallback, fallback, out _, out _);
                            else
                                exact = ImageFormatHelpers.ConvertImageToBgr32PreserveDpi(exact, fallback, out _, out _);

                            var entry = new ImageEntry(UID, exact)
                            {
                                Path = file,
                                Name = System.IO.Path.GetFileNameWithoutExtension(file)
                            };
                            // Only ensure DPI if not already valid (avoid double patch)
                            if (entry.ImageDpiX < 10 || entry.ImageDpiY < 10)
                                entry.EnsureDpi(fallback);
                            entry.SaveRequested += OnImageEntrySaveRequested;

                            // Step 6: Generate low-res preview (ImageLow)
                            try
                            {
                                using (var ms = new MemoryStream(exact, false))
                                {
                                    var bmp = new System.Windows.Media.Imaging.BitmapImage();
                                    bmp.BeginInit();
                                    bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                                    bmp.CreateOptions = System.Windows.Media.Imaging.BitmapCreateOptions.IgnoreColorProfile;
                                    bmp.StreamSource = ms;
                                    bmp.DecodePixelWidth = thumbnailMaxEdge;
                                    bmp.DecodePixelHeight = thumbnailMaxEdge;
                                    bmp.EndInit();
                                    bmp.Freeze();
                                    entry.ImageLow = bmp;
                                }
                            }
                            catch { /* If thumbnail fails, leave null */ }

                            lock (lockObj)
                            {
                                result.Add(entry);
                            }
                        }
                    }
                    finally
                    {
                        if (buffer != null)
                            System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Failed to load image: {file}");
                }
            });
            return result;
        });

        // Only marshal to UI thread for the final add
        int order = 1;
        for (int i = 0; i < imageEntries.Count; i += batchSize)
        {
            var batch = imageEntries.Skip(i).Take(batchSize).ToList();
            foreach (var entry in batch)
            {
                entry.Order = order++;
            }
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                foreach (var entry in batch)
                {
                    ImageEntries.Add(entry);
                }
            }, System.Windows.Threading.DispatcherPriority.Background);
        }

        ImageCount = ImageEntries.Count;
    }

    /// <summary>
    /// Loads images from the associated database.
    /// Ensures DPI is patched (if missing) when preserving original format.
    /// </summary>
    public async Task LoadImagesFromDatabase()
    {
        if (ImageRollsDatabase == null)
        {
            Logger.Error("ImageRollsDatabase is null.");
            return;
        }

        if (ImageEntries.Count > 0)
            return;

        Logger.Info($"Loading label images from database: {Name}");

        List<ImageEntry> images = ImageRollsDatabase.SelectAllImages(UID);
        List<Task> taskList = [];

        var fallback = TargetDPI > 0 ? TargetDPI : 600;

        foreach (ImageEntry f in images)
        {
            f.SaveRequested += OnImageEntrySaveRequested;

            if (GlobalAppSettings.Instance.PreseveImageFormat)
            {
                // Patch DPI only if missing
                f.EnsureDpi(fallback);
            }
            else
            {
                // If the roll is set to NOT preserve format but existing entries were stored previously
                // in original formats, we leave as-is to avoid changing persisted content silently.
                // (Normalization happens for new device/import additions.)
            }

            Task tsk = Application.Current.Dispatcher.BeginInvoke(() => ImageEntries.Add(f)).Task;
            taskList.Add(tsk);
        }

        await Task.WhenAll([.. taskList]);

        ResetImageOrderAndSort();
    }

    /// <summary>
    /// Gets an existing or new ImageEntry for a given file path.
    /// Applies PreseveImageFormat logic and optional normalization/DPI patch.
    /// </summary>
    public (ImageEntry entry, bool isNew) GetImageEntry(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return (null, false);

            byte[] bytes = File.ReadAllBytes(path);

            var fallback = TargetDPI > 0 ? TargetDPI : 600;

            ImageEntry ire;
            if (GlobalAppSettings.Instance.PreseveImageFormat)
            {
                // Keep original bytes; patch DPI only if missing
                bytes = ImageFormatHelpers.EnsureDpi(bytes, fallback, fallback, out _, out _);
                ire = new ImageEntry(UID, bytes)
                {
                    Path = path,
                    Name = System.IO.Path.GetFileNameWithoutExtension(path)
                };
                // EnsureDpi will be idempotent (already patched above).
                ire.EnsureDpi(fallback);
            }
            else
            {
                // Normalize to BGR32 bitmap (BMP encoder) with enforced DPI
                bytes = ImageFormatHelpers.ConvertImageToBgr32PreserveDpi(bytes, fallback, out _, out _);
                ire = new ImageEntry(UID, bytes)
                {
                    Path = path,
                    Name = System.IO.Path.GetFileNameWithoutExtension(path)
                };
                // Normalized bytes are guaranteed to include DPI now.
            }

            ire.SaveRequested += OnImageEntrySaveRequested;

            ImageEntry existing = ImageEntries.FirstOrDefault(x => x.UID == ire.UID);
            if (existing != null)
            {
                Logger.Warning($"Image already exists in roll: {path}");
                return (existing, false);
            }

            return (ire, true);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Failed to load image: {path}");
        }

        return (null, false);
    }

    /// <summary>
    /// Gets an existing or new ImageEntry for raw in-memory bytes (e.g., device acquisition).
    /// Ensures DPI patch or normalization consistent with global setting.
    /// </summary>
    public (ImageEntry entry, bool isNew) GetImageEntry(byte[] rawImage)
    {
        try
        {
            if (rawImage == null || rawImage.Length == 0)
                return (null, false);

            var fallback = TargetDPI > 0 ? TargetDPI : 600;
            byte[] bytes = rawImage;

            if (GlobalAppSettings.Instance.PreseveImageFormat)
            {
                bytes = ImageFormatHelpers.EnsureDpi(bytes, fallback, fallback, out _, out _);
            }
            else
            {
                bytes = ImageFormatHelpers.ConvertImageToBgr32PreserveDpi(bytes, fallback, out _, out _);
            }

            var ire = new ImageEntry(UID, bytes);
            ire.EnsureDpi(fallback);
            ire.SaveRequested += OnImageEntrySaveRequested;

            ImageEntry existing = ImageEntries.FirstOrDefault(x => x.UID == ire.UID);
            if (existing != null)
            {
                Logger.Warning("Image already exists in roll (raw image).");
                return (existing, false);
            }

            return (ire, true);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to load image from raw bytes.");
        }

        return (null, false);
    }

    #endregion

    #region Image Management

    /// <summary>
    /// Adds a single image to the roll at the specified position.
    /// </summary>
    /// <param name="position">The position where the image should be added.</param>
    /// <param name="newImage">The new image to add.</param>
    /// <param name="relativeTo">The image to which the position is relative (for Above/Below).</param>
    public void AddImage(ImageAddPositions position, ImageEntry newImage, ImageEntry relativeTo = null) => AddImages(position, [newImage], relativeTo);

    /// <summary>
    /// Adds a list of images to the roll at the specified position.
    /// </summary>
    /// <param name="position">The position where the images should be added.</param>
    /// <param name="newImages">The list of new images to add.</param>
    /// <param name="relativeTo">The image to which the position is relative (for Above/Below).</param>
    public void AddImages(ImageAddPositions position, List<ImageEntry> newImages, ImageEntry relativeTo = null)
    {
        if (newImages != null && newImages.Count != 0)
        {
            switch (position)
            {
                case ImageAddPositions.Top:
                    InsertImagesAtOrder(newImages, 1);
                    break;
                case ImageAddPositions.Bottom:
                    InsertImagesAtOrder(newImages, ImageEntries.Count + 1);
                    break;
                case ImageAddPositions.Above:
                    if (relativeTo == null)
                    {
                        Logger.Warning("No image result provided for insertion above.");
                        return;
                    }
                    InsertImagesAtOrder(newImages, ImageEntries.IndexOf(relativeTo) + 1);
                    break;
                case ImageAddPositions.Below:
                    if (relativeTo == null)
                    {
                        Logger.Warning("No image result provided for insertion below.");
                        return;
                    }
                    InsertImagesAtOrder(newImages, ImageEntries.IndexOf(relativeTo) + 2);
                    break;
            }
        }

        OnPropertyChanged(nameof(ImageEntries));
    }

    /// <summary>
    /// Inserts a list of images at a specific order index.
    /// </summary>
    /// <param name="newImages">The list of images to insert.</param>
    /// <param name="targetOrder">The target order index.</param>
    public void InsertImagesAtOrder(List<ImageEntry> newImages, int targetOrder)
    {
        if (targetOrder < 1) targetOrder = 1;
        if (targetOrder > ImageEntries.Count + 1) targetOrder = ImageEntries.Count + 1;

        var ordered = ImageEntries.OrderBy(x => x.Order).ToList();
        // Adjust the order of existing items to make space for the new item
        foreach (ImageEntry currentImage in ordered)
        {
            if (currentImage.Order >= targetOrder)
                currentImage.Order += newImages.Count;
        }

        // Insert each new image at the adjusted target order
        for (var i = 0; i < newImages.Count; i++)
        {
            ImageEntry newImage = newImages[i];
            newImage.Order = targetOrder + i;
            ImageEntries.Add(newImage);
            SaveImage(newImage);
        }

        ImageCount = ImageEntries.Count;
        SaveRoll();
    }

    /// <summary>
    /// Moves an image to the top of the roll.
    /// </summary>
    /// <param name="imageToMove">The image to move.</param>
    public void MoveImageTop(ImageEntry imageToMove)
    {
        if (imageToMove == null || ImageEntries.Count < 2) return;
        MoveImage(imageToMove, 1);
    }

    /// <summary>
    /// Moves an image one position up in the roll.
    /// </summary>
    /// <param name="imageToMove">The image to move.</param>
    public void MoveImageUp(ImageEntry imageToMove)
    {
        if (imageToMove == null || ImageEntries.Count < 2) return;
        var oldOrder = imageToMove.Order;
        if (oldOrder > 1)
        {
            MoveImage(imageToMove, oldOrder - 1);
        }
    }

    /// <summary>
    /// Moves an image one position down in the roll.
    /// </summary>
    /// <param name="imageToMove">The image to move.</param>
    public void MoveImageDown(ImageEntry imageToMove)
    {
        if (imageToMove == null || ImageEntries.Count < 2) return;
        var oldOrder = imageToMove.Order;
        if (oldOrder < ImageEntries.Count)
        {
            MoveImage(imageToMove, oldOrder + 1);
        }
    }

    /// <summary>
    /// Moves an image to the bottom of the roll.
    /// </summary>
    /// <param name="imageToMove">The image to move.</param>
    public void MoveImageBottom(ImageEntry imageToMove)
    {
        if (imageToMove == null || ImageEntries.Count < 2) return;
        MoveImage(imageToMove, ImageEntries.Count);
    }

    /// <summary>
    /// Deletes an image from the roll and the database.
    /// </summary>
    /// <param name="imageEntry">The image entry to delete.</param>
    public void DeleteImage(ImageEntry imageEntry)
    {
        if (!ImageRollsDatabase.DeleteImage(UID, imageEntry.UID))
            Logger.Error($"Failed to delete image from roll: {imageEntry.UID}");

    }

    /// <summary>
    /// Creates a lightweight copy of the image roll using JSON serialization.
    /// </summary>
    /// <returns>A new <see cref="ImageRoll"/> instance with the same property values.</returns>
    public ImageRoll CopyLite()
    {
        ImageRoll roll = JsonConvert.DeserializeObject<ImageRoll>(JsonConvert.SerializeObject(this));
        return roll;
    }

    #endregion

    #region Commands

    /// <summary>
    /// Saves the current state of the image roll to the database.
    /// </summary>
    [RelayCommand]
    private void SaveRoll()
    {
        if (RollType == ImageRollTypes.Directory)
            return;

        ImageRollsManager.SaveUserImageRoll(this);
        OnPropertyChanged(nameof(StandardGroup));
    }

    /// <summary>
    /// Saves a specific image entry to the database.
    /// </summary>
    /// <param name="image">The image entry to save.</param>
    [RelayCommand]
    public void SaveImage(ImageEntry image)
    {
        if (RollType == ImageRollTypes.Directory)
            return;

        ImageRollsManager.SaveImageEntry(image);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Handles changes to application settings.
    /// </summary>
    private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(RightAlignOverflow))
            OnPropertyChanged(nameof(RightAlignOverflow));
    }

    /// <summary>
    /// Handles the SaveRequested event from an ImageEntry.
    /// </summary>
    private void OnImageEntrySaveRequested(ImageEntry imageEntry) => SaveImage(imageEntry);

    /// <summary>
    /// Resets the order of all images in the roll and sorts the collection.
    /// </summary>
    private void ResetImageOrderAndSort()
    {
        var images = ImageEntries.OrderBy(x => x.Order).ToList();
        for (var i = 0; i < images.Count; i++)
        {
            if (images[i].Order != i + 1)
            {
                images[i].Order = i + 1;
                // SaveImage is now called by the OnOrderChanged event in ImageEntry
            }
        }

        ImageCount = ImageEntries.Count;
        SaveRoll();
    }

    /// <summary>
    /// Moves an image to a new order position, adjusting other images accordingly.
    /// </summary>
    /// <param name="imageToMove">The image to move.</param>
    /// <param name="newOrder">The new order position for the image.</param>
    private void MoveImage(ImageEntry imageToMove, int newOrder)
    {
        if (imageToMove.Order == newOrder) return;

        var oldOrder = imageToMove.Order;

        if (oldOrder < newOrder) // Moving down
        {
            foreach (ImageEntry img in ImageEntries.Where(i => i.Order > oldOrder && i.Order <= newOrder))
            {
                img.Order--;
            }
        }
        else // Moving up
        {
            foreach (ImageEntry img in ImageEntries.Where(i => i.Order >= newOrder && i.Order < oldOrder))
            {
                img.Order++;
            }
        }

        imageToMove.Order = newOrder;
        ImageMoved?.Invoke(this, imageToMove);
    }

    #endregion

    #region IDisposable Implementation

    /// <summary>
    /// Releases the resources used by the <see cref="ImageRoll"/> object.
    /// </summary>
    public void Dispose()
    {
        App.Settings.PropertyChanged -= Settings_PropertyChanged;
        GC.SuppressFinalize(this);
    }

    #endregion
}