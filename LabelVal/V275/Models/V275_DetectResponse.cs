using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelVal.V275.Models
{
    public class V275_DetectResponse
    {
            public bool active { get; set; }
            public Region region { get; set; }
            public Detection[] detections { get; set; }
        public class Region
        {
            public int x { get; set; }
            public int y { get; set; }
            public int width { get; set; }
            public int height { get; set; }
        }

        public class Detection
        {
            public string symbology { get; set; }
            public Region1 region { get; set; }
            public int orientation { get; set; }
        }

        public class Region1
        {
            public int x { get; set; }
            public int y { get; set; }
            public int width { get; set; }
            public int height { get; set; }
        }

    }
}
