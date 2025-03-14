using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.Extensions;
using BarcodeVerification.lib.ISO;
using Lvs95xx.lib.Core.Models;
using Newtonsoft.Json.Linq;

namespace LabelVal.Sectors.Extensions;

public static class ListReportDataExtensions
{
    public static string GetParameter(this List<ReportData> report, AvailableParameters parameter, AvailableDevices device, AvailableSymbologies symbology)
    {
        string path = parameter.GetParameterPath(device, symbology);
        return report.GetParameter(path);
    }
    public static string GetParameter(this List<ReportData> report, string key) => report.Find((e) => e.ParameterName.Equals(key))?.ParameterValue;
    public static List<string> GetParameters(this List<ReportData> report, string key) => report.FindAll((e) => e.ParameterName.StartsWith(key)).Select((e) => e.ParameterValue).ToList();
}
