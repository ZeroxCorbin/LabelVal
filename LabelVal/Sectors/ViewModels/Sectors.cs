using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using V5_REST_Lib.Models;

namespace LabelVal.Sectors.ViewModels;

public class Template
{
    public class TemplateMatchMode
    {
        public int MatchMode { get; set; }
        public int UserDefinedDataTrueSize { get; set; }
        public string FixedText { get; set; }
    }

    public class BlemishMaskLayers
    {
        public V275_REST_lib.Models.Job.Layer[] Layers { get; set; }
    }

    public V275_REST_lib.Models.Job.Sector V275Template { get; }
    public Results_QualifiedResult V5Template { get; }

    public string Name { get; set; }
    public string Username { get; set; }
    public int Top { get; set; }
    public string Symbology { get; set; }


    public TemplateMatchMode MatchSettings { get; set; }
    public BlemishMaskLayers BlemishMask { get; set; }

    public Template(V275_REST_lib.Models.Job.Sector sectorTemplate)
    {
        V275Template = sectorTemplate;

        Name = sectorTemplate.name;
        Username = sectorTemplate.username;
        Top = sectorTemplate.top;
        Symbology = sectorTemplate.symbology;

        MatchSettings = new Template.TemplateMatchMode
        {
            MatchMode = sectorTemplate.matchSettings.matchMode,
            UserDefinedDataTrueSize = sectorTemplate.matchSettings.userDefinedDataTrueSize,
            FixedText = sectorTemplate.matchSettings.fixedText
        };
        BlemishMask = new Template.BlemishMaskLayers
        {
            Layers = sectorTemplate.blemishMask?.layers
        };
    }

    public Template(Results_QualifiedResult Report, string name)
    {
        V5Template = Report;

        Name = name;
        Username = name;
        Top = Report.x;

        Symbology = GetV5Symbology(Report);

    }

    private string GetV5Symbology(Results_QualifiedResult Report)
    {
        if (Report.Code128 != null)
            return "Code128";
        else if (Report.Datamatrix != null)
            return "DataMatrix";
        else if (Report.QR != null)
            return "QR";
        else if (Report.PDF417 != null)
            return "PDF417";
        else if (Report.UPC != null)
            return "UPC";
        else
            return "Unknown";
    }

}


//Report.Type
//Report.SymbolType
//Report.DecodeText
//Report.Text
//Report.BlemishCount
//Report.Score
//Report.data.xDimension
//Report.data.aperture

//Report.OverallGradeString
//Report.OverallGradeValue

//Report.data.gs1Results
//Report.data.gs1Results.formattedOut

public class Report
{

    public class ModuleData
    {
        public int[] ModuleModulation { get; set; }
        public int[] ModuleReflectance { get; set; }

        public int QuietZone { get; set; }

        public int NumRows { get; set; }
        public int NumColumns { get; set; }

        public double CosAngle0 { get; set; }
        public double CosAngle1 { get; set; }

        public double SinAngle0 { get; set; }
        public double SinAngle1 { get; set; }

        public double DeltaX { get; set; }
        public double DeltaY { get; set; }

        public double Xne { get; set; }
        public double Yne { get; set; }

        public double Xnw { get; set; }
        public double Ynw { get; set; }

        public double Xsw { get; set; }
        public double Ysw { get; set; }
    }


    public string Type { get; set; }
    public string SymbolType { get; set; }
    public string DecodeText { get; set; }
    public string Text { get; set; }
    public int BlemishCount { get; set; }
    public double Score { get; set; }
    public double XDimension { get; set; }
    public double Aperture { get; set; }

    public string OverallGradeString { get; set; }
    public double OverallGradeValue { get; set; }

    public V275_REST_lib.Models.Report_InspectSector_Common.Gs1results GS1Results { get; set; }
    public string FormattedOut { get; set; }

    public ModuleData ExtendedData { get; set; }

