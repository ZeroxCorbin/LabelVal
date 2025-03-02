using CsvHelper;
using LabelVal.LVS_95xx.Sectors;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;

namespace LabelVal.Sectors.Interfaces;

public enum StandardsTypes
{
    [Description("None")]
    None,
    [Description("Unsupported")]
    Unsupported, //Unsupported table
    [Description("ISO/IEC 15415 & 15416")]
    ISO15415_15416,
    [Description("ISO/IEC 15415 (2D)")]
    ISO15415,
    [Description("ISO/IEC 15416 (1D)")]
    ISO15416,
    [Description("GS1")]
    GS1,
    [Description("OCR/OCV")]
    OCR_OCV,
    [Description("ISO/IEC 29158 (DPM)")]
    ISO29158,
}

public enum GS1TableNames
{
    [Description("None")]
    None,
    [Description("Unsupported")]
    Unsupported, //Unsupported table
    [Description("1")]
    _1, //Trade items scanned in General Retail POS and NOT General Distribution.
    [Description("1.8200")]
    _1_8200, //AI (8200)
    [Description("2")]
    _2, //Trade items scanned in General Distribution.
    [Description("3")]
    _3, //Trade items scanned in General Retail POS and General Distribution.
    [Description("4")]
    _4,
    [Description("5")]
    _5,
    [Description("6")]
    _6,
    [Description("7.1")]
    _7_1,
    [Description("7.2")]
    _7_2,
    [Description("7.3")]
    _7_3,
    [Description("7.4")]
    _7_4,
    [Description("8")]
    _8,
    [Description("9")]
    _9,
    [Description("10")]
    _10,
    [Description("11")]
    _11,
    [Description("12.1")]
    _12_1,
    [Description("12.2")]
    _12_2,
    [Description("12.3")]
    _12_3
}

public partial interface ISector
{
    ITemplate Template { get; }
    IReport Report { get; }

    ISectorDetails SectorDetails { get; }
    bool IsWarning { get; }
    bool IsError { get; }

    StandardsTypes DesiredStandard { get; }
    GS1TableNames DesiredGS1Table { get; }
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
        sb.Append(char.ToUpper(camelCase[0]));
        for (int i = 1; i < camelCase.Length; i++)
        {
            if (char.IsUpper(camelCase[i]))
                sb.Append(' ');
            sb.Append(camelCase[i]);
        }
        return sb.ToString();
    }
}