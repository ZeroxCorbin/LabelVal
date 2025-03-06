using BarcodeVerification.lib.Common;
using CommunityToolkit.Mvvm.ComponentModel;
using LabelVal.Sectors.Classes;
using LabelVal.Sectors.Interfaces;
using Lvs95xx.lib.Core.Controllers;
using Lvs95xx.lib.Core.Models;
using System.Collections.ObjectModel;

namespace LabelVal.LVS_95xx.Sectors;

public partial class SectorDetails : ObservableObject, ISectorDetails
{
    public ISector Sector { get; set; }

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

    public SectorDifferences? Compare(ISectorDetails compare) => SectorDifferences.Compare(this, compare);

    public SectorDetails() { }
    public SectorDetails(ISector sector) => Process(sector);
    public void Process(ISector sector)
    {
        if (sector is not LVS_95xx.Sectors.Sector sec)
            return;

        Sector = sector;
        FullReport report = sec.L95xxFullReport;

        Name = report.Name;
        UserName = report.Name;
        IsNotEmpty = false;

        List<Alarm> alarms = [];

        bool isGS1 = GetParameter("GS1 Data", report.ReportData) != null;
        bool is2D = sec.Report.SymbolType.GetRegionType(AvailableDevices.L95) == AvailableRegionTypes.Type2D;

        if (is2D && Sector.Report.Standard != AvailableStandards.DPM)
        {
            IsNotEmpty = true;

            foreach (string war in GetParameters("Warning", report.ReportData))
                alarms.Add(new Alarm() { Name = war, Category = 1 });

            GradeValues.Add(new GradeValue("decode", -1, GetParameter("Decode", report.ReportData, true).StartsWith("PASS") ? new Grade("", 4.0f, "A") : new Grade("Decode", 0.0f, "F")));
            GradeValues.Add(GetGradeValue("symbolContrast", GetParameter("Contrast", report.ReportData)));
            GradeValues.Add(new GradeValue("modulation", -1, GetGrade("", GetParameter("Modulation", report.ReportData))));
            GradeValues.Add(new GradeValue("reflectanceMargin", -1, GetGrade("", GetParameter("Reflectance margin", report.ReportData))));
            GradeValues.Add(GetGradeValue("axialNonUniformity", GetParameter("Axial nonuniformity", report.ReportData)));
            GradeValues.Add(GetGradeValue("gridNonUniformity", GetParameter("Grid nonuniformity", report.ReportData)));
            GradeValues.Add(GetGradeValue("unusedErrorCorrection", GetParameter("Unused EC", report.ReportData)));

            string fx = GetParameter("Fixed pattern damage", report.ReportData);
            GradeValues.Add(new GradeValue("fixedPatternDamage", -1, new Grade("", ParseFloat(fx), GetLetter(ParseFloat(fx)))));

            Values.Add(new Value_("minimumReflectance", ParseInt(GetParameter("Rmin", report.ReportData))));
            Values.Add(new Value_("maximumReflectance", ParseInt(GetParameter("Rmax", report.ReportData))));

            if (Sector.Report.SymbolType is not AvailableSymbologies.Aztec)
            {
                Values.Add(new Value_("xPrintGrowthX", ParseInt(GetParameter("X print", report.ReportData))));
                Values.Add(new Value_("xPrintGrowthY", ParseInt(GetParameter("Y print", report.ReportData))));
                Values.Add(new Value_("contrastUniformity", ParseInt(GetParameter("Contrast un", report.ReportData))));
            }

            if (isGS1)
            {
                string ch = GetParameter("Cell width", report.ReportData);
                float cellSizeX = ParseFloat(ch);

                ch = GetParameter("Cell height", report.ReportData);
                float cellSizeY = ParseFloat(ch);

                string sz = GetParameter("Size", report.ReportData);
                string[] sz2 = sz.Split('x');
                Gs1ValueResults.Add(new ValueResult("symbolWidth", cellSizeX * ParseInt(sz2[0]), "PASS"));
                Gs1ValueResults.Add(new ValueResult("symbolHeight", cellSizeY * ParseInt(sz2[1]), "PASS"));

                Alarm al = alarms.Find((e) => e.Name.Contains("minimum Xdim"));
                Gs1ValueResults.Add(new ValueResult("cellHeight", cellSizeX, al == null ? "PASS" : "FAIL"));
                Gs1ValueResults.Add(new ValueResult("cellWidth", cellSizeY, al == null ? "PASS" : "FAIL"));

                Gs1Grades.Add(GetGrade("L1", GetParameter("L1 (", report.ReportData)));
                Gs1Grades.Add(GetGrade("L2", GetParameter("L2 (", report.ReportData)));
                Gs1Grades.Add(GetGrade("QZL1", GetParameter("QZL1", report.ReportData)));
                Gs1Grades.Add(GetGrade("QZL2", GetParameter("QZL2", report.ReportData)));
                Gs1Grades.Add(GetGrade("OCTASA", GetParameter("OCTASA", report.ReportData)));

            }

            foreach (Alarm a in alarms)
                Alarms.Add(a);

        }
        //DPM
        else if (is2D && Sector.Report.Standard == AvailableStandards.DPM)
        {
            IsNotEmpty = true;

            foreach (string war in GetParameters("Warning", report.ReportData))
                alarms.Add(new Alarm() { Name = war, Category = 1 });

            GradeValues.Add(new GradeValue("decode", -1, GetParameter("Decode", report.ReportData, true).StartsWith("PASS") ? new Grade("", 4.0f, "A") : new Grade("Decode", 0.0f, "F")));
            GradeValues.Add(GetGradeValue("cellContrast", GetParameter("Cell con", report.ReportData)));
            GradeValues.Add(GetGradeValue("minimumReflectance", GetParameter("Minimum refle", report.ReportData)));
            GradeValues.Add(new GradeValue("cellModulation", -1, GetGrade("", GetParameter("Cell modu", report.ReportData))));
            GradeValues.Add(GetGradeValue("axialNonUniformity", GetParameter("Axial nonuniformity", report.ReportData)));
            GradeValues.Add(GetGradeValue("gridNonUniformity", GetParameter("Grid nonuniformity", report.ReportData)));
            GradeValues.Add(GetGradeValue("unusedErrorCorrection", GetParameter("Unused EC", report.ReportData)));

            string fx = GetParameter("Fixed pattern damage", report.ReportData);
            GradeValues.Add(new GradeValue("fixedPatternDamage", -1, new Grade("", ParseFloat(fx), GetLetter(ParseFloat(fx)))));

            string ch = GetParameter("Cell width", report.ReportData);
            float cellSizeX = ParseFloat(ch);

            ch = GetParameter("Cell height", report.ReportData);
            float cellSizeY = ParseFloat(ch);

            string sz = GetParameter("Size", report.ReportData);
            string[] sz2 = sz.Split('x');
            Values.Add(new Value_("symbolWidth", (int)(cellSizeX * ParseInt(sz2[0]))));
            Values.Add(new Value_("symbolHeight", (int)(cellSizeY * ParseInt(sz2[1]))));

            //Values.Add(new Value_("contrastUniformity", ParseInt(GetParameter("Contrast un", report.ReportData))));

            Alarm al = alarms.Find((e) => e.Name.Contains("minimum Xdim"));
            Values.Add(new Value_("cellHeight", (int)cellSizeX));
            Values.Add(new Value_("cellWidth", (int)cellSizeY));

            foreach (Alarm a in alarms)
                Alarms.Add(a);

        }
        else if (Sector.Report.SymbolType is not AvailableSymbologies.PDF and not AvailableSymbologies.PDF417 and not AvailableSymbologies.MicroPDF417)
        {

            IsNotEmpty = true;

            foreach (string war in GetParameters("Warning", report.ReportData))
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

                string kv1 = GetParameter("Xdim", report.ReportData);
                Alarm item = alarms.Find((e) => e.Name.Contains("minimum Xdim"));
                Gs1ValueResults.Add(new ValueResult("symbolXDim", ParseFloat(kv1), item == null ? "PASS" : "FAIL"));

                kv1 = GetParameter("Bar height", report.ReportData);
                item = alarms.Find((e) => e.Name.Contains("minimum height"));
                Gs1ValueResults.Add(new ValueResult("symbolBarHeight", ParseFloat(kv1), item == null ? "PASS" : "FAIL"));
            }

            Values.Add(new Value_("maximumReflectance", ParseInt(GetParameter("Rmax", report.ReportData))));

            ValueResults.Add(new ValueResult("edgeDetermination", 100, GetParameter("Edge", report.ReportData)));

            string kv = GetParameter("Quiet", report.ReportData);
            if (kv != null && kv.Contains("ERR"))
            {
                string[] spl2 = kv.Split(' ');
                if (spl2.Count() == 2)
                    ValueResults.Add(new ValueResult("quietZone", ParseInt(spl2[0]), spl2[1]));
            }
            else
                ValueResults.Add(new ValueResult("quietZone", 100, kv));

            foreach (Alarm item in alarms)
                Alarms.Add(item);
        }
        else
        {
            IsNotEmpty = true;

            GradeValues.Add(GetGradeValue("symbolContrast", GetParameter("Contrast", report.ReportData)));
            ValueResults.Add(new ValueResult("symbolXDim", ParseFloat(GetParameter("XDim", report.ReportData)), "PASS"));

            Values.Add(new Value_("minimumReflectance", (int)Math.Ceiling(ParseFloat(GetParameter("Rmin", report.ReportData)))));

            string[] spl = GetParameter("Codeword Yield", report.ReportData).Split(' ');
            if (spl.Count() == 2)
                GradeValues.Add(new GradeValue("CodewordYield", ParseInt(spl[1]), GetGrade("", spl[0])));
            else
                GradeValues.Add(new GradeValue("CodewordYield", -1, new Grade("", 0.0f, "F")));

            GradeValues.Add(new GradeValue("CodewordPQ", -1, GetGrade("", GetParameter("Codeword PQ", report.ReportData))));
        }
    }

    private string GetParameter(string key, List<ReportData> report, bool equal = false) => report.Find((e) => equal ? e.ParameterName.Equals(key) : e.ParameterName.StartsWith(key))?.ParameterValue;
    private List<string> GetParameters(string key, List<ReportData> report) => report.FindAll((e) => e.ParameterName.StartsWith(key)).Select((e) => e.ParameterValue).ToList();

    private string[] GetKeyValuePair(string key, List<string> report)
    {
        string item = report.Find((e) => e.StartsWith(key));

        //if it was not found or the item does not contain a comma.
        return item?.Contains(',') != true ? null : [item[..item.IndexOf(',')], item[(item.IndexOf(',') + 1)..]];
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
        List<string> warn = splitPacket.FindAll((e) => e.StartsWith(name));

        List<string> ret = [];
        foreach (string line in warn)
        {
            //string[] spl1 = new string[2];
            //spl1[0] = line.Substring(0, line.IndexOf(','));
            ret.Add(line[(line.IndexOf(',') + 1)..]);
        }
        return ret.ToArray();
    }
    private float ParseFloat(string value)
    {
        if (value == null)
            return -1;
        string digits = new(value.Trim().TakeWhile("0123456789.".Contains
                                ).ToArray());

        return float.TryParse(digits, out float val) ? val : 0;

    }

    private static int ParseInt(string value)
    {
        string digits = new(value.Trim().TakeWhile("0123456789".Contains
                                ).ToArray());

        return int.TryParse(digits, out int val) ? val : 0;
    }

    private GradeValue GetGradeValue(string name, string data)
    {
        string[] spl2 = data.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        if (spl2.Count() != 2)
            return null;

        float tmp = ParseFloat(spl2[0]);

        return new GradeValue(name, ParseFloat(spl2[1]), new Grade(name, tmp, GetLetter(tmp)));
    }

    private Grade GetGrade(string name, string data)
    {
        if (data == null)
            return new Grade(name, 0.0f, "F");

        float tmp = ParseFloat(data);
        return new Grade(name, tmp, GetLetter(tmp));
    }

    private static string GetLetter(float value) => value switch
    {
        4.0f => "A",
        <= 3.9f and >= 3.0f => "B",
        <= 2.9f and >= 2.0f => "C",
        <= 1.9f and >= 1.0f => "D",
        <= 0.9f and >= 0.0f => "F",
        _ => "F"
    };

}
