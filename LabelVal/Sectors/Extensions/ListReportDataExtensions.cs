using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.Extensions;
using LabelVal.Sectors.Classes;
using LabelVal.Sectors.Interfaces;
using Lvs95xx.lib.Core.Models;
using System.Text;
using System.Windows;

namespace LabelVal.Sectors.Extensions;

public static class ListReportDataExtensions
{
    public static bool FallsWithin(this System.Drawing.Point point, ISector sector) =>
        //I want to know if the point falls within the sector
        point.X >= sector.Report.Left && point.X <= sector.Report.Left + sector.Report.Width &&
               point.Y >= sector.Report.Top && point.Y <= sector.Report.Top + sector.Report.Height;
    public static bool Contains(this System.Drawing.Point point, System.Drawing.Point contains, double radius = 50) => 
        Math.Sqrt(Math.Pow(point.X - contains.X, 2) + Math.Pow(point.Y - contains.Y, 2)) <= radius;


    public static string GetSectorReport(this ISector sector, string rollID, bool toClipboard = false, bool noPArams = false)
    {
        if (sector == null)
            return "";

        StringBuilder writer = new();

        _ = writer.AppendLine($"Name{delimiter}Value{delimiter}Suffix{delimiter}GradeValue{delimiter}Grade");
        _ = writer.AppendLine($"Device{delimiter}{sector.Report.Device.GetDescription()}{GetDelimiter(3)}");
        _ = writer.AppendLine($"Version{delimiter}{sector.Template.Version}{delimiter}{sector.Report.Units.GetDescription()}{GetDelimiter(2)}");
        //This will be a delimeted string with one delimeter.
        _ = writer.AppendLine($"Roll ID{delimiter}{rollID}{GetDelimiter(2)}");
        _ = writer.AppendLine($"Sector Name{delimiter}{sector.Template.Name}{GetDelimiter(3)}");
        _ = writer.AppendLine($"Sector Username{delimiter}{sector.Template.Username}{GetDelimiter(3)}");
        _ = writer.AppendLine($"Region Type{delimiter}{sector.Report.RegionType.GetDescription()}{GetDelimiter(3)}");
        _ = writer.AppendLine($"Standard{delimiter}{sector.Report.Standard.GetDescription()}{GetDelimiter(3)}");
        _ = writer.AppendLine($"{sector.Report.SymbolType}{delimiter}\"{sector.Report.DecodeText.Replace("\r", "").Replace("\n", "")}\"{GetDelimiter(2)}{sector.Report.OverallGrade.Grade.Value}{delimiter}{sector.Report.OverallGrade.Grade.Letter}");
        _ = writer.AppendLine(sector.Report.OverallGrade.ToDelimitedString(delimiter));
        _ = writer.AppendLine($"X Dimension{delimiter}{sector.Report.XDimension}{GetDelimiter(3)}");
        _ = writer.AppendLine($"Angle{delimiter}{sector.Report.AngleDeg}{GetDelimiter(3)}");

        _ = writer.AppendLine($"GS1 Table{delimiter}{sector.Report.GS1Table.GetDescription()}{GetDelimiter(3)}");
        _ = writer.AppendLine($"GS1 Results{delimiter}\"{sector.Report.GS1Results?.FormattedOut.Replace("\r", "").Replace("\n", "")}\"{GetDelimiter(3)}");

        _ = writer.AppendLine($"Has Error{delimiter}{(sector.IsWarning || sector.IsError ? "1" : "0")}{GetDelimiter(3)}");
           

        if (!noPArams)
            foreach (BarcodeVerification.lib.ISO.ParameterTypes.IParameterValue grade in sector.SectorDetails.Parameters)
            {
                _ = writer.AppendLine(grade.ToDelimitedString(delimiter)).Replace("<units>", sector.Report.Units.GetDescription());
            }

        if (toClipboard)
            Clipboard.SetText(writer.ToString());

        return writer.ToString();

    }
    private static char delimiter => (char)SectorOutputSettings.CurrentDelimiter;
    private static string GetDelimiter(int count) => new(delimiter, count);

