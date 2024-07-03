using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace LabelVal.Run.Databases;

public partial class RunEntry : ObservableObject
{
    [SQLite.PrimaryKey] public string UID => StartTime.ToString();
    public long StartTime { get; } = DateTime.Now.Ticks;

    [ObservableProperty] private long endTime = long.MaxValue;
    partial void OnEndTimeChanged(long value) => OnPropertyChanged(nameof(IsComplete));
    public bool IsComplete => EndTime < long.MaxValue;

    [ObservableProperty] private int completed;
    [ObservableProperty] private string gradingStandard;
    [ObservableProperty] private string productPart;
    [ObservableProperty] private string cameraMAC;

    [ObservableProperty][property: SQLite.Ignore] private bool runDBMissing;
}
