using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelVal.V275.Models
{
    public class V275_Report_InspectSector_OCV
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
            public V275_Report_InspectSector_Common.Alarm[] alarms { get; set; }
            public string text { get; set; }
            public int score { get; set; }
            public Chardata[] charData { get; set; }
        }

        public class Chardata
        {
            public string id { get; set; }
            public int match { get; set; }
            public int artifactId { get; set; }
            public bool fail { get; set; }
            public bool warn { get; set; }
            public Bounds bounds { get; set; }
            public Bestmatch[] bestMatches { get; set; }
        }

        public class Bounds
        {
            public int left { get; set; }
            public int top { get; set; }
            public int width { get; set; }
            public int height { get; set; }
        }

        public class Bestmatch
        {
            public string id { get; set; }
            public int fontIndex { get; set; }
            public int match { get; set; }
            public int differenceArtifact { get; set; }
        }

    }
}
