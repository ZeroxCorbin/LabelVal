using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V275_Testing.V275.Models
{
    public class V275_Report
    {
       public Inspectlabel inspectLabel { get; set; }

        public class Inspectlabel
        {
            public int repeat { get; set; }
            public int voidRepeat { get; set; }
            public int iteration { get; set; }
            public string result { get; set; }
            public int width { get; set; }
            public int height { get; set; }
            public Useraction userAction { get; set; }
            public object[] inspectSector { get; set; }
            public object[] ioLines { get; set; }
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
