using CommunityToolkit.Mvvm.ComponentModel;
using LabelVal.Sectors.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using V275_REST_lib.Models;

namespace LabelVal.V5.Sectors;

public partial class SectorDifferences : ObservableObject, ISectorDifferences
{
    [ObservableProperty] private string userName;
    [ObservableProperty] private string type;
    [ObservableProperty] private string units;
    [ObservableProperty] private bool isNotOCVMatch = false;
    [ObservableProperty] private string oCVMatchText;
    [ObservableProperty] private bool isSectorMissing;
    [ObservableProperty] private string sectorMissingText;
    [ObservableProperty] private bool isNotEmpty = false;

    public ObservableCollection<GradeValue> GradeValues { get; } = [];
    public ObservableCollection<ValueResult> ValueResults { get; } = [];
    public ObservableCollection<ValueResult> Gs1ValueResults { get; } = [];
    public ObservableCollection<Grade> Gs1Grades { get; } = [];
    public ObservableCollection<Value_> Values { get; } = [];
    public ObservableCollection<Alarm> Alarms { get; } = [];
    public ObservableCollection<Blemish> Blemishes { get; } = [];

    public ISectorDifferences Compare(ISectorDifferences compare)
    {
        var results = new SectorDifferences
        {
            UserName = UserName,
            Type = Type
        };

        if (Type is "ocr" or "ocr")
        {
            if (!OCVMatchText.Equals(compare.OCVMatchText))
            {
                if (compare.OCVMatchText != null)
                {
                    results.IsNotEmpty = true;
                    results.IsNotOCVMatch = true;
                    results.OCVMatchText = $"{OCVMatchText} / {compare.OCVMatchText}";
                }
            }
        }

        foreach (var src in GradeValues)
            if (compare.GradeValues.FirstOrDefault((x) => x.Name == src.Name) is GradeValue cmp)
            {
                if (cmp == null) continue;

                if (!ISectorDifferences.CompareGradeValue(src, cmp))
                {
                    results.GradeValues.Add(cmp);
                    results.IsNotEmpty = true;
                }
            }
            else
            {
                results.GradeValues.Add(src);
                results.IsNotEmpty = true;
            }

        foreach (var src in ValueResults)
            if (compare.ValueResults.FirstOrDefault((x) => x.Name == src.Name) is ValueResult cmp)
            {
                if (cmp == null) continue;

                if (!ISectorDifferences.CompareValueResult(src, cmp))
                {
                    results.ValueResults.Add(cmp);
                    results.IsNotEmpty = true;
                }
            }
            else
            {
                results.ValueResults.Add(src);
                results.IsNotEmpty = true;
            }

        foreach (var src in Values)
            if (compare.Values.FirstOrDefault((x) => x.Name == src.Name) is Value_ cmp)
            {
                if (cmp == null) continue;

                if (!ISectorDifferences.CompareValue(src, cmp))
                {
                    results.Values.Add(cmp);
                    results.IsNotEmpty = true;
                }
            }
            else
            {
                results.Values.Add(src);
                results.IsNotEmpty = true;
            }

        foreach (var src in Gs1ValueResults)
            if (compare.Gs1ValueResults.FirstOrDefault((x) => x.Name == src.Name) is ValueResult cmp)
            {
                if (cmp == null) continue;

                if (!ISectorDifferences.CompareValueResult(src, cmp))
                {
                    results.Gs1ValueResults.Add(cmp);
                    results.IsNotEmpty = true;
                }
            }
            else
            {
                results.Gs1ValueResults.Add(src);
                results.IsNotEmpty = true;
            }

        foreach (var src in Gs1Grades)
            if (compare.Gs1Grades.FirstOrDefault((x) => x.Name == src.Name) is Grade cmp)
            {
                if (cmp == null) continue;

                if (!ISectorDifferences.CompareGrade(src, cmp))
                {
                    results.Gs1Grades.Add(cmp);
                    results.IsNotEmpty = true;
                }
            }
            else
            {
                results.Gs1Grades.Add(src);
                results.IsNotEmpty = true;
            }

        foreach (var aS in Alarms)
        {
            var found = false;
            foreach (var aC in compare.Alarms)
            {
                //if (Type != "blemish")
                //{
                if (aS.Name == aC.Name)
                {
                    found = true;
                    if (!ISectorDifferences.CompareAlarm(aS, aC))
                    {
                        results.Alarms.Add(aC);
                        results.IsNotEmpty = true;
                    }
                }
                //}
            }

            if (!found)
            {
                results.Alarms.Add(aS);
                results.IsNotEmpty = true;
            }
        }

        foreach (var aS in compare.Alarms)
        {
            var found = false;
            foreach (var aC in Alarms)
            {
                //if (Type != "blemish")
                //{
                if (aS.Name == aC.Name)
                {
                    found = true;
                    if (!ISectorDifferences.CompareAlarm(aS, aC))
                    {
                        results.Alarms.Add(aC);
                        results.IsNotEmpty = true;
                    }
                }
                //}
            }

            if (!found)
            {
                results.Alarms.Add(aS);
                results.IsNotEmpty = true;
            }
        }

        return results;
    }

