using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.GS1;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LibImageUtilities.ImageTypes.Png;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Drawing.Printing;
using System.IO;
using System.Threading.Tasks;

namespace LabelVal.ImageRolls.ViewModels;

[JsonObject(MemberSerialization.OptIn)]
public partial class ImageRollEntry : ObservableRecipient, IRecipient<PropertyChangedMessage<PrinterSettings>>
{
    /// <summary>
    /// The unique identifier for the ImageRollEntry. This is also called the RollID.
    /// </summary>
    [JsonProperty][SQLite.PrimaryKey] public string UID { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// If this is a fixed image roll then this is the path to the directory where the images are stored.
    /// </summary>
    public string Path { get; set; }
    /// <summary>
    /// Indictaes if this is a fixed image roll. True if the <see cref="Path"/> is not null or empty."/>
    /// </summary>
    [SQLite.Ignore] public bool IsFixedImageRoll => !string.IsNullOrEmpty(Path);

    [ObservableProperty][property: JsonProperty] private string name;
    [ObservableProperty][property: JsonProperty] private int imageCount;
    [ObservableProperty][property: JsonProperty("Standard")][property: SQLite.Column("Standard")] private AvailableStandards? selectedStandard;
    partial void OnSelectedStandardChanged(AvailableStandards? value) { if (value != AvailableStandards.GS1) SelectedGS1Table = null; OnPropertyChanged(nameof(StandardDescription)); }
    public string StandardDescription => SelectedStandard.GetDescription();

    [ObservableProperty][property: JsonProperty("GS1Table")][property: SQLite.Column("GS1Table")] private AvailableTables? selectedGS1Table;
    partial void OnSelectedGS1TableChanged(AvailableTables? value) => OnPropertyChanged(nameof(GS1TableNumber));
    public double GS1TableNumber => SelectedGS1Table is null ? 0 : double.Parse(SelectedGS1Table.GetDescription());

    //If writeSectorsBeforeProcess is true the system will write the templates sectors before processing an image.
    //Normally the template is left untouched. I.e. When using a sequential OCR tool.
    [ObservableProperty][property: JsonProperty] private bool writeSectorsBeforeProcess = false;
    [ObservableProperty][property: JsonProperty] private int targetDPI;

    [ObservableProperty][property: JsonProperty] private bool isLocked = false;

    [ObservableProperty][property: SQLite.Ignore] private PrinterSettings selectedPrinter;

    [SQLite.Ignore] public ObservableCollection<ImageEntry> Images { get; set; } = [];
    [SQLite.Ignore] public Databases.ImageRollsDatabase ImageRollsDatabase { get; set; }

    [ObservableProperty] private bool rightAlignOverflow = App.Settings.GetValue(nameof(RightAlignOverflow), false);

    public ImageRollEntry()
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

    public Task LoadImages() => IsFixedImageRoll ? LoadImagesFromDirectory() : LoadImagesFromDatabase();

    public async Task LoadImagesFromDirectory()
    {
        if (Images.Count > 0)
            return;

        Logger.LogInfo($"Loading label images from standards directory: {App.AssetsImageRollsRoot}\\{Name}\\");

        List<string> images = [];
        foreach (string f in Directory.EnumerateFiles(Path))
            if (System.IO.Path.GetExtension(f) == ".png")
                images.Add(f);

        List<Task> taskList = [];

        List<string> sorted = images.OrderBy(x => x).ToList();
        int i = 1;
        foreach (string f in sorted)
        {
            ImageEntry image = GetNewImageEntry(f, i++);
            if (image == null)
                continue;

            Task tsk = App.Current.Dispatcher.BeginInvoke(() => AddImage(image)).Task;
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
            Task tsk = App.Current.Dispatcher.BeginInvoke(() => AddImage(f)).Task;

            taskList.Add(tsk);
        }

        await Task.WhenAll([.. taskList]);
    }

    private void CheckImageEntryOrder(ImageEntry[] entries)
    {
        bool invalid = entries.Any(x => x.Order < 0);

        List<ImageEntry> images = !invalid ? [.. entries.OrderBy(x => x.Order)] : [.. entries.OrderBy(x => x.Path)];

        for (int i = 0; i < images.Count; i++)
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
            Png pngImage = new(path);
            if (!pngImage.Chunks.ContainsKey(ChunkTypes.pHYs))
                pngImage.Chunks.Add(ChunkTypes.pHYs, new PHYS_Chunk());

            ImageEntry image = new(UID, pngImage.RawData, TargetDPI, TargetDPI)
            {
                Order = order
            };

            if (Images.Any(e => e.UID == image.UID))
            {
                Logger.LogWarning($"Image already exists in roll: {Path}");
                return null;
            }

            return image;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Failed to load image: {Path}");
        }

        return null;
    }
    public ImageEntry GetNewImageEntry(byte[] rawImage)
    {
        try
        {
            ImageEntry image = new(UID, LibImageUtilities.ImageTypes.Png.Utilities.GetPng(rawImage), TargetDPI, TargetDPI)
            {
                Order = Images.Count > 0 ? Images.Max(img => img.Order) + 1 : 1
            };

            if (Images.Any(e => e.UID == image.UID))
            {
                Logger.LogWarning($"Image already exists in roll: {Path}");
                return null;
            }

            return image;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Failed to load image: {Path}");
        }

        return null;
    }

