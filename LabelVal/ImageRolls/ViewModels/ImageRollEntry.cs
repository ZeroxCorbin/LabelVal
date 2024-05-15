using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using LabelVal.Messages;
using LabelVal.Sectors.ViewModels;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Drawing.Printing;
using System.Text.RegularExpressions;

namespace LabelVal.ImageRolls.ViewModels;

[JsonObject(MemberSerialization.OptIn)]
public partial class ImageRollEntry : ObservableRecipient, IRecipient<PrinterMessages.SelectedPrinterChanged>
{
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

    public ImageRollEntry() => Images.CollectionChanged += (s, e) => ImageCount = Images.Count;

    public ImageRollEntry(string name, string path)
    {
        Name = name;
        Path = path;

        Images.CollectionChanged += (s, e) => ImageCount = Images.Count;
    }

    public void Receive(PrinterMessages.SelectedPrinterChanged message) => SelectedPrinter = message.Value;
}
