using LabelVal.Sectors.Interfaces;
using V5_REST_Lib.Models;

namespace LabelVal.V5.Sectors;

public class Template : ITemplate
{
    public ResultsAlt.Decodedata DecodeData { get; }
    public Config.Toollist ToolList { get; }

    public string Name { get; set; }
    public string Username { get; set; }
    public int Top { get; set; }
    public System.Drawing.Point CenterPoint { get; set; }
    public string Symbology { get; set; }

    public TemplateMatchMode MatchSettings { get; set; }
    public BlemishMaskLayers BlemishMask { get; set; }

    public Template(ResultsAlt.Decodedata decodeData, Config.Toollist toolList, string name)
    {
        DecodeData = decodeData;
        ToolList = toolList;

        Name = name;
        Username = name;

        if (!DecodeData.read)
        {
            Top = toolList.SymbologyTool.regionList[0].Region.shape.RectShape.y;
            CenterPoint = new System.Drawing.Point(toolList.SymbologyTool.regionList[0].Region.shape.RectShape.x, toolList.SymbologyTool.regionList[0].Region.shape.RectShape.y);
            Symbology = "Unknown";
        }
        else
        {
            Top = DecodeData.angleDeg > 45 ? DecodeData.y : DecodeData.y;
            CenterPoint = new System.Drawing.Point(DecodeData.x, DecodeData.y);
            Symbology = GetV5Symbology(DecodeData);
        }
    }

    public Template() { }

    private string GetV5Symbology(ResultsAlt.Decodedata Report)
    {
        if (Report.Code128 != null)
            return "Code128";
        else if (Report.Datamatrix != null)
            return "DataMatrix";
        else if (Report.QR != null)
            return "QR";
        else if (Report.PDF417 != null)
            return "PDF417";
        else return Report.UPC != null ? "UPC" : "Unknown";
    }
}
