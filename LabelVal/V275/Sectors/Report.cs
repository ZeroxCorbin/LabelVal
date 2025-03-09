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
    public AvailableSymbologies SymbolType { get; set; }

    public double Top { get; set; }
    public double Left { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public double AngleDeg { get; set; }


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

    public Report(JObject report)
    {
        Original = report;

        SymbolType = report["data"]["symbolType"].ToString().GetSymbology(AvailableDevices.V275);
        Type = SymbolType.GetSymbologyRegionType(AvailableDevices.V275);

        Top = report["top"].ToObject<double>();
        Left = report["left"].ToObject<double>();
        Width = report["width"].ToObject<double>();
        Height = report["height"].ToObject<double>();
        AngleDeg = 0;

        XDimension = report["data"]["xDimension"].ToObject<double>();
        Aperture = report["data"]["aperture"].ToObject<double>();
        Units = report["data"]["lengthUnit"].ToString();

        DecodeText = report["data"]["decodeText"].ToString();

        OverallGradeString = report["data"]["overallGrade"]["string"].ToString();
        OverallGradeValue = report["data"]["overallGrade"]["grade"]["value"].ToObject<double>();
        OverallGradeLetter = report["data"]["overallGrade"]["grade"]["letter"].ToString();

        if (report["data"]["gs1Results"] != null)
        {
            List<string> fld = [];
            foreach (JProperty f in report["data"]["gs1Results"]["fields"])
                fld.Add($"({f.Name}) {f.Value.ToString().Trim('{', '}', ' ')}");
            GS1Results = new Gs1Results
            {
                Validated = report["data"]["gs1Results"]["validated"].ToObject<bool>(),
                Input = report["data"]["gs1Results"]["input"].ToString(),
                FormattedOut = report["data"]["gs1Results"]["formattedOut"].ToString(),
                Fields = fld,
                Error = report["data"]["gs1Results"]["error"].ToString()
            };
        }

        if (report["data"]["extendedData"] != null)
            ExtendedData = JsonConvert.DeserializeObject<ModuleData>(JsonConvert.SerializeObject(report["data"]["extendedData"]));

        //switch (report)
        //{
        //    case Report_InspectSector_OCR:
        //        Report_InspectSector_OCR ocr = (Report_InspectSector_OCR)report;

        //        SymbolType = AvailableSymbologies.OCR;
        //        Type = SymbolType.GetSymbologyRegionType(AvailableDevices.V275);

        //        Top = ocr.top;
        //        Left = ocr.left;
        //        Width = ocr.width;
        //        Height = ocr.height;
        //        AngleDeg = 0;

        //        Text = ocr.data.text;
        //        Score = ocr.data.score;
        //        break;

        //    case Report_InspectSector_OCV:
        //        Report_InspectSector_OCV ocv = (Report_InspectSector_OCV)report;

        //        SymbolType = AvailableSymbologies.OCV;
        //        Type = SymbolType.GetSymbologyRegionType(AvailableDevices.V275);

        //        Top = ocv.top;
        //        Left = ocv.left;
        //        Width = ocv.width;
        //        Height = ocv.height;
        //        AngleDeg = 0;

        //        Text = ocv.data.text;
        //        Score = ocv.data.score;
        //        break;

        //    case Report_InspectSector_Blemish:
        //        Report_InspectSector_Blemish blem = (Report_InspectSector_Blemish)report;

        //        SymbolType = AvailableSymbologies.Blemish;
        //        Type = SymbolType.GetSymbologyRegionType(AvailableDevices.V275);

        //        Top = blem.top;
        //        Left = blem.left;
        //        Width = blem.width;
        //        Height = blem.height;
        //        AngleDeg = 0;

        //        BlemishCount = blem.data.blemishCount;
        //        break;
        //}
    }
}
