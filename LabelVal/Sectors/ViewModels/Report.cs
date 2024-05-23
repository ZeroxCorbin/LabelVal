using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using V275_REST_lib.Models;
using V5_REST_Lib.Models;

namespace LabelVal.Sectors.ViewModels
{
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
                    var v1D = (V275_REST_lib.Models.Report_InspectSector_Verify1D)report;
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
                    var v2D = (V275_REST_lib.Models.Report_InspectSector_Verify2D)report;
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
                    var ocr = (V275_REST_lib.Models.Report_InspectSector_OCR)report;
                    Type = ocr.type;
                    Text = ocr.data.text;
                    Score = ocr.data.score;

                    break;

                case V275_REST_lib.Models.Report_InspectSector_OCV:
                    var ocv = (V275_REST_lib.Models.Report_InspectSector_OCV)report;
                    Type = ocv.type;
                    Text = ocv.data.text;
                    Score = ocv.data.score;

                    break;

                case V275_REST_lib.Models.Report_InspectSector_Blemish:
                    var blem = (V275_REST_lib.Models.Report_InspectSector_Blemish)report;
                    Type = blem.type;
                    BlemishCount = blem.data.blemishCount;

                    break;

                case Results_QualifiedResult:

                    var v5 = (Results_QualifiedResult)report;
                    Type = V5GetType(v5);
                    SymbolType = V5GetSymbology(v5.type);
                    DecodeText = v5.dataUTF8;
                    //XDimension = v5.ppe;

                    if (v5.grading != null)
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
                            OverallGradeLetter = L95xxGetGrade(spl2[0]).letter;

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
            if (value.Contains("Code 128"))
                return "code128";

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

        private string V5GetSymbology(string type)
        {
            if (type == "Datamatrix")
                return "DataMatrix";
            else
                return type;
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

        private string V5GetLetter(int grade) => grade switch
        {
            65 => "A",
            66 => "B",
            67 => "C",
            68 => "D",
            70 => "F",
            _ => throw new System.NotImplementedException(),
        };
    }
}
