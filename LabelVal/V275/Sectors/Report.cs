using LabelVal.Sectors.Interfaces;
using Newtonsoft.Json;
using V275_REST_lib.Models;

namespace LabelVal.V275.Sectors;

public class Report : IReport
{
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

                break;

            case Report_InspectSector_OCV:
                Report_InspectSector_OCV ocv = (Report_InspectSector_OCV)report;
                Type = ocv.type;
                Text = ocv.data.text;
                Score = ocv.data.score;

                break;

            case Report_InspectSector_Blemish:
                Report_InspectSector_Blemish blem = (Report_InspectSector_Blemish)report;
                Type = blem.type;
                BlemishCount = blem.data.blemishCount;

                break;
        }
    }
}
