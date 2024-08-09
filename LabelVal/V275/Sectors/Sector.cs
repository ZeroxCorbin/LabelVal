using LabelVal.Sectors.Interfaces;

namespace LabelVal.V275.Sectors;

public class Sector : ISector
{
    public V275_REST_lib.Models.Job.Sector V275Sector { get; }

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

    public Sector(V275_REST_lib.Models.Job.Sector sector, object report, StandardsTypes standard, GS1TableNames table)
    {
        V275Sector = sector;

        Report = new Report(report);
        Template = new Template(sector);

        SectorDifferences = new SectorDifferences(report, Template.Username);

        DesiredStandard = standard;
        DesiredGS1Table = table;

        if (sector.type is "verify1D" or "verify2D" && sector.gradingStandard != null)
            Report.Standard = sector.gradingStandard.enabled ? StandardsTypes.GS1 : StandardsTypes.ISO15415_15416;

        if (Report.Standard == StandardsTypes.GS1)
            Report.GS1Table = GetGS1Table(sector.gradingStandard.tableId);

        int highCat = 0;
        foreach (Alarm alm in SectorDifferences.Alarms)
            if (highCat < alm.Category)
                highCat = alm.Category;

        if (highCat == 1)
            IsWarning = true;
        else if (highCat == 2)
            IsError = true;
    }

    private GS1TableNames GetGS1Table(string tableId)
    => tableId switch
    {
        "1" => GS1TableNames._1,
        "2" => GS1TableNames._2,
        "3" => GS1TableNames._3,
        "4" => GS1TableNames._4,
        "5" => GS1TableNames._5,
        "6" => GS1TableNames._6,
        "7.1" => GS1TableNames._7_1,
        "7.2" => GS1TableNames._7_2,
        "7.3" => GS1TableNames._7_3,
        "7.4" => GS1TableNames._7_4,
        "8" => GS1TableNames._8,
        "9" => GS1TableNames._9,
        "10" => GS1TableNames._10,
        "11" => GS1TableNames._11,
        "12.1" => GS1TableNames._12_1,
        "12.2" => GS1TableNames._12_2,
        "12.3" => GS1TableNames._12_3,
        _ => GS1TableNames.Unsupported,
    };

}
