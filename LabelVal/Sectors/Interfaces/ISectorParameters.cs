using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.ISO.ParameterTypes;
using System.Collections.ObjectModel;

namespace LabelVal.Sectors.Interfaces;

public interface ISectorParameters
{
    ISector Sector { get; set; }

    string OCVMatchText { get; set; }
    bool IsNotOCVMatch { get; set; }

    bool IsSectorMissing { get; set; }
    string SectorMissingText { get; set; }

    ObservableCollection<IParameterValue> Parameters { get; }

    ObservableCollection<Alarm> Alarms { get; }
    ObservableCollection<Blemish> Blemishes { get; }

    Classes.SectorDifferences Compare(ISectorParameters compare);
}

