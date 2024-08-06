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
    [ObservableProperty] private string name;
    [ObservableProperty] private string userName;

    [ObservableProperty] private string symbolType;

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
            Name = Name,
            UserName = UserName,
            SymbolType = SymbolType
        };

        if (SymbolType is "ocr" or "ocr")
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
    public SectorDifferences(List<string> splitPacket, string userName, bool isPDF417) => Process(splitPacket, userName, isPDF417);
    public void Process(List<string> splitPacket, string userName, bool isPDF417)
    {
        UserName = userName;
        IsNotEmpty = false;

        float cellSizeX = 0, cellSizeY = 0;
        var alarms = new List<Alarm>();

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
                    alarms.Add(new Alarm() { Name = spl1[1], Category = 1 });
                    continue;
                }

                if (spl1[0].Equals("Decode"))
                {
                    GradeValues.Add(new GradeValue("decode", -1, GetValues("Decode", splitPacket)[0].StartsWith("PASS") ? new Grade("Decode", 4.0f, "A") : new Grade("Decode", 0.0f, "F")));
                    continue;
                }

                if (spl1[0].Equals("Contrast"))
                {
                    GradeValues.Add(GetGradeValue("contrast", spl1[1]));
                    continue;
                }

                if (spl1[0].StartsWith("Modulation"))
                {
                    GradeValues.Add(new GradeValue("modulation", -1, GetGrade("modulation", spl1[1])));
                    continue;
                }

                if (spl1[0].StartsWith("Reflectance"))
                {
                    GradeValues.Add(new GradeValue("reflectance", -1, GetGrade("reflectance", spl1[1])));
                    continue;
                }

                if (spl1[0].StartsWith("Axial "))
                {
                    GradeValues.Add(GetGradeValue("axialNonUniformity",spl1[1]));
                    continue;
                }

                if (spl1[0].StartsWith("Grid "))
                {
                    GradeValues.Add(GetGradeValue("gridNonUniformity", spl1[1]));
                    continue;
                }

                if (spl1[0].StartsWith("Unused "))
                {
                    GradeValues.Add(GetGradeValue("unusedECC", spl1[1]));
                    continue;
                }

                if (spl1[0].StartsWith("Fixed"))
                {
                    GradeValues.Add(GetGradeValue("fixedPatternDamage", spl1[1]));
                    continue;
                }


                if (spl1[0].StartsWith("Rmin"))
                {
                    Values.Add(new Value_("minimumReflectance",ParseInt(spl1[1])));
                    continue;
                }
                if (spl1[0].StartsWith("Rmax"))
                {
                    Values.Add(new Value_("maximumReflectance", ParseInt(spl1[1])));
                    continue;
                }


                if (spl1[0].StartsWith("X print"))
                {
                    Gs1Grades.Add(GetGrade("growthX", spl1[1]));
                    continue;
                }
                if (spl1[0].StartsWith("Y print"))
                {
                    Gs1Grades.Add(GetGrade("growthY", spl1[1]));
                    continue;
                }

                if (spl1[0].StartsWith("Cell height"))
                {
                    var item = alarms.Find((e) => e.Name.Contains("minimum Xdim"));

                    cellSizeX = ParseFloat(spl1[1]);

                    ValueResults.Add(new ValueResult("cellHeight", cellSizeX, item == null ? "PASS" : "FAIL"));
                    continue;
                }
                if (spl1[0].StartsWith("Cell width"))
                {
                    var item = alarms.Find((e) => e.Name.Contains("minimum Xdim"));

                    cellSizeY = ParseFloat(spl1[1]);

                    ValueResults.Add(new ValueResult("cellWidth", cellSizeY, item == null ? "PASS" : "FAIL"));

                    continue;
                }
                if (spl1[0].Equals("Size"))
                {
                    var spl2 = spl1[1].Split('x');

                    ValueResults.Add(new ValueResult("symbolWidth", cellSizeX * ParseInt(spl2[0]), "PASS"));
                    ValueResults.Add(new ValueResult("symbolHeight", cellSizeY * ParseInt(spl2[1]), "PASS"));
                    continue;
                }

                if (spl1[0].StartsWith("L1 ("))
                {
                    Gs1Grades.Add(GetGrade("L1", spl1[1]));
                    continue;
                }
                if (spl1[0].StartsWith("L2"))
                {
                    Gs1Grades.Add(GetGrade("L2", spl1[1]));
                    //sect.data.gs1SymbolQuality.L2 = GetGrade(spl1[1]);
                    continue;
                }
                if (spl1[0].StartsWith("QZL1"))
                {
                    Gs1Grades.Add(GetGrade("QZL1", spl1[1]));
                    continue;
                }
                if (spl1[0].StartsWith("QZL2"))
                {

                    Gs1Grades.Add(GetGrade("QZL2", spl1[1]));
                    continue;
                }
                if (spl1[0].StartsWith("OCTASA"))
                {
                    Gs1Grades.Add(GetGrade("OCTASA", spl1[1]));
                    continue;
                }
            }

            foreach (var item in alarms)
                Alarms.Add(item);
        }
        else if (isPDF417)
        {
            //PDF417
            GradeValues.Add(GetGradeValue("symbolContrast", GetValues("Contrast", splitPacket)[0]));
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

                    ValueResults.Add(new ValueResult("symbolXDim", xdim, "PASS"));
                    continue;
                }

                if (spl1[0].StartsWith("Rmin"))
                {
                    var val = (int)Math.Ceiling(ParseFloat(spl1[1]));

                    Values.Add(new Value_("minimumReflectance", val));
                    continue;
                }

                if (spl1[0].StartsWith("Codeword y"))
                {
                    var spl2 = spl1[1].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    if (spl2.Count() != 2) continue;

                    GradeValues.Add(new GradeValue("CodewordY", ParseInt(spl2[1]) , GetGrade("CodewordY", spl2[0])));
                    continue;
                }

                if (spl1[0].StartsWith("Codeword P"))
                {
                    GradeValues.Add(new GradeValue("CodewordP", -1, GetGrade("CodewordP", spl1[1])));
                    continue;
                }
            }

        }
        else
        {
            GradeValues.Add(new GradeValue("decode", -1, GetValues("Decode", splitPacket)[0].StartsWith("PASS") ? new Grade("Decode", 4.0f, "A") : new Grade("Decode", 0.0f, "F")));

            GradeValues.Add(GetGradeValue("symbolContrast", GetValues("Contrast", splitPacket)[0]));
            // GradeValues.Add(new GradeValue("edgeContrast", GetGradeValue(GetValues("Contrast", splitPacket)[0])));
            GradeValues.Add(GetGradeValue("modulation", GetValues("Modulation", splitPacket)[0]));
            GradeValues.Add(GetGradeValue("defects", GetValues("Defects", splitPacket)[0]));
            GradeValues.Add(GetGradeValue("decodability", GetValues("Decodability", splitPacket)[0]));
            GradeValues.Add(new GradeValue("MinRef", -1, GetValues("Min Ref", splitPacket)[0].StartsWith("PASS") ? new Grade("Min Ref", 4.0f, "A") : new Grade("Min Ref", 0.0f, "F")));

            Values.Add(new Value_("maximumReflectance", ParseInt(GetValues("Rmax", splitPacket)[0])));

            ValueResults.Add(new ValueResult("edgeDetermination", 100, GetValues("Edge", splitPacket)[0]));

            foreach (var data in splitPacket)
            {
                if (!data.Contains(','))
                    continue;

                var spl1 = new string[2];
                spl1[0] = data.Substring(0, data.IndexOf(','));
                spl1[1] = data.Substring(data.IndexOf(',') + 1);

                if (spl1[0].StartsWith("Warning"))
                {
                    alarms.Add(new Alarm() { Name = spl1[1], Category = 1 });
                    continue;
                }

                if (spl1[0].StartsWith("Rmin"))
                {
                    var val = (int)Math.Ceiling(ParseFloat(spl1[1]));

                    Values.Add(new Value_("minimumReflectance", val));
                    continue;
                }


                if (spl1[0].StartsWith("Unused "))
                {
                    GradeValues.Add(GetGradeValue("unusedErrorCorrection",spl1[1]));
                    continue;
                }

                if (spl1[0].StartsWith("Xdim"))
                {
                    var xdim = ParseFloat(spl1[1]);

                    var item = alarms.Find((e) => e.Name.Contains("minimum Xdim"));

                    ValueResults.Add(new ValueResult("symbolXDim", xdim, item == null ? "PASS" : "FAIL"));

                    continue;
                }

                if (spl1[0].StartsWith("Bar height"))
                {
                    var val = ParseFloat(spl1[1]) * 1000;

                    var item = alarms.Find((e) => e.Name.Contains("minimum height"));

                    ValueResults.Add(new ValueResult("barHeight", val, item == null ? "PASS" : "FAIL"));
                    continue;
                }

                if (spl1[0].StartsWith("Quiet"))
                {
                    if (spl1[1].Contains("ERR"))
                    {
                        var spl2 = spl1[1].Split(' ');

                        if (spl2.Count() != 2) continue;


                        ValueResults.Add(new ValueResult("quietZoneLeft", ParseInt(spl2[0]), spl2[1]));
                        ValueResults.Add(new ValueResult("quietZoneRight", ParseInt(spl2[0]), spl2[1]));
                    }
                    else
                    {
                        ValueResults.Add(new ValueResult("quietZoneLeft", 100, spl1[1]));
                        ValueResults.Add(new ValueResult("quietZoneRight", 100, spl1[1]));
                    }

                    continue;
                }


            }

            foreach (var item in alarms)
                Alarms.Add(item);

        }
    }

    private string[] GetKeyValuePair(string key, List<string> report)
    {
        string item = report.Find((e) => e.StartsWith(key));

        //if it was not found or the item does not contain a comma.
        return item?.Contains(',') != true ? null : ([item[..item.IndexOf(',')], item[(item.IndexOf(',') + 1)..]]);
    }
    private List<string[]> GetMultipleKeyValuePairs(string key, List<string> report)
    {
        List<string> items = report.FindAll((e) => e.StartsWith(key));

        if (items == null || items.Count == 0)
            return null;

        List<string[]> res = [];
        foreach (string item in items)
        {
            if (!item.Contains(','))
                continue;

            res.Add([item[..item.IndexOf(',')], item[(item.IndexOf(',') + 1)..]]);
        }
        return res;
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

    private GradeValue GetGradeValue(string name, string data)
    {
        var spl2 = data.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        if (spl2.Count() != 2)
            return null;

        var tmp = ParseFloat(spl2[0]);

        return new GradeValue(name, ParseInt(spl2[1]), new Grade(name, tmp, GetLetter(tmp)));
    }

    private Grade GetGrade(string name, string data)
    {
        var tmp = ParseFloat(data);
        return new Grade(name, tmp, GetLetter(tmp));
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
