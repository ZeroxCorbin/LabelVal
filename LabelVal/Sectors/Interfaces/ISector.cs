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

}

