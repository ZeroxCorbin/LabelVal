using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.ISO.ParameterTypes;
using LabelVal.Sectors.Interfaces;

namespace LabelVal.Sectors.Classes;

public class SectorDifferences
{
    public static SectorDifferencesSettings Settings=> SectorDifferencesSettings.Instance;

    public string Name { get; set; }
    public string Username { get; set; }
    public bool IsSectorMissing { get; set; }
    public string SectorMissingText { get; set; }

    public bool HasDifferences { get; set; }

    public List<SectorElement> Parameters { get; } = [];

    public static SectorDifferences Compare(ISectorParameters previous, ISectorParameters current)
    {

        SectorDifferences differences = new()
        {
            Name = current.Sector.Template.Name,
            Username = current.Sector.Template.Username,
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

