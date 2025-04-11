using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.GS1;
using BarcodeVerification.lib.ISO;
using System.Text;
using System.Windows;
using Wpf.lib.Extentions;

namespace LabelVal.Sectors.Interfaces;

public partial interface ISector
{
    AvailableDevices Device { get; }
    string Version { get; }
    ISectorTemplate Template { get; }
    ISectorReport Report { get; }

    ISectorParameters SectorDetails { get; }
    bool IsWarning { get; }
    bool IsError { get; }

    AvailableStandards DesiredStandard { get; }
    AvailableTables DesiredGS1Table { get; }
    bool IsWrongStandard { get; }

    bool IsFocused { get; set; }
    bool IsMouseOver { get; set; }

    public static bool FallsWithin(System.Drawing.Point point, ISector sector, double radius = 50)
    {
        //I want to know if the point is within a radius of the center point of the sector

        return point.X >= sector.Report.CenterPoint.X - radius &&
               point.X <= sector.Report.CenterPoint.X + radius &&
               point.Y >= sector.Report.CenterPoint.Y - radius &&
               point.Y <= sector.Report.CenterPoint.Y + radius;
    }
}