    public SectorDifferences() { }
    public SectorDifferences(V5_REST_Lib.Models.Results_QualifiedResult results, string userName) => Process(results, userName);
    public void Process(V5_REST_Lib.Models.Results_QualifiedResult results, string userName)
    {
        UserName = userName;
        IsNotEmpty = false;

        Type = V5GetSymbolType(results);
        Units = "mil";

        OCVMatchText = null;
        Blemishes.Clear();

        GradeValues.Clear();
        ValueResults.Clear();
        Values.Clear();
        Alarms.Clear();
        Gs1ValueResults.Clear();
        Gs1Grades.Clear();

        if (Type == "verify2D" && results.grading.iso15415 != null)
        {
            IsNotEmpty = true;

            GradeValues.Add(new GradeValue("contrast", results.grading.iso15415.contrast.value, new Grade("contrast", results.grading.iso15415.contrast.grade, V5GetGradeLetter(results.grading.iso15415.contrast.letter))));

            GradeValues.Add(new GradeValue("modulation", results.grading.iso15415.modulation.value, new Grade("modulation", results.grading.iso15415.modulation.grade, V5GetGradeLetter(results.grading.iso15415.modulation.letter))));

            GradeValues.Add(new GradeValue("reflectanceMargin", results.grading.iso15415.reflectanceMargin.value, new Grade("reflectanceMargin", results.grading.iso15415.reflectanceMargin.grade, V5GetGradeLetter(results.grading.iso15415.reflectanceMargin.letter))));

            GradeValues.Add(new GradeValue("axialNonUniformity", results.grading.iso15415.axialNonUniformity.value, new Grade("axialNonUniformity", results.grading.iso15415.axialNonUniformity.grade, V5GetGradeLetter(results.grading.iso15415.axialNonUniformity.letter))));

            GradeValues.Add(new GradeValue("gridNonUniformity", results.grading.iso15415.gridNonUniformity.value, new Grade("gridNonUniformity", results.grading.iso15415.gridNonUniformity.grade, V5GetGradeLetter(results.grading.iso15415.gridNonUniformity.letter))));

            GradeValues.Add(new GradeValue("unusedECC", results.grading.iso15415.unusedECC.value, new Grade("unusedECC", results.grading.iso15415.unusedECC.grade, V5GetGradeLetter(results.grading.iso15415.unusedECC.letter))));

            GradeValues.Add(new GradeValue("fixedPatternDamage", results.grading.iso15415.fixedPatternDamage.value, new Grade("fixedPatternDamage", results.grading.iso15415.fixedPatternDamage.grade, V5GetGradeLetter(results.grading.iso15415.fixedPatternDamage.letter))));

        }
        else if (Type == "verify1D" && results.grading.iso15416 is { overall: not null })
        {
            IsNotEmpty = true;

            GradeValues.Add(new GradeValue("decode", results.grading.iso15416.decode.value, new Grade("decode", results.grading.iso15416.decode.grade, V5GetGradeLetter(results.grading.iso15416.decode.letter))));

            GradeValues.Add(new GradeValue("symbolContrast", results.grading.iso15416.symbolContrast.value, new Grade("symbolContrast", results.grading.iso15416.symbolContrast.grade, V5GetGradeLetter(results.grading.iso15416.symbolContrast.letter))));

            GradeValues.Add(new GradeValue("minimumEdgeContrast", results.grading.iso15416.minimumEdgeContrast.value, new Grade("minimumEdgeContrast", results.grading.iso15416.minimumEdgeContrast.grade, V5GetGradeLetter(results.grading.iso15416.minimumEdgeContrast.letter))));

            GradeValues.Add(new GradeValue("modulation", results.grading.iso15416.modulation.value, new Grade("modulation", results.grading.iso15416.modulation.grade, V5GetGradeLetter(results.grading.iso15416.modulation.letter))));

            GradeValues.Add(new GradeValue("defects", results.grading.iso15416.defects.value, new Grade("defects", results.grading.iso15416.defects.grade, V5GetGradeLetter(results.grading.iso15416.defects.letter))));

            GradeValues.Add(new GradeValue("decodability", results.grading.iso15416.decodability.value, new Grade("decodability", results.grading.iso15416.decodability.grade, V5GetGradeLetter(results.grading.iso15416.decodability.letter))));

            GradeValues.Add(new GradeValue("minimumReflectance", results.grading.iso15416.minimumReflectance.value, new Grade("minimumReflectance", results.grading.iso15416.minimumReflectance.grade, V5GetGradeLetter(results.grading.iso15416.minimumReflectance.letter))));


            ValueResults.Add(new ValueResult("edgeDetermination", results.grading.iso15416.edgeDetermination.value, results.grading.iso15416.edgeDetermination.letter == 65 ? "PASS" : "FAIL"));

            ValueResults.Add(new ValueResult("quietZone", results.grading.iso15416.quietZone.value, results.grading.iso15416.quietZone.letter == 65 ? "PASS" : "FAIL"));
        }

        if (Type == "verify2D")
        {
            if (results.Datamatrix != null)
            {
                Values.Add(new Value_("rows", results.Datamatrix.rows));
                Values.Add(new Value_("columns", results.Datamatrix.columns));
                Values.Add(new Value_("uec", results.Datamatrix.uec));
                Values.Add(new Value_("ecc", results.Datamatrix.ecc));
                Values.Add(new Value_("mirror", results.Datamatrix.mirror ? 1 : 0));
                Values.Add(new Value_("readerConfig", results.Datamatrix.readerConfig ? 1 : 0));
            }
            else if (results.QR != null)
            {
                Values.Add(new Value_("rows", results.QR.rows));
                Values.Add(new Value_("columns", results.QR.columns));
                Values.Add(new Value_("uec", results.QR.uec));
                Values.Add(new Value_("mirror", results.QR.mirror ? 1 : 0));
                Values.Add(new Value_("model", results.QR.model));
                Values.Add(new Value_("locatorCount", results.QR.locator.Count()));
            }
        }
        else if (Type == "verify1D")
        {
            if (results.Code128 != null)
                Values.Add(new Value_("barCount", results.Code128.barCount));
            else if (results.PDF417 != null)
            {
                Values.Add(new Value_("rows", results.PDF417.rows));
                Values.Add(new Value_("columns", results.PDF417.columns));
                Values.Add(new Value_("ecc", results.PDF417.ecc));
            }
            else if (results.UPC != null)
            {
                Values.Add(new Value_("barCount", results.UPC.barCount));
                Values.Add(new Value_("supplemental", results.UPC.supplemental));
            }
        }

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
    private static string V5GetSymbolType(V5_REST_Lib.Models.Results_QualifiedResult results)
    {
        if (results.Code128 != null)
            return "verify1D";
        else if (results.Datamatrix != null)
            return "verify2D";
        else if (results.QR != null)
            return "verify2D";
        else if (results.PDF417 != null)
            return "verify1D";
        else return results.UPC != null ? "verify1D" : "Unknown";
    }
}
