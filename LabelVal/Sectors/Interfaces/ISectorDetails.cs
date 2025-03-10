using BarcodeVerification.lib.ISO;
using LabelVal.Sectors.Classes;
using System.Collections.ObjectModel;

namespace LabelVal.Sectors.Interfaces;

public interface ISectorDetails
{
    ISector Sector { get; set; }

    string Units { get; set; }

    string OCVMatchText { get; set; }
    bool IsNotOCVMatch { get; set; }

    bool IsSectorMissing { get; set; }
    string SectorMissingText { get; set; }

    bool IsNotEmpty { get; set; }

    ObservableCollection<IParameterValue> Grades { get; } 
    ObservableCollection<IParameterValue> PassFails { get; }

    ObservableCollection<ValueDouble> ValueDoubles { get; }
    ObservableCollection<ValueString> ValueStrings { get; }

    ObservableCollection<Alarm> Alarms { get; }
    ObservableCollection<Blemish> Blemishes { get; }

    ObservableCollection<AvailableParameters> MissingParameters { get; }

    SectorDifferences Compare(ISectorDetails compare);
}

