using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.Extensions;
using BarcodeVerification.lib.GS1;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabelVal.Sectors.Extensions;
using LabelVal.Sectors.Interfaces;
using Newtonsoft.Json.Linq;

namespace LabelVal.V5.Sectors;

public partial class Sector : ObservableObject, ISector
{
    public AvailableDevices Device { get; } = AvailableDevices.V5;
    public string Version { get; }

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

    public Sector(JObject report, JObject template, AvailableStandards standard, AvailableTables table, string version)
    {
        Version = version;
        DesiredStandard = standard;
        DesiredGS1Table = table;
        
        string toolUid = report.GetParameter<string>("toolUid");
        if (string.IsNullOrWhiteSpace(toolUid))
        {
            toolUid = $"SymbologyTool_{report.GetParameter<int>("toolSlot")}";
        }

        Template = new SectorTemplate(report, template, toolUid, version);
        Report = new SectorReport(report, Template, table);
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
    private void CopyToClipBoard(int rollID) => this.GetSectorReport(rollID.ToString(), true);
}
