using LabelVal.Sectors.Interfaces;
using V5_REST_Lib.Models;

namespace LabelVal.LVS_95xx.Sectors
{
    public class Template : ITemplate
    {
        public string Name { get; set; }
        public string Username { get; set; }
        public int Top { get; set; }
        public System.Drawing.Point CenterPoint { get; set; }
        public string Symbology { get; set; }

        public TemplateMatchMode MatchSettings { get; set; }
        public BlemishMaskLayers BlemishMask { get; set; }

        public Template(ITemplate template)
        {
            Name = template.Name;
            Username = template.Username;
            Top = template.Top;
            CenterPoint = template.CenterPoint;

            Symbology = template.Symbology;

            MatchSettings = template.MatchSettings;
            BlemishMask = template.BlemishMask;
        }
    }
}
