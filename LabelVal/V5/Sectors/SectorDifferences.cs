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
    public ObservableCollection<Value> Values { get; } = [];
    public ObservableCollection<Report_InspectSector_Common.Alarm> Alarms { get; } = [];
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
            if (compare.Values.FirstOrDefault((x) => x.Name == src.Name) is Value cmp)
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
                if (aS.name == aC.name)
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
                if (aS.name == aC.name)
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

            GradeValues.Add(new GradeValue("decode",
                new Report_InspectSector_Common.GradeValue()
                {
                    grade = new Report_InspectSector_Common.Grade() { letter = V5GetGradeLetter(results.grading.iso15415.decode.letter), value = results.grading.iso15415.decode.grade },
                    value = results.grading.iso15415.decode.value
                }));

            GradeValues.Add(new GradeValue("contrast",
                new Report_InspectSector_Common.GradeValue()
                {
                    grade = new Report_InspectSector_Common.Grade() { letter = V5GetGradeLetter(results.grading.iso15415.contrast.letter), value = results.grading.iso15415.contrast.grade },
                    value = results.grading.iso15415.contrast.value
                }));

            GradeValues.Add(new GradeValue("modulation",
                new Report_InspectSector_Common.GradeValue()
                {
                    grade = new Report_InspectSector_Common.Grade() { letter = V5GetGradeLetter(results.grading.iso15415.modulation.letter), value = results.grading.iso15415.modulation.grade },
                    value = results.grading.iso15415.modulation.value
                }));

            GradeValues.Add(new GradeValue("reflectanceMargin",
                new Report_InspectSector_Common.GradeValue()
                {
                    grade = new Report_InspectSector_Common.Grade() { letter = V5GetGradeLetter(results.grading.iso15415.reflectanceMargin.letter), value = results.grading.iso15415.reflectanceMargin.grade },
                    value = results.grading.iso15415.reflectanceMargin.value
                }));

            GradeValues.Add(new GradeValue("axialNonUniformity",
                new Report_InspectSector_Common.GradeValue()
                {
                    grade = new Report_InspectSector_Common.Grade() { letter = V5GetGradeLetter(results.grading.iso15415.axialNonUniformity.letter), value = results.grading.iso15415.axialNonUniformity.grade },
                    value = results.grading.iso15415.axialNonUniformity.value
                }));

            GradeValues.Add(new GradeValue("gridNonUniformity",
                new Report_InspectSector_Common.GradeValue()
                {
                    grade = new Report_InspectSector_Common.Grade() { letter = V5GetGradeLetter(results.grading.iso15415.gridNonUniformity.letter), value = results.grading.iso15415.gridNonUniformity.grade },
                    value = results.grading.iso15415.gridNonUniformity.value
                }));

            GradeValues.Add(new GradeValue("unusedECC",
                new Report_InspectSector_Common.GradeValue()
                {
                    grade = new Report_InspectSector_Common.Grade() { letter = V5GetGradeLetter(results.grading.iso15415.unusedECC.letter), value = results.grading.iso15415.unusedECC.grade },
                    value = results.grading.iso15415.unusedECC.value
                }));

            GradeValues.Add(new GradeValue("fixedPatternDamage",
                new Report_InspectSector_Common.GradeValue()
                {
                    grade = new Report_InspectSector_Common.Grade() { letter = V5GetGradeLetter(results.grading.iso15415.fixedPatternDamage.letter), value = results.grading.iso15415.fixedPatternDamage.grade },
                    value = results.grading.iso15415.fixedPatternDamage.value
                }));
        }
        else if (Type == "verify1D" && results.grading.iso15416 is { overall: not null })
        {
            IsNotEmpty = true;

            GradeValues.Add(new GradeValue("decode",
                new Report_InspectSector_Common.GradeValue()
                {
                    grade = new Report_InspectSector_Common.Grade() { letter = V5GetGradeLetter(results.grading.iso15416.decode.letter), value = results.grading.iso15416.decode.grade },
                    value = results.grading.iso15416.decode.value
                }));

            GradeValues.Add(new GradeValue("symbolContrast",
                new Report_InspectSector_Common.GradeValue()
                {
                    grade = new Report_InspectSector_Common.Grade() { letter = V5GetGradeLetter(results.grading.iso15416.symbolContrast.letter), value = results.grading.iso15416.symbolContrast.grade },
                    value = results.grading.iso15416.symbolContrast.value
                }));

            GradeValues.Add(new GradeValue("minimumEdgeContrast",
                new Report_InspectSector_Common.GradeValue()
                {
                    grade = new Report_InspectSector_Common.Grade() { letter = V5GetGradeLetter(results.grading.iso15416.minimumEdgeContrast.letter), value = results.grading.iso15416.minimumEdgeContrast.grade },
                    value = results.grading.iso15416.minimumEdgeContrast.value
                }));

            GradeValues.Add(new GradeValue("modulation",
                new Report_InspectSector_Common.GradeValue()
                {
                    grade = new Report_InspectSector_Common.Grade() { letter = V5GetGradeLetter(results.grading.iso15416.modulation.letter), value = results.grading.iso15416.modulation.grade },
                    value = results.grading.iso15416.modulation.value
                }));

            GradeValues.Add(new GradeValue("defects",
                new Report_InspectSector_Common.GradeValue()
                {
                    grade = new Report_InspectSector_Common.Grade() { letter = V5GetGradeLetter(results.grading.iso15416.defects.letter), value = results.grading.iso15416.defects.grade },
                    value = results.grading.iso15416.defects.value
                }));

            GradeValues.Add(new GradeValue("decodability",
                new Report_InspectSector_Common.GradeValue()
                {
                    grade = new Report_InspectSector_Common.Grade() { letter = V5GetGradeLetter(results.grading.iso15416.decodability.letter), value = results.grading.iso15416.decodability.grade },
                    value = results.grading.iso15416.decodability.value
                }));

            GradeValues.Add(new GradeValue("minimumReflectance",
                new Report_InspectSector_Common.GradeValue()
                {
                    grade = new Report_InspectSector_Common.Grade() { letter = V5GetGradeLetter(results.grading.iso15416.minimumReflectance.letter), value = results.grading.iso15416.minimumReflectance.grade },
                    value = results.grading.iso15416.minimumReflectance.value
                }));



            //GradeValues.Add(new GradeValue("edgeDetermination",
            //    new Report_InspectSector_Common.GradeValue()
            //    {
            //        grade = new Report_InspectSector_Common.Grade() { letter = V5GetGradeLetter(results.grading.iso15416.edgeDetermination.letter), value = results.grading.iso15416.edgeDetermination.grade },
            //        value = results.grading.iso15416.edgeDetermination.value
            //    }));

            //GradeValues.Add(new GradeValue("quietZone",
            //    new Report_InspectSector_Common.GradeValue()
            //    {
            //        grade = new Report_InspectSector_Common.Grade() { letter = V5GetGradeLetter(results.grading.iso15416.quietZone.letter), value = results.grading.iso15416.quietZone.grade },
            //        value = results.grading.iso15416.quietZone.value
            //    }));

            ValueResults.Add(new ValueResult("edgeDetermination", new Report_InspectSector_Common.ValueResult() { value = results.grading.iso15416.edgeDetermination.value, result = results.grading.iso15416.edgeDetermination.letter == 65 ? "PASS" : "FAIL" }));

            ValueResults.Add(new ValueResult("quietZone", new Report_InspectSector_Common.ValueResult() { value = results.grading.iso15416.quietZone.value, result = results.grading.iso15416.quietZone.letter == 65 ? "PASS" : "FAIL" }));


            //GradeValues.Add(new GradeValue("overall",
            //    new Report_InspectSector_Common.GradeValue()
            //    {
            //        grade = new Report_InspectSector_Common.Grade() { letter = V5GetGradeLetter(results.grading.iso15416.overall.letter), value = results.grading.iso15416.overall.grade },
            //        value = results.grading.iso15416.overall.value
            //    }));




        }

        if (Type == "verify2D")
        {
            if (results.Datamatrix != null)
            {
                Values.Add(new Value("rows", new Report_InspectSector_Common.Value() { value = results.Datamatrix.rows }));
                Values.Add(new Value("columns", new Report_InspectSector_Common.Value() { value = results.Datamatrix.columns }));


                Values.Add(new Value("uec", new Report_InspectSector_Common.Value() { value = results.Datamatrix.uec }));
                Values.Add(new Value("ecc", new Report_InspectSector_Common.Value() { value = results.Datamatrix.ecc }));

                Values.Add(new Value("mirror", new Report_InspectSector_Common.Value() { value = results.Datamatrix.mirror ? 1 : 0 }));
                Values.Add(new Value("readerConfig", new Report_InspectSector_Common.Value() { value = results.Datamatrix.readerConfig ? 1 : 0 }));
            }
            else if (results.QR != null)
            {
                Values.Add(new Value("rows", new Report_InspectSector_Common.Value() { value = results.QR.rows }));
                Values.Add(new Value("columns", new Report_InspectSector_Common.Value() { value = results.QR.columns }));


                Values.Add(new Value("uec", new Report_InspectSector_Common.Value() { value = results.QR.uec }));
                //Values.Add(new Value("ecl", new Report_InspectSector_Common.Value() { value = results.QR.ecl }));

                Values.Add(new Value("mirror", new Report_InspectSector_Common.Value() { value = results.QR.mirror ? 1 : 0 }));
                Values.Add(new Value("model", new Report_InspectSector_Common.Value() { value = results.QR.model }));
                Values.Add(new Value("locatorCount", new Report_InspectSector_Common.Value() { value = results.QR.locator.Count() }));
            }


        }
        else if (Type == "verify1D")
        {
            if (results.Code128 != null)
                Values.Add(new Value("barCount", new Report_InspectSector_Common.Value() { value = results.Code128.barCount }));
            else if (results.PDF417 != null)
            {
                Values.Add(new Value("rows", new Report_InspectSector_Common.Value() { value = results.PDF417.rows }));
                Values.Add(new Value("columns", new Report_InspectSector_Common.Value() { value = results.PDF417.columns }));

                Values.Add(new Value("ecc", new Report_InspectSector_Common.Value() { value = results.PDF417.ecc }));
            }
            else if (results.UPC != null)
            {
                Values.Add(new Value("barCount", new Report_InspectSector_Common.Value() { value = results.UPC.barCount }));
                Values.Add(new Value("supplemental", new Report_InspectSector_Common.Value() { value = results.UPC.supplemental }));

                //Values.Add(new Value("version", new Report_InspectSector_Common.Value() { value = results.UPC.version }));
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
