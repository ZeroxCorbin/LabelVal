using LabelVal.Sectors.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace LabelVal.LVS_95xx.Sectors;

public class Sector : ISector
{
    public string L95xxPacket { get; }

    public ITemplate Template { get; }
    public IReport Report { get; }

    public ISectorDifferences SectorDifferences { get; }
    public bool IsWarning { get; }
    public bool IsError { get; }

    public StandardsTypes DesiredStandard { get; }
    public GS1TableNames DesiredGS1Table { get; }
    public bool IsWrongStandard
    {
        get
        {
            switch (DesiredStandard)
            {
                case StandardsTypes.None:
                    return false;
                case StandardsTypes.Unsupported:
                    return true;
                case StandardsTypes.ISO15415_15416:
                    {
                        return Report.Standard switch
                        {
                            StandardsTypes.ISO15415_15416 or StandardsTypes.ISO15415 or StandardsTypes.ISO15416 or StandardsTypes.Unsupported => false,
                            _ => true,
                        };
                    }
                case StandardsTypes.ISO15415:
                    {
                        return Report.Standard switch
                        {
                            StandardsTypes.ISO15415_15416 or StandardsTypes.ISO15415 => false,
                            _ => true,
                        };
                    }
                case StandardsTypes.ISO15416:
                    {
                        return Report.Standard switch
                        {
                            StandardsTypes.ISO15415_15416 or StandardsTypes.ISO15416 => false,
                            _ => true,
                        };
                    }
                case StandardsTypes.GS1:
                    {
                        return Report.Standard switch
                        {
                            StandardsTypes.GS1 => Report.GS1Table != DesiredGS1Table,
                            _ => true,
                        };
                    }
                default:
                    return true;
            }
        }
    }

    //L95xx; The template is the currently selected sector.
    public Sector(ITemplate template, string packet, StandardsTypes standard, GS1TableNames table)
    {
        L95xxPacket = packet;
        List<string> spl = packet.Split('\r').ToList();

        Report = new Report(spl);
        Template = new Template(template);

        SectorDifferences = new SectorDifferences(spl, Template.Username, Report.SymbolType == "pdf417");

        DesiredStandard = standard;
        DesiredGS1Table = table;

        Report.Standard = GetMultipleKeyValuePairs("GS1 Data", spl) != null ? StandardsTypes.GS1 : StandardsTypes.ISO15415_15416;
        Report.GS1Table = GS1TableNames.Unsupported;

        int highCat = 0;
        foreach (Alarm alm in SectorDifferences.Alarms)
            if (highCat < alm.Category)
                highCat = alm.Category;

        if (highCat == 1)
            IsWarning = true;
        else if (highCat == 2)
            IsError = true;
    }

    private List<string[]> GetMultipleKeyValuePairs(string key, List<string> report)
    {
        List<string> items = report.FindAll((e) => e.StartsWith(key));

        if (items == null || items.Count == 0)
            return null;

        List<string[]> res = [];
        foreach (string item in items)
        {
            if (!item.Contains(','))
                continue;

            res.Add([item[..item.IndexOf(',')], item[(item.IndexOf(',') + 1)..]]);
        }
        return res;
    }
}
