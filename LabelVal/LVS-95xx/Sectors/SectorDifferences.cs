using CommunityToolkit.Mvvm.ComponentModel;
using LabelVal.Sectors.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using V275_REST_lib.Models;

namespace LabelVal.LVS_95xx.Sectors;

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
    public SectorDifferences(List<string> splitPacket, string userName, bool isPDF417) => Process(splitPacket, userName, isPDF417);
    public void Process(List<string> splitPacket, string userName, bool isPDF417)
    {
        UserName = userName;
        IsNotEmpty = false;

        float cellSizeX = 0, cellSizeY = 0;
        var alarms = new List<Report_InspectSector_Common.Alarm>();

        if (splitPacket.Find((e) => e.StartsWith("Cell size")) != null)
        {
            //Verify 2D

            foreach (var data in splitPacket)
            {
                if (!data.Contains(','))
                    continue;

                IsNotEmpty = true;

                var spl1 = new string[2];
                spl1[0] = data.Substring(0, data.IndexOf(','));
                spl1[1] = data.Substring(data.IndexOf(',') + 1);

                if (spl1[0].StartsWith("Warning"))
                {
                    alarms.Add(new Report_InspectSector_Common.Alarm() { name = spl1[1], category = 1 });
                    continue;
                }

                if (spl1[0].Equals("Decode"))
                {
                    GradeValues.Add(new GradeValue("decode",
                        new Report_InspectSector_Common.GradeValue()
                        {
                            grade = spl1[1].StartsWith("PASS") ? new Report_InspectSector_Common.Grade() { letter = "A", value = 4.0f } : new Report_InspectSector_Common.Grade() { letter = "F", value = 0.0f },
                            value = -1
                        }));
                    continue;
                }

                if (spl1[0].Equals("Contrast"))
                {
                    GradeValues.Add(new GradeValue("contrast", GetGradeValue(spl1[1])));
                    continue;
                }

                if (spl1[0].StartsWith("Modulation"))
                {
                    GradeValues.Add(new GradeValue("modulation", new Report_InspectSector_Common.GradeValue() { grade = GetGrade(spl1[1]), value = -1 }));
                    continue;
                }

                if (spl1[0].StartsWith("Reflectance"))
                {
                    GradeValues.Add(new GradeValue("reflectance", new Report_InspectSector_Common.GradeValue() { grade = GetGrade(spl1[1]), value = -1 }));
                    continue;
                }

                if (spl1[0].StartsWith("Axial "))
                {
                    GradeValues.Add(new GradeValue("axialNonUniformity", GetGradeValue(spl1[1])));
                    continue;
                }

                if (spl1[0].StartsWith("Grid "))
                {
                    GradeValues.Add(new GradeValue("gridNonUniformity", GetGradeValue(spl1[1])));
                    continue;
                }

                if (spl1[0].StartsWith("Unused "))
                {
                    GradeValues.Add(new GradeValue("unusedECC", GetGradeValue(spl1[1])));
                    continue;
                }

                if (spl1[0].StartsWith("Fixed"))
                {
                    GradeValues.Add(new GradeValue("fixedPatternDamage", GetGradeValue(spl1[1])));
                    continue;
                }


                if (spl1[0].StartsWith("Rmin"))
                {
                    Values.Add(new Value("minimumReflectance", new Report_InspectSector_Common.Value() { value = ParseInt(spl1[1]) }));
                    continue;
                }
                if (spl1[0].StartsWith("Rmax"))
                {
                    Values.Add(new Value("maximumReflectance", new Report_InspectSector_Common.Value() { value = ParseInt(spl1[1]) }));
                    continue;
                }


                if (spl1[0].StartsWith("X print"))
                {
                    Gs1Grades.Add(new Grade("growthX", GetGrade(spl1[1])));
                    continue;
                }
                if (spl1[0].StartsWith("Y print"))
                {
                    Gs1Grades.Add(new Grade("growthY", GetGrade(spl1[1])));
                    continue;
                }

                if (spl1[0].StartsWith("Cell height"))
                {
                    var item = alarms.Find((e) => e.name.Contains("minimum Xdim"));

                    cellSizeX = ParseFloat(spl1[1]);

                    ValueResults.Add(new ValueResult("cellHeight", new Report_InspectSector_Common.ValueResult() { value = cellSizeX, result = item == null ? "PASS" : "FAIL" }));
                    continue;
                }
                if (spl1[0].StartsWith("Cell width"))
                {
                    var item = alarms.Find((e) => e.name.Contains("minimum Xdim"));

                    cellSizeY = ParseFloat(spl1[1]);

                    ValueResults.Add(new ValueResult("cellWidth", new Report_InspectSector_Common.ValueResult() { value = cellSizeY, result = item == null ? "PASS" : "FAIL" }));

                    continue;
                }
                if (spl1[0].Equals("Size"))
                {
                    var spl2 = spl1[1].Split('x');

                    ValueResults.Add(new ValueResult("symbolWidth", new Report_InspectSector_Common.ValueResult() { value = cellSizeX * ParseInt(spl2[0]), result = "PASS" }));
                    ValueResults.Add(new ValueResult("symbolHeight", new Report_InspectSector_Common.ValueResult() { value = cellSizeY * ParseInt(spl2[1]), result = "PASS" }));
                    continue;
                }

                if (spl1[0].StartsWith("L1 ("))
                {
                    Gs1Grades.Add(new Grade("L1", GetGrade(spl1[1])));
                    continue;
                }
                if (spl1[0].StartsWith("L2"))
                {
                    Gs1Grades.Add(new Grade("L2", GetGrade(spl1[1])));
                    //sect.data.gs1SymbolQuality.L2 = GetGrade(spl1[1]);
                    continue;
                }
                if (spl1[0].StartsWith("QZL1"))
                {
                    Gs1Grades.Add(new Grade("QZL1", GetGrade(spl1[1])));
                    continue;
                }
                if (spl1[0].StartsWith("QZL2"))
                {

                    Gs1Grades.Add(new Grade("QZL2", GetGrade(spl1[1])));
                    continue;
                }
                if (spl1[0].StartsWith("OCTASA"))
                {
                    Gs1Grades.Add(new Grade("OCTASA", GetGrade(spl1[1])));
                    continue;
                }
            }

            foreach (var item in alarms)
                Alarms.Add(item);
        }
        else if (isPDF417)
        {
            //PDF417
            GradeValues.Add(new GradeValue("symbolContrast", GetGradeValue(GetValues("Contrast", splitPacket)[0])));
            foreach (var data in splitPacket)
            {
                if (!data.Contains(','))
                    continue;

                var spl1 = new string[2];
                spl1[0] = data.Substring(0, data.IndexOf(','));
                spl1[1] = data.Substring(data.IndexOf(',') + 1);

                if (spl1[0].StartsWith("Xdim"))
                {
                    var xdim = ParseFloat(spl1[1]);

                    ValueResults.Add(new ValueResult("symbolXDim", new Report_InspectSector_Common.ValueResult() { value = xdim, result = "PASS" }));
                    continue;
                }

                if (spl1[0].StartsWith("Rmin"))
                {
                    var val = (int)Math.Ceiling(ParseFloat(spl1[1]));

                    Values.Add(new Value("minimumReflectance", new Report_InspectSector_Common.Value() { value = val }));
                    continue;
                }

                if (spl1[0].StartsWith("Codeword y"))
                {
                    var spl2 = spl1[1].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    if (spl2.Count() != 2) continue;

                    GradeValues.Add(new GradeValue("CodewordY", new Report_InspectSector_Common.GradeValue() { grade = GetGrade(spl2[0]), value = ParseInt(spl2[1]) }));
                    continue;
                }

                if (spl1[0].StartsWith("Codeword P"))
                {
                    GradeValues.Add(new GradeValue("CodewordP", new Report_InspectSector_Common.GradeValue() { grade = GetGrade(spl1[1]), value = -1 }));
                    continue;
                }
            }

        }
        else
        {


            GradeValues.Add(new GradeValue("decode",
                new Report_InspectSector_Common.GradeValue()
                {
                    grade = GetValues("Decode,", splitPacket)[0].StartsWith("PASS") ? new Report_InspectSector_Common.Grade() { letter = "A", value = 4.0f } : new Report_InspectSector_Common.Grade() { letter = "F", value = 0.0f },
                    value = -1
                }));

            GradeValues.Add(new GradeValue("symbolContrast", GetGradeValue(GetValues("Contrast", splitPacket)[0])));
            // GradeValues.Add(new GradeValue("edgeContrast", GetGradeValue(GetValues("Contrast", splitPacket)[0])));
            GradeValues.Add(new GradeValue("modulation", GetGradeValue(GetValues("Modulation", splitPacket)[0])));
            GradeValues.Add(new GradeValue("defects", GetGradeValue(GetValues("Defects", splitPacket)[0])));
            GradeValues.Add(new GradeValue("decodability", GetGradeValue(GetValues("Decodability", splitPacket)[0])));
            GradeValues.Add(new GradeValue("MinRef",
                new Report_InspectSector_Common.GradeValue()
                {
                    grade = GetValues("Min Ref", splitPacket)[0].StartsWith("PASS") ? new Report_InspectSector_Common.Grade() { letter = "A", value = 4.0f } : new Report_InspectSector_Common.Grade() { letter = "F", value = 0.0f },
                    value = -1
                }));

            Values.Add(new Value("maximumReflectance", new Report_InspectSector_Common.Value() { value = ParseInt(GetValues("Rmax", splitPacket)[0]) }));

            ValueResults.Add(new ValueResult("edgeDetermination", new Report_InspectSector_Common.ValueResult() { value = 100, result = GetValues("Edge", splitPacket)[0] }));

            foreach (var data in splitPacket)
            {
                if (!data.Contains(','))
                    continue;

                var spl1 = new string[2];
                spl1[0] = data.Substring(0, data.IndexOf(','));
                spl1[1] = data.Substring(data.IndexOf(',') + 1);

                if (spl1[0].StartsWith("Warning"))
                {
                    alarms.Add(new Report_InspectSector_Common.Alarm() { name = spl1[1], category = 1 });
                    continue;
                }

                if (spl1[0].StartsWith("Rmin"))
                {
                    var val = (int)Math.Ceiling(ParseFloat(spl1[1]));

                    Values.Add(new Value("minimumReflectance", new Report_InspectSector_Common.Value() { value = val }));
                    continue;
                }


                if (spl1[0].StartsWith("Unused "))
                {
                    GradeValues.Add(new GradeValue("unusedErrorCorrection", GetGradeValue(spl1[1])));
                    continue;
                }

                if (spl1[0].StartsWith("Xdim"))
                {
                    var xdim = ParseFloat(spl1[1]);

                    var item = alarms.Find((e) => e.name.Contains("minimum Xdim"));

                    ValueResults.Add(new ValueResult("symbolXDim", new Report_InspectSector_Common.ValueResult() { value = xdim, result = item == null ? "PASS" : "FAIL" }));

                    continue;
                }

                if (spl1[0].StartsWith("Bar height"))
                {
                    var val = ParseFloat(spl1[1]) * 1000;

                    var item = alarms.Find((e) => e.name.Contains("minimum height"));

                    ValueResults.Add(new ValueResult("barHeight", new Report_InspectSector_Common.ValueResult() { value = val, result = item == null ? "PASS" : "FAIL" }));
                    continue;
                }

                if (spl1[0].StartsWith("Quiet"))
                {
                    if (spl1[1].Contains("ERR"))
                    {
                        var spl2 = spl1[1].Split(' ');

                        if (spl2.Count() != 2) continue;


                        ValueResults.Add(new ValueResult("quietZoneLeft", new Report_InspectSector_Common.ValueResult() { value = ParseInt(spl2[0]), result = spl2[1] }));
                        ValueResults.Add(new ValueResult("quietZoneRight", new Report_InspectSector_Common.ValueResult() { value = ParseInt(spl2[0]), result = spl2[1] }));
                    }
                    else
                    {
                        ValueResults.Add(new ValueResult("quietZoneLeft", new Report_InspectSector_Common.ValueResult() { value = 100, result = spl1[1] }));
                        ValueResults.Add(new ValueResult("quietZoneRight", new Report_InspectSector_Common.ValueResult() { value = 100, result = spl1[1] }));
                    }

                    continue;
                }


            }

            foreach (var item in alarms)
                Alarms.Add(item);

        }
    }
    private string[] GetValues(string name, List<string> splitPacket)
    {
        var warn = splitPacket.FindAll((e) => e.StartsWith(name));

        var ret = new List<string>();
        foreach (var line in warn)
        {
            //string[] spl1 = new string[2];
            //spl1[0] = line.Substring(0, line.IndexOf(','));
            ret.Add(line.Substring(line.IndexOf(',') + 1));
        }
        return ret.ToArray();
    }
    private float ParseFloat(string value)
    {
        var digits = new string(value.Trim().TakeWhile(c =>
                                ("0123456789.").Contains(c)
                                ).ToArray());

        if (float.TryParse(digits, out var val))
            return val;
        else
            return 0;

    }

    private static int ParseInt(string value)
    {
        var digits = new string(value.Trim().TakeWhile(c =>
                                ("0123456789").Contains(c)
                                ).ToArray());

        return int.TryParse(digits, out var val) ? val : 0;
    }

    private Report_InspectSector_Common.GradeValue GetGradeValue(string data)
    {
        var spl2 = data.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        if (spl2.Count() != 2)
            return null;

        var tmp = ParseFloat(spl2[0]);

        return new Report_InspectSector_Common.GradeValue()
        {
            grade = new Report_InspectSector_Common.Grade()
            {
                value = tmp,
                letter = GetLetter(tmp)
            },
            value = ParseInt(spl2[1])
        };

    }

    private Report_InspectSector_Common.Grade GetGrade(string data)
    {
        var tmp = ParseFloat(data);

        return new Report_InspectSector_Common.Grade()
        {
            value = tmp,
            letter = GetLetter(tmp)
        };
    }

    private static string GetLetter(float value)
    {
        return value switch
        {
            4.0f => "A",
            <= 3.9f and >= 3.0f => "B",
            <= 2.9f and >= 2.0f => "C",
            <= 1.9f and >= 1.0f => "D",
            <= 0.9f and >= 0.0f => "F",
            _ => "F"
        };
    }

}
