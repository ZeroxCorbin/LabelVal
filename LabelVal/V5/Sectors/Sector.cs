using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.GS1;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabelVal.Sectors.Interfaces;
using Newtonsoft.Json.Linq;
using V5_REST_Lib.Models;

namespace LabelVal.V5.Sectors;

public partial class Sector : ObservableObject, ISector
{
    public ISectorTemplate Template { get; }
    public ISectorReport Report { get; }

    public ISectorParameters SectorDetails { get; }
    public bool IsWarning { get; }
    public bool IsError { get; }

    public AvailableStandards DesiredStandard { get; set; }
    public AvailableTables DesiredGS1Table { get; set; }
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
                            AvailableStandards.GS1 => Report.GS1Table != DesiredGS1Table,
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

    public Sector(JObject report, JObject template, AvailableStandards standard, AvailableTables table, string name, string version)
    {
        Report = new SectorReport(report, template);
        Template = new SectorTemplate(report, template, name, version);

        DesiredStandard = standard;
        DesiredGS1Table = table;

        SectorDetails = new SectorDetails(this);

        foreach (Alarm alm in SectorDetails.Alarms)
        {
            if (alm.Category == AvaailableAlarmCategories.Warning)
                IsWarning = true;

            if (alm.Category == AvaailableAlarmCategories.Error)
                IsError = true;
        }
    }

    [RelayCommand]
    private void CopyToClipBoard() => ISector.CopyCSVToClipboard(this);
}
