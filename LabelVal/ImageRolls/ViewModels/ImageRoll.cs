using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.Extensions;
using BarcodeVerification.lib.GS1;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.Main.ViewModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing.Printing;
using System.IO;
using System.Threading.Tasks;

namespace LabelVal.ImageRolls.ViewModels;

[SQLite.StoreAsText]
[JsonConverter(typeof(StringEnumConverter))]
public enum ImageRollTypes
{
    [Description("Database")]
    Database,
    [Description("Directory")]
    Directory,
}

[SQLite.StoreAsText]
[JsonConverter(typeof(StringEnumConverter))]
public enum ImageRollImageTypes
{
    [Description("Source")]
    Source,
    [Description("Stored")]
    Stored
}

[SQLite.StoreAsText]
[JsonConverter(typeof(StringEnumConverter))]
public enum ImageRollSectorTypes
{
    [Description("Fixed")]
    Fixed,
    [Description("Dynamic")]
    Dynamic
}

[SQLite.StoreAsText]
[JsonConverter(typeof(StringEnumConverter))]
public enum ImageAddPositions
{
    Top,
    Above,
    Below,
    Bottom
}

[JsonObject(MemberSerialization.OptIn)]
public partial class ImageRoll : ObservableRecipient, IRecipient<PropertyChangedMessage<PrinterSettings>>
{
    public GlobalAppSettings AppSettings => GlobalAppSettings.Instance;

    /// <summary>
    /// The unique identifier for the ImageRollEntry. This is also called the RollID.
    /// </summary>
    [JsonProperty][SQLite.PrimaryKey] public string UID { get; set; } = Guid.NewGuid().ToString();

    [SQLite.Ignore] public Databases.ImageRollsDatabase ImageRollsDatabase { get; set; }

    /// <summary>
    /// If this is a fixed image roll then this is the path to the directory where the images are stored.
    /// </summary>
    public string Path { get; set; }
    /// <summary>
    /// Indictaes if this is a fixed image roll. True if the <see cref="Path"/> is not null or empty."/>
    /// </summary>
    [SQLite.Ignore] public ImageRollTypes RollType => !string.IsNullOrEmpty(Path) ? ImageRollTypes.Directory : ImageRollTypes.Database;

    [ObservableProperty][property: JsonProperty] private ImageRollImageTypes imageType = ImageRollImageTypes.Source;
    //If SectorType is true the system will write the templates sectors before processing an image.
    //Normally the template is left untouched. I.e. When using a sequential OCR tool.
    [ObservableProperty][property: JsonProperty] private ImageRollSectorTypes sectorType = ImageRollSectorTypes.Dynamic;

    [ObservableProperty][property: JsonProperty] private string name;
    [ObservableProperty][property: JsonProperty] private int imageCount;
    [ObservableProperty][property: JsonProperty("Standard")][property: SQLite.Column("Standard")] private AvailableStandards selectedStandard;
    partial void OnSelectedStandardChanged(AvailableStandards value) { if (value != AvailableStandards.GS1) SelectedGS1Table = AvailableTables.Unknown; OnPropertyChanged(nameof(StandardDescription)); }
    public string StandardDescription => SelectedStandard.GetDescription();

    [ObservableProperty][property: JsonProperty("GS1Table")][property: SQLite.Column("GS1Table")] private AvailableTables selectedGS1Table;
    partial void OnSelectedGS1TableChanged(AvailableTables value) => OnPropertyChanged(nameof(GS1TableNumber));
    public double GS1TableNumber => SelectedGS1Table is AvailableTables.Unknown ? 0 : double.Parse(SelectedGS1Table.GetDescription());

    /// <summary>
    /// <see cref="TargetDPI"/>
    /// </summary>
    [ObservableProperty][property: JsonProperty] private int targetDPI;

    /// <summary>
    /// The list of images in the roll.
    /// </summary>
    [SQLite.Ignore] public ObservableCollection<ImageEntry> Images { get; set; } = [];

