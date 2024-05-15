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

public enum GS1TableTypes
{
    None,
    Unsupported, //Unsupported table
    Tabel_1, //Trade items scanned in General Retail POS and NOT General Distribution.
    Tabel_1_8200, //AI (8200)
    Tabel_2, //Trade items scanned in General Distribution.
    Tabel_3, //Trade items scanned in General Retail POS and General Distribution.
    Tabel_4,
    Tabel_5,
    Tabel_6,
    Tabel_7_1,
    Tabel_7_2,
    Tabel_7_3,
    Tabel_7_4,
    Tabel_8,
    Tabel_9,
    Tabel_10,
    Tabel_11,
    Tabel_12_1,
    Tabel_12_2,
    Tabel_12_3,


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
    public GS1TableTypes GS1Table { get; }

    public StandardsTypes DesiredStandard { get; }
    public GS1TableTypes DesiredGS1Table { get; }


    public bool IsGS1Standard => Standard == StandardsTypes.GS1;
    public bool IsWrongStandard => Standard != DesiredStandard || GS1Table != DesiredGS1Table;

    public Sector() { }

    //V275
    public Sector(V275_REST_lib.Models.Job.Sector sector, object report, StandardsTypes standard, GS1TableTypes table)
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

    private GS1TableTypes V275GetGS1Table(string tableId)
        => tableId switch
        {
            "2" => GS1TableTypes.Tabel_2,
            "4" => GS1TableTypes.Tabel_4,
            "5" => GS1TableTypes.Tabel_5,
            "6" => GS1TableTypes.Tabel_6,
            "8" => GS1TableTypes.Tabel_8,
            "9" => GS1TableTypes.Tabel_9,
            "10" => GS1TableTypes.Tabel_10,
            "11" => GS1TableTypes.Tabel_11,
            "12.2" => GS1TableTypes.Tabel_12_2,
            "12.3" => GS1TableTypes.Tabel_12_3,
            _ => GS1TableTypes.Unsupported,
        };

    //V5
    public Sector(Results_QualifiedResult results, string name, StandardsTypes standard, GS1TableTypes table)
    {
        Report = new Report(results);
        Template = new Template(results, name);

        SectorDifferences.V5Process(results, Template.Username);

        DesiredStandard = standard;
        DesiredGS1Table = table;

        Standard = results.grading != null ? V5GetStandard(results.grading) : StandardsTypes.None;
        GS1Table = GS1TableTypes.None;
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
    public Sector(Template template, string packet, StandardsTypes standard, GS1TableTypes table)
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
