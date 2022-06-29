using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelVal.V275.Models
{
    public class V275_Event_Base : Core.BaseViewModel
    {
        public Event _event { get; set; }

        public class Event : Core.BaseViewModel
        {
            public string time { get; set; }
            public string source { get; set; }
            public int item { get; set; }
            public string name { get; set; }
            public object data { get; set; }
        }

    }
}
