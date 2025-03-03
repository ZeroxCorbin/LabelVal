using LabelVal.Sectors.Classes;
using LabelVal.Sectors.Interfaces;
using System;
using System.Windows;
using System.Windows.Media;
using V5_REST_Lib.Models;

namespace LabelVal.V5.Sectors;

public class Template : ITemplate
{
    public Config.Toollist ToolList { get; }

    public string Name { get; set; }
    public string Username { get; set; }

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

    public Template(ResultsAlt.Decodedata report, Config.Toollist toolList, string name)
    {
        ToolList = toolList;

        Name = name;
        Username = name;

        // Update the properties
        if(toolList.SymbologyTool.regionList.Length > 0 && toolList.SymbologyTool.regionList[0].Region.shape.type == "RectShape")
        {
            Left = toolList.SymbologyTool.regionList[0].Region.shape.RectShape.x;
            Top = toolList.SymbologyTool.regionList[0].Region.shape.RectShape.y;
            Width = toolList.SymbologyTool.regionList[0].Region.shape.RectShape.width;
            Height = toolList.SymbologyTool.regionList[0].Region.shape.RectShape.height;
        }
        else
        {
            if (report.region != null)
            {
                Left = report.region.xOffset;
                Top = report.region.yOffset;
                Width = report.region.width;
                Height = report.region.height;
            }
        }

        AngleDeg = 0;

        CenterPoint = new System.Drawing.Point(report.x, report.y);

        Orientation = 0;
        SymbologyType = GetV5Symbology(report);
    }

    public Template() { }

    private string GetV5Symbology(ResultsAlt.Decodedata report)
    {
        if (report.Code128 != null)
            return "Code128";
        else if (report.Datamatrix != null)
            return "DataMatrix";
        else if (report.QR != null)
            return "QR";
        else if (report.PDF417 != null)
            return "PDF417";
        else return report.UPC != null ? "UPC" : "Unknown";
    }
}
