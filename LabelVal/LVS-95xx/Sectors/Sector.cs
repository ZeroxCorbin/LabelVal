using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.GS1;
using BarcodeVerification.lib.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabelVal.Sectors.Extensions;
using LabelVal.Sectors.Interfaces;

using Newtonsoft.Json.Linq;

namespace LabelVal.LVS_95xx.Sectors;

public partial class Sector : ObservableObject, ISector
{
    public AvailableDevices Device { get; } = AvailableDevices.L95;
    public string Version { get; }

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

    public Sector(JObject template, JObject report, AvailableStandards standard, AvailableTables table, string version)
    {
        Version = version;
        DesiredStandard = standard;
        DesiredGS1Table = table;

        Template = new SectorTemplate(template, Version);
        Report = new SectorReport(report, Template);
        SectorDetails = new SectorParameters(this);

        foreach (Alarm alm in SectorDetails.Alarms)
        {
            if (alm.Category == AvaailableAlarmCategories.Warning)
                IsWarning = true;

            if (alm.Category == AvaailableAlarmCategories.Error)
                IsError = true;
        }
    }

    [RelayCommand]
    private void CopyToClipBoard(int rollID) => this.GetSectorReport(rollID.ToString(), true);

}
