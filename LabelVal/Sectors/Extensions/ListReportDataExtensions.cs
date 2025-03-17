using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.Extensions;
using BarcodeVerification.lib.ISO;
using LabelVal.Sectors.Interfaces;
using Lvs95xx.lib.Core.Models;

namespace LabelVal.Sectors.Extensions;

public static class ListReportDataExtensions
{
    public static bool FallsWithin(this System.Drawing.Point point, ISector sector) =>
        //I want to know if the point falls within the sector
        point.X >= sector.Report.Left && point.X <= sector.Report.Left + sector.Report.Width &&
               point.Y >= sector.Report.Top && point.Y <= sector.Report.Top + sector.Report.Height;

    public static string GetParameter(this List<ReportData> report, AvailableParameters parameter, AvailableDevices device, AvailableSymbologies symbology)
    {
        string path = parameter.GetParameterPath(device, symbology);
        return report.GetParameter(path);
    }
    public static string GetParameter(this List<ReportData> report, string key) => report.Find((e) => e.ParameterName.Equals(key))?.ParameterValue;
    public static List<string> GetParameters(this List<ReportData> report, string key) => report.FindAll((e) => e.ParameterName.StartsWith(key)).Select((e) => e.ParameterValue).ToList();
}
