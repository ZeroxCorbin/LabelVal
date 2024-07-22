using System.Collections.ObjectModel;
using V275_REST_lib.Models;

namespace LabelVal.Sectors.Interfaces;

public class SectorDifferencesSettings
{
    public bool GradeValue_UseGradeLetter { get => App.Settings.GetValue("Diff_GradeValue_UseGradeLetter", true); set => App.Settings.SetValue("Diff_GradeValue_UseGradeLetter", value); }
    public double GradeValue_GradeValueTolerance { get => App.Settings.GetValue("Diff_GradeValue_GradeValueTolerance", 0.01); set => App.Settings.SetValue("Diff_GradeValue_GradeValueTolerance", value); }
    public bool GradeValue_UseValue { get => App.Settings.GetValue("Diff_GradeValue_UseValue", false); set => App.Settings.SetValue("Diff_GradeValue_UseValue", value); }
    public double GradeValue_ValueTolerance { get => App.Settings.GetValue("Diff_GradeValue_ValueTolerance", 0.1); set => App.Settings.SetValue("Diff_GradeValue_ValueTolerance", value); }

    public bool Grade_UseGradeLetter { get => App.Settings.GetValue("Diff_Grade_UseGradeLetter", true); set => App.Settings.SetValue("Diff_Grade_UseGradeLetter", value); }
    public double Grade_GradeValueTolerance { get => App.Settings.GetValue("Diff_Grade_GradeValueTolerance", 0.1); set => App.Settings.SetValue("Diff_Grade_GradeValueTolerance", value); }

    public bool ValueResult_UseResult { get => App.Settings.GetValue("Diff_ValueResult_UseResult", true); set => App.Settings.SetValue("Diff_ValueResult_UseResult", value); }
    public double ValueResult_ValueTolerance { get => App.Settings.GetValue("Diff_ValueResult_ValueTolerance", 5.0); set => App.Settings.SetValue("Diff_ValueResult_ValueTolerance", value); }

    public double Value_ValueTolerance { get => App.Settings.GetValue("Diff_Value_ValueTolerance", 5.0); set => App.Settings.SetValue("Diff_Value_ValueTolerance", value); }
}

public class GradeValue
{
    public string Name { get; set; }
    public int Value { get; set; }
    public Grade Grade { get; set; }


    public GradeValue(string name, int value, Grade grade)
    {
        Value = value;
        Grade = grade;
        Name = name;
    }
    public GradeValue(string name, GradeValue gradeValue)
    {
        if (gradeValue != null)
        {
            Value = gradeValue.Value;
            Grade = gradeValue.Grade;
        }
        Name = name;
    }
}
public class Grade
{
    public string Name { get; set; }
    public float Value { get; set; }
    public string Letter { get; set; }


    public Grade(string name, float value, string letter)
    {
        Value = value;
        Letter = letter;
        Name = name;
    }
    public Grade(string name, Grade grade)
    {
        if (grade != null)
        {
            Value = grade.Value;
            Letter = grade.Letter;
        }
        Name = name;
    }
}
public class ValueResult
{
    public string Name { get; set; }
    public float Value { get; set; }
    public string Result { get; set; }

    public ValueResult(string name, float value, string result)
    {
        Value = value;
        Result = result;
        Name = name;
    }
}
public class Value_
{
    public string Name { get; set; }
    public int Value { get; set; }

    public Value_(string name, int value)
    {
        Value = value;
        Name = name;
    }
}
public class Blemish
{
    public int Top { get; set; }
    public int Left { get; set; }
    public int Height { get; set; }
    public int Width { get; set; }
    public string Type { get; set; }

    public System.Drawing.Rectangle Rectangle => new(Top, Left, Width, Height);

    public Blemish(int top, int left, int height, int width, string type)
    {
        Top = top;
        Left = left;
        Height = height;
        Width = width;
        Type = type;
    }
}

public class Alarm
{
    public string Name { get; set; }
    public int Category { get; set; }
    public SubAlarm_ Data { get; set; }
    public Useraction UserAction { get; set; }
}
public class SubAlarm_
{
    public string Text { get; set; }
    public int Index { get; set; }
    public string SubAlarm { get; set; }
    public string Expected { get; set; }
}
public class Useraction
{
    public string Action { get; set; }
    public string User { get; set; }
    public string Note { get; set; }
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
    ObservableCollection<Value_> Values { get; }
    ObservableCollection<Alarm> Alarms { get; }
    ObservableCollection<Blemish> Blemishes { get; }

    ISectorDifferences Compare(ISectorDifferences compare);

    public static bool CompareGrade(Grade source, Grade compare)
    {
        return Settings.Grade_UseGradeLetter
            ? source.Letter == compare.Letter
            : (compare.Value <= source.Value + Settings.Grade_GradeValueTolerance) && (compare.Value >= source.Value - Settings.Grade_GradeValueTolerance);
    }
    public static bool CompareGradeValue(GradeValue source, GradeValue compare)
    {
        return Settings.GradeValue_UseGradeLetter
            ? source.Grade.Letter == compare.Grade.Letter
            : Settings.GradeValue_UseValue
                ? (compare.Value <= source.Value + Settings.GradeValue_ValueTolerance) && (compare.Value >= source.Value - Settings.GradeValue_ValueTolerance)
                : (compare.Grade.Value <= source.Grade.Value + Settings.GradeValue_GradeValueTolerance) && (compare.Grade.Value >= source.Grade.Value - Settings.GradeValue_GradeValueTolerance);
    }
    public static bool CompareValueResult(ValueResult source, ValueResult compare)
    {
        return Settings.ValueResult_UseResult
            ? source.Result == compare.Result
            : (compare.Value <= source.Value + Settings.ValueResult_ValueTolerance) && (compare.Value >= source.Value - Settings.ValueResult_ValueTolerance);
    }
    public static bool CompareValue(Value_ source, Value_ compare) => (compare.Value <= source.Value + Settings.Value_ValueTolerance) && (compare.Value >= source.Value - Settings.Value_ValueTolerance);
    public static bool CompareAlarm(Alarm source, Alarm compare) => source.Category == compare.Category && source.Data.SubAlarm == compare.Data.SubAlarm;
}