    public static string GetSectorsReport(this System.Collections.ObjectModel.ObservableCollection<ISector> sectors, string rollID, bool toClipboard = false)
    {
        System.Text.StringBuilder sb = new();

        //Add each parameter to the list of parameters.
        List<Parameters> parameters = [];

        if (SectorOutputSettings.CurrentIncludeParameters == SectorOutputIncludeParameters.All)
        {
            foreach (Parameters param in Enum.GetValues(typeof(Parameters)))
            {
                if (Parameters.CommonParameters.Contains(param) || param is Parameters.Unknown)
                    continue;

                parameters.Add(param);
            }
        }
        else if (SectorOutputSettings.CurrentIncludeParameters == SectorOutputIncludeParameters.Relevant)
        {
            foreach (ISector sector in sectors)
            {
                foreach (BarcodeVerification.lib.ISO.ParameterTypes.IParameterValue parameter in sector.SectorDetails.Parameters)
                {
                    if (!parameters.Contains(parameter.Parameter))
                        parameters.Add(parameter.Parameter);
                }
            }
        }

        Dictionary<Parameters, List<string>> parameterCsvs = [];

        foreach (Parameters parameter in parameters)
        {
            foreach (ISector sector in sectors)
            {
                BarcodeVerification.lib.ISO.ParameterTypes.IParameterValue param = sector.SectorDetails.Parameters.FirstOrDefault(p => p.Parameter == parameter);
                if (param != null)
                {
                    if (!parameterCsvs.ContainsKey(parameter))
                        parameterCsvs.Add(parameter, []);
                    parameterCsvs[parameter].Add(param.ToDelimitedString(delimiter) + delimiter);
                }
                else
                {
                    if (!parameterCsvs.ContainsKey(parameter))
                        parameterCsvs.Add(parameter, []);
                    parameterCsvs[parameter].Add($"{parameter.GetDescription()}{delimiter}---{GetDelimiter(4)}");
                }
            }
        }

        Dictionary<ISector, List<string>> sectorHeaderLines = [];

        foreach (ISector sector in sectors)
        {
            if (!sectorHeaderLines.ContainsKey(sector))
                sectorHeaderLines.Add(sector, []);

            string csv = ListReportDataExtensions.GetSectorReport(sector, rollID, false, true);
            string[] lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                sectorHeaderLines[sector].Add(line.Trim() + delimiter);
            }
        }

        int linecnt = 0;
        foreach (KeyValuePair<ISector, List<string>> keyValuePair in sectorHeaderLines)
        {
            if (keyValuePair.Value.Count > linecnt)
                linecnt = keyValuePair.Value.Count;
        }

        for (int i = 0; i < linecnt; i++)
        {
            foreach (KeyValuePair<ISector, List<string>> keyValuePair in sectorHeaderLines)
            {
                _ = keyValuePair.Value.Count > i ? sb.Append(keyValuePair.Value[i]) : sb.Append($"{GetDelimiter(5)}");
            }
            if (i < linecnt)
                _ = sb.AppendLine();
        }

        foreach (KeyValuePair<Parameters, List<string>> keyValuePair in parameterCsvs)
        {
            foreach (string csv in keyValuePair.Value)
            {
                _ = sb.Append(csv).Replace("<units>", sectors[0].Report.Units.GetDescription());
            }
            _ = sb.AppendLine();
        }

        if (toClipboard)
            Clipboard.SetText(sb.ToString());
        return sb.ToString();
    }

    //public static string GetParameter(this List<ReportData> report, AvailableParameters parameter, AvailableDevices device, AvailableSymbologies symbology)
    //{
    //    string path = parameter.GetParameterPath(device, symbology);
    //    return report.GetParameter(path);
    //}
    //public static string GetParameter(this List<ReportData> report, string key) => report.Find((e) => e.ParameterName.Equals(key))?.ParameterValue;
    //public static List<string> GetParameters(this List<ReportData> report, string key) => report.FindAll((e) => e.ParameterName.StartsWith(key)).Select((e) => e.ParameterValue).ToList();
}
