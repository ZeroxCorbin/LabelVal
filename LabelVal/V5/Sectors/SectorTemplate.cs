using LabelVal.Sectors.Classes;
using LabelVal.Sectors.Interfaces;
using Newtonsoft.Json.Linq;
using BarcodeVerification.lib.Extensions;

namespace LabelVal.V5.Sectors;

public class SectorTemplate : ISectorTemplate
{
    public JObject ToolList { get; }

    public string Name { get; set; }
    public string Username { get; set; }

    public string Version { get; set; }

    public double Top { get; set; }
    public double Left { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public double AngleDeg { get; set; }

    public System.Drawing.Point CenterPoint { get; set; }

    public string SymbologyType { get; set; }

    public double Orientation { get; set; }
    public TemplateMatchMode MatchSettings { get; set; }
    public BlemishMaskLayers BlemishMask { get; set; }

    public SectorTemplate(JObject decodeData, JObject toolList, string name, string version)
    {
        Version = version;

        ToolList = toolList;

        Name = name;
        Username = name;

        // Update the properties
        if (toolList.GetParameter<int>("SymbologyTool.regionList.Length") > 0 && toolList.GetParameter<string>("SymbologyTool.regionList[0].Region.shape.type") == "RectShape")
        {
            Left = toolList.GetParameter<double>("SymbologyTool.regionList[0].Region.shape.RectShape.x");
            Top = toolList.GetParameter<double>("SymbologyTool.regionList[0].Region.shape.RectShape.y");
            Width = toolList.GetParameter<double>("SymbologyTool.regionList[0].Region.shape.RectShape.width");
            Height = toolList.GetParameter<double>("SymbologyTool.regionList[0].Region.shape.RectShape.height");
        }
        else
        {
            if (decodeData.GetParameter<JObject>("region") != null)
            {
                Left = decodeData.GetParameter<double>("region.xOffset");
                Top = decodeData.GetParameter<double>("region.yOffset");
                Width = decodeData.GetParameter<double>("region.width");
                Height = decodeData.GetParameter<double>("region.height");
            }
        }

        AngleDeg = 0;

        CenterPoint = new System.Drawing.Point(decodeData.GetParameter<int>("x"), decodeData.GetParameter<int>("y"));

        Orientation = 0;
        SymbologyType = GetV5Symbology(decodeData);
        Version = version;
    }

    public SectorTemplate() { }

    private string GetV5Symbology(JObject report)
    {
        if (report.GetParameter<JObject>("Code128") != null)
            return "Code128";
        else if (report.GetParameter<JObject>("Datamatrix") != null)
            return "DataMatrix";
        else if (report.GetParameter<JObject>("QR") != null)
            return "QR";
        else return report.GetParameter<JObject>("PDF417") != null ? "PDF417" : report.GetParameter<JObject>("UPC") != null ? "UPC" : "Unknown";
    }
}
