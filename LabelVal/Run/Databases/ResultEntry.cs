using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelVal.Run.Databases
{
    public partial class ResultEntry : ObservableObject
    {
        [ObservableProperty][property: SQLite.PrimaryKey] private long timeDate = DateTime.Now.Ticks;

        [ObservableProperty] private int loopCount;
        [ObservableProperty] private int labelImageOrder;
        [ObservableProperty] private string labelImageUID;
        [ObservableProperty] private byte[] labelImage;
        [ObservableProperty] private string labelTemplate;
        [ObservableProperty] private string labelReport;
        [ObservableProperty] private byte[] repeatImage;
        [ObservableProperty] private byte[] repeatGoldenImage;
        [ObservableProperty] private string repeatReport;
    }
}