    [RelayCommand]
    private async Task AddImagesFromDrive()
    {
        Utilities.FileUtilities.LoadFileDialogSettings settings = new()
        {
            Title = "Select image(s) to add to roll.",
            Multiselect = true,
            Filters =
            [
                new Utilities.FileUtilities.FileDialogFilter("Image Files", ["png", "bmp"]),
                new Utilities.FileUtilities.FileDialogFilter("Image Files (Add Fiducial)", ["png", "bmp"]),
            ]
        };

        if (Utilities.FileUtilities.LoadFileDialog(settings))
        {
            //Sort the selected files by name
            List<string> sorted = settings.SelectedFiles.OrderBy(x => x).ToList();

            int last = 0;
            if (Images.Count > 0)
            {
                List<ImageEntry> sortedImages = Images.OrderBy(x => x.Order).ToList();
                last = sortedImages.Last().Order;
            }
            List<ImageEntry> newImgs = [];
            int i = last + 1;
            foreach (string f in sorted)
                newImgs.Add(GetNewImageEntry(f, i++));

            AddImages(newImgs);
        }
    }

    public void AddImage(ImageEntry image)
    {
        if (image == null)
            return;

        if (!App.Current.Dispatcher.CheckAccess())
        {
            _ = App.Current.Dispatcher.BeginInvoke(() => AddImage(image));
            return;
        }

        //Add new/update ImageEntry in database if not Rooted
        SaveImage(image);

        try
        {
            Images.Add(image);
            ImageCount = Images.Count;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Failed to load image: {Path}");
        }
    }

    public void AddImages(List<ImageEntry> images)
    {
        if (images == null || images.Count == 0)
            return;

        if (!App.Current.Dispatcher.CheckAccess())
        {
            _ = App.Current.Dispatcher.BeginInvoke(() => AddImages(images));
            return;
        }

        foreach (ImageEntry image in images)
        {
            //Add new/update ImageEntry in database if not Rooted
            SaveImage(image);

            try
            {
                Images.Add(image);
                ImageCount = Images.Count;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Failed to load image: {Path}");
            }
        }
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
        if (IsFixedImageRoll)
            return;

        _ = ImageRollsDatabase.InsertOrReplaceImageRoll(this);
    }
    [RelayCommand]
    public void SaveImage(ImageEntry image)
    {
        if (IsFixedImageRoll)
            return;

        _ = ImageRollsDatabase.InsertOrReplaceImage(image);
    }

    public ImageRollEntry CopyLite() => JsonConvert.DeserializeObject<ImageRollEntry>(JsonConvert.SerializeObject(this));

}
