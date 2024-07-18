using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.Extensions;
using LabelVal.Sectors.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static System.ComponentModel.Design.ObjectSelectorEditor;

namespace LabelVal.ImageRolls.ViewModels;

[JsonObject(MemberSerialization.OptIn)]

public partial class ImageRollEntry : ObservableRecipient, IRecipient<PropertyChangedMessage<PrinterSettings>>
{
    public Array StandardsTypes
    {
        get
        {
            List<StandardsTypes> lst = Enum.GetValues(typeof(StandardsTypes)).Cast<StandardsTypes>().ToList();
            lst.Remove(Sectors.ViewModels.StandardsTypes.Unsupported);

            List<string> names = [];
            foreach (StandardsTypes name in lst)
                names.Add(name.GetDescription());

            return names.ToArray();
        }
    }
    public Array GS1TableNames
    {
        get
        {
            List<GS1TableNames> lst = Enum.GetValues(typeof(GS1TableNames)).Cast<GS1TableNames>().ToList();
            lst.Remove(Sectors.ViewModels.GS1TableNames.Unsupported);
            lst.Remove(Sectors.ViewModels.GS1TableNames.None);

            List<string> names = [];
            foreach (GS1TableNames name in lst)
                names.Add(name.GetDescription());

            return names.ToArray();
        }
    }

    [ObservableProperty][property: JsonProperty][property: SQLite.PrimaryKey] private string uID = Guid.NewGuid().ToString();
    [ObservableProperty][property: JsonProperty] private string name;
    [ObservableProperty] private string path;
    partial void OnPathChanged(string value) => OnPropertyChanged(nameof(IsRooted));
    [SQLite.Ignore] public bool IsRooted => !string.IsNullOrEmpty(Path);

    [ObservableProperty][property: JsonProperty] private int imageCount;

    [ObservableProperty][property: JsonProperty("Standard")][property: SQLite.Column("Standard")] private StandardsTypes selectedStandard;
    partial void OnSelectedStandardChanged(StandardsTypes value) { if (value != Sectors.ViewModels.StandardsTypes.GS1) SelectedGS1Table = Sectors.ViewModels.GS1TableNames.None; OnPropertyChanged(nameof(StandardDescription)); }
    public string StandardDescription => SelectedStandard.GetDescription();

    [ObservableProperty][property: JsonProperty("GS1Table")][property: SQLite.Column("GS1Table")] private GS1TableNames selectedGS1Table;
    partial void OnSelectedGS1TableChanged(GS1TableNames value) => OnPropertyChanged(nameof(GS1TableNumber));
    public double GS1TableNumber => SelectedGS1Table is Sectors.ViewModels.GS1TableNames.None or Sectors.ViewModels.GS1TableNames.Unsupported
                ? 0
                : double.Parse(SelectedGS1Table.GetDescription());

    //If writeSectorsBeforeProcess is true the system will write the templates sectors before processing an image.
    //Normally the template is left untouched. I.e. When using a sequential OCR tool.
    [ObservableProperty][property: JsonProperty] private bool writeSectorsBeforeProcess = false;
    [ObservableProperty][property: JsonProperty] private int targetDPI;

    [ObservableProperty][property: JsonProperty] private bool isLocked = false;

    [ObservableProperty][property: SQLite.Ignore] private PrinterSettings selectedPrinter;

    [SQLite.Ignore] public ObservableCollection<ImageEntry> Images { get; set; } = [];
    [SQLite.Ignore] public Databases.ImageRollsDatabase ImageRollsDatabase { get; set; }

    public ImageRollEntry()
    {
        IsActive = true;
        RecieveAll();
    }
    public ImageRollEntry(string name, string path, Databases.ImageRollsDatabase imageRollsDatabase)
    {
        IsActive = true;
        RecieveAll();

        ImageRollsDatabase = imageRollsDatabase;

        Name = name;
        Path = path;
    }

    private void RecieveAll()
    {
        RequestMessage<PrinterSettings> message = new();
        WeakReferenceMessenger.Default.Send(message);
        SelectedPrinter = message.Response;
    }
    public void Receive(PropertyChangedMessage<PrinterSettings> message) => SelectedPrinter = message.NewValue;

