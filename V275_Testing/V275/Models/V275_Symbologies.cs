using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V275_Testing.V275.Models
{
    public class V275_Symbologies : Core.BaseViewModel
    {
        public class Symbol : Core.BaseViewModel
        {
            public string symbolType { get; set; }
            public string symbology { get; set; }
            public string regionType { get; set; }
            public bool directional { get; set; }
        }

    }
}
