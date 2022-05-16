using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V275_Testing.V275.Models
{
    public class V275_Report_InspectSector
    {

            public string name { get; set; }
            public string type { get; set; }
            public int top { get; set; }
            public int left { get; set; }
            public int width { get; set; }
            public int height { get; set; }
            public Data data { get; set; }

        public class Data : V275_Report_InspectSector_2D.Data
        {
            public int artifactId { get; set; }
            public GradeValue edgeContrast { get; set; }
            public GradeValue defects { get; set; }
            public GradeValue decodability { get; set; }
            public ValueResult quietZoneLeft { get; set; }
            public ValueResult quietZoneRight { get; set; }
        }

        public class GradeValue
        {
            public Grade grade { get; set; }
            public int value { get; set; }
        }

        public class Grade
        {
            public float value { get; set; }
            public string letter { get; set; }
        }

        public class ValueResult
        {
            public object value { get; set; }
            public string result { get; set; }
        }

        public class Overallgrade
        {
            public Grade grade { get; set; }
            public string _string { get; set; }
        }



        public class Decode
        {
            public Grade grade { get; set; }
            public Edgedetermination edgeDetermination { get; set; }
        }

        public class Edgedetermination
        {
            public int value { get; set; }
            public string result { get; set; }
        }

        public class Minimumreflectance
        {
            public Grade grade { get; set; }
            public int value { get; set; }
        }

        public class Maximumreflectance
        {
            public int value { get; set; }
        }





        public class Gs1symbolquality
        {
            public ValueResult symbolXdim { get; set; }
            public ValueResult symbolBarHeight { get; set; }
        }

        public class Gs1results
        {
            public bool validated { get; set; }
            public string input { get; set; }
            public string formattedOut { get; set; }
            public Fields fields { get; set; }
            public string error { get; set; }
        }

        public class Fields
        {
            public string _01 { get; set; }
        }

        public class Alarm
        {
            public string name { get; set; }
            public int category { get; set; }
            public Data1 data { get; set; }
            public Useraction userAction { get; set; }
        }

        public class Data1
        {
            public string text { get; set; }
            public int index { get; set; }
            public string subAlarm { get; set; }
            public string expected { get; set; }
        }

        public class Useraction
        {
            public string action { get; set; }
            public string user { get; set; }
            public string note { get; set; }
        }

    }
}