    /// <summary>
    /// If the roll is locked, the images cannot be modified.
    /// <see cref="IsLocked"/>"/>
    /// </summary>
    [ObservableProperty][property: JsonProperty] private bool isLocked = false;

    [ObservableProperty][property: SQLite.Ignore] private PrinterSettings selectedPrinter;

    [ObservableProperty] private bool rightAlignOverflow = App.Settings.GetValue(nameof(RightAlignOverflow), false);

    public ImageRoll()
    {
        App.Settings.PropertyChanged += Settings_PropertyChanged;
        IsActive = true;
        RecieveAll();
    }

    private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(RightAlignOverflow))
            RightAlignOverflow = App.Settings.GetValue(nameof(RightAlignOverflow), false);
    }

    //public ImageRollEntry(bool inactive)
    //{
    //    IsActive = inactive;

    //    if(IsActive)
    //        RecieveAll();
    //}
    //public ImageRollEntry(string name, string path, Databases.ImageRollsDatabase imageRollsDatabase)
    //{
    //    IsActive = true;
    //    RecieveAll();

    //    ImageRollsDatabase = imageRollsDatabase;

    //    Name = name;
    //    Path = path;
    //}

    private void RecieveAll()
    {
        RequestMessage<PrinterSettings> ret1 = WeakReferenceMessenger.Default.Send(new RequestMessage<PrinterSettings>());
        if (ret1.HasReceivedResponse)
            SelectedPrinter = ret1.Response;
    }
    public void Receive(PropertyChangedMessage<PrinterSettings> message) => SelectedPrinter = message.NewValue;

    public Task LoadImages() => RollType == ImageRollTypes.Directory ? LoadImagesFromDirectory() : LoadImagesFromDatabase();

    public async Task LoadImagesFromDirectory()
    {
        if (Images.Count > 0)
            return;

        Logger.LogInfo($"Loading label images from standards directory: {App.AssetsImageRollsRoot}\\{Name}\\");

        List<string> images = [];
        foreach (var f in Directory.EnumerateFiles(Path))
            if (System.IO.Path.GetExtension(f) == ".png")
                images.Add(f);

        List<Task> taskList = [];

        var sorted = images.OrderBy(x => x).ToList();
        var order = 1;
        foreach (var f in sorted)
        {
            ImageEntry image = GetNewImageEntry(f, order++);
            if (image == null)
                continue;

            Task tsk = App.Current.Dispatcher.BeginInvoke(() => AddImage(ImageAddPositions.Bottom, image)).Task;
            taskList.Add(tsk);
        }

        await Task.WhenAll([.. taskList]);
    }
    public async Task LoadImagesFromDatabase()
    {
        if (ImageRollsDatabase == null)
        {
            Logger.LogError("ImageRollsDatabase is null.");
            return;
        }

        if (Images.Count > 0)
            return;

        Logger.LogInfo($"Loading label images from database: {Name}");

        List<ImageEntry> images = ImageRollsDatabase.SelectAllImages(UID);
        List<Task> taskList = [];

        CheckImageEntryOrder([.. images]);

        foreach (ImageEntry f in images)
        {
            Task tsk = App.Current.Dispatcher.BeginInvoke(() => AddImage(ImageAddPositions.Bottom, f)).Task;

            taskList.Add(tsk);
        }

        await Task.WhenAll([.. taskList]);
    }

    private void CheckImageEntryOrder(ImageEntry[] entries)
    {
        var invalid = entries.Any(x => x.Order < 0);

        List<ImageEntry> images = !invalid ? [.. entries.OrderBy(x => x.Order)] : [.. entries.OrderBy(x => x.Path)];

        for (var i = 0; i < images.Count; i++)
        {
            if (images[i].Order != i + 1)
            {
                images[i].Order = i + 1;
                SaveImage(images[i]);
            }
        }
    }

    public ImageEntry GetNewImageEntry(string path, int order)
    {
        try
        {
            var ire = new ImageEntry(UID, path);

            ImageEntry imageEntry = Images.FirstOrDefault(x => x.UID == ire.UID);
            if (imageEntry != null)
            {
                Logger.LogWarning($"Image already exists in roll: {Path}");
                return imageEntry;
            }

            ire.Order = order;

            return ire;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Failed to load image: {Path}");
        }

        return null;
    }
    public ImageEntry GetNewImageEntry(byte[] rawImage, ImageAddPositions position)
    {
        try
        {
            var ire = new ImageEntry(UID, rawImage, TargetDPI);

            ImageEntry imageentry = Images.FirstOrDefault(x => x.UID == ire.UID);
            if (imageentry != null)
            {
                Logger.LogWarning($"Image already exists in roll: {Path}");
                return imageentry;
            }

            ire.Order = position == ImageAddPositions.Top ? 1 : Images.Count + 1;

            return ire;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Failed to load image: {Path}");
        }

        return null;
    }

    public void DeleteImage(ImageEntry imageEntry)
    {
        if (ImageRollsDatabase.DeleteImage(UID, imageEntry.UID))
            _ = Images.Remove(imageEntry);
        ImageCount = Images.Count;
    }

    [RelayCommand]
    private void SaveRoll()
    {
        if (RollType == ImageRollTypes.Directory)
            return;

        _ = ImageRollsDatabase.InsertOrReplaceImageRoll(this);
    }
    [RelayCommand]
    public void SaveImage(ImageEntry image)
    {
        if (RollType == ImageRollTypes.Directory)
            return;

        _ = ImageRollsDatabase.InsertOrReplaceImage(image);
    }

    public ImageRoll CopyLite() => JsonConvert.DeserializeObject<ImageRoll>(JsonConvert.SerializeObject(this));

    public void AddImage(ImageAddPositions position, ImageEntry newImage, ImageEntry relativeTo = null) => AddImages(position, [newImage], relativeTo);
    public void AddImages(ImageAddPositions position, List<ImageEntry> newImages, ImageEntry relativeTo = null)
    {
        // Prompt the user to select an image or multiple images
        if (newImages != null && newImages.Count != 0)
        {
            switch (position)
            {
                case ImageAddPositions.Top:
                    InsertImagesAtOrder(newImages, 1);
                    break;
                case ImageAddPositions.Bottom:
                    InsertImagesAtOrder(newImages, Images.Count + 1);
                    break;
                case ImageAddPositions.Above:
                    if (relativeTo == null)
                    {
                        Logger.LogWarning("No image result provided for insertion above.");
                        return;
                    }
                    InsertImagesAtOrder(newImages, relativeTo.Order);
                    break;
                case ImageAddPositions.Below:
                    if (relativeTo == null)
                    {
                        Logger.LogWarning("No image result provided for insertion below.");
                        return;
                    }
                    InsertImagesAtOrder(newImages, relativeTo.Order + 1);
                    break;
            }
        }
    }

    public void InsertImagesAtOrder(List<ImageEntry> newImages, int targetOrder)
    {
        // Adjust the order of existing items to make space for the new item
        AdjustOrdersBeforeInsert(targetOrder, newImages.Count);

        // Insert each new image at the adjusted target order
        foreach (ImageEntry newImage in newImages)
        {
            // Set the order of the new item
            newImage.Order = targetOrder++;
            SaveImage(newImage);
            Images.Add(newImage);
        }
    }

    private void AdjustOrdersBeforeInsert(int targetOrder, int count)
    {
        var sorted = Images.OrderBy(img => img.Order).ToList();
        foreach (ImageEntry item in sorted)
        {
            if (item.Order >= targetOrder)
            {
                // Increment the order of existing items that come after the target order
                item.Order += count;
                SaveImage(item);
            }
        }
    }
}
