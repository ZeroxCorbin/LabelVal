using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ControlzEx.Standard;
using LabelVal.Messages;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Documents;
using V275_REST_lib.Models;
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
                Type = V5GetType(v5);
                SymbolType = V5GetSymbology(v5);
                DecodeText = v5.dataUTF8;
                //XDimension = v5.ppe;

                if (v5.grading != null)
                    if (Type == "verify1D")
                    {
                        if (v5.grading.iso15416 == null)
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
                        if (v5.grading.iso15415 == null)
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
                break;
            case List<string>:

                Type = ((List<string>)report).Find((e) => e.StartsWith("Cell size")) == null ? "verify1D" : "verify2D";

                foreach (var data in (List<string>)report)
                {
                    if (!data.Contains(','))
                        continue;

                    string[] spl1 = new string[2];
                    spl1[0] = data.Substring(0, data.IndexOf(','));
                    spl1[1] = data.Substring(data.IndexOf(',') + 1);

                    if (spl1[0].StartsWith("Symbology"))
                    {
                        SymbolType = L95xxGetSymbolType(spl1[1]);

                        //verify1D
                        if (SymbolType == "dataBar")
                        {
                            var item = ((List<string>)report).Find((e) => e.StartsWith("DataBar"));
                            if (item != null)
                            {
                                var spl2 = item.Split(',');

                                if (spl2.Count() != 2)
                                    continue;

                                SymbolType += spl2[1];
                            }
                        }
                        continue;
                    }

                    if (spl1[0].StartsWith("Decoded"))
                    {
                        DecodeText = spl1[1];
                        continue;
                    }

                    //Verify2D
                    if (spl1[0].StartsWith("Cell size"))
                    {
                        XDimension = L95xxParseFloat(spl1[1]);
                        continue;
                    }

                    //Verify1D
                    if (spl1[0].StartsWith("Xdim"))
                    {
                        XDimension = L95xxParseFloat(spl1[1]);
                        continue;
                    }


                    if (spl1[0].StartsWith("Overall"))
                    {
                        var spl2 = spl1[1].Split('/');

                        if (spl2.Count() < 3) continue;

                        OverallGradeValue = L95xxGetGrade(spl2[0]).value;// new Report_InspectSector_Common.Overallgrade() { grade = GetGrade(spl2[0]), _string = spl1[1] };
                        OverallGradeString = spl1[1];

                        Aperture = L95xxParseFloat(spl2[1]);
                        continue;
                    }

                }



                break;
        }

    }

    private string L95xxGetLetter(float value)
    {
        if (value == 4.0f)
            return "A";

        if (value <= 3.9f && value >= 3.0f)
            return "B";

        if (value <= 2.9f && value >= 2.0f)
            return "C";

        if (value <= 1.9f && value >= 1.0f)
            return "D";

        if (value <= 0.9f && value >= 0.0f)
            return "F";

        return "F";
    }
    private float L95xxParseFloat(string value)
    {
        var digits = new string(value.Trim().TakeWhile(c =>
                                ("0123456789.").Contains(c)
                                ).ToArray());

        if (float.TryParse(digits, out var val))
            return val;
        else
            return 0;

    }
    private Report_InspectSector_Common.Grade L95xxGetGrade(string data)
    {
        float tmp = L95xxParseFloat(data);

        return new Report_InspectSector_Common.Grade()
        {
            value = tmp,
            letter = L95xxGetLetter(tmp)
        };
    }


    private string L95xxGetSymbolType(string value)
    {
        if (value.Contains("UPC-A"))
            return "upcA";

        if (value.Contains("UPC-B"))
            return "upcB";

        if (value.Contains("EAN-13"))
            return "ean13";

        if (value.Contains("EAN-8"))
            return "ean8";

        if (value.Contains("DataBar"))
            return "dataBar";

        if (value.Contains("Code 39"))
            return "code39";

        if (value.Contains("Code 93"))
            return "code93";

        if (value.StartsWith("GS1 QR"))
            return "qrCode";

        if (value.StartsWith("Micro"))
            return "microQrCode";

        if (value.Contains("Data Matrix"))
            return "dataMatrix";

        if (value.Contains("Aztec"))
            return "aztec";

        if (value.Contains("Codabar"))
            return "codaBar";

        if (value.Contains("ITF"))
            return "i2of5";

        if (value.Contains("PDF417"))
            return "pdf417";
        return "";
    }
    private string V5GetSymbology(Results_QualifiedResult Report)
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
    private string V5GetType(Results_QualifiedResult Report)
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

public partial class Sector : ObservableObject
{
    /// <summary>
    /// Used for OCV, OCR: matchSettings.matchMode, matchSettings.userDefinedDataTrueSize, matchSettings.fixedText
    /// Used for Blemish: blemishMask.layers
    /// Many uses: name, username, top, symbology,
    /// </summary>
    [ObservableProperty] private Template template;
    [ObservableProperty] private Report report;
    [ObservableProperty] private SectorDifferences sectorDifferences = new();

    public V275_REST_lib.Models.Job.Sector V275Template { get; }

    [ObservableProperty] private bool isWarning;
    [ObservableProperty] private bool isError;

    [ObservableProperty] private bool isGS1Standard;

    [ObservableProperty] private bool isWrongStandard;
    partial void OnIsWrongStandardChanged(bool value) => OnPropertyChanged(nameof(IsNotWrongStandard));
    public bool IsNotWrongStandard => !IsWrongStandard;

    public Sector() { }

    //V275
    public Sector(V275_REST_lib.Models.Job.Sector template, object report, bool isWrongStandard, bool isGS1Standard)
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
    public Sector(Results_QualifiedResult results, string name, bool isWrongStandard = false, bool isGS1Standard = false)
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
    public Sector(Template template, string packet, bool isWrongStandard, bool isGS1Standard)
    {
        var spl = packet.Split('\r').ToList();

        Report = new Report(spl);

        Template = template;

        IsWrongStandard = isWrongStandard;
        IsGS1Standard = isGS1Standard;

        SectorDifferences.L95xxProcess(spl, Template.Username, IsGS1Standard, Report.SymbolType == "pdf417");

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
