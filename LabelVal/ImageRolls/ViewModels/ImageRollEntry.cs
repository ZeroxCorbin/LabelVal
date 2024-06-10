using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using LabelVal.Messages;
using LabelVal.Sectors.ViewModels;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Printing;
using System.IO;
using System.Threading.Tasks;

namespace LabelVal.ImageRolls.ViewModels;

[JsonObject(MemberSerialization.OptIn)]

public partial class ImageRollEntry : ObservableRecipient, IRecipient<PrinterMessages.SelectedPrinterChanged>
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    [ObservableProperty][property: JsonProperty][property: SQLite.PrimaryKey] private string uID = Guid.NewGuid().ToString();
    [ObservableProperty][property: JsonProperty] private string name;
    [ObservableProperty] private string path;
    partial void OnPathChanged(string value) => OnPropertyChanged(nameof(IsRooted));
    [SQLite.Ignore] public bool IsRooted => !string.IsNullOrEmpty(path);

    [ObservableProperty] private int imageCount;

    [ObservableProperty][property: JsonProperty("Standard")][property: SQLite.Column("Standard")] private StandardsTypes selectedStandard;
    [ObservableProperty][property: JsonProperty("GS1Table")][property: SQLite.Column("GS1Table")] private GS1TableNames selectedGS1Table;

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

    public void Receive(PrinterMessages.SelectedPrinterChanged message) => SelectedPrinter = message.Value;

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

    private void AddImage(string path)
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
    private void Add()
    {
        string path;
        if ((path = Utilities.FileUtilities.GetLoadFilePath("", "PNG|*.png|BMP|*.bmp", "Load Image")) != "")
        {
            AddImage(path);
            //Save();
        }
            
    }

    public void AddImage(ImageEntry imageEntry) => Images.Add(imageEntry);

    [RelayCommand]
    private void Save()
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
}
