using BarcodeVerification.lib.Extensions;
using LabelVal.Sectors.Classes;
using LabelVal.Sectors.Interfaces;
using Newtonsoft.Json.Linq;

namespace LabelVal.V275.Sectors;

public class SectorTemplate : ISectorTemplate
{
    public object Original { get; set; }

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

    public SectorTemplate(JObject template, string version)
    {  
        
        Original = template;

        Version = version;

        Name = template.GetParameter<string>("name");
        Username = template.GetParameter<string>("username");

        Top = template.GetParameter<double>("top");
        Left = template.GetParameter<double>("left");
        Width = template.GetParameter<double>("width");
        Height = template.GetParameter<double>("height");
        AngleDeg = template.GetParameter<double>("angle");

        Orientation = template.GetParameter<double>("orientation");

        if (template.GetParameter<JObject>("matchSettings") != null)
        {
            MatchSettings = new TemplateMatchMode
            {
                MatchMode = template.GetParameter<int>("matchSettings.matchMode"),
                UserDefinedDataTrueSize = template.GetParameter<int>("matchSettings.userDefinedDataTrueSize"),
                FixedText = template.GetParameter<string>("matchSettings.fixedText")
            };
        }

        //BlemishMask = new BlemishMaskLayers
        //{
        //    Layers = template.GetParameter<List<string>>("blemishMask.layers")
        //};

    }

}
