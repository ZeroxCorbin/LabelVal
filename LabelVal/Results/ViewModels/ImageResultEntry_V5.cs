using BarcodeVerification.lib.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.Results.Databases;
using LabelVal.Sectors.Classes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Media;

namespace LabelVal.Results.ViewModels;
public partial class ImageResultEntry
{
    [ObservableProperty] private Databases.V5Result v5ResultRow;
    partial void OnV5ResultRowChanged(V5Result value) => V5StoredImage = value?.Stored;

    [ObservableProperty] private ImageEntry v5StoredImage;
    [ObservableProperty] private DrawingImage v5StoredImageOverlay;

    [ObservableProperty] private ImageEntry v5CurrentImage;
    [ObservableProperty] private DrawingImage v5CurrentImageOverlay;

    public JObject V5CurrentTemplate { get; set; }
    public string V5SerializeTemplate => JsonConvert.SerializeObject(V5CurrentTemplate);

    public JObject V5CurrentReport { get; private set; }
    public string V5SerializeReport => JsonConvert.SerializeObject(V5CurrentReport);

    public ObservableCollection<Sectors.Interfaces.ISector> V5CurrentSectors { get; } = [];
    public ObservableCollection<Sectors.Interfaces.ISector> V5StoredSectors { get; } = [];
    public ObservableCollection<SectorDifferences> V5DiffSectors { get; } = [];

    [ObservableProperty] private Sectors.Interfaces.ISector v5FocusedStoredSector = null;
    [ObservableProperty] private Sectors.Interfaces.ISector v5FocusedCurrentSector = null;

    [ObservableProperty] private bool isV5Working = false;
    partial void OnIsV5WorkingChanged(bool value) => OnPropertyChanged(nameof(IsNotV5Working));
    public bool IsNotV5Working => !IsV5Working;

    [ObservableProperty] private bool isV5Faulted = false;
    partial void OnIsV5FaultedChanged(bool value) => OnPropertyChanged(nameof(IsNotV5Faulted));
    public bool IsNotV5Faulted => !IsV5Faulted;

    [RelayCommand]
    public void V5Process(ImageResultEntryImageTypes imageType)
    {
        bool simAddSec = ImageResults.SelectedScanner.Controller.IsSimulator && ImageResults.SelectedImageRoll.WriteSectorsBeforeProcess && !string.IsNullOrEmpty(V5ResultRow?.Template);
        bool simDetSec = ImageResults.SelectedScanner.Controller.IsSimulator && ImageResults.SelectedImageRoll.WriteSectorsBeforeProcess && string.IsNullOrEmpty(V5ResultRow?.Template);
        bool camAddSec = !ImageResults.SelectedScanner.Controller.IsSimulator && ImageResults.SelectedImageRoll.WriteSectorsBeforeProcess && !string.IsNullOrEmpty(V5ResultRow?.Template);
        bool camDetSec = !ImageResults.SelectedScanner.Controller.IsSimulator && ImageResults.SelectedImageRoll.WriteSectorsBeforeProcess && string.IsNullOrEmpty(V5ResultRow?.Template);

        BringIntoView?.Invoke();

        V5_REST_Lib.Controllers.Label lab = new(V5ProcessResults, simAddSec || camAddSec ? V5ResultRow._Config : null, simDetSec || camDetSec, ImageResults.SelectedImageRoll.SelectedGS1Table);

        if (imageType == ImageResultEntryImageTypes.Source)
            lab.Image = PrepareImage(SourceImage);
        else if (imageType == ImageResultEntryImageTypes.V5Stored)
            lab.Image = PrepareImage(V5ResultRow.Stored);

        IsV5Working = true;
        IsV5Faulted = false;

        _ = Task.Run(() => ImageResults.SelectedScanner.Controller.ProcessLabel(lab));
    }

