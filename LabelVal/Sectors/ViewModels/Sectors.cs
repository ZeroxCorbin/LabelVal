using CommunityToolkit.Mvvm.ComponentModel;
using V5_REST_Lib.Models;

namespace LabelVal.Sectors.ViewModels;

public class Template
{
    public class TemplateMatchMode
    {
        public int MatchMode { get; set; }
        public int UserDefinedDataTrueSize { get; set; }
        public string FixedText { get; set; }
    }

    public class BlemishMaskLayers
    {
        public V275_REST_lib.Models.Job.Layer[] Layers { get; set; }
    }

    public V275_REST_lib.Models.Job.Sector V275Template { get; }
    public Results_QualifiedResult V5Template { get; }

    public string Name { get; set; }
    public string Username { get; set; }
    public int Top { get; set; }
    public string Symbology { get; set; }


    public TemplateMatchMode MatchSettings { get; set; }
    public BlemishMaskLayers BlemishMask { get; set; }

    public Template(V275_REST_lib.Models.Job.Sector sectorTemplate)
    {
        V275Template = sectorTemplate;

        Name = sectorTemplate.name;
        Username = sectorTemplate.username;
        Top = sectorTemplate.top;
        Symbology = sectorTemplate.symbology;

        MatchSettings = new Template.TemplateMatchMode
        {
            MatchMode = sectorTemplate.matchSettings.matchMode,
            UserDefinedDataTrueSize = sectorTemplate.matchSettings.userDefinedDataTrueSize,
            FixedText = sectorTemplate.matchSettings.fixedText
        };
        BlemishMask = new Template.BlemishMaskLayers
        {
            Layers = sectorTemplate.blemishMask?.layers
        };
    }

    public Template(Results_QualifiedResult reportSector, string name)
    {
        V5Template = reportSector;

        Name = name;
        Username = name;
        Top = reportSector.x;

        Symbology = GetV5Symbology(reportSector);

    }

    private string GetV5Symbology(Results_QualifiedResult reportSector)
    {
        if (reportSector.Code128 != null)
            return "Code128";
        else if (reportSector.Datamatrix != null)
            return "DataMatrix";
        else if (reportSector.QR != null)
            return "QR";
        else if (reportSector.PDF417 != null)
            return "PDF417";
        else if (reportSector.UPC != null)
            return "UPC";
        else
            return "Unknown";
    }

}

public partial class Sectors : ObservableObject
{
    /// <summary>
    /// Used for OCV, OCR: matchSettings.matchMode, matchSettings.userDefinedDataTrueSize, matchSettings.fixedText
    /// Used for Blemish: blemishMask.layers
    /// Many uses: name, username, top, symbology,
    /// </summary>
    [ObservableProperty] private Template templateSector;
    [ObservableProperty] private object reportSector;
    [ObservableProperty] private SectorDifferences sectorResults = new();

    public V275_REST_lib.Models.Job.Sector V275Template { get; }

    [ObservableProperty] private bool isWarning;
    [ObservableProperty] private bool isError;

    [ObservableProperty] private bool isGS1Standard;

    [ObservableProperty] private bool isWrongStandard;
    partial void OnIsWrongStandardChanged(bool value) => OnPropertyChanged(nameof(IsNotWrongStandard));
    public bool IsNotWrongStandard => !IsWrongStandard;

    public Sectors() { }
    public Sectors(V275_REST_lib.Models.Job.Sector templateSector, object reportSector, bool isWrongStandard, bool isGS1Standard)
    {
        ReportSector = reportSector;
        V275Template = templateSector;
        TemplateSector = new Template(templateSector);

        IsWrongStandard = isWrongStandard;
        IsGS1Standard = isGS1Standard;

        SectorResults.Process(reportSector, TemplateSector.Username, IsGS1Standard);

        var highCat = 0;

        foreach (var alm in SectorResults.Alarms)
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

    public Sectors(Results_QualifiedResult reportSector, string name, bool isWrongStandard = false, bool isGS1Standard = false)
    {
        ReportSector = reportSector;
        TemplateSector = new Template(reportSector, name);

        IsWrongStandard = isWrongStandard;
        IsGS1Standard = isGS1Standard;

        //SectorResults.Process(reportSector, TemplateSector.Username, IsGS1Standard);

        //var highCat = 0;

        //foreach (var alm in SectorResults.Alarms)
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

    public Sectors(Template templateSector, object reportSector, bool isWrongStandard, bool isGS1Standard)
    {
        ReportSector = reportSector;
        TemplateSector = templateSector;

        IsWrongStandard = isWrongStandard;
        IsGS1Standard = isGS1Standard;

        SectorResults.Process(reportSector, TemplateSector.Username, IsGS1Standard);

        var highCat = 0;

        foreach (var alm in SectorResults.Alarms)
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

    //public Sectors(Config templateSector, Results_QualifiedResult reportSector, bool isWrongStandard, bool isGS1Standard)
    //{
    //    ReportSector = reportSector;
    //    TemplateSector = templateSector;

    //    IsWrongStandard = isWrongStandard;
    //    IsGS1Standard = isGS1Standard;

    //    SectorResults.Process(reportSector, TemplateSector.Username, IsGS1Standard);

    //    var highCat = 0;

    //    foreach (var alm in SectorResults.Alarms)
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
