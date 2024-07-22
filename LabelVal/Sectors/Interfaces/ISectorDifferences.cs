using System.Collections.ObjectModel;
using V275_REST_lib.Models;

namespace LabelVal.Sectors.Interfaces;

public class GradeValue : Report_InspectSector_Common.GradeValue
{
    public string Name { get; set; }

    public GradeValue(string name, Report_InspectSector_Common.GradeValue data)
    {
        if (data != null)
        {
            value = data.value;
            grade = data.grade;
        }
        Name = name;
    }
}
public class Grade : Report_InspectSector_Common.Grade
{
    public string Name { get; set; }

    public Grade(string name, Report_InspectSector_Common.Grade data)
    {
        value = data.value;
        letter = data.letter;
        Name = name;
    }
}
public class ValueResult : Report_InspectSector_Common.ValueResult
{
    public string Name { get; set; }

    public ValueResult(string name, Report_InspectSector_Common.ValueResult data)
    {
        value = data.value;
        result = data.result;
        Name = name;
    }
}
public class Value : Report_InspectSector_Common.Value
{
    public string Name { get; set; }

    public Value(string name, Report_InspectSector_Common.Value data)
    {
        value = data.value;
        Name = name;
    }
}
public class Blemish : Report_InspectSector_Blemish.Blemish
{
    public System.Drawing.Rectangle Rectangle => new(top, left, width, height);

    public Blemish(Report_InspectSector_Blemish.Blemish data)
    {
        top = data.top;
        left = data.left;
        height = data.height;
        width = data.width;
        type = data.type;
    }
}

public interface ISectorDifferences
{
    public static SectorDifferencesSettings Settings { get; } = new SectorDifferencesSettings();

    string UserName { get; set; }
    string Type { get; set; }
    string Units { get; set; }
    bool IsNotOCVMatch { get; set; }
    string OCVMatchText { get; set; }
    bool IsSectorMissing { get; set; }
    string SectorMissingText { get; set; }
    bool IsNotEmpty { get; set; }

    ObservableCollection<GradeValue> GradeValues { get; }
    ObservableCollection<ValueResult> ValueResults { get; }
    ObservableCollection<ValueResult> Gs1ValueResults { get; }
    ObservableCollection<Grade> Gs1Grades { get; }
    ObservableCollection<Value> Values { get; }
    ObservableCollection<Report_InspectSector_Common.Alarm> Alarms { get; }
    ObservableCollection<Blemish> Blemishes { get; }

    ISectorDifferences Compare(ISectorDifferences compare);

    public static bool CompareGrade(Report_InspectSector_Common.Grade source, Report_InspectSector_Common.Grade compare)
    {
        return Settings.Grade_UseGradeLetter
            ? source.letter == compare.letter
            : (compare.value <= source.value + Settings.Grade_GradeValueTolerance) && (compare.value >= source.value - Settings.Grade_GradeValueTolerance);
    }
    public static bool CompareGradeValue(Report_InspectSector_Common.GradeValue source, Report_InspectSector_Common.GradeValue compare)
    {
        return Settings.GradeValue_UseGradeLetter
            ? source.grade.letter == compare.grade.letter
            : Settings.GradeValue_UseValue
                ? (compare.value <= source.value + Settings.GradeValue_ValueTolerance) && (compare.value >= source.value - Settings.GradeValue_ValueTolerance)
                : (compare.grade.value <= source.grade.value + Settings.GradeValue_GradeValueTolerance) && (compare.grade.value >= source.grade.value - Settings.GradeValue_GradeValueTolerance);
    }
    public static bool CompareValueResult(Report_InspectSector_Common.ValueResult source, Report_InspectSector_Common.ValueResult compare)
    {
        return Settings.ValueResult_UseResult
            ? source.result == compare.result
            : (compare.value <= source.value + Settings.ValueResult_ValueTolerance) && (compare.value >= source.value - Settings.ValueResult_ValueTolerance);
    }
    public static bool CompareValue(Report_InspectSector_Common.Value source, Report_InspectSector_Common.Value compare) => (compare.value <= source.value + Settings.Value_ValueTolerance) && (compare.value >= source.value - Settings.Value_ValueTolerance);
    public static bool CompareAlarm(Report_InspectSector_Common.Alarm source, Report_InspectSector_Common.Alarm compare) => source.category == compare.category && source.data.subAlarm == compare.data.subAlarm;
}
