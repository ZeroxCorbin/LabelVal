using CommunityToolkit.Mvvm.ComponentModel;

namespace LabelVal.Sectors.ViewModels;

public partial class Sectors : ObservableObject
{

    [ObservableProperty] private V275_REST_lib.Models.Job.Sector templateSector;
    [ObservableProperty] private object reportSector;
    [ObservableProperty] private SectorDifferences sectorResults = new();

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
        TemplateSector = templateSector;

        IsWrongStandard = isWrongStandard;
        IsGS1Standard = isGS1Standard;

        SectorResults.Process(reportSector, TemplateSector.username, IsGS1Standard);

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
}
