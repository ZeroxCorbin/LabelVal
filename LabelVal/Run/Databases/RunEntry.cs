using CommunityToolkit.Mvvm.ComponentModel;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.Sectors.Interfaces;
using System;

namespace LabelVal.Run.Databases;

public partial class RunEntry : ObservableObject
{
    public RunEntry() { }
    public RunEntry(RunDatabase runDatabase, ImageRollEntry imageRoll,int loops)
        //string productPart, string cameraMAC, )
    {
        RunDatabase = runDatabase;

        //ProductPart = productPart;
        //CameraMAC = cameraMAC;
        //PrinterName = imageRoll.SelectedPrinter.PrinterName;

        ImageRollName = imageRoll.Name;
        GradingStandard = imageRoll.SelectedStandard;
        Gs1TableName = imageRoll.SelectedGS1Table;
        TargetDPI = imageRoll.TargetDPI;


        Loops = loops;
    }

    [SQLite.PrimaryKey] public long StartTime { get; set; } = DateTime.Now.Ticks;

    //Cant be used for DB quries.
    public string UID => StartTime.ToString() ?? "";
    public DateTime StartDateTime => new(StartTime);

    [ObservableProperty][property: SQLite.Ignore] private RunStates state;

    [SQLite.Ignore] public RunDatabase RunDatabase { get; set; }

    //public string ProductPart { get; set; }
    //public string CameraMAC { get; set; }
    //public string PrinterName { get; set; }

    public string ImageRollName { get; set; }
    public StandardsTypes GradingStandard { get; set; }
    public GS1TableNames Gs1TableName { get; set; }
    public double TargetDPI { get; set; }

    public int Loops { get; set; }
    public int CompletedLoops { get; set; }
    public long EndTime { get; set; } = long.MaxValue;
    public bool IsComplete => EndTime < long.MaxValue;

    [ObservableProperty][property: SQLite.Ignore] private bool runDBMissing;

}
