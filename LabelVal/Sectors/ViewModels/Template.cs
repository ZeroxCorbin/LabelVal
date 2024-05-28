using NHibernate.Hql.Ast;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using V5_REST_Lib.Models;

namespace LabelVal.Sectors.ViewModels
{
    public class Template
    {
        public class TemplateMatchMode
        {
            public int MatchMode { get; set; }
            public int UserDefinedDataTrueSize { get; set; }
            public string FixedText { get; set; }
        }

        public class BlemishMaskLayers
        {
            public V275_REST_lib.Models.Job.Layer[] Layers { get; set; }
        }

        public V275_REST_lib.Models.Job.Sector V275Sector { get; }
        public Results_QualifiedResult V5Results { get; }

        public string Name { get; set; }
        public string Username { get; set; }
        public int Top { get; set; }
        public string Symbology { get; set; }


        public TemplateMatchMode MatchSettings { get; set; }
        public BlemishMaskLayers BlemishMask { get; set; }

        public Template(V275_REST_lib.Models.Job.Sector sectorTemplate)
        {
            V275Sector = sectorTemplate;

            Name = sectorTemplate.name;
            Username = sectorTemplate.username;
            Top = sectorTemplate.top;
            Symbology = sectorTemplate.symbology;

            if (sectorTemplate.matchSettings != null)
                MatchSettings = new Template.TemplateMatchMode
                {
                    MatchMode = sectorTemplate.matchSettings.matchMode,
                    UserDefinedDataTrueSize = sectorTemplate.matchSettings.userDefinedDataTrueSize,
                    FixedText = sectorTemplate.matchSettings.fixedText
                };


            BlemishMask = new Template.BlemishMaskLayers
            {
                Layers = sectorTemplate.blemishMask?.layers
            };
        }

        public Template(Results_QualifiedResult Report, string name)
        {
            V5Results = Report;

            Name = name;
            Username = name;

            if (Report.angleDeg > 45)
                Top = Report.y;
            else
                Top = Report.y;

            Symbology = GetV5Symbology(Report);

        }

        public Template(Template template)
        {
            Name = template.Name;
            Username = template.Username;
            Top = template.Top;
            Symbology = template.Symbology;

            MatchSettings = template.MatchSettings;
            BlemishMask = template.BlemishMask;
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
