using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.Extensions;
using BinaryTools.Elf;
using LabelVal.Sectors.Interfaces;
using LabelVal.Sectors.Output;
using Newtonsoft.Json.Linq;
using System.Text;

namespace LabelVal.Sectors.Extensions;

public static class ListReportDataExtensions
{
    public static bool FallsWithin(this System.Drawing.Point point, ISector sector) =>
        //I want to know if the point falls within the sector
        point.X >= sector.Report.Left && point.X <= sector.Report.Left + sector.Report.Width &&
               point.Y >= sector.Report.Top && point.Y <= sector.Report.Top + sector.Report.Height;
    public static bool Contains(this System.Drawing.Point point, System.Drawing.Point contains, double radius = 50) =>
        Math.Sqrt(Math.Pow(point.X - contains.X, 2) + Math.Pow(point.Y - contains.Y, 2)) <= radius;

    public static string GetDelimetedSectorReport(this ISector sector, string rollID, bool noPArams = false)
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
        _ = writer.AppendLine($"Application Standard{delimiter}{sector.Report.ApplicationStandard.GetDescription()}{GetDelimiter(3)}");
        _ = writer.AppendLine($"GS1 Table{delimiter}{sector.Report.GS1Table.GetDescription()}{GetDelimiter(3)}");
        _ = writer.AppendLine($"Grading Standard{delimiter}{sector.Report.GradingStandard.GetDescription()}{GetDelimiter(3)}");
        _ = writer.AppendLine($"{sector.Report.Symbology}{delimiter}\"{sector.Report.DecodeText.Replace("\r", "").Replace("\n", "")}\"{GetDelimiter(2)}{sector.Report.OverallGrade.Grade.LetterValue}{delimiter}{sector.Report.OverallGrade.Grade.Letter}");
        _ = writer.AppendLine(sector.Report.OverallGrade.ToDelimitedString(delimiter));
        _ = writer.AppendLine($"Angle{delimiter}{sector.Report.AngleDeg}{GetDelimiter(3)}");

        //_ = writer.AppendLine($"GS1 Results{delimiter}\"{sector.Report.GS1Results?.FormattedOut.Replace("\r", "").Replace("\n", "")}\"{GetDelimiter(3)}");
        _ = writer.AppendLine($"Has Error{delimiter}{(sector.IsWarning || sector.IsError ? "1" : "0")}{GetDelimiter(3)}");


