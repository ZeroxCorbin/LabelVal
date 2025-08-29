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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
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

public delegate void ImageMovedEventHandler(object sender, ImageEntry imageEntry);

[JsonObject(MemberSerialization.OptIn)]
public partial class ImageRoll : ObservableRecipient, IRecipient<PropertyChangedMessage<PrinterSettings>>
{
    public event ImageMovedEventHandler ImageMoved;

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

    [ObservableProperty][property: JsonProperty("GradingStandard")][property: SQLite.Column("GradingStandard")] private GradingStandards selectedGradingStandard;
    partial void OnSelectedGradingStandardChanged(GradingStandards value) { OnPropertyChanged(nameof(GradingStandardDescription)); OnPropertyChanged(nameof(StandardGroup)); }
    public string GradingStandardDescription => SelectedGradingStandard.GetDescription();

    [ObservableProperty][property: JsonProperty("ApplicationStandard")][property: SQLite.Column("ApplicationStandard")] private ApplicationStandards selectedApplicationStandard;
    partial void OnSelectedApplicationStandardChanged(ApplicationStandards value) { OnPropertyChanged(nameof(ApplicationStandardDescription)); OnPropertyChanged(nameof(StandardGroup)); }
    public string ApplicationStandardDescription => SelectedApplicationStandard.GetDescription();

    [ObservableProperty][property: JsonProperty("GS1Table")][property: SQLite.Column("GS1Table")] private GS1Tables selectedGS1Table;
    partial void OnSelectedGS1TableChanged(GS1Tables value) => OnPropertyChanged(nameof(GS1TableNumber));
    public double GS1TableNumber => SelectedGS1Table is GS1Tables.Unknown ? 0 : double.Parse(SelectedGS1Table.GetDescription());

    public string StandardGroup => $"{SelectedApplicationStandard}-{SelectedGradingStandard}";

    /// <summary>
    /// <see cref="TargetDPI"/>
    /// </summary>
    [ObservableProperty][property: JsonProperty] private int targetDPI;

    /// <summary>
    /// The list of images in the roll.
    /// </summary>
    [SQLite.Ignore] public ObservableCollection<ImageEntry> ImageEntries { get; set; } = [];

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
        if (ImageEntries.Count > 0)
            return;

        Logger.LogInfo($"Loading label images from standards directory: {App.AssetsImageRollsRoot}\\{Name}\\");

        List<string> images = [];
        foreach (var f in Directory.EnumerateFiles(Path))
            if (System.IO.Path.GetExtension(f) == ".png")
                images.Add(f);

        List<Task> taskList = [];

        var sorted = images.OrderBy(x => x).ToList();
        foreach (var f in sorted)
        {
            (ImageEntry entry, var isNew) = GetImageEntry(f);
            if (entry == null)
                continue;

            if (isNew)
            {
                Task tsk = App.Current.Dispatcher.BeginInvoke(() => AddImage(ImageAddPositions.Bottom, entry)).Task;
                taskList.Add(tsk);
            }
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

        if (ImageEntries.Count > 0)
            return;

        Logger.LogInfo($"Loading label images from database: {Name}");

        List<ImageEntry> images = ImageRollsDatabase.SelectAllImages(UID);
        List<Task> taskList = [];

        foreach (ImageEntry f in images)
        {
            f.SaveRequested += OnImageEntrySaveRequested;
            Task tsk = App.Current.Dispatcher.BeginInvoke(() => ImageEntries.Add(f)).Task;
            taskList.Add(tsk);
        }

        await Task.WhenAll([.. taskList]);

        ResetImageOrderAndSort();
    }

