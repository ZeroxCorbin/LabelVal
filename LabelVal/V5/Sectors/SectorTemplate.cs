using LabelVal.Sectors.Classes;
using LabelVal.Sectors.Interfaces;
using Newtonsoft.Json.Linq;
using BarcodeVerification.lib.Extensions;

namespace LabelVal.V5.Sectors;

public class SectorTemplate : ISectorTemplate
{
    public JObject Original { get; set; }

    public string Name { get; set; }
    public string Username { get; set; }

    public string Version { get; set; }

    public double Top { get; set; }
    public double Left { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public double AngleDeg { get; set; }

    public double Orientation { get; set; }

    public TemplateMatchMode MatchSettings { get; set; }
    public BlemishMaskLayers BlemishMask { get; set; }

    public SectorTemplate(JObject report, JObject template, string name, string version)
    {
        if (report == null || template == null)
            return;

        Original = template;
        Version = version;
        Name = name;
        Username = name;

        int toolSlot = report.GetParameter<int>("toolSlot") - 1;

        // Update the properties
        if (template.GetParameter<JArray>($"response.data.job.toolList[{toolSlot}].SymbologyTool.regionList").Count > 0 && template.GetParameter<string>($"response.data.job.toolList[{toolSlot}].SymbologyTool.regionList[0].Region.shape.type") == "RectShape")
        {   
            Width = template.GetParameter<double>($"response.data.job.toolList[{toolSlot}].SymbologyTool.regionList[0].Region.shape.RectShape.width");
            Height = template.GetParameter<double>($"response.data.job.toolList[{toolSlot}].SymbologyTool.regionList[0].Region.shape.RectShape.height");
            Left = template.GetParameter<double>($"response.data.job.toolList[{toolSlot}].SymbologyTool.regionList[0].Region.shape.RectShape.x");
            Top = template.GetParameter<double>($"response.data.job.toolList[{toolSlot}].SymbologyTool.regionList[0].Region.shape.RectShape.y");

        }
        else
        {
            if (report.GetParameter<JObject>("region") != null)
            {
                Left = report.GetParameter<double>("region.xOffset");
                Top = report.GetParameter<double>("region.yOffset");
                Width = report.GetParameter<double>("region.width");
                Height = report.GetParameter<double>("region.height");
            }
        }

        AngleDeg = 0;
        Orientation = 0;

    }

    public SectorTemplate() { }
}
