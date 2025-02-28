using CommunityToolkit.Mvvm.ComponentModel;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.Sectors.Interfaces;
using System;

namespace LabelVal.Run.Databases;

public partial class RunEntry : ObservableObject
{
    public RunEntry() { }

    [SQLite.PrimaryKey] public long StartTime { get; set; } = DateTime.Now.Ticks;

    #region Cant be used for DB quries.
    public string UID => StartTime.ToString() ?? "";
    public DateTime StartDateTime => new(StartTime);
    #endregion

    [ObservableProperty][property: SQLite.Ignore] private RunStates state;
    [SQLite.Ignore] public ResultsDatabase ResultsDatabase { get; set; }

    public string ProductPart { get; set; }
    public string CameraMAC { get; set; }
    public string PrinterName { get; set; }

    public string ImageRollName { get; set; }
    public string ImageRollUID { get; set; }


    public StandardsTypes GradingStandard { get; set; }
    public GS1TableNames Gs1TableName { get; set; }
    //public double TargetDPI { get; set; }

    public int DesiredLoops { get; set; }
    public int CompletedLoops { get; set; }
    public long EndTime { get; set; } = long.MaxValue;

    public bool IsComplete => EndTime < long.MaxValue;

    [ObservableProperty][property: SQLite.Ignore] private bool runDBMissing;

}
