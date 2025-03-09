using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.GS1;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabelVal.Sectors.Interfaces;
using Newtonsoft.Json.Linq;

namespace LabelVal.V275.Sectors;

public partial class Sector : ObservableObject, ISector
{
    public V275_REST_Lib.Models.Job.Sector V275Sector { get; }

    public ITemplate Template { get; }
    public IReport Report { get; }

    public ISectorDetails SectorDetails { get; }
    public bool IsWarning { get; }
    public bool IsError { get; }

    public AvailableStandards? DesiredStandard { get; }
    public AvailableTables? DesiredGS1Table { get; }
    public bool IsWrongStandard
    {
        get
        {
            switch (DesiredStandard)
            {
                case null:
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
                            AvailableStandards.ISO or AvailableStandards.ISO15415 or AvailableStandards.ISO15416 or null => false,
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

    public Sector(V275_REST_Lib.Models.Job.Sector sector, JObject report, AvailableStandards? standard, AvailableTables? table, string version)
    {
        V275Sector = sector;

        Report = new Report(report);
        Template = new Template(sector, version);

        DesiredStandard = standard;
        DesiredGS1Table = table;

        if (sector.type is "verify1D" or "verify2D" && sector.gradingStandard != null)
            Report.Standard = sector.gradingStandard.enabled ? AvailableStandards.GS1 : AvailableStandards.ISO;

        if (Report.Standard == AvailableStandards.GS1)
            Report.GS1Table = sector.gradingStandard.tableId.GetTable(AvailableDevices.V275);

        SectorDetails = new SectorDetails(this);

        int highCat = 0;
        foreach (Alarm alm in SectorDetails.Alarms)
            if (highCat < alm.Category)
                highCat = alm.Category;

        if (highCat == 1)
            IsWarning = true;
        else if (highCat == 2)
            IsError = true;
    }

    [RelayCommand]
    private void CopyToClipBoard() => ISector.CopyCSVToClipboard(this);

}
