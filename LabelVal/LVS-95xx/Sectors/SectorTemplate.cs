using LabelVal.Sectors.Interfaces;
using V5_REST_Lib.Models;
using Lvs95xx.lib.Core.Controllers;
using LabelVal.Sectors.Classes;

namespace LabelVal.LVS_95xx.Sectors
{
    public class SectorTemplate : ISectorTemplate
    {
        public string Name { get; set; }
        public string Username { get; set; }

        public string Version { get; set; }

        public double Top { get; set; }
        public double Left { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double AngleDeg { get; set; }

        public double Orientation { get; set; }

        public string SymbologyType { get; set; }

        public TemplateMatchMode MatchSettings { get; set; }
        public BlemishMaskLayers BlemishMask { get; set; }

        public SectorTemplate() { }
        public SectorTemplate(ISectorTemplate template, string version)
        {
            if(template == null)
                return;
            
            Version = version;

            Name = template.Name;
            Username = template.Username;

            Top = template.Top;
            Left = template.Left;
            Width = template.Width;
            Height = template.Height;
            AngleDeg = template.AngleDeg;

            Orientation = template.Orientation;

            SymbologyType = template.SymbologyType;

            MatchSettings = template.MatchSettings;
            BlemishMask = template.BlemishMask;
        }

        public SectorTemplate(FullReport report, string version)
        {
            if (report == null)
                return;

            Version = version;

            Name = report.Name;
            Username = report.Name;

            Top = report.Report.Y1;
            Left = report.Report.X1;
            Width = report.Report.SizeX;
            Height = report.Report.SizeY;
            //AngleDeg = report.Report.Angle;

            Version = version;

            //Orientation = template.Orientation;

            //SymbologyType = report.Report.;

            //MatchSettings = template.MatchSettings;
            //BlemishMask = template.BlemishMask;
        }

        private string GetSymbology(ResultsAlt.Decodedata report)
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
}
