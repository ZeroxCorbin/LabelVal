using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelVal.V275.Models
{
    public class V275_Report : Core.BaseViewModel
    {
       public Inspectlabel inspectLabel { get; set; }

        public class Inspectlabel : Core.BaseViewModel
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
