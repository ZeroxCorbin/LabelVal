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

        // Create the Rect
        Rect rect = new(
            toolList.SymbologyTool.regionList[0].Region.shape.RectShape.x ,
            toolList.SymbologyTool.regionList[0].Region.shape.RectShape.y,
            toolList.SymbologyTool.regionList[0].Region.shape.RectShape.width,
            toolList.SymbologyTool.regionList[0].Region.shape.RectShape.height);

        // Create the RotateTransform
        RotateTransform rotateTransform = new(0, toolList.SymbologyTool.regionList[0].Region.shape.RectShape.x- toolList.SymbologyTool.regionList[0].Region.shape.RectShape.width / 2,
            toolList.SymbologyTool.regionList[0].Region.shape.RectShape.y - toolList.SymbologyTool.regionList[0].Region.shape.RectShape.height / 2);

        // Apply the rotation to the Rect
        Point topLeft = rotateTransform.Transform(new Point(rect.Left, rect.Top));
        Point topRight = rotateTransform.Transform(new Point(rect.Right, rect.Top));
        Point bottomLeft = rotateTransform.Transform(new Point(rect.Left, rect.Bottom));
        Point bottomRight = rotateTransform.Transform(new Point(rect.Right, rect.Bottom));

        // Calculate the new bounding box
        double newLeft = Math.Min(Math.Min(topLeft.X, topRight.X), Math.Min(bottomLeft.X, bottomRight.X));
        double newTop = Math.Min(Math.Min(topLeft.Y, topRight.Y), Math.Min(bottomLeft.Y, bottomRight.Y));
        double newRight = Math.Max(Math.Max(topLeft.X, topRight.X), Math.Max(bottomLeft.X, bottomRight.X));
        double newBottom = Math.Max(Math.Max(topLeft.Y, topRight.Y), Math.Max(bottomLeft.Y, bottomRight.Y));

        // Update the properties
        Top = newTop;
        Left = newLeft;
        Width = newRight - newLeft;
        Height = newBottom - newTop;
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
