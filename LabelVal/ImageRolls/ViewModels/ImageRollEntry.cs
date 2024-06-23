using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.Extensions;
using LabelVal.Messages;
using LabelVal.Sectors.ViewModels;
using LabelVal.Sectors.Views;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ZstdSharp.Unsafe;

namespace LabelVal.ImageRolls.ViewModels;

[JsonObject(MemberSerialization.OptIn)]

public partial class ImageRollEntry : ObservableRecipient, IRecipient<PropertyChangedMessage<PrinterSettings>>
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    public Array StandardsTypes
    {
        get
        {
            var lst = Enum.GetValues(typeof(StandardsTypes)).Cast<StandardsTypes>().ToList();
            lst.Remove(Sectors.ViewModels.StandardsTypes.Unsupported);

            List<string> names = [];
            foreach (var name in lst)
                names.Add(name.GetDescription());

            return names.ToArray();
        }
    }
    public Array GS1TableNames
    {
        get
        {
            var lst = Enum.GetValues(typeof(GS1TableNames)).Cast<GS1TableNames>().ToList();
            lst.Remove(Sectors.ViewModels.GS1TableNames.Unsupported);
            lst.Remove(Sectors.ViewModels.GS1TableNames.None);

            List<string> names = [];
            foreach (var name in lst)
                names.Add(name.GetDescription());

            return names.ToArray();
        }
    }

    [ObservableProperty][property: JsonProperty][property: SQLite.PrimaryKey] private string uID = Guid.NewGuid().ToString();
    [ObservableProperty][property: JsonProperty] private string name;
    [ObservableProperty] private string path;
    partial void OnPathChanged(string value) => OnPropertyChanged(nameof(IsRooted));
    [SQLite.Ignore] public bool IsRooted => !string.IsNullOrEmpty(Path);

    [ObservableProperty] private int imageCount;

    [ObservableProperty][property: JsonProperty("Standard")][property: SQLite.Column("Standard")] private StandardsTypes selectedStandard;
    partial void OnSelectedStandardChanged(StandardsTypes value) { if (value != Sectors.ViewModels.StandardsTypes.GS1) SelectedGS1Table = Sectors.ViewModels.GS1TableNames.None; OnPropertyChanged(nameof(StandardDescription)); }
    public string StandardDescription => SelectedStandard.GetDescription();


    [ObservableProperty][property: JsonProperty("GS1Table")][property: SQLite.Column("GS1Table")] private GS1TableNames selectedGS1Table;
    partial void OnSelectedGS1TableChanged(GS1TableNames value) => OnPropertyChanged(nameof(GS1TableNumber));
    public double GS1TableNumber
    {
        get
        {
            if (SelectedGS1Table is Sectors.ViewModels.GS1TableNames.None or Sectors.ViewModels.GS1TableNames.Unsupported)
                return 0;

            return double.Parse(SelectedGS1Table.GetDescription());
        }
    }

    //If writeSectorsBeforeProcess is true the system will write the templates sectors before processing an image.
    //Normally the template is left untouched. I.e. When using a sequential OCR tool.
    [ObservableProperty][property: JsonProperty] private bool writeSectorsBeforeProcess = false;
    [ObservableProperty][property: JsonProperty] private int targetDPI;

    [ObservableProperty][property: JsonProperty] private bool isLocked = false;

    [ObservableProperty][property: SQLite.Ignore] private PrinterSettings selectedPrinter;

    [SQLite.Ignore] public ObservableCollection<ImageEntry> Images { get; set; } = [];

    [SQLite.Ignore] public Databases.ImageRolls ImageRollsDatabase { get; set; }

    public ImageRollEntry() { Images.CollectionChanged += (s, e) => ImageCount = Images.Count; IsActive = true; }

    public ImageRollEntry(string name, string path, PrinterSettings printerSettings, Databases.ImageRolls imageRollsDatabase)
    {
        SelectedPrinter = printerSettings;
        ImageRollsDatabase = imageRollsDatabase;

        Name = name;
        Path = path;

        Images.CollectionChanged += (s, e) => ImageCount = Images.Count;

        IsActive = true;
    }

    public void Receive(PropertyChangedMessage<PrinterSettings> message) => SelectedPrinter = message.NewValue;

    public Task LoadImages()
    {
        if (IsRooted)
            return LoadImagesFromDirectory();
        else
            return LoadImagesFromDatabase();
    }

    public async Task LoadImagesFromDirectory()
    {
        if (Images.Count > 0)
            return;

        Logger.Info("Loading label images from standards directory: {name}", $"{App.AssetsImageRollRoot}\\{Name}\\");

        List<string> images = [];
        foreach (var f in Directory.EnumerateFiles(Path))
            if (System.IO.Path.GetExtension(f) == ".png")
                images.Add(f);

        List<Task> taskList = [];

        var sorted = images.OrderBy(x => x).ToList();
        int i = 0;
        foreach (var f in sorted)
        {
            var tsk = App.Current.Dispatcher.BeginInvoke(() => AddImage(f, i++)).Task;
            taskList.Add(tsk);
        }

        await Task.WhenAll([.. taskList]);
    }

    public async Task LoadImagesFromDatabase()
    {
        if (Images.Count > 0)
            return;

        Logger.Info("Loading label images from database: {name}", Name);

        var images = ImageRollsDatabase.SelectAllImages(UID);
        List<Task> taskList = [];

        CheckImageEntryOrder([.. images]);

        foreach (var f in images)
        {
            var tsk = App.Current.Dispatcher.BeginInvoke(() => AddImage(f)).Task;
            taskList.Add(tsk);
        }

        await Task.WhenAll([.. taskList]);


    }

    public void ConfirmOrder() => CheckImageEntryOrder([.. Images]);
    private void CheckImageEntryOrder(ImageEntry[] entries)
    {
        bool invalid = entries.Any(x => x.Order < 0 || x.Order >= entries.Length);

        List<ImageEntry> images = invalid ? [.. entries.OrderBy(x => x.Order)] : [.. entries.OrderBy(x => x.Path)];

        for (int i = 0; i < images.Count; i++)
        {
            if (images[i].Order != i)
            {
                images[i].Order = i;
                SaveImage(images[i]);
            }
        }
    }

    [RelayCommand]
    private void AddImage()
    {
        var settings = new Utilities.FileUtilities.LoadFileDialogSettings
        {
            Title = "Select image(s) to add to roll.",
            Multiselect = true,
            Filters =
            [
                new Utilities.FileUtilities.LoadFileDialogFilter
                {
                    Description = "Image Files", Extensions = ["png", "bmp"]
                },
                new Utilities.FileUtilities.LoadFileDialogFilter
                {
                    Description = "Image Files (Add Fiducial)", Extensions = ["png", "bmp"]
                },
                new Utilities.FileUtilities.LoadFileDialogFilter
                {
                    Description = "PNG Files", Extensions = ["png"]
                },
                new Utilities.FileUtilities.LoadFileDialogFilter
                {
                    Description = "BMP Files", Extensions = ["bmp"]
                },
            ]
        };

        if (Utilities.FileUtilities.LoadFileDialog(settings))
        {
            var sorted = settings.SelectedFiles.OrderBy(x => x).ToList();
            int last = -1;
            if (Images.Count > 0)
            {
                var sortedImages = Images.OrderBy(x => x.Order).ToList();
                last = sortedImages.Last().Order;
            }

            int i = last + 1;
            foreach (var f in sorted)
                AddImage(f, i++);
        }

        CheckImageEntryOrder([.. Images]);
    }

    public void AddImage(ImageEntry imageEntry) => Images.Add(imageEntry);
    public void AddImage(string path, int order)
    {
        try
        {
            var image = new ImageEntry(UID, path, TargetDPI, TargetDPI)
            {
                Order = order
            };
            if (Images.Any(e => e.UID == image.UID))
            {
                Logger.Warn("Image already exists in roll: {path}", path);
                return;
            }
            SaveImage(image);
            Images.Add(image);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to load image: {name}", path);
        }
    }

    public void DeleteImage(ImageEntry imageEntry)
    {
        ImageRollsDatabase.DeleteImage(imageEntry.UID);
        Images.Remove(imageEntry);

        ConfirmOrder();
    }

    [RelayCommand]
    private void SaveRoll()
    {
        if (IsRooted)
            return;

        _ = ImageRollsDatabase.InsertOrReplaceImageRoll(this);
    }
    [RelayCommand]
    private void SaveImage(ImageEntry image)
    {
        if (IsRooted)
            return;

        _ = ImageRollsDatabase.InsertOrReplaceImage(image);
    }

    public ImageRollEntry CopyLite() => JsonConvert.DeserializeObject<ImageRollEntry>(JsonConvert.SerializeObject(this));
}
