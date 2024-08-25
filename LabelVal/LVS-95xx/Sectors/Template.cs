using L95xx_Lib.Models;
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

        public Template() { }
        public Template(ITemplate template)
        {
            if(template == null)
                return;
            
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

        public Template(FullReport report)
        {
            if (report == null)
                return;

            Name = report.Name;
            Username = report.Name;

            Top = report.Report.Y1;
            Left = report.Report.X1;
            Width = report.Report.SizeX;
            Height = report.Report.SizeY;
            //AngleDeg = report.Report.Angle;

            CenterPoint = new System.Drawing.Point(report.Report.X1 + (report.Report.SizeX / 2), report.Report.Y1 + (report.Report.SizeY / 2));

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
