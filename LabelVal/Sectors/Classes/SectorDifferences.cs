using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.ISO;
using LabelVal.Sectors.Interfaces;

namespace LabelVal.Sectors.Classes;

public class SectorDifferences
{
    public static SectorDifferencesSettings Settings { get; } = new SectorDifferencesSettings();

    public string Name { get; set; }
    public string Username { get; set; }
    public string Units { get; set; }

    public bool IsSectorMissing { get; set; }
    public string SectorMissingText { get; set; }

    public bool HasDifferences { get; set; }

    public List<SectorElement> Parameters { get; } = [];

    public static SectorDifferences Compare(ISectorDetails previous, ISectorDetails current)
    {

        SectorDifferences differences = new()
        {
            Name = current.Sector.Template.Name,
            Username = current.Sector.Template.Username,
            Units = current.Units,
        };


        foreach (IParameterValue pre in previous.Parameters)
        {
            IParameterValue cur = current.Parameters.FirstOrDefault(x => x.Parameter == pre.Parameter);
            if (cur != null)
            {
                if (new SectorElement(pre, cur, current.Sector.Report.SymbolType).Difference)
                    differences.Parameters.Add(new SectorElement( pre, cur, current.Sector.Report.SymbolType));
            }
            else
                differences.Parameters.Add(new SectorElement(pre, null, current.Sector.Report.SymbolType));
        }

        return differences.Parameters.Count > 0 ? differences : null;
    }
}

