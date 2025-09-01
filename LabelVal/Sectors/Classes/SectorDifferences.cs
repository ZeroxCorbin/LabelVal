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


        foreach (var pre in previous.Parameters)
        {
            var cur = current.Parameters.FirstOrDefault(x => x.Parameter == pre.Parameter);
            if (cur != null)
            {
                if (new SectorElement(pre, cur, current.Sector.Report.Symbology).Difference)
                    differences.Parameters.Add(new SectorElement( pre, cur, current.Sector.Report.Symbology));
            }
            else
                differences.Parameters.Add(new SectorElement(pre, null, current.Sector.Report.Symbology));
        }

        return differences.Parameters.Count > 0 ? differences : null;
    }
}

