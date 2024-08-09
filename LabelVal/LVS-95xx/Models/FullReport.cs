using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelVal.LVS_95xx.Models;
public class FullReport
{
    public Report Report { get; set; }
    public List<ReportData> ReportData { get; set; }
}