    public Report(object report)
    {
        switch (report)
        {
            case V275_REST_lib.Models.Report_InspectSector_Verify1D:
                var v1D = (V275_REST_lib.Models.Report_InspectSector_Verify1D)report;
                Type = v1D.type;
                SymbolType = v1D.data.symbolType;
                DecodeText = v1D.data.decodeText;
                //OCR/V Only: Text = v1D.data.text;
                //OCR/V Only: Score = v1D.data.score;
                //Blemish Only: BlemishCount = v1D.data.blemishCount;
                XDimension = v1D.data.xDimension;
                Aperture = v1D.data.aperture;

                OverallGradeString = v1D.data.overallGrade._string;
                OverallGradeValue = v1D.data.overallGrade.grade.value;

                if (v1D.data.gs1Results != null)
                {
                    GS1Results = v1D.data.gs1Results;
                    FormattedOut = v1D.data.gs1Results.formattedOut;
                }

                break;

            case V275_REST_lib.Models.Report_InspectSector_Verify2D:
                var v2D = (V275_REST_lib.Models.Report_InspectSector_Verify2D)report;
                Type = v2D.type;
                SymbolType = v2D.data.symbolType;
                DecodeText = v2D.data.decodeText;
                //OCR/V Only: Text = v1D.data.text;
                //OCR/V Only: Score = v1D.data.score;
                //Blemish Only: BlemishCount = v1D.data.blemishCount;
                XDimension = v2D.data.xDimension;
                Aperture = v2D.data.aperture;

                OverallGradeString = v2D.data.overallGrade._string;
                OverallGradeValue = v2D.data.overallGrade.grade.value;

                if (v2D.data.gs1Results != null)
                {
                    GS1Results = v2D.data.gs1Results;
                    FormattedOut = v2D.data.gs1Results.formattedOut;
                }

                if (v2D.data.extendedData != null)
                    ExtendedData = JsonConvert.DeserializeObject<ModuleData>(JsonConvert.SerializeObject(v2D.data.extendedData));

                break;

            case Results_QualifiedResult:

                var v5 = (Results_QualifiedResult)report;
                Type = GetV5Type(v5);
                SymbolType = GetV5Symbology(v5);
                DecodeText = v5.dataUTF8;
                //XDimension = v5.ppe;

                if (v5.grading != null)
                    if (Type == "verify1D")
                    {
                        if(v5.grading.iso15416 == null)
                        {
                            OverallGradeString = "No Grade";
                            OverallGradeValue = 0;
                        }
                        else
                        {
                            OverallGradeString = $"{v5.grading.iso15416.overall.grade:f1}/00/600";
                            OverallGradeValue = v5.grading.iso15416.overall.grade;
                        }
                    }
                    else if (Type == "verify2D")
                    {
                        if(v5.grading.iso15415 == null)
                        {
                            OverallGradeString = "No Grade";
                            OverallGradeValue = 0;
                        }
                        else
                        {
                            OverallGradeString = $"{v5.grading.iso15415.overall.grade:f1}/00/600";
                            OverallGradeValue = v5.grading.iso15415.overall.grade;
                        }
                    }

                {

                }
                break;
        }

    }

    private string GetV5Symbology(Results_QualifiedResult Report)
    {
        if (Report.Code128 != null)
            return "Code128";
        else if (Report.Datamatrix != null)
            return "DataMatrix";
        else if (Report.QR != null)
            return "QR";
        else if (Report.PDF417 != null)
            return "PDF417";
        else if (Report.UPC != null)
            return "UPC";
        else
            return "Unknown";
    }

    private string GetV5Type(Results_QualifiedResult Report)
    {
        if (Report.Code128 != null)
            return "verify1D";
        else if (Report.Datamatrix != null)
            return "verify2D";
        else if (Report.QR != null)
            return "verify2D";
        else if (Report.PDF417 != null)
            return "verify1D";
        else if (Report.UPC != null)
            return "verify1D";
        else
            return "Unknown";
    }
}

public partial class Sectors : ObservableObject
{
    /// <summary>
    /// Used for OCV, OCR: matchSettings.matchMode, matchSettings.userDefinedDataTrueSize, matchSettings.fixedText
    /// Used for Blemish: blemishMask.layers
    /// Many uses: name, username, top, symbology,
    /// </summary>
    [ObservableProperty] private Template template;
    [ObservableProperty] private object report;
    [ObservableProperty] private SectorDifferences sectorDifferences = new();

