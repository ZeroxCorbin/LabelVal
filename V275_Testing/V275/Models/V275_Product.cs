using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V275_Testing.V275.Models
{
    public class V275_Product : Core.BaseViewModel
    {
        public string name { get; set; }
        public string part { get; set; }
        public Version version { get; set; }
        public string compileDate { get; set; }

        public class Version : Core.BaseViewModel
        {
            public int major { get; set; }
            public int minor { get; set; }
            public int service { get; set; }
            public int build { get; set; }
        }

    }
}
