using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.GS1;
using BarcodeVerification.lib.ISO;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using Wpf.lib.Extentions;

namespace LabelVal.Sectors.Interfaces;

public partial interface ISector
{
    Devices Device { get; }
    string Version { get; }
    ISectorTemplate Template { get; }
    ISectorReport Report { get; }

    ISectorParameters SectorDetails { get; }
    bool IsWarning { get; }
    bool IsError { get; }

    public ApplicationStandards DesiredApplicationStandard { get; }
    public ObservableCollection<GradingStandards> DesiredGradingStandards { get; }
    public GS1Tables DesiredGS1Table { get; }

    bool IsWrongStandard { get; }
    bool IsFocused { get; set; }
    bool IsMouseOver { get; set; }

    bool ShowApplicationParameters { get; set; }
    bool ShowGradingParameters { get; set; }
    bool ShowSymbologyParameters { get; set; }

    public static bool FallsWithin(System.Drawing.Point point, ISector sector, double radius = 50)
    {
        //I want to know if the point is within a radius of the center point of the sector

        return point.X >= sector.Report.CenterPoint.X - radius &&
               point.X <= sector.Report.CenterPoint.X + radius &&
               point.Y >= sector.Report.CenterPoint.Y - radius &&
               point.Y <= sector.Report.CenterPoint.Y + radius;
    }
}

