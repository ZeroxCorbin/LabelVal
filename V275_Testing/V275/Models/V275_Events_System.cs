using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V275_Testing.V275.Models
{
    public class V275_Events_System
    {


            public string time { get; set; }
            public string source { get; set; }
            public int item { get; set; }
            public string name { get; set; }
            public Data data { get; set; }

        public class Data
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
        }



    }
}
