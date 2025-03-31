using BarcodeVerification.lib.Common;
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
public partial class ImageResultDeviceEntry_V275(ImageResultEntry imageResultsEntry) : ObservableObject, IImageResultDeviceEntry
{
    public ImageResultEntry ImageResultEntry { get; } = imageResultsEntry;
    public ImageResultsManager ImageResultsManager => ImageResultEntry.ImageResultsManager;

    public ImageResultEntryDevices Device { get; } = ImageResultEntryDevices.V275;
    public string Version => throw new NotImplementedException();

    [ObservableProperty] private Databases.Result resultRow;
    partial void OnResultRowChanged(Databases.Result value) => StoredImage = ResultRow?.Stored;
    public Result Result { get => ResultRow; set => ResultRow = value; }

    [ObservableProperty] private ImageEntry storedImage;
    [ObservableProperty] private DrawingImage storedImageOverlay;

    [ObservableProperty] private ImageEntry currentImage;
    [ObservableProperty] private DrawingImage currentImageOverlay;

    public JObject CurrentTemplate { get; set; }
    public string SerializeTemplate => JsonConvert.SerializeObject(CurrentTemplate);

    public JObject CurrentReport { get; private set; }
    public string SerializeReport => JsonConvert.SerializeObject(CurrentReport);

    public ObservableCollection<Sectors.Interfaces.ISector> CurrentSectors { get; } = [];
    public ObservableCollection<Sectors.Interfaces.ISector> StoredSectors { get; } = [];
    public ObservableCollection<SectorDifferences> DiffSectors { get; } = [];

    [ObservableProperty] private Sectors.Interfaces.ISector focusedStoredSector = null;
    [ObservableProperty] private Sectors.Interfaces.ISector focusedCurrentSector = null;

    [ObservableProperty] private bool isWorking = false;
    partial void OnIsWorkingChanged(bool value) => OnPropertyChanged(nameof(IsNotWorking));
    public bool IsNotWorking => !IsWorking;

    [ObservableProperty] private bool isFaulted = false;
    partial void OnIsFaultedChanged(bool value) => OnPropertyChanged(nameof(IsNotFaulted));
    public bool IsNotFaulted => !IsFaulted;

    [ObservableProperty] private bool isSelected = false;
    partial void OnIsSelectedChanging(bool value) { if (value) ImageResultEntry.ImageResultsManager.ResetSelected(Device); }

    public delegate void ProcessImageDelegate(ImageResultEntry imageResults, string type);
    public event ProcessImageDelegate ProcessImage;

