using System.Collections.Generic;

namespace LabelVal.LVS_95xx.Models;
public class FullReport
{
    public string Name { get; set; }
    public string Packet { get; set; }
    public Report Report { get; set; }
    public List<ReportData> ReportData { get; set; }
}
