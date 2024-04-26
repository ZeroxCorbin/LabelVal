using CommunityToolkit.Mvvm.ComponentModel;
using V275_REST_lib.Models;

namespace LabelVal.WindowViewModels;

public partial class SectorControlViewModel : ObservableObject
{
    [ObservableProperty] private Job.Sector jobSector;
    [ObservableProperty] private object reportSector;
    [ObservableProperty] private SectorDifferenceViewModel sectorResults = new();

    [ObservableProperty] private bool isWarning;
    [ObservableProperty] private bool isError;

    [ObservableProperty] private bool isGS1Standard;

    [ObservableProperty] private bool isWrongStandard;
    partial void OnIsWrongStandardChanged(bool value) => OnPropertyChanged(nameof(IsNotWrongStandard));
    public bool IsNotWrongStandard => !IsWrongStandard;

    public SectorControlViewModel() { }
    public SectorControlViewModel(Job.Sector jobSector, object reportSector, bool isWrongStandard, bool isGS1Standard)
    {
        ReportSector = reportSector;
        JobSector = jobSector;
        IsWrongStandard = isWrongStandard;
        IsGS1Standard = isGS1Standard;

        SectorResults.Process(reportSector, jobSector.username, IsGS1Standard);

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
