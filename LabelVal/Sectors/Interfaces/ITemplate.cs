using System.Drawing;
using V5_REST_Lib.Models;

namespace LabelVal.Sectors.Interfaces;

public class TemplateMatchMode
{
    public int MatchMode { get; set; }
    public int UserDefinedDataTrueSize { get; set; }
    public string FixedText { get; set; }
}

public class BlemishMaskLayers
{
    public V275_REST_lib.Models.Job.Layer[] Layers { get; set; }
}

public interface ITemplate
{
    string Name { get; set; }
    string Username { get; set; }

    double Top { get; set; }
    double Left { get; set; }
    double Width { get; set; }
    double Height { get; set; }
    double AngleDeg { get; set; }

    double Orientation { get; set; }

    Point CenterPoint { get; set; }

    string SymbologyType { get; set; }

    TemplateMatchMode MatchSettings { get; set; }
    BlemishMaskLayers BlemishMask { get; set; }
}


