using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.GS1;
using BarcodeVerification.lib.ISO;
using System.Text;
using System.Windows;
using Wpf.lib.Extentions;

namespace LabelVal.Sectors.Interfaces;

public partial interface ISector
{
    ISectorTemplate Template { get; }
    ISectorReport Report { get; }

    ISectorParameters SectorDetails { get; }
    bool IsWarning { get; }
    bool IsError { get; }

    AvailableStandards DesiredStandard { get; }
    AvailableTables DesiredGS1Table { get; }
    bool IsWrongStandard { get; }

    bool IsFocused { get; set; }
    bool IsMouseOver { get; set; }

    class CSVResults
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public string Grade { get; set; }
        public string GradeValue { get; set; }
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

