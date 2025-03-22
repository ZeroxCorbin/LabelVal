using LabelVal.Sectors.Interfaces;
using BarcodeVerification.lib.Extensions;
using LabelVal.Sectors.Classes;
using Newtonsoft.Json.Linq;

namespace LabelVal.LVS_95xx.Sectors
{
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

        public SectorTemplate() { }

        public SectorTemplate(JObject template, string version)
        {
            Version = version;
            Original = template;

            if (template == null)
                return;

            Name = template.GetParameter<string>("Name");
            Username = Name;

            Top = template.GetParameter<double>("Report.Y1");
            Left = template.GetParameter<double>("Report.X1");
            Width = template.GetParameter<double>("Report.SizeX");
            Height = template.GetParameter<double>("Report.SizeY");
            AngleDeg = 0;
            Orientation = 0;
        }
    }
}
