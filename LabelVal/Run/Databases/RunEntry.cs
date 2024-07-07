using CommunityToolkit.Mvvm.ComponentModel;
using LabelVal.Sectors.ViewModels;
using System;
using System.IO;

namespace LabelVal.Run.Databases;

public partial class RunEntry : ObservableObject
{
    public RunEntry() { }
    public RunEntry(RunDatabase runDatabase, StandardsTypes gradingStandard, string productPart, string cameraMAC, int loops)
    {
        RunDatabase = runDatabase;
        this.gradingStandard = gradingStandard;
        this.productPart = productPart;
        this.cameraMAC = cameraMAC;
        this.loops = loops;
    }

    [SQLite.PrimaryKey] public long StartTime { get; set; } = DateTime.Now.Ticks;

    public string UID => StartTime.ToString();
    public DateTime StartDateTime => new(StartTime);

    [ObservableProperty][property: SQLite.Ignore] private RunStates state;

    [SQLite.Ignore] public RunDatabase RunDatabase { get; set; }
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
