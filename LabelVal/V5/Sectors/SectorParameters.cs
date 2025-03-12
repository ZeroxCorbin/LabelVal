using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.ISO.ParameterTypes;
using CommunityToolkit.Mvvm.ComponentModel;
using LabelVal.Sectors.Classes;
using LabelVal.Sectors.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using V5_REST_Lib.Models;

namespace LabelVal.V5.Sectors;

public partial class SectorDetails : ObservableObject, ISectorParameters
{
    public ISector Sector { get; set; }

    [ObservableProperty] private string units;
    [ObservableProperty] private bool isNotOCVMatch = false;
    [ObservableProperty] private string oCVMatchText;
    [ObservableProperty] private bool isSectorMissing;
    [ObservableProperty] private string sectorMissingText;
    [ObservableProperty] private bool isNotEmpty = false;

    public ObservableCollection<IParameterValue> Parameters { get; } 
    public ObservableCollection<Alarm> Alarms { get; } = [];
    public ObservableCollection<Blemish> Blemishes { get; } = [];

    public SectorDifferences Compare(ISectorParameters compare) => SectorDifferences.Compare(this, compare);

    public SectorDetails() { }
    public SectorDetails( ISector sector, string username) => Process(sector, username);
    public void Process(ISector sector, string username)
    {
        if (sector is not V5.Sectors.Sector sec)
            return;

        Sector = sector;
        ResultsAlt.Decodedata results = sec.V5Sector;

        IsNotEmpty = false;

       // SymbolType = V5GetSymbolType(results);
        Units = "mil";

        OCVMatchText = null;
        Blemishes.Clear();

        //GradeValues.Clear();
        //ValueResults.Clear();
        //Values.Clear();
        //Alarms.Clear();
        //Gs1ValueResults.Clear();
        //Gs1Grades.Clear();

        //if (SymbolType == "verify2D" && results.grading.iso15415 != null)
        //{
        //    IsNotEmpty = true;

        //    GradeValues.Add(new GradeValue("contrast", results.grading.iso15415.contrast.value, new Grade("", results.grading.iso15415.contrast.grade, V5GetGradeLetter(results.grading.iso15415.contrast.letter))));

        //    GradeValues.Add(new GradeValue("modulation", results.grading.iso15415.modulation.value, new Grade("", results.grading.iso15415.modulation.grade, V5GetGradeLetter(results.grading.iso15415.modulation.letter))));

        //    GradeValues.Add(new GradeValue("reflectanceMargin", results.grading.iso15415.reflectanceMargin.value, new Grade("", results.grading.iso15415.reflectanceMargin.grade, V5GetGradeLetter(results.grading.iso15415.reflectanceMargin.letter))));

        //    GradeValues.Add(new GradeValue("axialNonUniformity", results.grading.iso15415.axialNonUniformity.value, new Grade("axialNonUniformity", results.grading.iso15415.axialNonUniformity.grade, V5GetGradeLetter(results.grading.iso15415.axialNonUniformity.letter))));

        //    GradeValues.Add(new GradeValue("gridNonUniformity", results.grading.iso15415.gridNonUniformity.value, new Grade("", results.grading.iso15415.gridNonUniformity.grade, V5GetGradeLetter(results.grading.iso15415.gridNonUniformity.letter))));

        //    GradeValues.Add(new GradeValue("unusedECC", results.grading.iso15415.unusedECC.value, new Grade("", results.grading.iso15415.unusedECC.grade, V5GetGradeLetter(results.grading.iso15415.unusedECC.letter))));

        //    GradeValues.Add(new GradeValue("fixedPatternDamage", results.grading.iso15415.fixedPatternDamage.value, new Grade("", results.grading.iso15415.fixedPatternDamage.grade, V5GetGradeLetter(results.grading.iso15415.fixedPatternDamage.letter))));

        //}
        //else if (SymbolType == "verify1D" && results.grading.iso15416 is { overall: not null })
        //{
        //    IsNotEmpty = true;

        //    GradeValues.Add(new GradeValue("decode", results.grading.iso15416.decode.value, new Grade("", results.grading.iso15416.decode.grade, V5GetGradeLetter(results.grading.iso15416.decode.letter))));

        //    GradeValues.Add(new GradeValue("symbolContrast", results.grading.iso15416.symbolContrast.value, new Grade("", results.grading.iso15416.symbolContrast.grade, V5GetGradeLetter(results.grading.iso15416.symbolContrast.letter))));

        //    GradeValues.Add(new GradeValue("minimumEdgeContrast", results.grading.iso15416.minimumEdgeContrast.value, new Grade("", results.grading.iso15416.minimumEdgeContrast.grade, V5GetGradeLetter(results.grading.iso15416.minimumEdgeContrast.letter))));

        //    GradeValues.Add(new GradeValue("modulation", results.grading.iso15416.modulation.value, new Grade("", results.grading.iso15416.modulation.grade, V5GetGradeLetter(results.grading.iso15416.modulation.letter))));

        //    GradeValues.Add(new GradeValue("defects", results.grading.iso15416.defects.value, new Grade("", results.grading.iso15416.defects.grade, V5GetGradeLetter(results.grading.iso15416.defects.letter))));

        //    GradeValues.Add(new GradeValue("decodability", results.grading.iso15416.decodability.value, new Grade("", results.grading.iso15416.decodability.grade, V5GetGradeLetter(results.grading.iso15416.decodability.letter))));

        //    GradeValues.Add(new GradeValue("minimumReflectance", results.grading.iso15416.minimumReflectance.value, new Grade("", results.grading.iso15416.minimumReflectance.grade, V5GetGradeLetter(results.grading.iso15416.minimumReflectance.letter))));

        //    ValueResults.Add(new ValueResult("edgeDetermination", results.grading.iso15416.edgeDetermination.value, results.grading.iso15416.edgeDetermination.letter == 65 ? "PASS" : "FAIL"));

        //    ValueResults.Add(new ValueResult("quietZone", results.grading.iso15416.quietZone.value, results.grading.iso15416.quietZone.letter == 65 ? "PASS" : "FAIL"));
        //}
        //if (SymbolType == "verify2D" && results.grading.standard == "iso29158")
        //{
        //    IsNotEmpty = true;

        //    var spl = results.grading.gradeReport.Split(' ');

        //    GradeValues.Add(new GradeValue("decode", -1, new Grade("", double.Parse(spl[1]), GetLetter(double.Parse(spl[1])))));
            
        //    GradeValues.Add(new GradeValue("axialNonUniformity", -1, new Grade("", double.Parse(spl[2]), GetLetter(double.Parse(spl[2])))));

        //    GradeValues.Add(new GradeValue("cellContrast", -1, new Grade("", double.Parse(spl[3]), GetLetter(double.Parse(spl[3])))));
           
        //    GradeValues.Add(new GradeValue("cellModulation", -1, new Grade("", double.Parse(spl[4]), GetLetter(double.Parse(spl[4])))));

        //    GradeValues.Add(new GradeValue("fixedPatternDamage", -1, new Grade("", double.Parse(spl[5]), GetLetter(double.Parse(spl[5])))));

        //    GradeValues.Add(new GradeValue("gridNonUniformity", -1, new Grade("", double.Parse(spl[6]), GetLetter(double.Parse(spl[6])))));

        //    //GradeValues.Add(new GradeValue("minimumReflectance", -1, new Grade("", double.Parse(spl[7]), GetLetter(double.Parse(spl[7])))));

        //    GradeValues.Add(new GradeValue("unusedECC", -1, new Grade("", double.Parse(spl[8]), GetLetter(double.Parse(spl[8])))));

        //}

        //if (SymbolType == "verify2D")
        //{
        //    if (results.Datamatrix != null)
        //    {
        //        Values.Add(new Value_("rows", results.Datamatrix.rows));
        //        Values.Add(new Value_("columns", results.Datamatrix.columns));
        //        Values.Add(new Value_("uec", results.Datamatrix.uec));
        //        Values.Add(new Value_("ecc", results.Datamatrix.ecc));
        //        Values.Add(new Value_("mirror", results.Datamatrix.mirror ? 1 : 0));
        //        Values.Add(new Value_("readerConfig", results.Datamatrix.readerConfig ? 1 : 0));
        //    }
        //    else if (results.QR != null)
        //    {
        //        Values.Add(new Value_("rows", results.QR.rows));
        //        Values.Add(new Value_("columns", results.QR.columns));
        //        Values.Add(new Value_("uec", results.QR.uec));
        //        Values.Add(new Value_("mirror", results.QR.mirror ? 1 : 0));
        //        Values.Add(new Value_("model", results.QR.model));
        //        Values.Add(new Value_("locatorCount", results.QR.locator.Count()));
        //    }
        //}
        //else if (SymbolType == "verify1D")
        //{
        //    if (results.Code128 != null)
        //        Values.Add(new Value_("barCount", results.Code128.barCount));
        //    else if (results.PDF417 != null)
        //    {
        //        Values.Add(new Value_("rows", results.PDF417.rows));
        //        Values.Add(new Value_("columns", results.PDF417.columns));
        //        Values.Add(new Value_("ecc", results.PDF417.ecc));
        //    }
        //    else if (results.UPC != null)
        //    {
        //        Values.Add(new Value_("barCount", results.UPC.barCount));
        //        Values.Add(new Value_("supplemental", results.UPC.supplemental));
        //    }
        //}
    }

    private static string V5GetGradeLetter(int grade) => grade switch
    {
        65 => "A",
        66 => "B",
        67 => "C",
        68 => "D",
        70 => "F",
        _ => throw new System.NotImplementedException(),
    };


    private static string V5GetSymbolType(ResultsAlt.Decodedata results)
    {
        if (results.Code128 != null)
            return "verify1D";
        else if (results.Datamatrix != null)
            return "verify2D";
        else if (results.QR != null)
            return "verify2D";
        else return results.PDF417 != null ? "verify1D" : results.UPC != null ? "verify1D" : "Unknown";
    }

    private static string GetLetter(double value) =>
value == 4.0f
? "A"
: value is <= 3.9f and >= 3.0f
? "B"
: value is <= 2.9f and >= 2.0f
? "C"
: value is <= 1.9f and >= 1.0f
? "D"
: value is <= 0.9f and >= 0.0f
? "F"
: "F";
}
