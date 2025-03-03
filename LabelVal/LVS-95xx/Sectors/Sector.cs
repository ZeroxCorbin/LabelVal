using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabelVal.Sectors.Classes;
using LabelVal.Sectors.Interfaces;
using Lvs95xx.lib.Core.Controllers;

namespace LabelVal.LVS_95xx.Sectors;

public partial class Sector : ObservableObject, ISector
{
    public FullReport L95xxFullReport { get; }

    public ITemplate Template { get; }
    public IReport Report { get; }

    public ISectorDetails SectorDetails { get; }
    public bool IsWarning { get; }
    public bool IsError { get; }

    public StandardsTypes DesiredStandard { get; }
    public Gs1TableNames DesiredGS1Table { get; }
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
                case StandardsTypes.ISO29158:
                    {
                        return Report.Standard switch
                        {
                            StandardsTypes.ISO29158 => false,
                            _ => true,
                        };
                    }
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
                            StandardsTypes.GS1 => Report.GS1Table != DesiredGS1Table && Report.GS1Table != Gs1TableNames.Unsupported,
                            _ => true,
                        };
                    }
                default:
                    return true;
            }
        }
    }

    public bool IsFocused { get; set; }
    public bool IsMouseOver { get; set; }

    public Sector(FullReport report, StandardsTypes standard, Gs1TableNames table)
    {
        L95xxFullReport = report;

        Report = new Report(report);
        Template = new Template(report, (string)report.GetSetting("Version"));

        //Standard and GS1Table are set in the Report constructor.
        DesiredStandard = standard;
        DesiredGS1Table = table;

        SectorDetails = new SectorDetails(this, Report.SymbolType == "pdf417");

        int highCat = 0;
        foreach (Alarm alm in SectorDetails.Alarms)
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

    [RelayCommand]
    private void CopyToClipBoard() => ISector.CopyCSVToClipboard(this);

}
