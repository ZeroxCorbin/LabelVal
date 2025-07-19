using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.Extensions;
using BarcodeVerification.lib.GS1;
using BarcodeVerification.lib.ISO.ParameterTypes;
using LabelVal.Sectors.Classes;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;

namespace LabelVal.Sectors.Interfaces;

public interface ISectorReport
{

    public Devices Device { get; }

    public Symbologies Symbology { get; }
    public SymbologySpecifications Specification { get; }
    public SymbologySpecificationTypes Type { get; }

    public ApplicationStandards ApplicationStandard { get; }
    public GradingStandards GradingStandard { get; }
    public GS1Tables GS1Table { get; }

    ObservableCollection<IParameterValue> Parameters { get; }

    JObject Original { get; }

    OverallGrade OverallGrade { get; }

    double XDimension { get; }
    double Aperture { get; }
    AvailableUnits Units { get; }

    double Top { get; }
    double Left { get; }
    double Width { get; }
    double Height { get; }
    double AngleDeg { get; }

    System.Drawing.Point CenterPoint { get; }

    string DecodeText { get; }

    //GS1
    GS1Decode GS1Results { get; }

    //OCR\OCV
    string Text { get; }
    double Score { get; }

    //Blemish
    int BlemishCount { get; }

    //V275 2D module data
    ModuleData ExtendedData { get; }
}

