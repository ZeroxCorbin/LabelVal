using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.Extensions;
using BarcodeVerification.lib.ISO;
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

    public static string GetSectorCsv(this ISector sector, bool toClipboard = false, bool noPArams = false)
    {
        if (sector == null)
            return "";

        StringBuilder writer = new();


        _ = writer.AppendLine("Name,Value,Suffix,GradeValue,Grade");
        _ = writer.AppendLine($"Version,{sector.Template.Version},{sector.Report.Units.GetDescription()},,");
        _ = writer.AppendLine($"{sector.Report.SymbolType},{sector.Report.DecodeText},,{sector.Report.OverallGrade.Grade.Value},{sector.Report.OverallGrade.Grade.Letter}");
        _ = writer.AppendLine(sector.Report.OverallGrade.ToCSVString());
        _ = writer.AppendLine($"X Dimension,{sector.Report.XDimension},,,");
        _ = writer.AppendLine($"Angle,{sector.Report.AngleDeg},,,");




        if (!noPArams)
            foreach (BarcodeVerification.lib.ISO.ParameterTypes.IParameterValue grade in sector.SectorDetails.Parameters)
            {
                _ = writer.AppendLine(grade.ToCSVString()).Replace("<units>", sector.Report.Units.GetDescription());
            }

        //List<string> compiled = [];

        //compiled.Add("Name,Value,Grade,GradeValue");

        //compiled.Add($"{sector.Template.Version},{sector.Report.Units},,");
        //compiled.Add($"{sector.Report.SymbolType},{sector.Report.DecodeText},{sector.Report.OverallGrade.Grade.Letter},{sector.Report.OverallGrade.Value}");
        //compiled.Add($"X Dimension,{sector.Report.XDimension},,");
        //compiled.Add($"Aperture,{sector.Report.Aperture},,");
        //compiled.Add($"Angle,{sector.Report.AngleDeg},,");
        //foreach (var grade in sector.SectorDetails.Parameters)
        //{
        //    compiled.Add(grade.ToCSVString());
        //}
        //compiled.Add(new CSVResults
        //{
        //    Name = "Version",
        //    Value = sector.Template.Version
        //});

        //compiled.Add(new CSVResults
        //{
        //    Name = "Units",
        //    Value = sector.Report.Units.ToString()
        //});

        //// Add the main report
        //compiled.Add(new CSVResults
        //{
        //    Name = sector.Report.SymbolType.ToString(),
        //    Value = sector.Report.DecodeText,
        //    Grade = sector.Report.OverallGrade.Grade.Letter,
        //    GradeValue = sector.Report.OverallGrade.Value
        //});

        ////Add the the details
        //compiled.Add(new CSVResults
        //{
        //    Name = "X Dimension",
        //    Value = sector.Report.XDimension.ToString()
        //});

        //compiled.Add(new CSVResults
        //{
        //    Name = "Aperture",
        //    Value = sector.Report.Aperture.ToString()
        //});

        //compiled.Add(new CSVResults
        //{
        //    Name = "Angle",
        //    Value = sector.Report.AngleDeg.ToString()
        //});

        //foreach (var grade in sector.SectorDetails.Parameters)
        //{
        //    compiled.Add(new CSVResults
        //    {
        //        Name = CamelCaseToWords(grade.Name),
        //        Value = grade.Value.ToString(),
        //        Grade = grade.Grade.Letter,
        //        GradeValue = grade.Grade.Value.ToString()
        //    });
        //}

        //using StringWriter writer = new();
        //using CsvWriter csv = new(writer, CultureInfo.InvariantCulture);
        //csv.WriteHeader(typeof(CSVResults));
        //csv.NextRecord();
        //csv.WriteRecords(compiled);
        //csv.NextRecord();

        if (toClipboard)
            Clipboard.SetText(writer.ToString());

        return writer.ToString();

    }

    public static string GetSectorsCSV(this System.Collections.ObjectModel.ObservableCollection<ISector> sectors, bool toClipboard = false)
    {
        System.Text.StringBuilder sb = new();

        //Add each parameter to the list of parameters.
        List<AvailableParameters> parameters = [];
        foreach (ISector sector in sectors)
        {
            foreach (BarcodeVerification.lib.ISO.ParameterTypes.IParameterValue parameter in sector.SectorDetails.Parameters)
            {
                if (!parameters.Contains(parameter.Parameter))
                    parameters.Add(parameter.Parameter);
            }
        }
        //Sort the parameters.
        parameters.Sort();

        Dictionary<AvailableParameters, List<string>> parameterCsvs = [];

        foreach (AvailableParameters parameter in parameters)
        {
            foreach (ISector sector in sectors)
            {
                BarcodeVerification.lib.ISO.ParameterTypes.IParameterValue param = sector.SectorDetails.Parameters.FirstOrDefault(p => p.Parameter == parameter);
                if (param != null)
                {
                    if (!parameterCsvs.ContainsKey(parameter))
                        parameterCsvs.Add(parameter, []);
                    parameterCsvs[parameter].Add(param.ToCSVString() + ',');
                }
                else
                {
                    if (!parameterCsvs.ContainsKey(parameter))
                        parameterCsvs.Add(parameter, []);
                    parameterCsvs[parameter].Add($"{parameter.GetDescription()},---,,,,");
                }
            }
        }

        Dictionary<ISector, List<string>> sectorHeaderLines = [];

        foreach (ISector sector in sectors)
        {
            if (!sectorHeaderLines.ContainsKey(sector))
                sectorHeaderLines.Add(sector, []);

            string csv = ListReportDataExtensions.GetSectorCsv(sector, false, true);
            string[] lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                sectorHeaderLines[sector].Add(line.Trim() + ',');
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
                _ = keyValuePair.Value.Count > i ? sb.Append(keyValuePair.Value[i]) : sb.Append(",,,,,");
            }
            if (i < linecnt)
                _ = sb.AppendLine();
        }
                ;

        foreach (KeyValuePair<AvailableParameters, List<string>> keyValuePair in parameterCsvs)
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


    public static string GetParameter(this List<ReportData> report, AvailableParameters parameter, AvailableDevices device, AvailableSymbologies symbology)
    {
        string path = parameter.GetParameterPath(device, symbology);
        return report.GetParameter(path);
    }
    public static string GetParameter(this List<ReportData> report, string key) => report.Find((e) => e.ParameterName.Equals(key))?.ParameterValue;
    public static List<string> GetParameters(this List<ReportData> report, string key) => report.FindAll((e) => e.ParameterName.StartsWith(key)).Select((e) => e.ParameterValue).ToList();
}
