using LabelVal.Sectors.Interfaces;
using V5_REST_Lib.Models;

namespace LabelVal.V5.Sectors;

public class Sector : ISector
{
    public ResultsAlt.Decodedata V5Sector { get; }

    public ITemplate Template { get; }
    public IReport Report { get; }

    public ISectorDifferences SectorDifferences { get; }
    public bool IsWarning { get; }
    public bool IsError { get; }

    public StandardsTypes DesiredStandard { get; set; }
    public GS1TableNames DesiredGS1Table { get; set; }
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

    public Sector(ResultsAlt.Decodedata decodeData, Config.Toollist toollist, string name, StandardsTypes standard, GS1TableNames table)
    {
        V5Sector = decodeData;

        Report = new Report(decodeData);
        Template = new Template(decodeData, toollist, name);

        SectorDifferences = new SectorDifferences(decodeData, Template.Username);

        DesiredStandard = standard;
        DesiredGS1Table = table;

        Report.Standard = decodeData.grading != null ? V5GetStandard(decodeData.grading) : StandardsTypes.None;
        Report.GS1Table = GS1TableNames.None; //GS1 is not supported in V5, yet

        int highCat = 0;
        foreach (Alarm alm in SectorDifferences.Alarms)
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
            _ => StandardsTypes.Unsupported,
        } : StandardsTypes.None;
}