    public void GetStored()
    {
        if (!App.Current.Dispatcher.CheckAccess())
        {
            _ = App.Current.Dispatcher.BeginInvoke(() => GetStored());
            return;
        }

        if (ImageResultEntry.SelectedDatabase == null)
        {
            Logger.LogError("No image results database selected.");
            return;
        }

        StoredSectors.Clear();

        try
        {
            Databases.Result row = ImageResultEntry.SelectedDatabase.Select_Result(Device, ImageResultEntry.ImageRollUID, ImageResultEntry.SourceImageUID, ImageResultEntry.ImageRollUID);

            if (row == null)
            {
                ResultRow = null;
                return;
            }

            List<Sectors.Interfaces.ISector> tempSectors = [];

            if (!string.IsNullOrEmpty(row.ReportString) && !string.IsNullOrEmpty(row.TemplateString))
            {
                foreach (JToken jSec in row.Template["sectors"])
                {
                    try
                    {
                        foreach (JObject rSec in row.Report.GetParameter<JArray>("inspectLabel.inspectSector"))
                        {

                            if (jSec["name"].ToString() == rSec["name"].ToString())
                            {

                                tempSectors.Add(new V275.Sectors.Sector((JObject)jSec, rSec, ImageResultEntry.ImageResultsManager.SelectedImageRoll.SelectedStandard, ImageResultEntry.ImageResultsManager.SelectedImageRoll.SelectedGS1Table, row.Template["jobVersion"].ToString()));

                                break;
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Logger.LogError(ex);
                        Logger.LogError($"Error while loading stored results from: {ImageResultEntry.SelectedDatabase.File.Name}");
                    }
                }
            }

            if (tempSectors.Count > 0)
            {
                tempSectors = ImageResultEntry.SortList3(tempSectors);

                foreach (Sectors.Interfaces.ISector sec in tempSectors)
                    StoredSectors.Add(sec);
            }

            ResultRow = row;
            RefreshStoredOverlay();

        }
        catch (System.Exception ex)
        {
            Logger.LogError(ex);
            Logger.LogError($"Error while loading stored results from: {ImageResultEntry.SelectedDatabase.File.Name}");
        }
    }
    public void Store()
    {
        if (StoredSectors.Count == 0)
        {
            Logger.LogError("No sectors to store.");
            return;
        }
        if (ImageResultEntry.SelectedDatabase == null)
        {
            Logger.LogError("No image results database selected.");
            return;
        }

        var res = new Databases.Result
        {
            Device = Device,
            ImageRollUID = ImageResultEntry.ImageRollUID,
            SourceImageUID = ImageResultEntry.SourceImageUID,
            RunUID = ImageResultEntry.ImageRollUID,
            Template = CurrentTemplate,
            Report = CurrentReport,
            Stored = CurrentImage
        };

        if (ImageResultEntry.SelectedDatabase.InsertOrReplace_Result(res) == null)
            Logger.LogError($"Error while storing results to: {ImageResultEntry.SelectedDatabase.File.Name}");
    }

    [RelayCommand]
    private void Process()
    {
        LabelHandlers type = LabelHandlers.CameraTrigger;
        type = ImageResultEntry.ImageResultsManager.SelectedV275Node.Controller.IsSimulator
            ? ImageResultEntry.ImageResultsManager.SelectedImageRoll.SectorType == ImageRollSectorTypes.Dynamic
                ? !string.IsNullOrEmpty(ResultRow.TemplateString)
                    ? LabelHandlers.SimulatorRestore
                    : LabelHandlers.SimulatorDetect
                : LabelHandlers.SimulatorTrigger
            : ImageResultEntry.ImageResultsManager.SelectedImageRoll.SectorType == ImageRollSectorTypes.Dynamic
                ? !string.IsNullOrEmpty(ResultRow.TemplateString)
                    ? LabelHandlers.CameraRestore
                    : LabelHandlers.CameraDetect
                : LabelHandlers.CameraTrigger;

        //ImageResultEntry.BringIntoView?.Invoke();

        V275_REST_Lib.Controllers.Label lab = new(ProcessRepeat, type is LabelHandlers.SimulatorRestore or LabelHandlers.CameraRestore ? [.. ResultRow.Template["sectors"]] : null, type, ImageResultEntry.ImageResultsManager.SelectedImageRoll.SelectedGS1Table);


            if (ImageResultEntry.ImageResultsManager.SelectedImageRoll.ImageType == ImageRollImageTypes.Source)
                lab.Image = ImageResultEntry.SourceImage.BitmapBytes;
            else if (ImageResultEntry.ImageResultsManager.SelectedImageRoll.ImageType == ImageRollImageTypes.Stored)
                lab.Image = ResultRow.Stored.ImageBytes;


        _ = ImageResultEntry.ImageResultsManager.SelectedV275Node.Controller.IsSimulator
            ? ImageResultEntry.ImageResultsManager.SelectedV275Node.Controller.ProcessLabel_Simulator(lab)
            : ImageResultEntry.ImageResultsManager.SelectedV275Node.Controller.ProcessLabel_Printer(lab, ImageResultEntry.PrintCount, ImageResultEntry.SelectedPrinter.PrinterName);

        IsWorking = true;
        IsFaulted = false;
    }
    private void ProcessRepeat(V275_REST_Lib.Controllers.Repeat repeat) => ProcessFullReport(repeat.FullReport);
    public void ProcessFullReport(V275_REST_Lib.Controllers.FullReport report)
    {
        if (!App.Current.Dispatcher.CheckAccess())
        {
            _ = App.Current.Dispatcher.BeginInvoke(() => ProcessFullReport(report));
            return;
        }

        try
        {
            if (report == null)
            {
                Logger.LogError("Full Report is null.");
                IsFaulted = true;
                return;
            }

            CurrentTemplate = report.Job;
            CurrentReport = report.Report;

            var jobString = JsonConvert.SerializeObject(report.Report);

            if (!ImageResultEntry.ImageResultsManager.SelectedV275Node.Controller.IsSimulator)
            {
                CurrentImage = new ImageEntry(ImageResultEntry.ImageRollUID, report.Image, 600);
            }
            else
            {
                using var img = new ImageMagick.MagickImage(report.Image);
                CurrentImage = new ImageEntry(ImageResultEntry.ImageRollUID, report.Image, (int)Math.Round(ImageResultEntry.SourceImage.Image.DpiX));
            }

            CurrentSectors.Clear();

            List<Sectors.Interfaces.ISector> tempSectors = [];
            foreach (JToken templateSec in CurrentTemplate["sectors"])
            {
                foreach (JToken currentSect in CurrentReport["inspectLabel"]["inspectSector"])
                {
                    try
                    {
                        if (templateSec["name"].ToString() == currentSect["name"].ToString())
                        {
                            tempSectors.Add(new V275.Sectors.Sector((JObject)templateSec, (JObject)currentSect, ImageResultEntry.ImageResultsManager.SelectedImageRoll.SelectedStandard, ImageResultEntry.ImageResultsManager.SelectedImageRoll.SelectedGS1Table, report.Job["jobVersion"].ToString()));
                            break;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Logger.LogError(ex);
                        Logger.LogError("Error while processing the repeat report.");
                    }
                }
            }

            if (tempSectors.Count > 0)
            {
                tempSectors = ImageResultEntry.SortList3(tempSectors);

                foreach (Sectors.Interfaces.ISector sec in tempSectors)
                    CurrentSectors.Add(sec);
            }

            GetSectorDiff();

            RefreshCurrentOverlay();

            IsFaulted = false;
        }
        catch (System.Exception ex)
        {
            Logger.LogError(ex);
            Logger.LogError("Error while processing the repeat report.");

            IsFaulted = true;
        }
        finally
        {
            IsWorking = false;
        }
    }

    public void ClearCurrent()
    {
        if (!App.Current.Dispatcher.CheckAccess())
        {
            _ = App.Current.Dispatcher.BeginInvoke(() => ClearCurrent());
            return;
        }

        CurrentSectors.Clear();
        DiffSectors.Clear();

        CurrentTemplate = null;
        CurrentReport = null;
        CurrentImage = null;
        CurrentImageOverlay = null;
    }

    private void GetSectorDiff()
    {
        DiffSectors.Clear();

        List<SectorDifferences> diff = [];

        //Compare; Do not check for missing her. To keep found at top of list.
        foreach (Sectors.Interfaces.ISector sec in StoredSectors)
        {
            foreach (Sectors.Interfaces.ISector cSec in CurrentSectors)
                if (sec.Template.Name == cSec.Template.Name)
                {
                    if (sec.Report.SymbolType == cSec.Report.SymbolType)
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
                            SectorMissingText = $"Stored Sector {sec.Report.SymbolType.GetDescription()}  : Current Sector  {cSec.Report.SymbolType.GetDescription()}"
                        };
                        diff.Add(dat);
                    }
                }
        }

        //Check for missing
        foreach (Sectors.Interfaces.ISector sec in StoredSectors)
        {
            var found = false;
            foreach (Sectors.Interfaces.ISector cSec in CurrentSectors)
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
        if (StoredSectors.Count > 0)
            foreach (Sectors.Interfaces.ISector sec in CurrentSectors)
            {
                var found = false;
                foreach (Sectors.Interfaces.ISector cSec in StoredSectors)
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
                DiffSectors.Add(d);

    }

    [RelayCommand]
    private Task<bool> Read() => ReadTask(0);
    public async Task<bool> ReadTask(int repeat)
    {
        V275_REST_Lib.Controllers.FullReport report;
        if ((report = await ImageResultEntry.ImageResultsManager.SelectedV275Node.Controller.GetFullReport(repeat, true)) == null)
        {
            Logger.LogError("Unable to read the repeat report from the node.");
            ClearCurrent();
            return false;
        }

        ProcessFullReport(report);
        return true;
    }

    [RelayCommand]
    private Task<int> Load() => LoadTask();
    public async Task<int> LoadTask()
    {
        if (!await ImageResultEntry.ImageResultsManager.SelectedV275Node.Controller.DeleteSectors())
            return -1;

        if (StoredSectors.Count == 0)
        {
            return !await ImageResultEntry.ImageResultsManager.SelectedV275Node.Controller.DetectSectors() ? -1 : 2;
        }

        foreach (Sectors.Interfaces.ISector sec in StoredSectors)
        {
            if (!await ImageResultEntry.ImageResultsManager.SelectedV275Node.Controller.AddSector(sec.Template.Name, JsonConvert.SerializeObject(((V275.Sectors.SectorTemplate)sec.Template).Original)))
                return -1;

            if (sec.Template.BlemishMask.Layers != null)
            {

                foreach (V275_REST_Lib.Models.Job.Layer layer in sec.Template.BlemishMask.Layers)
                {
                    if (!await ImageResultEntry.ImageResultsManager.SelectedV275Node.Controller.AddMask(sec.Template.Name, JsonConvert.SerializeObject(layer)))
                    {
                        if (layer.value != 0)
                            return -1;
                    }
                }
            }
        }

        return 1;
    }

    public void RefreshOverlays()
    {
        RefreshCurrentOverlay();
        RefreshStoredOverlay();
    }
    public void RefreshCurrentOverlay() => CurrentImageOverlay = IImageResultDeviceEntry.CreateSectorsImageOverlay(CurrentImage, CurrentSectors);
    public void RefreshStoredOverlay() => StoredImageOverlay = IImageResultDeviceEntry.CreateSectorsImageOverlay(StoredImage, StoredSectors);
}
