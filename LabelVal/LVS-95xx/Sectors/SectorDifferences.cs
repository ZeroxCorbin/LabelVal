using CommunityToolkit.Mvvm.ComponentModel;
using Lvs95xx.lib.Core.Models;
using LabelVal.Sectors.Interfaces;
using Org.BouncyCastle.Crypto.Prng;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using V275_REST_Lib.Models;

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
    public SectorDifferences(FullReport report, bool isPDF417, StandardsTypes standard) => Process(report, isPDF417, standard);
    public void Process(FullReport report, bool isPDF417, StandardsTypes standard)
    {
        UserName = report.Name;
        IsNotEmpty = false;

        var alarms = new List<Alarm>();

        var isGS1 = GetParameter("GS1 Data", report.ReportData) != null;
        var is2D = GetParameter("Cell size", report.ReportData) != null;

        if (is2D && standard != StandardsTypes.ISO29158)
        {
            IsNotEmpty = true;

            foreach (var war in GetParameters("Warning", report.ReportData))
                alarms.Add(new Alarm() { Name = war, Category = 1 });

            GradeValues.Add(new GradeValue("decode", -1, GetParameter("Decode", report.ReportData, true).StartsWith("PASS") ? new Grade("", 4.0f, "A") : new Grade("Decode", 0.0f, "F")));
            GradeValues.Add(GetGradeValue("symbolContrast", GetParameter("Contrast", report.ReportData)));
            GradeValues.Add(new GradeValue("modulation", -1, GetGrade("", GetParameter("Modulation", report.ReportData))));
            GradeValues.Add(new GradeValue("reflectanceMargin", -1, GetGrade("", GetParameter("Reflectance margin", report.ReportData))));
            GradeValues.Add(GetGradeValue("axialNonUniformity", GetParameter("Axial nonuniformity", report.ReportData)));
            GradeValues.Add(GetGradeValue("gridNonUniformity", GetParameter("Grid nonuniformity", report.ReportData)));
            GradeValues.Add(GetGradeValue("unusedErrorCorrection", GetParameter("Unused EC", report.ReportData)));

            var fx = GetParameter("Fixed pattern damage", report.ReportData);
            GradeValues.Add(new GradeValue("fixedPatternDamage", -1, new Grade("", ParseFloat(fx), GetLetter(ParseFloat(fx)))));

            Values.Add(new Value_("minimumReflectance", ParseInt(GetParameter("Rmin", report.ReportData))));
            Values.Add(new Value_("maximumReflectance", ParseInt(GetParameter("Rmax", report.ReportData))));
            Values.Add(new Value_("xPrintGrowthX", ParseInt(GetParameter("X print", report.ReportData))));
            Values.Add(new Value_("xPrintGrowthY", ParseInt(GetParameter("Y print", report.ReportData))));

            if (isGS1)
            {
                var ch = GetParameter("Cell width", report.ReportData);
                float cellSizeX = ParseFloat(ch);

                ch = GetParameter("Cell height", report.ReportData);
                float cellSizeY = ParseFloat(ch);

                var sz = GetParameter("Size", report.ReportData);
                var sz2 = sz.Split('x');
                Gs1ValueResults.Add(new ValueResult("symbolWidth", cellSizeX * ParseInt(sz2[0]), "PASS"));
                Gs1ValueResults.Add(new ValueResult("symbolHeight", cellSizeY * ParseInt(sz2[1]), "PASS"));

                var al = alarms.Find((e) => e.Name.Contains("minimum Xdim"));
                Gs1ValueResults.Add(new ValueResult("cellHeight", cellSizeX, al == null ? "PASS" : "FAIL"));
                Gs1ValueResults.Add(new ValueResult("cellWidth", cellSizeY, al == null ? "PASS" : "FAIL"));

                Gs1Grades.Add(GetGrade("L1", GetParameter("L1 (", report.ReportData)));
                Gs1Grades.Add(GetGrade("L2", GetParameter("L2 (", report.ReportData)));
                Gs1Grades.Add(GetGrade("QZL1", GetParameter("QZL1", report.ReportData)));
                Gs1Grades.Add(GetGrade("QZL2", GetParameter("QZL2", report.ReportData)));
                Gs1Grades.Add(GetGrade("OCTASA", GetParameter("OCTASA", report.ReportData)));

            }

            foreach (var a in alarms)
                Alarms.Add(a);

        }
        else if(is2D && standard == StandardsTypes.ISO29158)
        {
            IsNotEmpty = true;

            foreach (var war in GetParameters("Warning", report.ReportData))
                alarms.Add(new Alarm() { Name = war, Category = 1 });

            GradeValues.Add(new GradeValue("decode", -1, GetParameter("Decode", report.ReportData, true).StartsWith("PASS") ? new Grade("", 4.0f, "A") : new Grade("Decode", 0.0f, "F")));
            GradeValues.Add(GetGradeValue("cellContrast", GetParameter("Cell con", report.ReportData)));
            GradeValues.Add(GetGradeValue("minimumReflectance", GetParameter("Minimum refle", report.ReportData)));
            GradeValues.Add(new GradeValue("cellModulation", -1, GetGrade("", GetParameter("Cell modu", report.ReportData))));
            GradeValues.Add(GetGradeValue("axialNonUniformity", GetParameter("Axial nonuniformity", report.ReportData)));
            GradeValues.Add(GetGradeValue("gridNonUniformity", GetParameter("Grid nonuniformity", report.ReportData)));
            GradeValues.Add(GetGradeValue("unusedErrorCorrection", GetParameter("Unused EC", report.ReportData)));

            var fx = GetParameter("Fixed pattern damage", report.ReportData);
            GradeValues.Add(new GradeValue("fixedPatternDamage", -1, new Grade("", ParseFloat(fx), GetLetter(ParseFloat(fx)))));


            var ch = GetParameter("Cell width", report.ReportData);
            float cellSizeX = ParseFloat(ch);

            ch = GetParameter("Cell height", report.ReportData);
            float cellSizeY = ParseFloat(ch);

            var sz = GetParameter("Size", report.ReportData);
            var sz2 = sz.Split('x');
            Values.Add(new Value_("symbolWidth", (int)(cellSizeX * ParseInt(sz2[0]))));
            Values.Add(new Value_("symbolHeight", (int)(cellSizeY * ParseInt(sz2[1]))));

            var al = alarms.Find((e) => e.Name.Contains("minimum Xdim"));
            Values.Add(new Value_("cellHeight", (int)cellSizeX));
            Values.Add(new Value_("cellWidth", (int)cellSizeY));

            foreach (var a in alarms)
                Alarms.Add(a);

        }
        else if (isPDF417)
        {
            IsNotEmpty = true;

            GradeValues.Add(GetGradeValue("symbolContrast", GetParameter("Contrast", report.ReportData)));
            ValueResults.Add(new ValueResult("symbolXDim", ParseFloat(GetParameter("XDim", report.ReportData)), "PASS"));

            Values.Add(new Value_("minimumReflectance", (int)Math.Ceiling(ParseFloat(GetParameter("Rmin", report.ReportData)))));

            var spl = GetParameter("Codeword Yield", report.ReportData).Split(' ');
            if (spl.Count() == 2)
                GradeValues.Add(new GradeValue("CodewordYield", ParseInt(spl[1]), GetGrade("", spl[0])));
            else
                GradeValues.Add(new GradeValue("CodewordYield", -1, new Grade("", 0.0f, "F")));

            GradeValues.Add(new GradeValue("CodewordPQ", -1, GetGrade("", GetParameter("Codeword PQ", report.ReportData))));
        }
        else
        {
            IsNotEmpty = true;

            foreach (var war in GetParameters("Warning", report.ReportData))
                alarms.Add(new Alarm() { Name = war, Category = 1 });

            GradeValues.Add(new GradeValue("decode", -1, GetParameter("Decode", report.ReportData, true).StartsWith("PASS") ? new Grade("", 4.0f, "A") : new Grade("Decode", 0.0f, "F")));
            GradeValues.Add(GetGradeValue("symbolContrast", GetParameter("Contrast", report.ReportData)));
            GradeValues.Add(GetGradeValue("modulation", GetParameter("Modulation", report.ReportData)));
            GradeValues.Add(GetGradeValue("defects", GetParameter("Defects", report.ReportData)));
            GradeValues.Add(GetGradeValue("decodability", GetParameter("Decodability", report.ReportData)));
            GradeValues.Add(new GradeValue("minimumReflectance", (int)Math.Ceiling(ParseFloat(GetParameter("Rmin", report.ReportData))), GetParameter("Min Ref", report.ReportData).StartsWith("PASS") ? new Grade("Min Ref", 4.0f, "A") : new Grade("Min Ref", 0.0f, "F")));

            if (isGS1)
            {
                //GradeValues.Add(GetGradeValue("unusedErrorCorrection", GetParameter("Unused ", report.ReportData)));

                var kv1 = GetParameter("Xdim", report.ReportData);
                var item = alarms.Find((e) => e.Name.Contains("minimum Xdim"));
                Gs1ValueResults.Add(new ValueResult("symbolXDim", ParseFloat(kv1), item == null ? "PASS" : "FAIL"));

                kv1 = GetParameter("Bar height", report.ReportData);
                item = alarms.Find((e) => e.Name.Contains("minimum height"));
                Gs1ValueResults.Add(new ValueResult("symbolBarHeight", ParseFloat(kv1), item == null ? "PASS" : "FAIL"));
            }

            Values.Add(new Value_("maximumReflectance", ParseInt(GetParameter("Rmax", report.ReportData))));

            ValueResults.Add(new ValueResult("edgeDetermination", 100, GetParameter("Edge", report.ReportData)));

            var kv = GetParameter("Quiet", report.ReportData);
            if (kv != null && kv.Contains("ERR"))
            {
                var spl2 = kv.Split(' ');
                if (spl2.Count() == 2)
                    ValueResults.Add(new ValueResult("quietZone", ParseInt(spl2[0]), spl2[1]));
            }
            else
                ValueResults.Add(new ValueResult("quietZone", 100, kv));

            foreach (var item in alarms)
                Alarms.Add(item);
        }
    }

    private string GetParameter(string key, List<ReportData> report, bool equal = false) => report.Find((e) => equal ? e.ParameterName.Equals(key) : e.ParameterName.StartsWith(key))?.ParameterValue;
    private List<string> GetParameters(string key, List<ReportData> report) => report.FindAll((e) => e.ParameterName.StartsWith(key)).Select((e) => e.ParameterValue).ToList();

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
