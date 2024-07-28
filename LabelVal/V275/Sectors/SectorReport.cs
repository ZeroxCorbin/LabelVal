using LabelVal.Sectors.Interfaces;
using Newtonsoft.Json;
using V275_REST_lib.Models;

namespace LabelVal.V275.Sectors;

public class Report : IReport
{
    public string Type { get; set; }
    public string SymbolType { get; set; }

    public double Top { get; set; }
    public double Left { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public double AngleDeg { get; set; }

    public string DecodeText { get; set; }
    public string Text { get; set; }
    public int BlemishCount { get; set; }
    public double Score { get; set; }
    public double XDimension { get; set; }
    public double Aperture { get; set; }

    public string OverallGradeString { get; set; }
    public double OverallGradeValue { get; set; }
    public string OverallGradeLetter { get; set; }

    public Gs1results GS1Results { get; set; }
    public string FormattedOut { get; set; }
    public ModuleData ExtendedData { get; set; }

    public Report(object report)
    {
        switch (report)
        {
            case Report_InspectSector_Verify1D:
                Report_InspectSector_Verify1D v1D = (Report_InspectSector_Verify1D)report;
                Type = v1D.type;
                SymbolType = v1D.data.symbolType;

                DecodeText = v1D.data.decodeText;
                XDimension = v1D.data.xDimension;
                Aperture = v1D.data.aperture;

                Top = v1D.top;
                Left = v1D.left;
                Width = v1D.width;
                Height = v1D.height;
                AngleDeg = 0;

                OverallGradeString = v1D.data.overallGrade._string;
                OverallGradeValue = v1D.data.overallGrade.grade.value;
                OverallGradeLetter = v1D.data.overallGrade.grade.letter;

                if (v1D.data.gs1Results != null)
                {
                    GS1Results = new Gs1results
                    {
                        validated = v1D.data.gs1Results.validated,
                        input = v1D.data.gs1Results.input,
                        formattedOut = v1D.data.gs1Results.formattedOut,
                        fields = new Fields
                        {
                            _01 = v1D.data.gs1Results.fields._01,
                            _90 = v1D.data.gs1Results.fields._90,
                            _10 = v1D.data.gs1Results.fields._10
                        },
                        error = v1D.data.gs1Results.error
                    };
                    FormattedOut = v1D.data.gs1Results.formattedOut;
                }

                break;

            case Report_InspectSector_Verify2D:
                Report_InspectSector_Verify2D v2D = (Report_InspectSector_Verify2D)report;
                Type = v2D.type;
                SymbolType = v2D.data.symbolType;

                DecodeText = v2D.data.decodeText;
                XDimension = v2D.data.xDimension;
                Aperture = v2D.data.aperture;

                Top = v2D.top;
                Left = v2D.left;
                Width = v2D.width;
                Height = v2D.height;
                AngleDeg = 0;

                OverallGradeString = v2D.data.overallGrade._string;
                OverallGradeValue = v2D.data.overallGrade.grade.value;
                OverallGradeLetter = v2D.data.overallGrade.grade.letter;

                if (v2D.data.gs1Results != null)
                {
                    GS1Results = new Gs1results
                    {
                        validated = v2D.data.gs1Results.validated,
                        input = v2D.data.gs1Results.input,
                        formattedOut = v2D.data.gs1Results.formattedOut,
                        fields = new Fields
                        {
                            _01 = v2D.data.gs1Results.fields._01,
                            _90 = v2D.data.gs1Results.fields._90,
                            _10 = v2D.data.gs1Results.fields._10
                        },
                        error = v2D.data.gs1Results.error
                    };
                    FormattedOut = v2D.data.gs1Results.formattedOut;
                }

                if (v2D.data.extendedData != null)
                    ExtendedData = JsonConvert.DeserializeObject<ModuleData>(JsonConvert.SerializeObject(v2D.data.extendedData));

                break;

            case Report_InspectSector_OCR:
                Report_InspectSector_OCR ocr = (Report_InspectSector_OCR)report;
                Type = ocr.type;
                Text = ocr.data.text;
                Score = ocr.data.score;

                Top = ocr.top;
                Left = ocr.left;
                Width = ocr.width;
                Height = ocr.height;
                AngleDeg = 0;

                break;

            case Report_InspectSector_OCV:
                Report_InspectSector_OCV ocv = (Report_InspectSector_OCV)report;
                Type = ocv.type;
                Text = ocv.data.text;
                Score = ocv.data.score;

                Top = ocv.top;
                Left = ocv.left;
                Width = ocv.width;
                Height = ocv.height;
                AngleDeg = 0;

                break;

            case Report_InspectSector_Blemish:
                Report_InspectSector_Blemish blem = (Report_InspectSector_Blemish)report;
                Type = blem.type;
                BlemishCount = blem.data.blemishCount;

                Top = blem.top;
                Left = blem.left;
                Width = blem.width;
                Height = blem.height;
                AngleDeg = 0;

                break;
        }
    }
}
