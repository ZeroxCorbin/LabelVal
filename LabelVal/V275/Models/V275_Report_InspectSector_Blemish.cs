using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelVal.V275.Models
{
    public class V275_Report_InspectSector_Blemish : Core.BaseViewModel
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
            public int blemishCount { get; set; }
            public int reportCount { get; set; }
            public Blemish[] blemishList { get; set; }
        }

        public class Blemish : Core.BaseViewModel
        {
            public string type { get; set; }
            public int top { get; set; }
            public int left { get; set; }
            public int width { get; set; }
            public int height { get; set; }
            public float maximumDimension { get; set; }
            public float residualArea { get; set; }
            public int maxTolerancePercent { get; set; }
            public int artifactId { get; set; }
        }

    }
}