    private byte[] PrepareImage(ImageEntry img)
    {
        if (img == null)
            return null;

        //If the image is greater than 5 mega pixels, resize it from the edges inward to 5 mega pixels.
        if (img.ImageTotalPixels > 5000000)
        {
            double ratio = Math.Sqrt(5000000.0 / img.ImageTotalPixels);
            int newWidth = (int)(img.Image.PixelWidth * ratio);
            int newHeight = (int)(img.Image.PixelHeight * ratio);
            System.Windows.Media.Imaging.BitmapImage newimg = LibImageUtilities.BitmapImage.ResizeImage(img.Image, newWidth, newHeight);
            return LibImageUtilities.BitmapImage.ImageToBytes(newimg, false);
        }

        return img.ImageBytes;
    }

    public void V5ProcessResults(V5_REST_Lib.Controllers.Repeat repeat) => V5ProcessResults(repeat?.FullReport);
    public void V5ProcessResults(V5_REST_Lib.Controllers.FullReport report)
    {
        if (!App.Current.Dispatcher.CheckAccess())
        {
            _ = App.Current.Dispatcher.BeginInvoke(() => V5ProcessResults(report));
            return;
        }

        if (report == null || report.FullImage == null)
        {
            Logger.LogError("Can not proces null results.");

            ClearRead(ImageResultEntryDevices.V5);

            IsV5Faulted = true;
            IsV5Working = false;
            return;
        }

        V5CurrentImage = new ImageEntry(ImageRollUID, LibImageUtilities.ImageTypes.Png.Utilities.GetPng(report.FullImage), 96);
        
        V5CurrentTemplate = ImageResults.SelectedScanner.Controller.Config;
        V5CurrentReport = report.Report;

        V5CurrentSectors.Clear();

        List<Sectors.Interfaces.ISector> tempSectors = [];
        //Tray and match a toolResult to a toolList
        foreach (JToken toolResult in V5CurrentReport.GetParameter<JArray>("event.data.toolResults"))
        {

            foreach (JToken result in ((JObject)toolResult).GetParameter<JArray>("results"))
            {
                try
                {
                    tempSectors.Add(new V5.Sectors.Sector((JObject)result, (JObject)V5CurrentTemplate, ImageResults.SelectedImageRoll.SelectedStandard, ImageResults.SelectedImageRoll.SelectedGS1Table, $"Symbology_{((JObject)result).GetParameter<int>("toolSlot")}", ImageResults.SelectedScanner.Controller.Version));
                }
                catch (System.Exception ex)
                {
                    Logger.LogError(ex, ex.StackTrace);
                    Logger.LogWarning("Error while processing results.");
                    continue;
                }
            }

        }

        if (tempSectors.Count > 0)
        {
            SortList(tempSectors);

            foreach (Sectors.Interfaces.ISector sec in tempSectors)
                V5CurrentSectors.Add(sec);
        }

        V5GetSectorDiff();

        UpdateV5CurrentImageOverlay();

        IsV5Working = false;
    }

    public void UpdateV5StoredImageOverlay() => V5StoredImageOverlay = CreateSectorsImageOverlay(V5StoredImage, V5StoredSectors);
    public void UpdateV5CurrentImageOverlay() => V5CurrentImageOverlay = CreateSectorsImageOverlay(V5CurrentImage, V5CurrentSectors);

    [RelayCommand] private void V5Load() => _ = V5LoadTask();