    public V275_REST_lib.Models.Job.Sector V275Template { get; }

    [ObservableProperty] private bool isWarning;
    [ObservableProperty] private bool isError;

    [ObservableProperty] private bool isGS1Standard;

    [ObservableProperty] private bool isWrongStandard;
    partial void OnIsWrongStandardChanged(bool value) => OnPropertyChanged(nameof(IsNotWrongStandard));
    public bool IsNotWrongStandard => !IsWrongStandard;

    public Sectors() { }

    //V275
    public Sectors(V275_REST_lib.Models.Job.Sector template, object report, bool isWrongStandard, bool isGS1Standard)
    {
        Report = new Report(report);

        V275Template = template;
        Template = new Template(template);

        IsWrongStandard = isWrongStandard;
        IsGS1Standard = isGS1Standard;

        SectorDifferences.V275Process(report, Template.Username, IsGS1Standard);

        var highCat = 0;

        foreach (var alm in SectorDifferences.Alarms)
        {
            //Alarms.Add(alm);
            if (highCat < alm.category)
                highCat = alm.category;
        }

        if (highCat == 1)
            IsWarning = true;
        else if (highCat == 2)
            IsError = true;

    }

    //V5
    public Sectors(Results_QualifiedResult results, string name, bool isWrongStandard = false, bool isGS1Standard = false)
    {
        Report = new Report(results);
        Template = new Template(results, name);

        IsWrongStandard = isWrongStandard;
        IsGS1Standard = isGS1Standard;

        SectorDifferences.V5Process(results, Template.Username, IsGS1Standard);

        //var highCat = 0;

        //foreach (var alm in SectorDifferences.Alarms)
        //{
        //    //Alarms.Add(alm);
        //    if (highCat < alm.category)
        //        highCat = alm.category;
        //}

        //if (highCat == 1)
        //    IsWarning = true;
        //else if (highCat == 2)
        //    IsError = true;

    }

    //L95xx
    public Sectors(Template template, object report, bool isWrongStandard, bool isGS1Standard)
    {
        Report = report;
        Template = template;

        IsWrongStandard = isWrongStandard;
        IsGS1Standard = isGS1Standard;

        SectorDifferences.V275Process(Report, Template.Username, IsGS1Standard);

        var highCat = 0;

        foreach (var alm in SectorDifferences.Alarms)
        {
            //Alarms.Add(alm);
            if (highCat < alm.category)
                highCat = alm.category;
        }

        if (highCat == 1)
            IsWarning = true;
        else if (highCat == 2)
            IsError = true;

    }


    //public Sectors(Config Template, Results_QualifiedResult Report, bool isWrongStandard, bool isGS1Standard)
    //{
    //    Report = Report;
    //    Template = Template;

    //    IsWrongStandard = isWrongStandard;
    //    IsGS1Standard = isGS1Standard;

    //    SectorDifferences.Process(Report, Template.Username, IsGS1Standard);

    //    var highCat = 0;

    //    foreach (var alm in SectorDifferences.Alarms)
    //    {
    //        //Alarms.Add(alm);
    //        if (highCat < alm.category)
    //            highCat = alm.category;
    //    }

    //    if (highCat == 1)
    //        IsWarning = true;
    //    else if (highCat == 2)
    //        IsError = true;

    //}

    //private Template V5ParseTemplate(Config config, Results results)
    //{

    //    return new Template
    //    {
    //        Name = config.Name,
    //        Username = config.Username,
    //        Top = config.Top,
    //        Symbology = config.Symbology,
    //        MatchSettings = new Template.TemplateMatchMode
    //        {
    //            MatchMode = config.MatchSettings.MatchMode,
    //            UserDefinedDataTrueSize = config.MatchSettings.UserDefinedDataTrueSize,
    //            FixedText = config.MatchSettings.FixedText
    //        },
    //        BlemishMask = new Template.BlemishMaskLayers
    //        {
    //            Layers = results.BlemishMask.Layers
    //        }
    //    };
    //}
}
