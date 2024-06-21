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
    [SQLite.Ignore] public bool IsRooted => !string.IsNullOrEmpty(path);

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
            if(SelectedGS1Table is Sectors.ViewModels.GS1TableNames.None or Sectors.ViewModels.GS1TableNames.Unsupported)
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

        List<Task> taskList = new List<Task>();
        foreach (var f in images)
        {
            var tsk = App.Current.Dispatcher.BeginInvoke(() => AddImage(f)).Task;
            taskList.Add(tsk);
        }

        await Task.WhenAll(taskList.ToArray());
    }

    public async Task LoadImagesFromDatabase()
    {
        if (Images.Count > 0)
            return;

        Logger.Info("Loading label images from database: {name}", Name);

        var images = ImageRollsDatabase.SelectAllImages(UID);
        List<Task> taskList = new List<Task>();
        foreach (var f in images)
        {
            var tsk = App.Current.Dispatcher.BeginInvoke(() => AddImage(f)).Task;
            taskList.Add(tsk);
        }

        await Task.WhenAll(taskList.ToArray());
    }


    [RelayCommand]
    private void AddImage()
    {
        List<string> path;
        if ((path = Utilities.FileUtilities.GetLoadFilePaths()).Count > 0)
        {
            foreach(var f in path)
                AddImage(f);
        }

    }
    public void AddImage(ImageEntry imageEntry) => Images.Add(imageEntry);
    public void AddImage(string path)
    {
        try
        {
            var image = new ImageEntry(UID, path, targetDPI, targetDPI);
            SaveImage(image);
            Images.Add(image);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to load image: {name}", path);
        }
    }


    [RelayCommand]
    private void SaveRoll()
    {
        if (IsRooted)
            return;

        ImageRollsDatabase.InsertOrReplaceImageRoll(this);
    }
    [RelayCommand]
    private void SaveImage(ImageEntry image)
    {
        if (IsRooted)
            return;

        ImageRollsDatabase.InsertOrReplaceImage(image);
    }

    public ImageRollEntry CopyLite() => JsonConvert.DeserializeObject<ImageRollEntry>(JsonConvert.SerializeObject(this));
}
