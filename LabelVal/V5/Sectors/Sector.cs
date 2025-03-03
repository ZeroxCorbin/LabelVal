using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.GS1;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabelVal.Sectors.Interfaces;
using V5_REST_Lib.Models;

namespace LabelVal.V5.Sectors;

public partial class Sector : ObservableObject, ISector
{
    public ResultsAlt.Decodedata V5Sector { get; }

    public ITemplate Template { get; }
    public IReport Report { get; }

    public ISectorDetails SectorDetails { get; }
    public bool IsWarning { get; }
    public bool IsError { get; }

    public AvailableStandards? DesiredStandard { get; set; }
    public AvailableTables? DesiredGS1Table { get; set; }
    public bool IsWrongStandard
    {
        get
        {
            switch (DesiredStandard)
            {
                case null:
                    return true;
                case AvailableStandards.ISO29158:
                    {
                        return Report.Standard switch
                        {
                            AvailableStandards.ISO29158 => false,
                            _ => true,
                        };
                    }
                case AvailableStandards.ISO15415_15416:
                    {
                        return Report.Standard switch
                        {
                            AvailableStandards.ISO15415_15416 or AvailableStandards.ISO15415 or AvailableStandards.ISO15416 or null => false,
                            _ => true,
                        };
                    }
                case AvailableStandards.ISO15415:
                    {
                        return Report.Standard switch
                        {
                            AvailableStandards.ISO15415_15416 or AvailableStandards.ISO15415 => false,
                            _ => true,
                        };
                    }
                case AvailableStandards.ISO15416:
                    {
                        return Report.Standard switch
                        {
                            AvailableStandards.ISO15415_15416 or AvailableStandards.ISO15416 => false,
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

    public Sector(ResultsAlt.Decodedata decodeData, Config.Toollist toollist, string name, AvailableStandards? standard, AvailableTables? table)
    {
        V5Sector = decodeData;

        Report = new Report(decodeData);
        Template = new Template(decodeData, toollist, name, "");

        DesiredStandard = standard;
        DesiredGS1Table = table;

        Report.Standard = decodeData.grading != null ? Standards.GetV5StandardEnum(decodeData.grading.standard) : null;
        Report.GS1Table = null; //GS1 is not supported in V5, yet

        SectorDetails = new SectorDetails(this, Template.Username);

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
