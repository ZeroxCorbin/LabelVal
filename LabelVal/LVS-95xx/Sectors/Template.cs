using LabelVal.Sectors.Interfaces;
using V5_REST_Lib.Models;

namespace LabelVal.LVS_95xx.Sectors
{
    public class Template : ITemplate
    {
        public string Name { get; set; }
        public string Username { get; set; }

        public double Top { get; set; }
        public double Left { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double AngleDeg { get; set; }

        public System.Drawing.Point CenterPoint { get; set; }

        public double Orientation { get; set; }
        public string SymbologyType { get; set; }

        public TemplateMatchMode MatchSettings { get; set; }
        public BlemishMaskLayers BlemishMask { get; set; }

        public Template(ITemplate template)
        {
            Name = template.Name;
            Username = template.Username;

            Top = template.Top;
            Left = template.Left;
            Width = template.Width;
            Height = template.Height;
            AngleDeg = template.AngleDeg;

            CenterPoint = template.CenterPoint;

            Orientation = template.Orientation;
            SymbologyType = template.SymbologyType;

            MatchSettings = template.MatchSettings;
            BlemishMask = template.BlemishMask;
        }
    }
}
