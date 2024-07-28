using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using V275_REST_lib.Models;

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

    public Job V275CurrentTemplate { get; set; }
    public Report V275CurrentReport { get; private set; }

    [ObservableProperty] private ObservableCollection<Sectors.Interfaces.ISector> v275CurrentSectors = [];
    [ObservableProperty] private ObservableCollection<Sectors.Interfaces.ISector> v275StoredSectors = [];
    [ObservableProperty] private ObservableCollection<Sectors.Interfaces.ISectorDifferences> v275DiffSectors = [];
    [ObservableProperty] private Sectors.Interfaces.ISector v275FocusedStoredSector = null;
    [ObservableProperty] private Sectors.Interfaces.ISector v275FocusedCurrentSector = null;

    [ObservableProperty] private bool isV275Working = false;
    partial void OnIsV275WorkingChanged(bool value) => OnPropertyChanged(nameof(IsNotV275Working));
    public bool IsNotV275Working => !IsV275Working;

    [ObservableProperty] private bool isV275Faulted = false;
    partial void OnIsV275FaultedChanged(bool value) => OnPropertyChanged(nameof(IsNotV275Faulted));
    public bool IsNotV275Faulted => !IsV275Faulted;

    [RelayCommand]
    private void V275Process(string imageType)
    {
        IsV275Working = true;
        IsV275Faulted = false;

        BringIntoView?.Invoke();
        V275ProcessImage?.Invoke(this, imageType);
    }
    [RelayCommand]
    private Task<bool> V275Read() => V275ReadTask(0);
    [RelayCommand]
    private Task<int> V275Load() => V275LoadTask();

    private void V275GetStored()
    {
        if (SelectedDatabase == null)
            return;

        V275StoredSectors.Clear();

        V275ResultRow = SelectedDatabase.Select_V275Result(RollUID, ImageUID);

        if (V275ResultRow == null)
            return;

        List<Sectors.Interfaces.ISector> tempSectors = [];
        if (!string.IsNullOrEmpty(V275ResultRow.Report) && !string.IsNullOrEmpty(V275ResultRow.Template))
        {
            foreach (Job.Sector jSec in V275ResultRow._Job.sectors)
            {
                foreach (JObject rSec in V275ResultRow._Report.inspectLabel.inspectSector)
                {
                    if (jSec.name == rSec["name"].ToString())
                    {

                        object fSec = V275DeserializeSector(rSec, false);

                        if (fSec == null)
                            break;

                        tempSectors.Add(new V275.Sectors.Sector(jSec, fSec, ImageResults.SelectedImageRoll.SelectedStandard, ImageResults.SelectedImageRoll.SelectedGS1Table));

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

        V275StoredImageOverlay = CreateSectorsImageOverlay(V275StoredImage, V275StoredSectors);
    }

    public async Task<bool> V275ReadTask(int repeat)
    {
        V275_REST_lib.Controller.FullReport report;
        if ((report = await ImageResults.SelectedNode.Connection.Read(repeat, true)) == null)
        {
            LogError("Unable to read the repeat report from the node.");

            ClearRead("V275");

            return false;
        }

        V275CurrentTemplate = report.job;
        V275CurrentReport = report.report;

        if (!ImageResults.SelectedNode.IsSimulator)
        {
            int dpi = 600;// SelectedPrinter.PrinterName.Contains("ZT620") ? 300 : 600;
            ImageUtilities.SetBitmapDPI(report.image, dpi);
            V275CurrentImage = new ImageEntry(RollUID, report.image, dpi);//ImageUtilities.ConvertToPng(report.image, 600);
        }
        else
        {
            ImageUtilities.SetBitmapDPI(report.image, (int)Math.Round(SourceImage.Image.DpiX));
            V275CurrentImage = new ImageEntry(RollUID, report.image, ImageResults.SelectedImageRoll.TargetDPI);
        }

        V275CurrentSectors.Clear();

        List<Sectors.Interfaces.ISector> tempSectors = [];
        foreach (Job.Sector jSec in V275CurrentTemplate.sectors)
        {
            foreach (JObject rSec in V275CurrentReport.inspectLabel.inspectSector)
            {
                if (jSec.name == rSec["name"].ToString())
                {

                    object fSec = V275DeserializeSector(rSec, ImageResults.SelectedImageRoll.SelectedStandard != Sectors.Interfaces.StandardsTypes.GS1 && ImageResults.SelectedNode.IsOldISO);

                    if (fSec == null)
                        break; //Not yet supported sector type

                    tempSectors.Add(new V275.Sectors.Sector(jSec, fSec, ImageResults.SelectedImageRoll.SelectedStandard, ImageResults.SelectedImageRoll.SelectedGS1Table));

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

        V275CurrentImageOverlay = CreateSectorsImageOverlay(V275CurrentImage, V275CurrentSectors);

        return true;
    }
    private void V275GetSectorDiff()
    {
        V275DiffSectors.Clear();

        List<Sectors.Interfaces.ISectorDifferences> diff = [];

        //Compare; Do not check for missing her. To keep found at top of list.
        foreach (Sectors.Interfaces.ISector sec in V275StoredSectors)
        {
            foreach (Sectors.Interfaces.ISector cSec in V275CurrentSectors)
                if (sec.Template.Name == cSec.Template.Name)
                {
                    if (sec.Template.SymbologyType == cSec.Template.SymbologyType)
                    {
                        diff.Add(sec.SectorDifferences.Compare(cSec.SectorDifferences));
                        continue;
                    }
                    else
                    {
                        V275.Sectors.SectorDifferences dat = new()
                        {
                            UserName = $"{sec.Template.Username} (SYMBOLOGY MISMATCH)",
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
                V275.Sectors.SectorDifferences dat = new()
                {
                    UserName = $"{sec.Template.Username} (MISSING)",
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
                    V275.Sectors.SectorDifferences dat = new()
                    {
                        UserName = $"{sec.Template.Username} (MISSING)",
                        IsSectorMissing = true,
                        SectorMissingText = "Not found in Stored Sectors"
                    };
                    diff.Add(dat);
                }
            }

        //ToDo: Sort the diff list
        foreach (Sectors.Interfaces.ISectorDifferences d in diff)
            if (d.IsNotEmpty || d.IsSectorMissing)
                V275DiffSectors.Add(d);

    }
    public async Task<int> V275LoadTask()
    {
        if (!await ImageResults.SelectedNode.Connection.DeleteSectors())
        {
            LogError(ImageResults.SelectedNode.Connection.Status);
            return -1;
        }

        if (V275StoredSectors.Count == 0)
        {
            if (!await ImageResults.SelectedNode.Connection.DetectSectors())
            {
                LogError(ImageResults.SelectedNode.Connection.Status);
                return -1;
            }

            return 2;
        }

        foreach (V275.Sectors.Sector sec in V275StoredSectors)
        {
            if (!await ImageResults.SelectedNode.Connection.AddSector(sec.Template.Name, JsonConvert.SerializeObject(sec.V275Sector)))
            {
                LogError(ImageResults.SelectedNode.Connection.Status);
                return -1;
            }

            if (sec.Template.BlemishMask.Layers != null)
            {
                foreach (Job.Layer layer in sec.Template.BlemishMask.Layers)
                {
                    if (!await ImageResults.SelectedNode.Connection.AddMask(sec.Template.Name, JsonConvert.SerializeObject(layer)))
                    {
                        if (layer.value != 0)
                        {
                            LogError(ImageResults.SelectedNode.Connection.Status);
                            return -1;
                        }
                    }
                }
            }
        }

        return 1;
    }

    private static object V275DeserializeSector(JObject reportSec, bool removeGS1Data)
    {
        if (reportSec["type"].ToString() == "verify1D")
        {
            if (removeGS1Data)
                _ = ((JObject)reportSec["data"]).Remove("gs1SymbolQuality");

            return JsonConvert.DeserializeObject<Report_InspectSector_Verify1D>(reportSec.ToString());
        }
        else if (reportSec["type"].ToString() == "verify2D")
        {
            if (removeGS1Data)
                _ = ((JObject)reportSec["data"]).Remove("gs1SymbolQuality");

            return JsonConvert.DeserializeObject<Report_InspectSector_Verify2D>(reportSec.ToString());
        }
        else
        {
            return reportSec["type"].ToString() == "ocr"
                ? JsonConvert.DeserializeObject<Report_InspectSector_OCR>(reportSec.ToString())
                : reportSec["type"].ToString() == "ocv"
                            ? JsonConvert.DeserializeObject<Report_InspectSector_OCV>(reportSec.ToString())
                            : reportSec["type"].ToString() == "blemish"
                                        ? JsonConvert.DeserializeObject<Report_InspectSector_Blemish>(reportSec.ToString())
                                        : (object)null;
        }
    }
}
