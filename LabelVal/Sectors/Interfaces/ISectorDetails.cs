using LabelVal.Sectors.Classes;
using System.Collections.ObjectModel;

namespace LabelVal.Sectors.Interfaces;

public interface ISectorDetails
{
    string Name { get; set; }
    string UserName { get; set; }

    string SymbolType { get; set; }

    string Units { get; set; }

    string OCVMatchText { get; set; }
    bool IsNotOCVMatch { get; set; }

    bool IsSectorMissing { get; set; }
    string SectorMissingText { get; set; }

    bool IsNotEmpty { get; set; }

    ObservableCollection<GradeValue> GradeValues { get; }
    ObservableCollection<ValueResult> ValueResults { get; }
    ObservableCollection<ValueResult> Gs1ValueResults { get; }
    ObservableCollection<Grade> Gs1Grades { get; }
    ObservableCollection<Value_> Values { get; }
    ObservableCollection<Alarm> Alarms { get; }
    ObservableCollection<Blemish> Blemishes { get; }

    SectorDifferences Compare(ISectorDetails compare);
}

