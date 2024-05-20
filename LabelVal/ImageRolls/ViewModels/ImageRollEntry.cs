using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using LabelVal.Messages;
using LabelVal.Sectors.ViewModels;
using Newtonsoft.Json;
using NHibernate.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace LabelVal.ImageRolls.ViewModels;

[JsonObject(MemberSerialization.OptIn)]
public partial class ImageRollEntry : ObservableRecipient, IRecipient<PrinterMessages.SelectedPrinterChanged>
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    [ObservableProperty][property: JsonProperty] private string uID = new Guid().ToString();
    [ObservableProperty][property: JsonProperty] private string name;
    [ObservableProperty] private string path;

    [ObservableProperty] private int imageCount;

    [ObservableProperty] private double imageDPI;
    [ObservableProperty] private bool isImageDPIConsistent;

    [ObservableProperty][property: JsonProperty("Standard")] private StandardsTypes selectedStandard;
    [ObservableProperty][property: JsonProperty("GS1Table")] private GS1TableNames selectedGS1Table;

    [ObservableProperty] private PrinterSettings selectedPrinter;
    partial void OnSelectedPrinterChanged(PrinterSettings value)
    {
        foreach (var image in Images)
        {
            
        }
    }

    //If writeSectorsBeforeProcess is true the system will write the templates sectors before processing an image.
    //Normally the template is left untouched. I.e. When using a sequential OCR tool.
    [ObservableProperty][property: JsonProperty] private bool writeSectorsBeforeProcess = false;
    [ObservableProperty][property: JsonProperty] private int targetDPI;

    [ObservableProperty][property: JsonProperty] private bool isLocked = false;

    public ObservableCollection<ImageEntry> Images { get; set; } = [];

    public ImageRollEntry() { Images.CollectionChanged += (s, e) => ImageCount = Images.Count; IsActive = true; }

    public ImageRollEntry(string name, string path, PrinterSettings printerSettings)
    {
        SelectedPrinter = printerSettings;

        Name = name;
        Path = path;

        Images.CollectionChanged += (s, e) => ImageCount = Images.Count;

        IsActive = true;
    }

    public void Receive(PrinterMessages.SelectedPrinterChanged message) => SelectedPrinter = message.Value;

    private List<ImageEntry> entries = new List<ImageEntry>();
    public async Task LoadImages()
    {
        if (Images.Count > 0)
            return;

        Logger.Info("Loading label images from standards directory: {name}", $"{App.AssetsImageRollRoot}\\{Name}\\");

        List<string> images = [];
        foreach (var f in Directory.EnumerateFiles(Path))
            if (System.IO.Path.GetExtension(f) == ".png")
                images.Add(f);


        entries.Clear();
        List<Task> taskList = new List<Task>();
        foreach (var f in images)
        {
            var tsk = App.Current.Dispatcher.BeginInvoke(() => LoadImage(f)).Task;
            taskList.Add(tsk);
        }

        await Task.WhenAll(taskList.ToArray());

    }

    private void LoadImage(string path)
    {
        try
        {
            var image = new ImageEntry(path, targetDPI, targetDPI);
            Images.Add(image);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to load image: {name}", path);
        }
    }
}
