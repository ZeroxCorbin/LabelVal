using CommunityToolkit.Mvvm.ComponentModel;
using System.Linq;
using V5_REST_Lib.Models;

namespace LabelVal.Sectors.ViewModels;

public partial class Sector : ObservableObject
{
    /// <summary>
    /// Used for OCV, OCR: matchSettings.matchMode, matchSettings.userDefinedDataTrueSize, matchSettings.fixedText
    /// Used for Blemish: blemishMask.layers
    /// Many uses: name, username, top, symbology,
    /// </summary>
    [ObservableProperty] private Template template;
    [ObservableProperty] private Report report;
    [ObservableProperty] private SectorDifferences sectorDifferences = new();

    [ObservableProperty] string l95xxPacket;

    public V275_REST_lib.Models.Job.Sector V275Template { get; }

    [ObservableProperty] private bool isWarning;
    [ObservableProperty] private bool isError;

    [ObservableProperty] private bool isGS1Standard;

    [ObservableProperty] private bool isWrongStandard;
    partial void OnIsWrongStandardChanged(bool value) => OnPropertyChanged(nameof(IsNotWrongStandard));
    public bool IsNotWrongStandard => !IsWrongStandard;

    public Sector() { }

    //V275
    public Sector(V275_REST_lib.Models.Job.Sector template, object report, bool isWrongStandard, bool isGS1Standard)
    {
        Report = new Report(report);

        V275Template = template;
        Template = new Template(template);

        IsWrongStandard = isWrongStandard;
        IsGS1Standard = isGS1Standard;

        SectorDifferences.V275Process(report, Template.Username, IsGS1Standard);

        var highCat = 0;

        foreach (var alm in SectorDifferences.Alarms)
        {
            //Alarms.Add(alm);
            if (highCat < alm.category)
                highCat = alm.category;
        }

        if (highCat == 1)
            IsWarning = true;
        else if (highCat == 2)
            IsError = true;

    }

    //V5
    public Sector(Results_QualifiedResult results, string name, bool isWrongStandard = false, bool isGS1Standard = false)
    {
        Report = new Report(results);
        Template = new Template(results, name);

        IsWrongStandard = isWrongStandard;
        IsGS1Standard = isGS1Standard;

        SectorDifferences.V5Process(results, Template.Username, IsGS1Standard);

        //var highCat = 0;

        //foreach (var alm in SectorDifferences.Alarms)
        //{
        //    //Alarms.Add(alm);
        //    if (highCat < alm.category)
        //        highCat = alm.category;
        //}

        //if (highCat == 1)
        //    IsWarning = true;
        //else if (highCat == 2)
        //    IsError = true;

    }

    //L95xx; The template is the currently selected sector.
    public Sector(Template template, string packet, bool isWrongStandard, bool isGS1Standard)
    {
        l95xxPacket = packet;
        var spl = packet.Split('\r').ToList();

        report = new Report(spl);
        Template = new Template(template);

        IsWrongStandard = isWrongStandard;
        IsGS1Standard = isGS1Standard;

        SectorDifferences.L95xxProcess(spl, Template.Username, IsGS1Standard, Report.SymbolType == "pdf417");

        var highCat = 0;

        foreach (var alm in SectorDifferences.Alarms)
        {
            //Alarms.Add(alm);
            if (highCat < alm.category)
                highCat = alm.category;
        }

        if (highCat == 1)
            IsWarning = true;
        else if (highCat == 2)
            IsError = true;

    }


    //public Sectors(Config Template, Results_QualifiedResult Report, bool isWrongStandard, bool isGS1Standard)
    //{
    //    Report = Report;
    //    Template = Template;

    //    IsWrongStandard = isWrongStandard;
    //    IsGS1Standard = isGS1Standard;

    //    SectorDifferences.Process(Report, Template.Username, IsGS1Standard);

    //    var highCat = 0;

    //    foreach (var alm in SectorDifferences.Alarms)
    //    {
    //        //Alarms.Add(alm);
    //        if (highCat < alm.category)
    //            highCat = alm.category;
    //    }

    //    if (highCat == 1)
    //        IsWarning = true;
    //    else if (highCat == 2)
    //        IsError = true;

    //}

    //private Template V5ParseTemplate(Config config, Results results)
    //{

    //    return new Template
    //    {
    //        Name = config.Name,
    //        Username = config.Username,
    //        Top = config.Top,
    //        Symbology = config.Symbology,
    //        MatchSettings = new Template.TemplateMatchMode
    //        {
    //            MatchMode = config.MatchSettings.MatchMode,
    //            UserDefinedDataTrueSize = config.MatchSettings.UserDefinedDataTrueSize,
    //            FixedText = config.MatchSettings.FixedText
    //        },
    //        BlemishMask = new Template.BlemishMaskLayers
    //        {
    //            Layers = results.BlemishMask.Layers
    //        }
    //    };
    //}
}