        if (!noPArams)
        {
            List<Parameters> parameters = GetParameters(new System.Collections.ObjectModel.ObservableCollection<ISector> { sector });
            foreach (Parameters param in parameters)
            {
                var grade = sector.SectorDetails.Parameters.FirstOrDefault(p => p.Parameter == param);
                if (grade != null)
                    _ = writer.AppendLine(grade.ToDelimitedString(delimiter)).Replace("<units>", sector.Report.Units.GetDescription());
                else
                    _ = writer.AppendLine($"{param.GetDescription()}{delimiter}---{GetDelimiter(4)}");

            }
        }
        return writer.ToString();
    }

    public static JObject GetJsonSectorReport(this ISector sector, string rollID, bool noPArams = false)
    {
        if (sector == null)
            return [];
        var jObject = new JObject
        {
            ["Device"] = sector.Report.Device.GetDescription(),
            ["Version"] = sector.Template.Version,
            ["Units"] = sector.Report.Units.GetDescription(),
            ["RollID"] = rollID,
            ["SectorName"] = sector.Template.Name,
            ["SectorUsername"] = sector.Template.Username,
            ["ApplicationStandard"] = sector.Report.ApplicationStandard.GetDescription(),
            ["GS1Table"] = sector.Report.GS1Table.GetDescription(),
            ["GradingStandard"] = sector.Report.GradingStandard.GetDescription(),
            ["Symbology"] = sector.Report.Symbology.ToString(),
            ["DecodeText"] = sector.Report.DecodeText,
            ["OverallGradeValue"] = sector.Report.OverallGrade.Grade.LetterValue,
            ["OverallGrade"] = sector.Report.OverallGrade.Grade.Letter,
            ["OverallGradeDetails"] = JObject.FromObject(sector.Report.OverallGrade),
            ["AngleDeg"] = sector.Report.AngleDeg,
            ["HasError"] = (sector.IsWarning || sector.IsError) ? 1 : 0
        };

        if (!noPArams)
        {
            List<Parameters> parameters = GetParameters(new System.Collections.ObjectModel.ObservableCollection<ISector> { sector });

            foreach(Parameters param in parameters)
            {
                var grade = sector.SectorDetails.Parameters.FirstOrDefault(p => p.Parameter == param);
                if (grade != null)
                    jObject[grade.Parameter.ToString()] = JObject.FromObject(grade);
                else
                    jObject[param.ToString()] = null;
            }
        }

        return jObject;
    }

    private static char delimiter => (char)SectorOutputSettings.CurrentDelimiter;
    private static string GetDelimiter(int count) => new(delimiter, count);

    public static string GetDelimetedSectorsReport(this System.Collections.ObjectModel.ObservableCollection<ISector> sectors, string rollID)
    {
        List<Parameters> parameters = GetParameters(sectors);

        System.Text.StringBuilder sb = new();

        Dictionary<ISector, List<string>> sectorHeaderLines = [];

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

        foreach (ISector sector in sectors)
        {
            if (!sectorHeaderLines.ContainsKey(sector))
                sectorHeaderLines.Add(sector, []);

            var csv = ListReportDataExtensions.GetDelimetedSectorReport(sector, rollID, true);
            var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                sectorHeaderLines[sector].Add(line.Trim() + delimiter);
            }
        }

        var linecnt = 0;
        foreach (KeyValuePair<ISector, List<string>> keyValuePair in sectorHeaderLines)
        {
            if (keyValuePair.Value.Count > linecnt)
                linecnt = keyValuePair.Value.Count;
        }

        for (var i = 0; i < linecnt; i++)
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
            foreach (var csv in keyValuePair.Value)
            {
                _ = sb.Append(csv).Replace("<units>", sectors[0].Report.Units.GetDescription());
            }
            _ = sb.AppendLine();
        }
        return sb.ToString();
    }

    public static JArray GetJsonSectorsReport(this System.Collections.ObjectModel.ObservableCollection<ISector> sectors, string rollID)
    {
        var jarray = new Newtonsoft.Json.Linq.JArray();
        foreach (ISector sector in sectors)
        {
            jarray.Add(sector.GetJsonSectorReport(rollID, false));
        }

        return jarray;
    }

    private static List<Parameters> GetParameters(System.Collections.ObjectModel.ObservableCollection<ISector> sectors)
    {
        //Add each parameter to the list of parameters.
        List<Parameters> parameters = [];

        if (SectorOutputSettings.CurrentIncludeParameters == SectorOutputIncludeParameters.All)
        {
            foreach (Parameters param in Enum.GetValues(typeof(Parameters)))
            {
                if (param is Parameters.Unknown or BarcodeVerification.lib.Common.Parameters.OverallGrade or Parameters.GradingStandard or Parameters.ApplicationStandard or Parameters.GS1Table)
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
                    if (parameter.Parameter is Parameters.Unknown or BarcodeVerification.lib.Common.Parameters.OverallGrade or Parameters.GradingStandard or Parameters.ApplicationStandard or Parameters.GS1Table)
                        continue;
                    parameters.Add(parameter.Parameter);
                }
            }
        }
        else if (SectorOutputSettings.CurrentIncludeParameters == SectorOutputIncludeParameters.Focused)
        {
            parameters = App.Settings.GetValue("SelectedParameters", new List<Parameters>());
        }

        return parameters;
    }

    //public static string GetParameter(this List<ReportData> report, AvailableParameters parameter, AvailableDevices device, AvailableSymbologies symbology)
    //{
    //    string path = parameter.GetPath(device, symbology);
    //    return report.GetParameter(path);
    //}
    //public static string GetParameter(this List<ReportData> report, string key) => report.Find((e) => e.ParameterName.Equals(key))?.ParameterValue;
    //public static List<string> GetParameters(this List<ReportData> report, string key) => report.FindAll((e) => e.ParameterName.StartsWith(key)).Select((e) => e.ParameterValue).ToList();
}
