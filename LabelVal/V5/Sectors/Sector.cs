using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabelVal.Sectors.Classes;
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

    public StandardsTypes DesiredStandard { get; set; }
    public Gs1TableNames DesiredGS1Table { get; set; }
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
                            StandardsTypes.GS1 => Report.GS1Table != DesiredGS1Table,
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

    public Sector(ResultsAlt.Decodedata decodeData, Config.Toollist toollist, string name, StandardsTypes standard, Gs1TableNames table)
    {
        V5Sector = decodeData;

        Report = new Report(decodeData);
        Template = new Template(decodeData, toollist, name, "");

        DesiredStandard = standard;
        DesiredGS1Table = table;

        Report.Standard = decodeData.grading != null ? V5GetStandard(decodeData.grading) : StandardsTypes.None;
        Report.GS1Table = Gs1TableNames.None; //GS1 is not supported in V5, yet

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
    private StandardsTypes V5GetStandard(ResultsAlt.Grading results)
        => results.standard != null ? results.standard switch
        {
            "iso15416" => StandardsTypes.ISO15416,
            "iso15415" => StandardsTypes.ISO15415,
            "iso29158" => StandardsTypes.ISO29158,
            _ => StandardsTypes.Unsupported,
        } : StandardsTypes.None;

    [RelayCommand]
    private void CopyToClipBoard() => ISector.CopyCSVToClipboard(this);
}
