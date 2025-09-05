using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.ISO.ParameterTypes;
using LabelVal.Sectors.Interfaces;
using System;
using System.Linq;

namespace LabelVal.Sectors.Classes;

public class SectorDifferences
{
    public static SectorDifferencesDatabaseSettings Settings => SectorDifferencesDatabaseSettings.Instance;

    public string Name { get; set; }
    public string Username { get; set; }
    public bool IsSectorMissing { get; set; }
    public string SectorMissingText { get; set; }

    public bool HasDifferences { get; set; }

    public List<SectorElement> Parameters { get; } = [];

    public static SectorDifferences Compare(ISectorParameters previous, ISectorParameters current)
    {
        return Compare(previous, current, Settings);
    }

    public static SectorDifferences Compare(ISectorParameters previous, ISectorParameters current, SectorDifferencesDatabaseSettings settings)
    {

        SectorDifferences differences = new()
        {
            Name = current.Sector.Template.Name,
            Username = current.Sector.Template.Username,
        };


        foreach (var pre in previous.Parameters)
        {
            var cur = current.Parameters.FirstOrDefault(x => x.Parameter == pre.Parameter);
            if (cur != null)
            {
                var compareSettings = GetCompareSettings(pre, settings);
                if (new SectorElement(pre, cur, compareSettings, current.Sector.Report.Symbology).Difference)
                    differences.Parameters.Add(new SectorElement(pre, cur, compareSettings, current.Sector.Report.Symbology));
            }
            else
                differences.Parameters.Add(new SectorElement(pre, null, GetCompareSettings(pre, settings), current.Sector.Report.Symbology));
        }

        return differences.Parameters.Count > 0 ? differences : null;
    }

    private static ICompareSettings GetCompareSettings(IParameterValue parameter, SectorDifferencesDatabaseSettings settings)
    {
        return parameter switch
        {
            OverallGrade => settings.OverallGradeCompareSettings,
            GradeValue => settings.GradeValueCompareSettings,
            ValuePassFail => settings.ValuePassFailCompareSettings,
            ValueDouble => settings.ValueDoubleCompareSettings,
            PassFail => settings.PassFailCompareSettings,
            GS1Decode => settings.GS1DecodeCompareSettings,
            ValueString => settings.ValueStringCompareSettings,
            Missing missing => settings.MissingCompareSettings,
            _ => null,
        };
    }
}