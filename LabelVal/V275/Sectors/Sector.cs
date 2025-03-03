using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabelVal.Sectors.Classes;
using LabelVal.Sectors.Interfaces;

namespace LabelVal.V275.Sectors;

public partial class Sector : ObservableObject, ISector
{
    public V275_REST_Lib.Models.Job.Sector V275Sector { get; }

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

    public Sector(V275_REST_Lib.Models.Job.Sector sector, object report, StandardsTypes standard, Gs1TableNames table)
    {
        V275Sector = sector;

        Report = new Report(report);
        Template = new Template(sector);

        SectorDetails = new SectorDetails(report, Template.Username);

        DesiredStandard = standard;
        DesiredGS1Table = table;

        if (sector.type is "verify1D" or "verify2D" && sector.gradingStandard != null)
            Report.Standard = sector.gradingStandard.enabled ? StandardsTypes.GS1 : StandardsTypes.ISO15415_15416;

        if (Report.Standard == StandardsTypes.GS1)
            Report.GS1Table = GetGS1Table(sector.gradingStandard.tableId);

        int highCat = 0;
        foreach (Alarm alm in SectorDetails.Alarms)
            if (highCat < alm.Category)
                highCat = alm.Category;

        if (highCat == 1)
            IsWarning = true;
        else if (highCat == 2)
            IsError = true;
    }

    private Gs1TableNames GetGS1Table(string tableId)
    => tableId switch
    {
        "1" => Gs1TableNames._1,
        "2" => Gs1TableNames._2,
        "3" => Gs1TableNames._3,
        "4" => Gs1TableNames._4,
        "5" => Gs1TableNames._5,
        "6" => Gs1TableNames._6,
        "7.1" => Gs1TableNames._7_1,
        "7.2" => Gs1TableNames._7_2,
        "7.3" => Gs1TableNames._7_3,
        "7.4" => Gs1TableNames._7_4,
        "8" => Gs1TableNames._8,
        "9" => Gs1TableNames._9,
        "10" => Gs1TableNames._10,
        "11" => Gs1TableNames._11,
        "12.1" => Gs1TableNames._12_1,
        "12.2" => Gs1TableNames._12_2,
        "12.3" => Gs1TableNames._12_3,
        _ => Gs1TableNames.Unsupported,
    };

    [RelayCommand]
    private void CopyToClipBoard() => ISector.CopyCSVToClipboard(this);

}
