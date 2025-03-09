using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.GS1;
using LabelVal.Sectors.Classes;
using LabelVal.Sectors.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using V275_REST_Lib.Models;

namespace LabelVal.V275.Sectors;

public class Report : IReport
{
    public object Original { get; set; }

    public AvailableRegionTypes Type { get; set; }

    public double Top { get; set; }
    public double Left { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public double AngleDeg { get; set; }

    public AvailableSymbologies SymbolType { get; set; }
    public double XDimension { get; set; }
    public double Aperture { get; set; }
    public string Units { get; set; }

    public string DecodeText { get; set; }

    public string OverallGradeString { get; set; }
    public double OverallGradeValue { get; set; }
    public string OverallGradeLetter { get; set; }

    public AvailableStandards? Standard { get; set; }
    public AvailableTables? GS1Table { get; set; }

    //GS1
    public Gs1Results GS1Results { get; set; }

    //OCR
    public string Text { get; set; }
    public double Score { get; set; }

    //Blemish
    public int BlemishCount { get; set; }

    //V275 2D module data
    public ModuleData ExtendedData { get; set; }

    public Report(object report)
    {
        Original = report;

        switch (report)
        {
            case Report_InspectSector_Verify1D:
                Report_InspectSector_Verify1D v1D = (Report_InspectSector_Verify1D)report;

                SymbolType = v1D.data.symbolType.GetSymbology(AvailableDevices.V275);
                Type = SymbolType.GetSymbologyRegionType(AvailableDevices.V275);

                Top = v1D.top;
                Left = v1D.left;
                Width = v1D.width;
                Height = v1D.height;
                AngleDeg = 0;

                XDimension = v1D.data.xDimension;
                Aperture = v1D.data.aperture;
                Units = v1D.data.lengthUnit;

                DecodeText = v1D.data.decodeText;

                OverallGradeString = v1D.data.overallGrade._string;
                OverallGradeValue = v1D.data.overallGrade.grade.value;
                OverallGradeLetter = v1D.data.overallGrade.grade.letter;

                if (v1D.data.gs1Results != null)
                {
                    List<string> fld = [];
                    foreach (JProperty f in v1D.data.gs1Results.fields)
                        fld.Add($"({f.Name}) {f.Value.ToString().Trim('{', '}', ' ')}");

                    GS1Results = new Gs1Results
                    {
                        Validated = v1D.data.gs1Results.validated,
                        Input = v1D.data.gs1Results.input,
                        FormattedOut = v1D.data.gs1Results.formattedOut,
                        Fields = fld,
                        Error = v1D.data.gs1Results.error
                    };
                }

                break;

            case Report_InspectSector_Verify2D:
                Report_InspectSector_Verify2D v2D = (Report_InspectSector_Verify2D)report;

                SymbolType = v2D.data.symbolType.GetSymbology(AvailableDevices.V275);
                Type = SymbolType.GetSymbologyRegionType(AvailableDevices.V275);

                Top = v2D.top;
                Left = v2D.left;
                Width = v2D.width;
                Height = v2D.height;
                AngleDeg = 0;

                XDimension = v2D.data.xDimension;
                Aperture = v2D.data.aperture;
                Units = v2D.data.lengthUnit;

                DecodeText = v2D.data.decodeText;

                OverallGradeString = v2D.data.overallGrade._string;
                OverallGradeValue = v2D.data.overallGrade.grade.value;
                OverallGradeLetter = v2D.data.overallGrade.grade.letter;

                if (v2D.data.gs1Results != null)
                {
                    List<string> fld = [];
                    foreach (JProperty f in v2D.data.gs1Results.fields)
                        fld.Add($"({f.Name}) {f.Value.ToString().Trim('{', '}', ' ')}");

                    GS1Results = new Gs1Results
                    {
                        Validated = v2D.data.gs1Results.validated,
                        Input = "^" + v2D.data.gs1Results.input.Replace("\u001d", "^"),
                        FormattedOut = v2D.data.gs1Results.formattedOut,
                        Fields = fld,
                        Error = v2D.data.gs1Results.error
                    };
                }

                if (v2D.data.extendedData != null)
                    ExtendedData = JsonConvert.DeserializeObject<ModuleData>(JsonConvert.SerializeObject(v2D.data.extendedData));

                break;

            case Report_InspectSector_OCR:
                Report_InspectSector_OCR ocr = (Report_InspectSector_OCR)report;

                SymbolType = AvailableSymbologies.OCR;
                Type = SymbolType.GetSymbologyRegionType(AvailableDevices.V275);

                Top = ocr.top;
                Left = ocr.left;
                Width = ocr.width;
                Height = ocr.height;
                AngleDeg = 0;

                Text = ocr.data.text;
                Score = ocr.data.score;
                break;

            case Report_InspectSector_OCV:
                Report_InspectSector_OCV ocv = (Report_InspectSector_OCV)report;

                SymbolType = AvailableSymbologies.OCV;
                Type = SymbolType.GetSymbologyRegionType(AvailableDevices.V275);

                Top = ocv.top;
                Left = ocv.left;
                Width = ocv.width;
                Height = ocv.height;
                AngleDeg = 0;

                Text = ocv.data.text;
                Score = ocv.data.score;
                break;

            case Report_InspectSector_Blemish:
                Report_InspectSector_Blemish blem = (Report_InspectSector_Blemish)report;

                SymbolType = AvailableSymbologies.Blemish;
                Type = SymbolType.GetSymbologyRegionType(AvailableDevices.V275);

                Top = blem.top;
                Left = blem.left;
                Width = blem.width;
                Height = blem.height;
                AngleDeg = 0;

                BlemishCount = blem.data.blemishCount;
                break;
        }
    }
}
