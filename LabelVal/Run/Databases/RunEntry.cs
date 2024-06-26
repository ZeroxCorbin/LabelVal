using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelVal.Run.Databases
{
    public partial class RunEntry : ObservableObject
    {
        [ObservableProperty][property: SQLite.PrimaryKey] private long timeDate;

        [ObservableProperty]private int completed;
        [ObservableProperty]private string gradingStandard;
        [ObservableProperty]private string productPart;
        [ObservableProperty]private string cameraMAC;
        [ObservableProperty][property: SQLite.Ignore]private bool runDBMissing;
    }
}
