using System;

namespace LabelVal.LVS_95xx.Models;
public class Report
{
    public int ReportID { get; set; }
    public string SectorID { get; set; }
    public DateTime LclTime { get; set; }
    public DateTime GmtTime { get; set; }
    public int X1 { get; set; }
    public int Y1 { get; set; }
    public int SizeX { get; set; }
    public int SizeY { get; set; }
    public byte[] Thumbnail { get; set; }
    public string Reference { get; set; }
    public string OverallGrade { get; set; }
    public string DecodedText { get; set; }
}