    private void OnImageEntrySaveRequested(ImageEntry imageEntry)
    {
        SaveImage(imageEntry);
    }

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
        // SortObservableCollectionByList(images, ImageEntries);
    }

    //public static void SortObservableCollectionByList(List<ImageEntry> list, ObservableCollection<ImageEntry> observableCollection)
    //{
    //    for (var i = 0; i < list.Count; i++)
    //    {
    //        ImageEntry item = list[i];
    //        var currentIndex = observableCollection.IndexOf(item);
    //        if (currentIndex != i)
    //        {
    //            observableCollection.Move(currentIndex, i);
    //        }
    //    }
    //}

    public (ImageEntry entry, bool isNew) GetImageEntry(string path)
    {
        try
        {
            var ire = new ImageEntry(UID, path);
            ire.SaveRequested += OnImageEntrySaveRequested;

            ImageEntry imageentry = ImageEntries.FirstOrDefault(x => x.UID == ire.UID);
            if (imageentry != null)
            {
                Logger.LogWarning($"Image already exists in roll: {Path}");
                return (imageentry, false);
            }

            return (ire, true);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Failed to load image: {Path}");
        }

        return (null, false);
    }
    public (ImageEntry entry, bool isNew) GetImageEntry(byte[] rawImage)
    {
        try
        {
            var ire = new ImageEntry(UID, rawImage, TargetDPI);
            ire.SaveRequested += OnImageEntrySaveRequested;

            ImageEntry imageentry = ImageEntries.FirstOrDefault(x => x.UID == ire.UID);
            if (imageentry != null)
            {
                Logger.LogWarning($"Image already exists in roll: {Path}");
                return (imageentry, false);
            }

            return (ire, true);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Failed to load image: {Path}");
        }

        return (null, false);
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
                    InsertImagesAtOrder(newImages, ImageEntries.Count + 1);
                    break;
                case ImageAddPositions.Above:
                    if (relativeTo == null)
                    {
                        Logger.LogWarning("No image result provided for insertion above.");
                        return;
                    }
                    InsertImagesAtOrder(newImages, ImageEntries.IndexOf(relativeTo) + 1);
                    break;
                case ImageAddPositions.Below:
                    if (relativeTo == null)
                    {
                        Logger.LogWarning("No image result provided for insertion below.");
                        return;
                    }
                    InsertImagesAtOrder(newImages, ImageEntries.IndexOf(relativeTo) + 2);
                    break;
            }


        }

        OnPropertyChanged(nameof(ImageEntries));
    }

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
        for (int i = 0; i < newImages.Count; i++)
        {
            ImageEntry newImage = newImages[i];
            newImage.Order = targetOrder + i;
            ImageEntries.Add(newImage);
            SaveImage(newImage);
        }

        ImageCount = ImageEntries.Count;
        SaveRoll();
    }

    public void MoveImageTop(ImageEntry imageToMove)
    {
        if (imageToMove == null || ImageEntries.Count < 2) return;
        MoveImage(imageToMove, 1);
    }

    public void MoveImageUp(ImageEntry imageToMove)
    {
        if (imageToMove == null || ImageEntries.Count < 2) return;
        int oldOrder = imageToMove.Order;
        if (oldOrder > 1)
        {
            MoveImage(imageToMove, oldOrder - 1);
        }
    }

    public void MoveImageDown(ImageEntry imageToMove)
    {
        if (imageToMove == null || ImageEntries.Count < 2) return;
        int oldOrder = imageToMove.Order;
        if (oldOrder < ImageEntries.Count)
        {
            MoveImage(imageToMove, oldOrder + 1);
        }
    }

    public void MoveImageBottom(ImageEntry imageToMove)
    {
        if (imageToMove == null || ImageEntries.Count < 2) return;
        MoveImage(imageToMove, ImageEntries.Count);
    }

    private void MoveImage(ImageEntry imageToMove, int newOrder)
    {
        if (imageToMove.Order == newOrder) return;

        int oldOrder = imageToMove.Order;

        if (oldOrder < newOrder) // Moving down
        {
            foreach (var img in ImageEntries.Where(i => i.Order > oldOrder && i.Order <= newOrder))
            {
                img.Order--;
            }
        }
        else // Moving up
        {
            foreach (var img in ImageEntries.Where(i => i.Order >= newOrder && i.Order < oldOrder))
            {
                img.Order++;
            }
        }

        imageToMove.Order = newOrder;
        ImageMoved?.Invoke(this, imageToMove);

        // The UI should update automatically if ImageEntries is an ObservableCollection and it's bound with a sort description.
        // If not, you may need to re-sort the view.
    }

    public void DeleteImage(ImageEntry imageEntry)
    {
        if (!ImageRollsDatabase.DeleteImage(UID, imageEntry.UID))
            Logger.LogError($"Failed to delete image for database: {imageEntry.UID}");

        if (ImageEntries.Remove(imageEntry))
        {
            imageEntry.SaveRequested -= OnImageEntrySaveRequested;
            Logger.LogInfo($"Image deleted from roll: {imageEntry.UID}");

            ResetImageOrderAndSort();
        }
        else
            Logger.LogError($"Failed to delete image from roll: {imageEntry.UID}");

    }
    public ImageRoll CopyLite() => JsonConvert.DeserializeObject<ImageRoll>(JsonConvert.SerializeObject(this));

}