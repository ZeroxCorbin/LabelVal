using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.Sectors.Classes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Media;

namespace LabelVal.Results.ViewModels;
public partial class ImageResultEntry
{
    public delegate void V275ProcessImageDelegate(ImageResultEntry imageResults, string type);
    public event V275ProcessImageDelegate V275ProcessImage;

    [ObservableProperty] private Databases.V275Result v275ResultRow;
    partial void OnV275ResultRowChanged(Databases.V275Result value) => V275StoredImage = V275ResultRow?.Stored;

    [ObservableProperty] private ImageEntry v275StoredImage;
    [ObservableProperty] private DrawingImage v275StoredImageOverlay;

    [ObservableProperty] private ImageEntry v275CurrentImage;
    [ObservableProperty] private DrawingImage v275CurrentImageOverlay;

    public JObject V275CurrentTemplate { get; set; }
    public string V275SerializeTemplate => JsonConvert.SerializeObject(V275CurrentTemplate);
    public JObject V275CurrentReport { get; private set; }
    public string V275SerializeReport => JsonConvert.SerializeObject(V275CurrentReport);

    public ObservableCollection<Sectors.Interfaces.ISector> V275CurrentSectors { get; } = [];
    public ObservableCollection<Sectors.Interfaces.ISector> V275StoredSectors { get; } = [];
    public ObservableCollection<SectorDifferences> V275DiffSectors { get; } = [];

    [ObservableProperty] private Sectors.Interfaces.ISector v275FocusedStoredSector = null;
    [ObservableProperty] private Sectors.Interfaces.ISector v275FocusedCurrentSector = null;

    [ObservableProperty] private bool isV275Working = false;
    partial void OnIsV275WorkingChanged(bool value) => OnPropertyChanged(nameof(IsNotV275Working));
    public bool IsNotV275Working => !IsV275Working;

    [ObservableProperty] private bool isV275Faulted = false;
    partial void OnIsV275FaultedChanged(bool value) => OnPropertyChanged(nameof(IsNotV275Faulted));
    public bool IsNotV275Faulted => !IsV275Faulted;

