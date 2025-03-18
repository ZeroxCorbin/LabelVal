using LabelVal.Sectors.Interfaces;
using V5_REST_Lib.Models;
using Lvs95xx.lib.Core.Controllers;
using LabelVal.Sectors.Classes;

namespace LabelVal.LVS_95xx.Sectors
{
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

        public SectorTemplate() { }

        public SectorTemplate(FullReport report, string version)
        {
            if (report == null)
                return;

            Original = report.Report;

            Version = version;
            Name = report.Name;
            Username = report.Name;

            Top = report.Report.Y1;
            Left = report.Report.X1;
            Width = report.Report.SizeX;
            Height = report.Report.SizeY;
            AngleDeg = 0;

            Orientation = 0;
        }
    }
}
