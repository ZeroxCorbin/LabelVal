using CommunityToolkit.Mvvm.ComponentModel;
using LabelVal.Run.Databases;
using LabelVal.Utilities;
using LabelVal.V275.ViewModels;
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
using V275_REST_lib.Models;

namespace LabelVal.result.ViewModels;

public partial class LabelViewModel : ObservableObject
{
    [ObservableProperty] private ResultDatabase.Result result;

    [ObservableProperty] private LedgerDatabase.LedgerEntry ledgerEntry;

    public ObservableCollection<Sectors> V275CurrentSectors { get; } = [];
    public ObservableCollection<Sectors> V275StoredSectors { get; } = [];
    public ObservableCollection<SectorDifferences> V275DiffSectors { get; } = []; 

    [ObservableProperty] private DrawingImage v275StoredSectorsImageOverlay;

    [ObservableProperty] private bool isGS1Standard;

    public LabelViewModel(ResultDatabase.Result result, LedgerDatabase.LedgerEntry ledgerEntry)
    {
        Result = result;
        LedgerEntry = ledgerEntry;
        IsGS1Standard = LedgerEntry.GradingStandard.StartsWith("GS1");

        GetV275StoredSectors();
        GetV275CurrentSectors();
        GetSectorDiff();
    }

    private void GetV275StoredSectors()
    {
        V275StoredSectors.Clear();

        List<Sectors> tempSectors = [];
        if (!string.IsNullOrEmpty(Result.LabelReport) && !string.IsNullOrEmpty(Result.LabelTemplate))
            foreach (var jSec in JsonConvert.DeserializeObject<Job>(Result.LabelTemplate).sectors)
            {
                var isWrongStandard = false;
                if (jSec.type is "verify1D" or "verify2D")
                    isWrongStandard = IsGS1Standard && (!jSec.gradingStandard.enabled || !LedgerEntry.GradingStandard.StartsWith($"{jSec.gradingStandard.standard} TABLE {jSec.gradingStandard.tableId}"));

                foreach (var rSec in JsonConvert.DeserializeObject<Report>(Result.LabelReport).inspectLabel.inspectSector.Cast<JObject>())
                {
                    if (jSec.name == rSec["name"].ToString())
                    {

                        var fSec = DeserializeSector(rSec);

                        if (fSec == null)
                            break;

                        tempSectors.Add(new Sectors(jSec, fSec, isWrongStandard, jSec.gradingStandard != null && jSec.gradingStandard.enabled));

                        break;
                    }
                }
            }

        if (tempSectors.Count > 0)
        {
            tempSectors = tempSectors.OrderBy(x => x.JobSector.top).ToList();

            foreach (var sec in tempSectors)
                V275StoredSectors.Add(sec);
        }
    }
    private void GetV275CurrentSectors()
    {
        V275CurrentSectors.Clear();

        List<Sectors> tempSectors = [];
        if (!string.IsNullOrEmpty(Result.RepeatReport) && !string.IsNullOrEmpty(Result.LabelTemplate))
            foreach (var jSec in JsonConvert.DeserializeObject<Job>(Result.LabelTemplate).sectors)
            {
                var isWrongStandard = false;
                if (jSec.type is "verify1D" or "verify2D")
                    isWrongStandard = IsGS1Standard && (!jSec.gradingStandard.enabled || !LedgerEntry.GradingStandard.StartsWith($"{jSec.gradingStandard.standard} TABLE {jSec.gradingStandard.tableId}"));

                foreach (var rSec in JsonConvert.DeserializeObject<Report>(Result.RepeatReport).inspectLabel.inspectSector.Cast<JObject>())
                {
                    if (jSec.name == rSec["name"].ToString())
                    {

                        var fSec = DeserializeSector(rSec);

                        if (fSec == null)
                            break;

                        tempSectors.Add(new Sectors(jSec, fSec, isWrongStandard, jSec.gradingStandard != null && jSec.gradingStandard.enabled));

                        break;
                    }
                }
            }

        if (tempSectors.Count > 0)
        {
            tempSectors = tempSectors.OrderBy(x => x.JobSector.top).ToList();

            foreach (var sec in tempSectors)
                V275CurrentSectors.Add(sec);
        }
    }
    private void GetSectorDiff()
    {
        List<SectorDifferences> diff = [];
        foreach (var sec in V275StoredSectors)
        {
            foreach (var cSec in V275CurrentSectors)
                if (sec.JobSector.name == cSec.JobSector.name)
                {
                    diff.Add(sec.SectorResults.Compare(cSec.SectorResults));
                    continue;
                }

            //if (!found)
            //{
            //    var dat = sec.SectorResults.Compare(new SectorDifferenceViewModel());
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
                var bmp = ImageUtilities.ConvertToBmp(Result.RepeatImage);
                _ = SaveImageBytesToFile(path, bmp);
                Clipboard.SetText(path);
            }
            else
            {
                var bmp = ImageUtilities.ConvertToBmp(Result.LabelImage);
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
            return JsonConvert.DeserializeObject<Report_InspectSector_Verify1D>(reportSec.ToString());
        }
        else
        {
            return reportSec["type"].ToString() == "verify2D"
                ? JsonConvert.DeserializeObject<Report_InspectSector_Verify2D>(reportSec.ToString())
                : reportSec["type"].ToString() == "ocr"
                            ? JsonConvert.DeserializeObject<Report_InspectSector_OCR>(reportSec.ToString())
                            : reportSec["type"].ToString() == "ocv"
                                        ? JsonConvert.DeserializeObject<Report_InspectSector_OCV>(reportSec.ToString())
                                        : reportSec["type"].ToString() == "blemish"
                                                    ? JsonConvert.DeserializeObject<Report_InspectSector_Blemish>(reportSec.ToString())
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
