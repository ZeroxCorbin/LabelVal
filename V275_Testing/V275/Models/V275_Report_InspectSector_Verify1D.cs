using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V275_Testing.V275.Models
{
    public class V275_Report_InspectSector_Verify1D : Core.BaseViewModel
    {
        public string name { get; set; }
        public string type { get; set; }
        public int top { get; set; }
        public int left { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public Data data { get; set; }

        public class Data : Core.BaseViewModel
        {
            public V275_Report_InspectSector_Common.Alarm[] alarms { get; set; }
            public string symbolType { get; set; }
            public string decodeText { get; set; }
            public string lengthUnit { get; set; }
            public float xDimension { get; set; }
            public float aperture { get; set; }
            public string lightSource { get; set; }
            public int artifactId { get; set; }

            public V275_Report_InspectSector_Common.Overallgrade overallGrade { get; set; }
            public V275_Report_InspectSector_Common.Decode decode { get; set; }

            public V275_Report_InspectSector_Common.GradeValue symbolContrast { get; set; }
            public V275_Report_InspectSector_Common.GradeValue edgeContrast { get; set; }
            public V275_Report_InspectSector_Common.GradeValue modulation { get; set; }
            public V275_Report_InspectSector_Common.GradeValue defects { get; set; }
            public V275_Report_InspectSector_Common.GradeValue decodability { get; set; }

            public V275_Report_InspectSector_Common.ValueResult quietZoneLeft { get; set; }
            public V275_Report_InspectSector_Common.ValueResult quietZoneRight { get; set; }

            public V275_Report_InspectSector_Common.GradeValue minimumReflectance { get; set; }
            public V275_Report_InspectSector_Common.Value maximumReflectance { get; set; }
            public Gs1symbolquality gs1SymbolQuality { get; set; }
            public Gs1results gs1Results { get; set; }
        }

        public class Gs1symbolquality : Core.BaseViewModel
        {
            public V275_Report_InspectSector_Common.ValueResult symbolXdim { get; set; }
            public V275_Report_InspectSector_Common.ValueResult symbolBarHeight { get; set; }
        }

        public class Gs1results : Core.BaseViewModel
        {
            public bool validated { get; set; }
            public string input { get; set; }
            public string formattedOut { get; set; }
            public Fields fields { get; set; }
            public string error { get; set; }
        }

        public class Fields : Core.BaseViewModel
        {
            public string _01 { get; set; }
        }



    }
}
