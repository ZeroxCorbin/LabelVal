using CommunityToolkit.Mvvm.ComponentModel;
using LabelVal.Results.Databases;
using LabelVal.Run.Databases;
using LabelVal.Utilities;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace LabelVal.Run.ViewModels;

public partial class Result : ObservableObject
{
    [ObservableProperty] private ImageResultGroup resultEntry;

    [ObservableProperty] private RunEntry runEntry;

    public ObservableCollection<Sectors.ViewModels.Sector> V275CurrentSectors { get; } = [];
    public ObservableCollection<Sectors.ViewModels.Sector> V275StoredSectors { get; } = [];
    public ObservableCollection<Sectors.ViewModels.SectorDifferences> V275DiffSectors { get; } = []; 

    [ObservableProperty] private DrawingImage v275SectorsImageOverlay;

    [ObservableProperty] private bool isGS1Standard;

    public Result(ImageResultGroup result, RunEntry ledgerEntry)
    {
        ResultEntry = result;
        RunEntry = ledgerEntry;
        IsGS1Standard = RunEntry.GradingStandard.StartsWith("GS1");

        GetV275StoredSectors();
        GetV275CurrentSectors();
        GetSectorDiff();
    }

    private void GetV275StoredSectors()
    {
        V275StoredSectors.Clear();

        List<Sectors.ViewModels.Sector> tempSectors = [];
        if (!string.IsNullOrEmpty(ResultEntry.LabelReport) && !string.IsNullOrEmpty(ResultEntry.LabelTemplate))
            foreach (var jSec in JsonConvert.DeserializeObject<V275_REST_lib.Models.Job>(ResultEntry.LabelTemplate).sectors)
            {
                //var isWrongStandard = false;
                //if (jSec.type is "verify1D" or "verify2D")
                //    isWrongStandard = IsGS1Standard && (!jSec.gradingStandard.enabled || !LedgerEntry.GradingStandard.StartsWith($"{jSec.gradingStandard.standard} TABLE {jSec.gradingStandard.tableId}"));

                foreach (var rSec in JsonConvert.DeserializeObject<V275_REST_lib.Models.Report>(ResultEntry.LabelReport).inspectLabel.inspectSector.Cast<JObject>())
                {
                    if (jSec.name == rSec["name"].ToString())
                    {

                        var fSec = DeserializeSector(rSec);

                        if (fSec == null)
                            break;

                        //tempSectors.Add(new Sectors.ViewModels.Sector(jSec, fSec, isWrongStandard, jSec.gradingStandard != null && jSec.gradingStandard.enabled));

                        break;
                    }
                }
            }

        if (tempSectors.Count > 0)
        {
            tempSectors = tempSectors.OrderBy(x => x.Template.Top).ToList();

            foreach (var sec in tempSectors)
                V275StoredSectors.Add(sec);
        }
    }
    private void GetV275CurrentSectors()
    {
        V275CurrentSectors.Clear();

        List<Sectors.ViewModels.Sector> tempSectors = [];
        if (!string.IsNullOrEmpty(ResultEntry.RepeatReport) && !string.IsNullOrEmpty(ResultEntry.LabelTemplate))
            foreach (var jSec in JsonConvert.DeserializeObject<V275_REST_lib.Models.Job>(ResultEntry.LabelTemplate).sectors)
            {
                //var isWrongStandard = false;
                //if (jSec.type is "verify1D" or "verify2D")
                //    isWrongStandard = IsGS1Standard && (!jSec.gradingStandard.enabled || !LedgerEntry.GradingStandard.StartsWith($"{jSec.gradingStandard.standard} TABLE {jSec.gradingStandard.tableId}"));

                foreach (var rSec in JsonConvert.DeserializeObject<V275_REST_lib.Models.Report>(ResultEntry.RepeatReport).inspectLabel.inspectSector.Cast<JObject>())
                {
                    if (jSec.name == rSec["name"].ToString())
                    {

                        var fSec = DeserializeSector(rSec);

                        if (fSec == null)
                            break;

                        //tempSectors.Add(new Sectors.ViewModels.Sector(jSec, fSec, isWrongStandard, jSec.gradingStandard != null && jSec.gradingStandard.enabled));

                        break;
                    }
                }
            }

        if (tempSectors.Count > 0)
        {
            tempSectors = tempSectors.OrderBy(x => x.Template.Top).ToList();

            foreach (var sec in tempSectors)
                V275CurrentSectors.Add(sec);
        }
    }
    private void GetSectorDiff()
    {
        List<Sectors.ViewModels.SectorDifferences> diff = [];
        foreach (var sec in V275StoredSectors)
        {
            foreach (var cSec in V275CurrentSectors)
                if (sec.Template.Name == cSec.Template.Name)
                {
                    diff.Add(sec.SectorDifferences.Compare(cSec.SectorDifferences));
                    continue;
                }

            //if (!found)
            //{
            //    var dat = sec.SectorDifferences.Compare(new SectorDifferenceViewModel());
            //    dat.IsSectorMissing = true;
            //    diff.Add(dat);
            //}

        }

        foreach (var d in diff)
            V275DiffSectors.Add(d);

    }

