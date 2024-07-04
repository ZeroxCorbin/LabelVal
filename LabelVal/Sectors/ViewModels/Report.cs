using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using V275_REST_lib.Models;
using V5_REST_Lib.Models;

namespace LabelVal.Sectors.ViewModels;

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
    public string OverallGradeLetter { get; set; }

    public V275_REST_lib.Models.Report_InspectSector_Common.Gs1results GS1Results { get; set; }
    public string FormattedOut { get; set; }

    public ModuleData ExtendedData { get; set; }

    public Report(object report)
    {
        switch (report)
        {
            case V275_REST_lib.Models.Report_InspectSector_Verify1D:
                Report_InspectSector_Verify1D v1D = (V275_REST_lib.Models.Report_InspectSector_Verify1D)report;
                Type = v1D.type;
                SymbolType = v1D.data.symbolType;

                DecodeText = v1D.data.decodeText;
                XDimension = v1D.data.xDimension;
                Aperture = v1D.data.aperture;

                OverallGradeString = v1D.data.overallGrade._string;
                OverallGradeValue = v1D.data.overallGrade.grade.value;
                OverallGradeLetter = v1D.data.overallGrade.grade.letter;

                if (v1D.data.gs1Results != null)
                {
                    GS1Results = v1D.data.gs1Results;
                    FormattedOut = v1D.data.gs1Results.formattedOut;
                }

                break;

            case V275_REST_lib.Models.Report_InspectSector_Verify2D:
                Report_InspectSector_Verify2D v2D = (V275_REST_lib.Models.Report_InspectSector_Verify2D)report;
                Type = v2D.type;
                SymbolType = v2D.data.symbolType;

                DecodeText = v2D.data.decodeText;
                XDimension = v2D.data.xDimension;
                Aperture = v2D.data.aperture;

                OverallGradeString = v2D.data.overallGrade._string;
                OverallGradeValue = v2D.data.overallGrade.grade.value;
                OverallGradeLetter = v2D.data.overallGrade.grade.letter;

                if (v2D.data.gs1Results != null)
                {
                    GS1Results = v2D.data.gs1Results;
                    FormattedOut = v2D.data.gs1Results.formattedOut;
                }

                if (v2D.data.extendedData != null)
                    ExtendedData = JsonConvert.DeserializeObject<ModuleData>(JsonConvert.SerializeObject(v2D.data.extendedData));

                break;

            case V275_REST_lib.Models.Report_InspectSector_OCR:
                Report_InspectSector_OCR ocr = (V275_REST_lib.Models.Report_InspectSector_OCR)report;
                Type = ocr.type;
                Text = ocr.data.text;
                Score = ocr.data.score;

                break;

            case V275_REST_lib.Models.Report_InspectSector_OCV:
                Report_InspectSector_OCV ocv = (V275_REST_lib.Models.Report_InspectSector_OCV)report;
                Type = ocv.type;
                Text = ocv.data.text;
                Score = ocv.data.score;

                break;

            case V275_REST_lib.Models.Report_InspectSector_Blemish:
                Report_InspectSector_Blemish blem = (V275_REST_lib.Models.Report_InspectSector_Blemish)report;
                Type = blem.type;
                BlemishCount = blem.data.blemishCount;

                break;

            case Results_QualifiedResult:

                Results_QualifiedResult v5 = (Results_QualifiedResult)report;
                Type = V5GetType(v5);
                SymbolType = V5GetSymbology(v5.type);
                DecodeText = v5.dataUTF8;
                //XDimension = v5.ppe;

                if (v5.grading != null)
                {
                    if (Type == "verify1D")
                    {
                        if (v5.grading.iso15416 == null || v5.grading.iso15416.overall == null)
                        {
                            OverallGradeString = "No Grade";
                            OverallGradeValue = 0;
                            OverallGradeLetter = "F";
                        }
                        else
                        {
                            OverallGradeString = $"{v5.grading.iso15416.overall.grade:f1}/00/600";
                            OverallGradeValue = v5.grading.iso15416.overall.grade;
                            OverallGradeLetter = V5GetLetter(v5.grading.iso15416.overall.letter);
                        }
                    }
                    else if (Type == "verify2D")
                    {
                        if (v5.grading.iso15415 == null)
                        {
                            OverallGradeString = "No Grade";
                            OverallGradeValue = 0;
                            OverallGradeLetter = "F";
                        }
                        else
                        {
                            OverallGradeString = $"{v5.grading.iso15415.overall.grade:f1}/00/600";
                            OverallGradeValue = v5.grading.iso15415.overall.grade;
                            OverallGradeLetter = V5GetLetter(v5.grading.iso15415.overall.letter);
                        }
                    }
                }
                else
                {
                    OverallGradeString = "No Grade";
                    OverallGradeValue = 0;
                    OverallGradeLetter = "F";
                }
                break;

            case List<string>:

                Type = ((List<string>)report).Find((e) => e.StartsWith("Cell size")) == null ? "verify1D" : "verify2D";

                foreach (string data in (List<string>)report)
                {
                    if (!data.Contains(','))
                        continue;

                    string[] spl1 = [data[..data.IndexOf(',')], data[(data.IndexOf(',') + 1)..]];
                    if (spl1[0].StartsWith("Symbology"))
                    {
                        SymbolType = L95xxGetSymbolType(spl1[1]);

                        //verify1D
                        if (SymbolType == "dataBar")
                        {
                            string item = ((List<string>)report).Find((e) => e.StartsWith("DataBar"));
                            if (item != null)
                            {
                                string[] spl2 = item.Split(',');

                                if (spl2.Length != 2)
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
                        string[] spl2 = spl1[1].Split('/');

                        if (spl2.Length < 3) continue;

                        OverallGradeValue = L95xxGetGrade(spl2[0]).value;// new Report_InspectSector_Common.Overallgrade() { grade = GetGrade(spl2[0]), _string = spl1[1] };
                        OverallGradeString = spl1[1];
                        OverallGradeLetter = L95xxGetGrade(spl2[0]).letter;

                        Aperture = L95xxParseFloat(spl2[1]);
                        continue;
                    }
                }
                break;
        }
    }

    private static string L95xxGetLetter(float value) =>
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

    private static float L95xxParseFloat(string value)
    {
        string digits = new(value.Trim().TakeWhile("0123456789.".Contains).ToArray());

        return float.TryParse(digits, out float val) ? val : 0;
    }
    private static Report_InspectSector_Common.Grade L95xxGetGrade(string data)
    {
        float tmp = L95xxParseFloat(data);

        return new Report_InspectSector_Common.Grade()
        {
            value = tmp,
            letter = L95xxGetLetter(tmp)
        };
    }

    private static string L95xxGetSymbolType(string value) =>
        value.Contains("Code 128")
        ? "code128"
        : value.Contains("UPC-A")
        ? "upcA"
        : value.Contains("UPC-B")
        ? "upcB"
        : value.Contains("EAN-13")
        ? "ean13"
        : value.Contains("EAN-8")
        ? "ean8"
        : value.Contains("DataBar")
        ? "dataBar"
        : value.Contains("Code 39")
        ? "code39"
        : value.Contains("Code 93")
        ? "code93"
        : value.StartsWith("GS1 QR")
        ? "qrCode"
        : value.StartsWith("Micro")
        ? "microQrCode"
        : value.Contains("Data Matrix")
        ? "dataMatrix"
        : value.Contains("Aztec")
        ? "aztec"
        : value.Contains("Codabar") ? "codaBar" : value.Contains("ITF") ? "i2of5" : value.Contains("PDF417") ? "pdf417" : "";

    private static string V5GetSymbology(string type) => type == "Datamatrix" ? "DataMatrix" : type;
    private static string V5GetType(Results_QualifiedResult Report) =>
        Report.Code128 != null
        ? "verify1D"
        : Report.Datamatrix != null
        ? "verify2D"
        : Report.QR != null ? "verify2D" : Report.PDF417 != null ? "verify1D" : Report.UPC != null ? "verify1D" : "Unknown";

    private static string V5GetLetter(int grade) =>
        grade switch
        {
            65 => "A",
            66 => "B",
            67 => "C",
            68 => "D",
            70 => "F",
            _ => throw new System.NotImplementedException(),
        };
}
