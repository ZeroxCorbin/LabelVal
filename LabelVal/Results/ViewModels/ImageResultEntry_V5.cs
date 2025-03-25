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
using V5_REST_Lib.Controllers;

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
        LabelHandlers type = LabelHandlers.CameraTrigger;
        if (ImageResults.SelectedScanner.Controller.IsSimulator)
        {
            if (ImageResults.SelectedImageRoll.SectorType == ImageRollSectorTypes.Dynamic)
            {
                type = !string.IsNullOrEmpty(V5ResultRow?.Template)
                    ? LabelHandlers.SimulatorRestore
                    : LabelHandlers.SimulatorDetect;
            }
            else
                type = LabelHandlers.SimulatorTrigger;
        }
        else
        {
            if (ImageResults.SelectedImageRoll.SectorType == ImageRollSectorTypes.Dynamic)
            {
                type = !string.IsNullOrEmpty(V5ResultRow?.Template)
                    ? LabelHandlers.CameraRestore
                    : LabelHandlers.CameraDetect;
            }
            else
                type = LabelHandlers.CameraTrigger;
        }

        BringIntoView?.Invoke();

        V5_REST_Lib.Controllers.Label lab = new(V5ProcessResults, type is LabelHandlers.SimulatorRestore or LabelHandlers.CameraRestore ? V5ResultRow._Config : null, type, ImageResults.SelectedImageRoll.SelectedGS1Table);

        if(type is LabelHandlers.SimulatorRestore or LabelHandlers.SimulatorDetect)
        {
            if (ImageResults.SelectedImageRoll.ImageType == ImageRollImageTypes.Source)
                lab.Image = PrepareImage(SourceImage);
            else if (ImageResults.SelectedImageRoll.ImageType == ImageRollImageTypes.Stored)
                lab.Image = PrepareImage(V5ResultRow.Stored);
        }

        _ = Task.Run(() => ImageResults.SelectedScanner.Controller.ProcessLabel(lab));

        IsV5Working = true;
        IsV5Faulted = false;

        
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

    public void V5ProcessResults(V5_REST_Lib.Controllers.Repeat repeat)
    {
        V5ProcessResults(repeat?.FullReport);
    }

    public void V5ProcessResults(V5_REST_Lib.Controllers.FullReport report)
    {
        if (!App.Current.Dispatcher.CheckAccess())
        {
            _ = App.Current.Dispatcher.BeginInvoke(() => V5ProcessResults(report));
            return;
        }

        try
        {

            if (report == null || report.FullImage == null)
            {
                Logger.LogError("Can not proces null results.");
                IsV5Faulted = true;
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
                        tempSectors.Add(new V5.Sectors.Sector((JObject)result, V5CurrentTemplate, ImageResults.SelectedImageRoll.SelectedStandard, ImageResults.SelectedImageRoll.SelectedGS1Table, V5CurrentTemplate.GetParameter<string>("response.message")));
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
                tempSectors = SortList3(tempSectors);

                foreach (Sectors.Interfaces.ISector sec in tempSectors)
                    V5CurrentSectors.Add(sec);
            }

            V5GetSectorDiff();

            UpdateV5CurrentImageOverlay();

            IsV5Faulted = false;
        }
        catch (System.Exception ex)
        {
            Logger.LogError(ex, ex.StackTrace);
            Logger.LogWarning("Error while processing results.");
            IsV5Faulted = true;
        }
        finally
        {
            IsV5Working = false;
        }
    }

    public void UpdateV5StoredImageOverlay()
    {
        V5StoredImageOverlay = CreateSectorsImageOverlay(V5StoredImage, V5StoredSectors);
    }

    public void UpdateV5CurrentImageOverlay()
    {
        V5CurrentImageOverlay = CreateSectorsImageOverlay(V5CurrentImage, V5CurrentSectors);
    }

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

        V5Result row = SelectedDatabase.Select_V5Result(ImageRollUID, SourceImageUID, ImageRollUID);

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
                        tempSectors.Add(new V5.Sectors.Sector((JObject)result, (JObject)row._Config, ImageResults.SelectedImageRoll.SelectedStandard, ImageResults.SelectedImageRoll.SelectedGS1Table, row._Config.GetParameter<string>("response.message")));
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
            _ = SortList3(tempSectors);

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
                    if (sec.Report.SymbolType == cSec.Report.SymbolType)
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
                            SectorMissingText = $"Stored Sector {sec.Report.SymbolType.GetDescription()}  : Current Sector  {cSec.Report.SymbolType.GetDescription()}"
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
    [RelayCommand]
    private Task<bool> V5Read()
    {
        return V5ReadTask();
    }

    public async Task<bool> V5ReadTask()
    {
        V5_REST_Lib.Controllers.FullReport result = await ImageResults.SelectedScanner.Controller.Trigger_Wait_Return(true);
        V5ProcessResults(result);
        //V275_REST_Lib.FullReport report;
        //if ((report = await ImageResults.SelectedNode.Controller.GetFullReport(repeat, true)) == null)
        //{
        //    Logger.LogError("Unable to read the repeat report from the node.");
        //    ClearRead(ImageResultEntryDevices.V275);
        //    return false;
        //}

        //V275ProcessRepeat(new V275_REST_Lib.Repeat(0, null) { FullReport = report });
        return true;
    }

    [RelayCommand]
    private Task<int> V5Load()
    {
        return V5LoadTask();
    }

    public async Task<int> V5LoadTask()
    {
        if (V5ResultRow == null)
        {
            Logger.LogError("No V5 result row selected.");
            return -1;
        }

        if (V5StoredSectors.Count == 0)
        {
            return 0;
            //return await ImageResults.SelectedScanner.Controller.Learn();
        }

        if (await ImageResults.SelectedScanner.Controller.CopySectorsSetConfig(null, V5ResultRow._Config) == V5_REST_Lib.Controllers.RestoreSectorsResults.Failure)
            return -1;

        //if (!await ImageResults.SelectedNode.Controller.DeleteSectors())
        //    return -1;

        //if (V275StoredSectors.Count == 0)
        //{
        //    return !await ImageResults.SelectedNode.Controller.DetectSectors() ? -1 : 2;
        //}

        //foreach (Sectors.Interfaces.ISector sec in V275StoredSectors)
        //{
        //    if (!await ImageResults.SelectedNode.Controller.AddSector(sec.Template.Name, JsonConvert.SerializeObject(((V275.Sectors.SectorTemplate)sec.Template).Original)))
        //        return -1;

        //    if (sec.Template.BlemishMask.Layers != null)
        //    {

        //        foreach (V275_REST_Lib.Models.Job.Layer layer in sec.Template.BlemishMask.Layers)
        //        {
        //            if (!await ImageResults.SelectedNode.Controller.AddMask(sec.Template.Name, JsonConvert.SerializeObject(layer)))
        //            {
        //                if (layer.value != 0)
        //                    return -1;
        //            }
        //        }
        //    }
        //}

        return 1;
    }
}