    [RelayCommand]
    private async Task V275Process(ImageResultEntryImageTypes type)
    {
        bool simAddSec = ImageResults.SelectedNode.Controller.IsSimulator && ImageResults.SelectedImageRoll.WriteSectorsBeforeProcess && V275ResultRow?._Job["sectors"] != null;
        bool simDetSec = ImageResults.SelectedNode.Controller.IsSimulator && ImageResults.SelectedImageRoll.WriteSectorsBeforeProcess && V275ResultRow?._Job["sectors"] == null;
        bool camAddSec = !ImageResults.SelectedNode.Controller.IsSimulator && ImageResults.SelectedImageRoll.WriteSectorsBeforeProcess && V275ResultRow?._Job["sectors"] != null;
        bool camDetSec = !ImageResults.SelectedNode.Controller.IsSimulator && ImageResults.SelectedImageRoll.WriteSectorsBeforeProcess && V275ResultRow?._Job["sectors"] == null;

        BringIntoView?.Invoke();

        V275_REST_Lib.Label lab = new()
        {
            Table = ImageResults.SelectedImageRoll.SelectedGS1Table,
        };

        if (type is ImageResultEntryImageTypes.Source or ImageResultEntryImageTypes.V275Print)
        {
            lab.Image = SourceImage.ImageBytes;
            lab.Dpi = (int)Math.Round(SourceImage.Image.DpiX, 0);
            lab.Sectors = simDetSec || camDetSec ? [] : camAddSec ? [.. V275ResultRow._Job["sectors"]] : null;
            lab.Table = ImageResults.SelectedImageRoll.SelectedGS1Table;
        }
        else if (type == ImageResultEntryImageTypes.V275Stored)
        {
            lab.Image = V275ResultRow.Stored.ImageBytes;
            lab.Dpi = (int)Math.Round(V275ResultRow.Stored.Image.DpiX, 0);
            lab.Sectors = simAddSec || camAddSec ? [.. V275ResultRow._Job["sectors"]] : null;
            lab.Table = ImageResults.SelectedImageRoll.SelectedGS1Table;
        }

        if (type == ImageResultEntryImageTypes.V275Print)
        {
            Logger.LogInfo("No node selected. Just printing!");
            PrintImage(lab.Image, PrintCount, SelectedPrinter.PrinterName);
            return;
        }

        IsV275Working = true;
        IsV275Faulted = false;

        lab.RepeatAvailable = V275ProcessRepeat;

        if (ImageResults.SelectedNode.Controller.IsSimulator)
        {
            Logger.LogInfo("Processing image with simulator.");
            IsV275Working = await ImageResults.SelectedNode.Controller.ProcessLabel_Simulator(lab);
            IsV275Faulted = !IsV275Working;
        }
        else
        {
            Logger.LogInfo("Processing image with printer.");
            IsV275Working = ImageResults.SelectedNode.Controller.ProcessLabel_Printer(lab, PrintCount, SelectedPrinter.PrinterName);
            IsV275Faulted = !IsV275Working;
        }
    }
    private void V275ProcessRepeat(V275_REST_Lib.Repeat repeat)
    {
        if (repeat == null)
        {
            Logger.LogError("Repeat is null.");
            IsV275Working = false;
            IsV275Faulted = true;
            return;
        }

        if (repeat.FullReport == null)
        {
            IsV275Working = false;
            IsV275Faulted = true;
            Logger.LogError("Fullreport is null.");
            return;
        }

        if (!App.Current.Dispatcher.CheckAccess())
        {
            _ = App.Current.Dispatcher.BeginInvoke(() => V275ProcessRepeat(repeat));
            return;
        }

        V275CurrentTemplate = repeat.FullReport.Job;
        V275CurrentReport = repeat.FullReport.Report;

        if (!ImageResults.SelectedNode.Controller.IsSimulator)
        {
            int dpi = 600;
            V275CurrentImage = new ImageEntry(ImageRollUID, LibImageUtilities.ImageTypes.Png.Utilities.GetPng(repeat.FullReport.Image, dpi), dpi);
        }
        else
        {
            V275CurrentImage = new ImageEntry(ImageRollUID, LibImageUtilities.ImageTypes.Png.Utilities.GetPng(repeat.FullReport.Image, (int)Math.Round(SourceImage.Image.DpiX)), ImageResults.SelectedImageRoll.TargetDPI);
        }

        V275CurrentSectors.Clear();

        List<Sectors.Interfaces.ISector> tempSectors = [];
        foreach (JToken templateSec in V275CurrentTemplate["sectors"])
        {
            foreach (JToken currentSect in V275CurrentReport["inspectLabel"]["inspectSector"])
            {
                if (templateSec["name"].ToString() == currentSect["name"].ToString())
                {
                    tempSectors.Add(new V275.Sectors.Sector((JObject)templateSec, (JObject)currentSect, ImageResults.SelectedImageRoll.SelectedStandard, ImageResults.SelectedImageRoll.SelectedGS1Table, repeat.FullReport.Job["jobVersion"].ToString()));
                    break;
                }
            }
        }

        if (tempSectors.Count > 0)
        {
            SortList(tempSectors);

            foreach (Sectors.Interfaces.ISector sec in tempSectors)
                V275CurrentSectors.Add(sec);
        }

        V275GetSectorDiff();

        UpdateV275CurrentImageOverlay();

        IsV275Working = false;
        IsV275Faulted = false;
    }
    private void V275GetSectorDiff()
    {
        V275DiffSectors.Clear();

        List<SectorDifferences> diff = [];

        //Compare; Do not check for missing her. To keep found at top of list.
        foreach (Sectors.Interfaces.ISector sec in V275StoredSectors)
        {
            foreach (Sectors.Interfaces.ISector cSec in V275CurrentSectors)
                if (sec.Template.Name == cSec.Template.Name)
                {
                    if (sec.Template.SymbologyType == cSec.Template.SymbologyType)
                    {
                        SectorDifferences res = sec.SectorDetails.Compare(cSec.SectorDetails);
                        if (res != null)
                            diff.Add(res);

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
        foreach (Sectors.Interfaces.ISector sec in V275StoredSectors)
        {
            bool found = false;
            foreach (Sectors.Interfaces.ISector cSec in V275CurrentSectors)
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
        if (V275StoredSectors.Count > 0)
            foreach (Sectors.Interfaces.ISector sec in V275CurrentSectors)
            {
                bool found = false;
                foreach (Sectors.Interfaces.ISector cSec in V275StoredSectors)
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

        //ToDo: Sort the diff list
        foreach (SectorDifferences d in diff)
            if (d.IsSectorMissing)
                V275DiffSectors.Add(d);

    }
    public void UpdateV275CurrentImageOverlay() => V275CurrentImageOverlay = CreateSectorsImageOverlay(V275CurrentImage, V275CurrentSectors);

    [RelayCommand] private Task<bool> V275Read() => V275ReadTask(0);
    public async Task<bool> V275ReadTask(int repeat)
    {
        V275_REST_Lib.FullReport report;
        if ((report = await ImageResults.SelectedNode.Controller.GetFullReport(repeat, true)) == null)
        {
            Logger.LogError("Unable to read the repeat report from the node.");
            ClearRead(ImageResultEntryDevices.V275);
            return false;
        }

        V275ProcessRepeat(new V275_REST_Lib.Repeat(0, null, ImageResults.SelectedNode.Controller.Product.part) { FullReport = report });
        return true;
    }

    [RelayCommand] private Task<int> V275Load() => V275LoadTask();
    public async Task<int> V275LoadTask()
    {
        if (!await ImageResults.SelectedNode.Controller.DeleteSectors())
            return -1;

        if (V275StoredSectors.Count == 0)
        {
            return !await ImageResults.SelectedNode.Controller.DetectSectors() ? -1 : 2;
        }

        foreach (Sectors.Interfaces.ISector sec in V275StoredSectors)
        {
            if (!await ImageResults.SelectedNode.Controller.AddSector(sec.Template.Name, JsonConvert.SerializeObject(((V275.Sectors.SectorTemplate)sec.Template).V275Sector)))
                return -1;

            if (sec.Template.BlemishMask.Layers != null)
            {
                foreach (V275_REST_Lib.Models.Job.Layer layer in sec.Template.BlemishMask.Layers)
                {
                    if (!await ImageResults.SelectedNode.Controller.AddMask(sec.Template.Name, JsonConvert.SerializeObject(layer)))
                    {
                        if (layer.value != 0)
                            return -1;
                    }
                }
            }
        }

        return 1;
    }

    private void V275GetStored()
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

        V275StoredSectors.Clear();

        try
        {
            Databases.V275Result row = SelectedDatabase.Select_V275Result(ImageRollUID, SourceImageUID);

            if (row == null)
            {
                V275ResultRow = null;
                return;
            }

            List<Sectors.Interfaces.ISector> tempSectors = [];

            if (!string.IsNullOrEmpty(row.Report) && !string.IsNullOrEmpty(row.Template))
            {
                foreach (JToken jSec in row._Job["sectors"])
                {
                    foreach (JObject rSec in row._Report["inspectLabel"]["inspectSector"])
                    {
                        if (jSec["name"].ToString() == rSec["name"].ToString())
                        {

                            tempSectors.Add(new V275.Sectors.Sector((JObject)jSec, rSec, ImageResults.SelectedImageRoll.SelectedStandard, ImageResults.SelectedImageRoll.SelectedGS1Table, row._Job["jobVersion"].ToString()));

                            break;
                        }
                    }
                }
            }

            if (tempSectors.Count > 0)
            {
                SortList(tempSectors);
                foreach (Sectors.Interfaces.ISector sec in tempSectors)
                    V275StoredSectors.Add(sec);
            }

            V275ResultRow = row;
            UpdateV275StoredImageOverlay();

        }
        catch (System.Exception ex)
        {
            Logger.LogError(ex);
            Logger.LogError($"Error while loading stored results from: {SelectedDatabase.File.Name}");
        }
    }
    private static object V275DeserializeSector(JObject reportSec, bool removeGS1Data)
    {
        if (reportSec["type"].ToString() == "verify1D")
        {
            if (removeGS1Data)
                _ = ((JObject)reportSec["data"]).Remove("gs1SymbolQuality");

            return JsonConvert.DeserializeObject<V275_REST_Lib.Models.Report_InspectSector_Verify1D>(reportSec.ToString());
        }
        else if (reportSec["type"].ToString() == "verify2D")
        {
            if (removeGS1Data)
                _ = ((JObject)reportSec["data"]).Remove("gs1SymbolQuality");

            return JsonConvert.DeserializeObject<V275_REST_Lib.Models.Report_InspectSector_Verify2D>(reportSec.ToString());
        }
        else
        {
            return reportSec["type"].ToString() == "ocr"
                ? JsonConvert.DeserializeObject<V275_REST_Lib.Models.Report_InspectSector_OCR>(reportSec.ToString())
                : reportSec["type"].ToString() == "ocv"
                            ? JsonConvert.DeserializeObject<V275_REST_Lib.Models.Report_InspectSector_OCV>(reportSec.ToString())
                            : reportSec["type"].ToString() == "blemish"
                                        ? JsonConvert.DeserializeObject<V275_REST_Lib.Models.Report_InspectSector_Blemish>(reportSec.ToString())
                                        : (object)null;
        }
    }
    public void UpdateV275StoredImageOverlay() => V275StoredImageOverlay = CreateSectorsImageOverlay(V275StoredImage, V275StoredSectors);

}
