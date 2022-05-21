using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V275_Testing.V275.Models
{
    public class V275_Report_InspectSector_Common
    { 

        public class Grade
        {
            public float value { get; set; }
            public string letter { get; set; }
        }

        public class Overallgrade
        {
            public Grade grade { get; set; }
            public string _string { get; set; }
        }

        public class GradeValue
        {
            public Grade grade { get; set; }
            public int value { get; set; } = -1;
        }

        public class ValueResult
        {
            public float value { get; set; }
            public string result { get; set; }
        }

        public class Value
        {
            public int value { get; set; }
        }

        public class Decode
        {
            public Grade grade { get; set; }
            //Verify2D only
            public int value { get; set; } = -1;

            //Verify1D only
            public ValueResult edgeDetermination { get; set; }
        }

        public class Alarm
        {
            public string name { get; set; }
            public int category { get; set; }
            public SubAlarm data { get; set; }
            public Useraction userAction { get; set; }
        }

        public class SubAlarm
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
