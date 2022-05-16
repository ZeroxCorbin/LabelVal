using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V275_Testing.V275.Models
{
    public interface IV275_Report_InspectSector
    {

        string name { get; set; }
        string type { get; set; }
        int top { get; set; }
        int left { get; set; }
        int width { get; set; }
        int height { get; set; }
        object data { get; set; }

        V275_Report_InspectSector_Verify1D GetVerify1D();

        V275_Report_InspectSector_Verify1D GetVerify2D();
    }
}
