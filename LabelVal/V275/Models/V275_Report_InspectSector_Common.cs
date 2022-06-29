using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelVal.V275.Models
{
    public class V275_Report_InspectSector_Common : Core.BaseViewModel
    { 

        public class Grade : Core.BaseViewModel
        {
            public float value { get; set; }
            public string letter { get; set; }
        }

        public class Overallgrade : Core.BaseViewModel
        {
            public Grade grade { get; set; }
            public string _string { get; set; }
        }

        public class GradeValue : Core.BaseViewModel
        {
            public Grade grade { get; set; }
            public int value { get; set; } = -1;
        }

        public class ValueResult : Core.BaseViewModel
        {
            public float value { get; set; }
            public string result { get; set; }
        }

        public class Value : Core.BaseViewModel
        {
            public int value { get; set; }
        }

        public class Decode : Core.BaseViewModel
        {
            public Grade grade { get; set; }
            //Verify2D only
            public int value { get; set; } = -1;

            //Verify1D only
            public ValueResult edgeDetermination { get; set; }
        }

        public class Alarm : Core.BaseViewModel
        {
            public string name { get; set; }
            public int category { get; set; }
            public SubAlarm data { get; set; }
            public Useraction userAction { get; set; }
        }

        public class SubAlarm : Core.BaseViewModel
        {
            public string text { get; set; }
            public int index { get; set; }
            public string subAlarm { get; set; }
            public string expected { get; set; }
        }

        public class Useraction : Core.BaseViewModel
        {
            public string action { get; set; }
            public string user { get; set; }
            public string note { get; set; }
        }

    }
}
