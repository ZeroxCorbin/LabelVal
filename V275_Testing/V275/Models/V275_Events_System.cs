using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V275_Testing.V275.Models
{
    public class V275_Events_System : Core.BaseViewModel
    {


        public string time { get; set; }
        public string source { get; set; }
        public int item { get; set; }
        public string name { get; set; }
        public Data data { get; set; }

        public class Data : Core.BaseViewModel
        {
            public string token { get; set; }
            public string id { get; set; }
            public string accessLevel { get; set; }
            public string state { get; set; }

            public int position { get; set; }
            public int repeat { get; set; }
            public int repeatWidth { get; set; }
            public int repeatHeight { get; set; }
            public int sectorCount { get; set; }

            public string fromState { get; set; }
            public string toState { get; set; }

            public Detection[] detections { get; set; }
        }

        public class Detection : Core.BaseViewModel
        {
            public string symbology { get; set; }
            public Region region { get; set; }
            public int orientation { get; set; }

            public class Region : Core.BaseViewModel
            {
                public int x { get; set; }
                public int y { get; set; }
                public int width { get; set; }
                public int height { get; set; }
            }
        }


        public class Rootobject : Core.BaseViewModel
        {
            public Event _event { get; set; }
        }

        public class Event : Core.BaseViewModel
        {
            public string time { get; set; }
            public string source { get; set; }
            public int item { get; set; }
            public string name { get; set; }
            public Data data { get; set; }
        }

    }
}
