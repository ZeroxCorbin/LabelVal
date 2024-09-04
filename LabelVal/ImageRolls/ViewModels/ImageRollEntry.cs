using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.Extensions;
using LabelVal.Sectors.Interfaces;
using LabelVal.Utilities;
using LibImageUtilities.ImageTypes.Png;
using Microsoft.Extensions.FileSystemGlobbing.Internal.PathSegments;
using Newtonsoft.Json;
using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Shapes;

namespace LabelVal.ImageRolls.ViewModels;

[JsonObject(MemberSerialization.OptIn)]
public partial class ImageRollEntry : ObservableRecipient, IRecipient<PropertyChangedMessage<PrinterSettings>>
{
    public Array StandardsTypes
    {
        get
        {
            List<StandardsTypes> lst = Enum.GetValues(typeof(StandardsTypes)).Cast<StandardsTypes>().ToList();
            lst.Remove(Sectors.Interfaces.StandardsTypes.Unsupported);

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
            lst.Remove(Sectors.Interfaces.GS1TableNames.Unsupported);
            lst.Remove(Sectors.Interfaces.GS1TableNames.None);

            List<string> names = [];
            foreach (GS1TableNames name in lst)
                names.Add(name.GetDescription());

            return names.ToArray();
        }
    }

    [JsonProperty][SQLite.PrimaryKey] public string UID { get; set; } = Guid.NewGuid().ToString();
    public string Path { get; set; }
    [SQLite.Ignore] public bool IsRooted => !string.IsNullOrEmpty(Path);

    [ObservableProperty][property: JsonProperty] private string name;
    [ObservableProperty][property: JsonProperty] private int imageCount;
    [ObservableProperty][property: JsonProperty("Standard")][property: SQLite.Column("Standard")] private StandardsTypes selectedStandard;
    partial void OnSelectedStandardChanged(StandardsTypes value) { if (value != Sectors.Interfaces.StandardsTypes.GS1) SelectedGS1Table = Sectors.Interfaces.GS1TableNames.None; OnPropertyChanged(nameof(StandardDescription)); }
    public string StandardDescription => SelectedStandard.GetDescription();

    [ObservableProperty][property: JsonProperty("GS1Table")][property: SQLite.Column("GS1Table")] private GS1TableNames selectedGS1Table;
    partial void OnSelectedGS1TableChanged(GS1TableNames value) => OnPropertyChanged(nameof(GS1TableNumber));
    public double GS1TableNumber => SelectedGS1Table is Sectors.Interfaces.GS1TableNames.None or Sectors.Interfaces.GS1TableNames.Unsupported
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
        var ret1 = WeakReferenceMessenger.Default.Send(new RequestMessage<PrinterSettings>());
        if (ret1.HasReceivedResponse)
            SelectedPrinter = ret1.Response;
    }
    public void Receive(PropertyChangedMessage<PrinterSettings> message) => SelectedPrinter = message.NewValue;

    public Task LoadImages() => IsRooted ? LoadImagesFromDirectory() : LoadImagesFromDatabase();

    public async Task LoadImagesFromDirectory()
    {
        if (Images.Count > 0)
            return;

        LogInfo($"Loading label images from standards directory: {App.AssetsImageRollsRoot}\\{Name}\\");

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
            var png = LibImageUtilities.ImageTypes.Png.Utilities.GetPng(File.ReadAllBytes(path));
            LibImageUtilities.ImageTypes.Png.Png pngImage = new(png);
            if(!pngImage.Chunks.ContainsKey(ChunkTypes.pHYs))
                pngImage.Chunks.Add(ChunkTypes.pHYs, new PHYS_Chunk());

            ImageEntry image = new(UID, pngImage.GetBytes(), TargetDPI, TargetDPI)
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
            ImageEntry image = new(UID, LibImageUtilities.ImageTypes.Png.Utilities.GetPng(rawImage), TargetDPI, TargetDPI)
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
            LogError($"Failed to load image: {Path}", ex);
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

        foreach (var image in images)
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
                LogError($"Failed to load image: {Path}", ex);
            }            
        }
    }

    public void DeleteImage(ImageEntry imageEntry)
    {
        if(ImageRollsDatabase.DeleteImage(imageEntry.UID))
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
