using CommunityToolkit.Mvvm.ComponentModel;
using LabelVal.Sectors.ViewModels;
using System;
using System.IO;

namespace LabelVal.Run.Databases;

public partial class RunEntry : ObservableObject
{
    public RunEntry() { }
    public RunEntry(StandardsTypes gradingStandard, string productPart, string cameraMAC, int loops)
    {
        this.gradingStandard = gradingStandard;
        this.productPart = productPart;
        this.cameraMAC = cameraMAC;
        this.loops = loops;
    }

    [SQLite.PrimaryKey] public string UID => StartTime.ToString();
    public long StartTime { get; } = DateTime.Now.Ticks;

    [ObservableProperty][property: SQLite.Ignore] private RunStates state;

    [ObservableProperty] private StandardsTypes gradingStandard;
    [ObservableProperty] private string productPart;
    [ObservableProperty] private string cameraMAC;
    [ObservableProperty] private int loops;

    [ObservableProperty] private int completedLoops;
    [ObservableProperty] private long endTime = long.MaxValue;
    partial void OnEndTimeChanged(long value) => OnPropertyChanged(nameof(IsComplete));
    public bool IsComplete => EndTime < long.MaxValue;


    [ObservableProperty][property: SQLite.Ignore] private bool runDBMissing;

}
