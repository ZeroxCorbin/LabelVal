using LabelVal.Sectors.Classes;
using System.Drawing;

namespace LabelVal.Sectors.Interfaces;
public interface ISectorTemplate
{
    object Original { get; set; }
    string Name { get; set; }
    string Username { get; set; }

    string Version { get; set; }

    double Top { get; set; }
    double Left { get; set; }
    double Width { get; set; }
    double Height { get; set; }
    double AngleDeg { get; set; }

    double Orientation { get; set; }

    TemplateMatchMode MatchSettings { get; set; }
    BlemishMaskLayers BlemishMask { get; set; }
}

