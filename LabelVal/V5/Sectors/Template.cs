using LabelVal.Sectors.Interfaces;
using V5_REST_Lib.Models;

namespace LabelVal.V5.Sectors
{
    public class Template : ITemplate
    {
        public Results_QualifiedResult V5Results { get; }

        public string Name { get; set; }
        public string Username { get; set; }
        public int Top { get; set; }
        public int Center { get; set; }
        public System.Drawing.Point CenterPoint { get; set; }
        public string Symbology { get; set; }

        public TemplateMatchMode MatchSettings { get; set; }
        public BlemishMaskLayers BlemishMask { get; set; }

        public Template(Results_QualifiedResult Report, string name)
        {
            V5Results = Report;

            Name = name;
            Username = name;

            if (Report.angleDeg > 45)
                Top = Report.y;
            else
                Top = Report.y;

            Center = Report.y + Report.x;
            CenterPoint = new System.Drawing.Point(Report.x, Report.y);

            Symbology = GetV5Symbology(Report);

        }


        public Template() { }

        private string GetV5Symbology(Results_QualifiedResult Report)
        {
            if (Report.Code128 != null)
                return "Code128";
            else if (Report.Datamatrix != null)
                return "DataMatrix";
            else if (Report.QR != null)
                return "QR";
            else if (Report.PDF417 != null)
                return "PDF417";
            else if (Report.UPC != null)
                return "UPC";
            else
                return "Unknown";
        }

    }
}
