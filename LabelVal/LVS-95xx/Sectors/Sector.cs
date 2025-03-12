using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.GS1;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabelVal.Sectors.Interfaces;
using Lvs95xx.lib.Core.Controllers;

namespace LabelVal.LVS_95xx.Sectors;

public partial class Sector : ObservableObject, ISector
{
    public FullReport L95xxFullReport { get; }

    public ISectorTemplate Template { get; }
    public ISectorReport Report { get; }

    public ISectorParameters SectorDetails { get; }
    public bool IsWarning { get; }
    public bool IsError { get; }

    public AvailableStandards DesiredStandard { get; }
    public AvailableTables DesiredGS1Table { get; }
    public bool IsWrongStandard
    {
        get
        {
            switch (DesiredStandard)
            {
                case AvailableStandards.Unknown:
                    return false;

                case AvailableStandards.DPM:
                    {
                        return Report.Standard switch
                        {
                            AvailableStandards.DPM => false,
                            _ => true,
                        };
                    }
                case AvailableStandards.ISO:
                    {
                        return Report.Standard switch
                        {
                            AvailableStandards.ISO or AvailableStandards.ISO15415 or AvailableStandards.ISO15416 => false,
                            _ => true,
                        };
                    }
                case AvailableStandards.ISO15415:
                    {
                        return Report.Standard switch
                        {
                            AvailableStandards.ISO or AvailableStandards.ISO15415 => false,
                            _ => true,
                        };
                    }
                case AvailableStandards.ISO15416:
                    {
                        return Report.Standard switch
                        {
                            AvailableStandards.ISO or AvailableStandards.ISO15416 => false,
                            _ => true,
                        };
                    }
                case AvailableStandards.GS1:
                    {
                        return Report.Standard switch
                        {
                            AvailableStandards.GS1 => Report.GS1Table != DesiredGS1Table && Report.GS1Table != null,
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

    public Sector(FullReport report, AvailableStandards standard, AvailableTables table)
    {
        L95xxFullReport = report;
        //Standard and GS1Table are set in the Report constructor.
        DesiredStandard = standard;
        DesiredGS1Table = table;

        Template = new SectorTemplate(report, (string)report.GetSetting("Version"));
        Report = new SectorReport(report);

        SectorDetails = new SectorParameters(this);

        foreach (Alarm alm in SectorDetails.Alarms)
        {
            if (alm.Category == AvaailableAlarmCategories.Warning)
                IsWarning = true;

            if (alm.Category == AvaailableAlarmCategories.Error)
                IsError = true;
        }
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
