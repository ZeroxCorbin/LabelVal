using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelVal.LVS_95xx.Models;
public class ReportData
{
    public int ReportID { get; set; }
    public int Category { get; set; }
    public int Sequence { get; set; }
    public string ParameterName { get; set; }
    public string ParameterValue { get; set; }
}