    public Task LoadImages() => IsRooted ? LoadImagesFromDirectory() : LoadImagesFromDatabase();

    public async Task LoadImagesFromDirectory()
    {
        if (Images.Count > 0)
            return;

        LogInfo($"Loading label images from standards directory: {App.AssetsImageRollRoot}\\{Name}\\");

        List<string> images = [];
        foreach (string f in Directory.EnumerateFiles(Path))
            if (System.IO.Path.GetExtension(f) == ".png")
                images.Add(f);

        List<Task> taskList = [];

        List<string> sorted = images.OrderBy(x => x).ToList();
        int i = 1;
        foreach (string f in sorted)
        {
            Task tsk = App.Current.Dispatcher.BeginInvoke(() => AddImage(f, i++)).Task;
            taskList.Add(tsk);
        }

        await Task.WhenAll([.. taskList]);
    }
    public async Task LoadImagesFromDatabase()
    {
        if(ImageRollsDatabase == null)
        {
            LogError("ImageRollsDatabase is null.");
            return;
        }

        if (Images.Count > 0)
            return;

        LogInfo($"Loading label images from database: {Name}");

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
            ImageEntry image = new(UID, path, TargetDPI, TargetDPI)
            {
                Order = order
            };

            if (Images.Any(e => e.UID == image.UID))
            {
                LogWarning($"Image already exists in roll: {Path}");
                return null;
            }

            return image;
        }
        catch (Exception ex)
        {
            LogError($"Failed to load image: {Path}", ex);
        }

        return null;
    }
    public ImageEntry GetNewImageEntry(byte[] rawImage)
    {
        try
        {
            ImageEntry image = new(UID, rawImage, TargetDPI, TargetDPI)
            {
                Order = Images.Count > 0 ? Images.Max(img => img.Order) + 1 : 1
            };

            if (Images.Any(e => e.UID == image.UID))
            {
                LogWarning($"Image already exists in roll: {Path}");
                return null;
            }

            return image;
        }
        catch (Exception ex)
        {
            LogError($"Failed to load image: {Path}", ex);
        }

        return null;
    }

    [RelayCommand]
    private void AddFirstImages()
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
            List<string> sorted = settings.SelectedFiles.OrderBy(x => x).ToList();
            int last = 0;
            if (Images.Count > 0)
            {
                List<ImageEntry> sortedImages = Images.OrderBy(x => x.Order).ToList();
                last = sortedImages.Last().Order;
            }

            int i = last + 1;
            foreach (string f in sorted)
                AddImage(f, i++);
        }
    }
    private void AddImage(string path, int order)
    {
        try
        {
            ImageEntry image = GetNewImageEntry(path, order);
            if (image == null)
                return;

            SaveImage(image);
            Images.Add(image);
            ImageCount = Images.Count;
        }
        catch (Exception ex)
        {
            LogError($"Failed to load image: {Path}", ex);
        }
    }
    public void AddImage(ImageEntry image)
    {
        try
        {
            if (image == null)
                return;

            SaveImage(image);
            Images.Add(image);
            ImageCount = Images.Count;
        }
        catch (Exception ex)
        {
            LogError($"Failed to load image: {Path}", ex);
        }
    }

    public void DeleteImage(ImageEntry imageEntry)
    {
        ImageRollsDatabase.DeleteImage(imageEntry.UID);
        Images.Remove(imageEntry);
        ImageCount = Images.Count;
    }

    [RelayCommand]
    private void SaveRoll()
    {
        if (IsRooted)
            return;

        _ = ImageRollsDatabase.InsertOrReplaceImageRoll(this);
    }
    [RelayCommand]
    public void SaveImage(ImageEntry image)
    {
        if (IsRooted)
            return;

        _ = ImageRollsDatabase.InsertOrReplaceImage(image);
    }

    public ImageRollEntry CopyLite() => JsonConvert.DeserializeObject<ImageRollEntry>(JsonConvert.SerializeObject(this));

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