    private void V5GetStored()
    {
        if (!App.Current.Dispatcher.CheckAccess())
        {
            _ = App.Current.Dispatcher.BeginInvoke(() => V275GetStored());
            return;
        }

        if (SelectedDatabase == null)
        {
            Logger.LogError("No image results database selected.");
            return;
        }

        V5StoredSectors.Clear();

        V5Result row = SelectedDatabase.Select_V5Result(ImageRollUID, SourceImageUID);

        if (row == null)
        {
            V5ResultRow = null;
            return;
        }

        if (row.Report == null || row.Template == null)
        {
            Logger.LogDebug("V5 result is missing data.");
            return;
        }


        List<Sectors.Interfaces.ISector> tempSectors = [];
        if (!string.IsNullOrEmpty(row.Report))
        {
            foreach (JToken toolResult in row._Report.GetParameter<JArray>("event.data.toolResults"))
            {

                try
                {
                    foreach (JToken result in ((JObject)toolResult).GetParameter<JArray>("results"))
                    {
                        tempSectors.Add(new V5.Sectors.Sector((JObject)result, (JObject)row._Config, ImageResults.SelectedImageRoll.SelectedStandard, ImageResults.SelectedImageRoll.SelectedGS1Table, $"Symbology_{((JObject)result).GetParameter<int>("toolSlot")}", ImageResults.SelectedScanner.Controller.Version));
                    }
                }
                catch (System.Exception ex)
                {
                    Logger.LogError(ex, ex.StackTrace);
                    Logger.LogWarning($"Error while loading stored results from: {SelectedDatabase.File.Name}");
                    continue;
                }
            }
        }


        if (tempSectors.Count > 0)
        {
            SortList(tempSectors);

            foreach (Sectors.Interfaces.ISector sec in tempSectors)
                V5StoredSectors.Add(sec);
        }

        V5ResultRow = row;
        UpdateV5StoredImageOverlay();

    }

    private void V5GetSectorDiff()
    {
        V5DiffSectors.Clear();

        List<SectorDifferences> diff = [];

        //Compare; Do not check for missing here. To keep found at top of list.
        foreach (Sectors.Interfaces.ISector sec in V5StoredSectors)
        {
            foreach (Sectors.Interfaces.ISector cSec in V5CurrentSectors)
                if (sec.Template.Name == cSec.Template.Name)
                {
                    if (sec.Template.SymbologyType == cSec.Template.SymbologyType)
                    {
                        SectorDifferences dat = SectorDifferences.Compare(sec.SectorDetails, cSec.SectorDetails);
                        if (dat != null)
                            diff.Add(dat);
                    }
                    else
                    {
                        SectorDifferences dat = new()
                        {
                            Username = $"{sec.Template.Username} (SYMBOLOGY MISMATCH)",
                            IsSectorMissing = true,
                            SectorMissingText = $"Stored Sector {sec.Template.SymbologyType} : Current Sector {cSec.Template.SymbologyType}"
                        };
                        diff.Add(dat);
                    }
                }
        }

        //Check for missing
        foreach (Sectors.Interfaces.ISector sec in V5StoredSectors)
        {
            bool found = false;
            foreach (Sectors.Interfaces.ISector cSec in V5CurrentSectors)
                if (sec.Template.Name == cSec.Template.Name)
                {
                    found = true;
                    continue;
                }

            if (!found)
            {
                SectorDifferences dat = new()
                {
                    Username = $"{sec.Template.Username} (MISSING)",
                    IsSectorMissing = true,
                    SectorMissingText = "Not found in current Sectors"
                };
                diff.Add(dat);
            }
        }

        //check for missing
        if (V5StoredSectors.Count > 0)
            foreach (Sectors.Interfaces.ISector sec in V5CurrentSectors)
            {
                bool found = false;
                foreach (Sectors.Interfaces.ISector cSec in V5StoredSectors)
                    if (sec.Template.Name == cSec.Template.Name)
                    {
                        found = true;
                        continue;
                    }

                if (!found)
                {
                    SectorDifferences dat = new()
                    {
                        Username = $"{sec.Template.Username} (MISSING)",
                        IsSectorMissing = true,
                        SectorMissingText = "Not found in Stored Sectors"
                    };
                    diff.Add(dat);
                }
            }

        foreach (SectorDifferences d in diff)
            if (d.IsSectorMissing)
                V5DiffSectors.Add(d);
    }
    public int V5LoadTask() => 1;

    private string V5GetLetter(double grade) => grade switch
    {
        double i when i == 4.0 => "A",
        double i when i is < 4.0 and >= 3.0 => "B",
        double i when i is < 3.0 and >= 2.0 => "C",
        double i when i is < 2.0 and >= 1.0 => "D",
        double i when i is < 1.0 and >= 0.0 => "F",
        _ => throw new System.NotImplementedException(),
    };

}