    public void SaveAction(object parameter)
    {
        var par = (string)parameter;

        var path = GetSaveFilePath();
        if (string.IsNullOrEmpty(path)) return;
        try
        {
            if (par == "stored")
            {
                var bmp = ImageUtilities.ConvertToBmp(ResultEntry.RepeatImage);
                _ = SaveImageBytesToFile(path, bmp);
                Clipboard.SetText(path);
            }
            else
            {
                var bmp = ImageUtilities.ConvertToBmp(ResultEntry.LabelImage);
                _ = SaveImageBytesToFile(path, bmp);
                Clipboard.SetText(path);
            }
        }
        catch (Exception)
        {

        }
    }
    private string GetSaveFilePath()
    {
        var saveFileDialog1 = new SaveFileDialog();
        saveFileDialog1.Filter = "Bitmap Image|*.bmp";//|Gif Image|*.gif|JPeg Image|*.jpg";
        saveFileDialog1.Title = "Save an Image File";
        _ = saveFileDialog1.ShowDialog();

        return saveFileDialog1.FileName;
    }
    private string SaveImageBytesToFile(string path, byte[] img)
    {
        File.WriteAllBytes(path, img);

        return "";
    }

    private object DeserializeSector(JObject reportSec)
    {
        if (reportSec["type"].ToString() == "verify1D")
        {
            return JsonConvert.DeserializeObject<V275_REST_lib.Models.Report_InspectSector_Verify1D>(reportSec.ToString());
        }
        else
        {
            return reportSec["type"].ToString() == "verify2D"
                ? JsonConvert.DeserializeObject<V275_REST_lib.Models.Report_InspectSector_Verify2D>(reportSec.ToString())
                : reportSec["type"].ToString() == "ocr"
                            ? JsonConvert.DeserializeObject<V275_REST_lib.Models.Report_InspectSector_OCR>(reportSec.ToString())
                            : reportSec["type"].ToString() == "ocv"
                                        ? JsonConvert.DeserializeObject<V275_REST_lib.Models.Report_InspectSector_OCV>(reportSec.ToString())
                                        : reportSec["type"].ToString() == "blemish"
                                                    ? JsonConvert.DeserializeObject<V275_REST_lib.Models.Report_InspectSector_Blemish>(reportSec.ToString())
                                                    : (object)null;
        }
    }

    //public void Clear()
    //{
    //    foreach (var sec in V275CurrentSectors)
    //        sec.Clear();
    //    V275CurrentSectors.Clear();
    //    V275CurrentSectors = null;

    //    foreach (var sec in V275StoredSectors)
    //        sec.Clear();
    //    V275StoredSectors.Clear();
    //    V275StoredSectors = null;

    //    foreach (var sec in V275DiffSectors)
    //        sec.Clear();
    //    V275DiffSectors.Clear();
    //    V275DiffSectors = null;

    //    result = null;
    //    LedgerEntry = null;
    //}

}
