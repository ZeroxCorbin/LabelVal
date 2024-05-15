using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Documents;
using V5_REST_Lib.Models;

namespace LabelVal.Sectors.ViewModels;

public enum StandardsTypes
{
    None,
    Unsupported, //Unsupported table
    ISO15415_15416,
    ISO15415,
    ISO15416,
    GS1,
}

public enum GS1TableNames
{
    None,
    Unsupported, //Unsupported table
    _1, //Trade items scanned in General Retail POS and NOT General Distribution.
    _1_8200, //AI (8200)
    _2, //Trade items scanned in General Distribution.
    _3, //Trade items scanned in General Retail POS and General Distribution.
    _4,
    _5,
    _6,
    _7_1,
    _7_2,
    _7_3,
    _7_4,
    _8,
    _9,
    _10,
    _11,
    _12_1,
    _12_2,
    _12_3,
}

public partial class Sector : ObservableObject
{
    public V275_REST_lib.Models.Job.Sector V275Sector { get; }
    public string L95xxPacket { get; }

    public Template Template { get; }
    public Report Report { get; }

    public bool IsWarning { get; }
    public bool IsError { get; }

    public SectorDifferences SectorDifferences { get; } = new();

    public StandardsTypes Standard { get; }
    public GS1TableNames GS1Table { get; }

    public StandardsTypes DesiredStandard { get; }
    public GS1TableNames DesiredGS1Table { get; }


    public bool IsGS1Standard => Standard == StandardsTypes.GS1;
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
                        switch (Standard)
                        {
                            case StandardsTypes.ISO15415_15416:
                            case StandardsTypes.ISO15415:
                            case StandardsTypes.ISO15416:
                                return false;
                            default:
                                return true;
                        }
                    }
                case StandardsTypes.ISO15415:
                    {
                        switch (Standard)
                        {
                            case StandardsTypes.ISO15415_15416:
                            case StandardsTypes.ISO15415:
                                return false;
                            default:
                                return true;
                        }
                    }
                case StandardsTypes.ISO15416:
                    {
                        switch (Standard)
                        {
                            case StandardsTypes.ISO15415_15416:
                            case StandardsTypes.ISO15416:
                                return false;
                            default:
                                return true;
                        }
                    }
                    case StandardsTypes.GS1:
                    {
                        switch (Standard)
                        {
                            case StandardsTypes.GS1:
                                return GS1Table != DesiredGS1Table;
                            default:
                                return true;
                        }
                    }
                default:
                    return true;
            }
        }
    }

    public Sector() { }

    //V275
    public Sector(V275_REST_lib.Models.Job.Sector sector, object report, StandardsTypes standard, GS1TableNames table)
    {
        V275Sector = sector;

        Report = new Report(report);
        Template = new Template(sector);

        SectorDifferences.V275Process(report, Template.Username);

        DesiredStandard = standard;
        DesiredGS1Table = table;

        if (sector.type is "verify1D" or "verify2D" && sector.gradingStandard != null)
            Standard = sector.gradingStandard.enabled ? StandardsTypes.GS1 : StandardsTypes.ISO15415_15416;

        if (Standard == StandardsTypes.GS1)
            GS1Table = V275GetGS1Table(sector.gradingStandard.tableId);

        var highCat = 0;
        foreach (var alm in SectorDifferences.Alarms)
            if (highCat < alm.category)
                highCat = alm.category;

        if (highCat == 1)
            IsWarning = true;
        else if (highCat == 2)
            IsError = true;
    }

    private GS1TableNames V275GetGS1Table(string tableId)
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

    //V5
    public Sector(Results_QualifiedResult results, string name, StandardsTypes standard, GS1TableNames table)
    {
        Report = new Report(results);
        Template = new Template(results, name);

        SectorDifferences.V5Process(results, Template.Username);

        DesiredStandard = standard;
        DesiredGS1Table = table;

        Standard = results.grading != null ? V5GetStandard(results.grading) : StandardsTypes.None;
        GS1Table = GS1TableNames.None;
        //if (Standard == StandardsTypes.GS1)
        //    GS1Table = V275GetGS1Table(sector.gradingStandard.tableId);

        var highCat = 0;
        foreach (var alm in SectorDifferences.Alarms)
            if (highCat < alm.category)
                highCat = alm.category;

        if (highCat == 1)
            IsWarning = true;
        else if (highCat == 2)
            IsError = true;
    }
    private StandardsTypes V5GetStandard(Results_Grading results)
        => results.standard != null ? results.standard switch
        {
            "iso15416" => StandardsTypes.ISO15416,
            "iso15415" => StandardsTypes.ISO15415,
            _ => StandardsTypes.Unsupported,
        } : StandardsTypes.None;

    //L95xx; The template is the currently selected sector.
    public Sector(Template template, string packet, StandardsTypes standard, GS1TableNames table)
    {
        L95xxPacket = packet;
        var spl = packet.Split('\r').ToList();

        Report = new Report(spl);
        Template = new Template(template);

        SectorDifferences.L95xxProcess(spl, Template.Username, Report.SymbolType == "pdf417");

        DesiredStandard = standard;
        DesiredGS1Table = table;

        Standard = standard;
        GS1Table = table;

        var highCat = 0;
        foreach (var alm in SectorDifferences.Alarms)
            if (highCat < alm.category)
                highCat = alm.category;

        if (highCat == 1)
            IsWarning = true;
        else if (highCat == 2)
            IsError = true;
    }
    private StandardsTypes L95xxGetStandard(List<string> spl)
    {
        return StandardsTypes.None;
    }

}
