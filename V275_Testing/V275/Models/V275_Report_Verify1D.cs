using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V275_Testing.V275.Models
{
    internal class V275_Report_Verify1D
    {
        public string name { get; set; }
        public string type { get; set; }
        public int top { get; set; }
        public int left { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public Data data { get; set; }

        public class Data
        {
            public V275_Report.Alarm[] alarms { get; set; }
            public string symbolType { get; set; }
            public string decodeText { get; set; }
            public string lengthUnit { get; set; }
            public float xDimension { get; set; }
            public float aperture { get; set; }
            public string lightSource { get; set; }
            public int artifactId { get; set; }
            public Overallgrade overallGrade { get; set; }
            public Symbolcontrast symbolContrast { get; set; }
            public Unusederrorcorrection unusedErrorCorrection { get; set; }
            public Cwyeild cwYeild { get; set; }
            public Cwprintquality cwPrintQuality { get; set; }
        }

        public class Overallgrade
        {
            public Grade grade { get; set; }
            public string _string { get; set; }
        }

        public class Grade
        {
            public float value { get; set; }
            public string letter { get; set; }
        }

        public class Symbolcontrast
        {
            public Grade1 grade { get; set; }
            public int value { get; set; }
        }

        public class Grade1
        {
            public float value { get; set; }
            public string letter { get; set; }
        }

        public class Unusederrorcorrection
        {
            public Grade2 grade { get; set; }
            public int value { get; set; }
        }

        public class Grade2
        {
            public float value { get; set; }
            public string letter { get; set; }
        }

        public class Cwyeild
        {
            public Grade3 grade { get; set; }
            public int value { get; set; }
        }

        public class Grade3
        {
            public float value { get; set; }
            public string letter { get; set; }
        }

        public class Cwprintquality
        {
            public Grade4 grade { get; set; }
            public int value { get; set; }
        }

        public class Grade4
        {
            public float value { get; set; }
            public string letter { get; set; }
        }



    }
}
