using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.GS1;
using CommunityToolkit.Mvvm.ComponentModel;
using LabelVal.Sectors.Classes;

namespace LabelVal.Run.Databases;

public partial class RunEntry : ObservableObject
{
    public RunEntry() 
    { 
        var start = DateTime.Now.Ticks;
        StartTime = start;
        UID = start.ToString();
    }

    public long StartTime { get; set; }
    [SQLite.PrimaryKey] public string UID { get; set; }

    [ObservableProperty] private RunStates state;
    [SQLite.Ignore] public ResultsDatabase ResultsDatabase { get; set; }

    public bool HasV275 { get; set; }
    public string V275Version { get; set; }
    public bool HasV5 { get; set; }
    public string V5Version { get; set; }
    public bool HasL95 { get; set; }
    public string L95Version { get; set; }

    public string ImageRollName { get; set; }
    public string ImageRollUID { get; set; }

    public GradingStandards GradingStandard { get; set; }
    public ApplicationStandards ApplicationStandard { get; set; }
    public GS1Tables Gs1TableName { get; set; }
    //public double TargetDPI { get; set; }

    public int DesiredLoops { get; set; }
    public int CompletedLoops { get; set; }
    public long EndTime { get; set; } = long.MaxValue;

    public bool IsComplete => EndTime < long.MaxValue;

    [ObservableProperty][property: SQLite.Ignore] private bool runDBMissing;

}
