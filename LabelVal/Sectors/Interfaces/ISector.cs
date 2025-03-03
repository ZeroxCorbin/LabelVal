using CsvHelper;
using LabelVal.Sectors.Classes;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;

namespace LabelVal.Sectors.Interfaces;

public partial interface ISector
{
    ITemplate Template { get; }
    IReport Report { get; }

    ISectorDetails SectorDetails { get; }
    bool IsWarning { get; }
    bool IsError { get; }

    StandardsTypes DesiredStandard { get; }
    Gs1TableNames DesiredGS1Table { get; }
    bool IsWrongStandard { get; }

    bool IsFocused { get; set; }
    bool IsMouseOver { get; set; }

    static bool FallsWithin(ISector sector, System.Drawing.Point point) =>
        point.X >= sector.Template.Left
            && point.X <= sector.Template.Left + sector.Template.Width &&
            point.Y >= sector.Template.Top
            && point.Y <= sector.Template.Top + sector.Template.Height;

    class CSVResults
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public string Grade { get; set; }
        public string GradeValue { get; set; }
    }

    static void CopyCSVToClipboard(ISector sector)
    {
        if (sector == null)
            return;

        List<CSVResults> compiled = [];

        // Add the main report
        compiled.Add(new CSVResults
        {
            Name = CamelCaseToWords(sector.Report.SymbolType),
            Value = sector.Report.DecodeText,
            Grade = sector.Report.OverallGradeLetter,
            GradeValue = sector.Report.OverallGradeString
        });

        compiled.Add(new CSVResults
        {
            Name = "Units",
            Value = sector.Report.Units
        });

        //Add the the details
        compiled.Add(new CSVResults
        {
            Name = "X Dimension",
            Value = sector.Report.XDimension.ToString()
        });

        compiled.Add(new CSVResults
        {
            Name = "Aperture",
            Value = sector.Report.Aperture.ToString()
        });

        compiled.Add(new CSVResults
        {
            Name = "Angle",
            Value = sector.Report.AngleDeg.ToString()
        });

        foreach (GradeValue grade in sector.SectorDetails.GradeValues)
        {
            compiled.Add(new CSVResults
            {
                Name = CamelCaseToWords(grade.Name),
                Value = grade.Value.ToString(),
                Grade = grade.Grade.Letter,
                GradeValue = grade.Grade.Value.ToString()
            });
        }

        foreach (Value_ grade in sector.SectorDetails.Values)
        {
            compiled.Add(new CSVResults
            {
                Name = CamelCaseToWords(grade.Name),
                Value = grade.Value.ToString(),

            });
        }

        foreach (Grade grade in sector.SectorDetails.Gs1Grades)
        {
            compiled.Add(new CSVResults
            {
                Name = CamelCaseToWords(grade.Name),
                Value = grade.Value.ToString(),
                GradeValue = grade.Value.ToString(),
                Grade = grade.Letter
            });
        }

        using StringWriter writer = new();
        using CsvWriter csv = new(writer, CultureInfo.InvariantCulture);
        csv.WriteHeader(typeof(CSVResults));
        csv.NextRecord();
        csv.WriteRecords(compiled);
        csv.NextRecord();

        Clipboard.SetText(writer.ToString());

    }

    private static string CamelCaseToWords(string camelCase)
    {
        if (string.IsNullOrEmpty(camelCase))
            return string.Empty;

        //ignore all uppcase strings
        if (camelCase.ToUpper() == camelCase)
            return camelCase;

        StringBuilder sb = new();
        _ = sb.Append(char.ToUpper(camelCase[0]));
        for (int i = 1; i < camelCase.Length; i++)
        {
            if (char.IsUpper(camelCase[i]))
                _ = sb.Append(' ');
            _ = sb.Append(camelCase[i]);
        }
        return sb.ToString();
    }
}




