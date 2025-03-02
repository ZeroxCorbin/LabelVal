namespace LabelVal.Sectors.Interfaces;

public class SectorDifferences
{
    public static SectorDifferencesSettings Settings { get; } = new SectorDifferencesSettings();

    //Name = Name,
    //        UserName = UserName,
    //        SymbolType = SymbolType
    //        Units = Units
    public string Name { get; set; }
    public string UserName { get; set; }
    public string SymbolType { get; set; }
    public string Units { get; set; }

    public bool IsSectorMissing { get; set; }
    public string SectorMissingText { get; set; }

    public static SectorDifferences Compare(ISectorDetails previous, ISectorDetails current)
    {

        SectorDifferences differences = new()
        {
            Name = current.Name,
            UserName = current.UserName,
            SymbolType = current.SymbolType,
            Units = current.Units,
        };

        foreach (var gradeValue in previous.GradeValues)
        {
            if (current.GradeValues.FirstOrDefault(x => x.Name == gradeValue.Name) is GradeValue currentGradeValue)
            {
                if(new SectorElements(gradeValue, currentGradeValue).Difference != null)
                    differences.GradeValues.Add(new SectorElements(gradeValue, currentGradeValue));
            }
        }

        foreach (var valueResult in previous.ValueResults)
        {
            if (current.ValueResults.FirstOrDefault(x => x.Name == valueResult.Name) is ValueResult currentValueResult)
            {
                if (new SectorElements(valueResult, currentValueResult).Difference != null)
                    differences.ValueResults.Add(new SectorElements(valueResult, currentValueResult));
            }
        }

        foreach (var gs1ValueResult in previous.Gs1ValueResults)
        {
            if (current.Gs1ValueResults.FirstOrDefault(x => x.Name == gs1ValueResult.Name) is ValueResult currentGs1ValueResult)
            {
                if (new SectorElements(gs1ValueResult, currentGs1ValueResult).Difference != null)
                    differences.Gs1ValueResults.Add(new SectorElements(gs1ValueResult, currentGs1ValueResult));
            }
        }

        foreach (var gs1Grade in previous.Gs1Grades)
        {
            if (current.Gs1Grades.FirstOrDefault(x => x.Name == gs1Grade.Name) is Grade currentGs1Grade)
            {
                if (new SectorElements(gs1Grade, currentGs1Grade).Difference != null)
                    differences.Gs1Grades.Add(new SectorElements(gs1Grade, currentGs1Grade));
            }
        }

        foreach (var value in previous.Values)
        {
            if (current.Values.FirstOrDefault(x => x.Name == value.Name) is Value_ currentValue)
            {
                if (new SectorElements(value, currentValue).Difference != null)
                    differences.Values.Add(new SectorElements(value, currentValue));
            }
        }

        foreach (var alarm in previous.Alarms)
        {
            if (current.Alarms.FirstOrDefault(x => x.Name == alarm.Name) is Alarm currentAlarm)
            {
                if (new SectorElements(alarm, currentAlarm).Difference != null)
                    differences.Alarms.Add(new SectorElements(alarm, currentAlarm));
            }
        }

        //foreach (var blemish in previous.Blemishes)
        //{
        //    if (current.Blemishes.FirstOrDefault(x => x. == blemish.Name) is Blemish currentBlemish)
        //    {
        //        if (new SectorElements(blemish, currentBlemish).Difference != null)
        //            differences.Blemishes.Add(new SectorElements(blemish, currentBlemish));
        //    }
        //}

        return differences;

    }

    public List<SectorElements> GradeValues { get; } = [];
    public List<SectorElements> ValueResults { get; } = [];
    public List<SectorElements> Gs1ValueResults { get; } = [];
    public List<SectorElements> Gs1Grades { get; } = [];
    public List<SectorElements> Values { get; } = [];
    public List<SectorElements> Alarms { get; } = [];
    public List<SectorElements> Blemishes { get; } = [];


    public static bool CompareGrade(Grade source, Grade compare) => Settings.Grade_UseGradeLetter
            ? source.Letter == compare.Letter
            : (compare.Value <= source.Value + Settings.Grade_GradeValueTolerance) && (compare.Value >= source.Value - Settings.Grade_GradeValueTolerance);
    public static bool CompareGradeValue(GradeValue source, GradeValue compare) => Settings.GradeValue_UseGradeLetter
            ? source.Grade.Letter == compare.Grade.Letter
            : Settings.GradeValue_UseValue
                ? (compare.Value <= source.Value + Settings.GradeValue_ValueTolerance) && (compare.Value >= source.Value - Settings.GradeValue_ValueTolerance)
                : (compare.Grade.Value <= source.Grade.Value + Settings.GradeValue_GradeValueTolerance) && (compare.Grade.Value >= source.Grade.Value - Settings.GradeValue_GradeValueTolerance);
    public static bool CompareValueResult(ValueResult source, ValueResult compare) => Settings.ValueResult_UseResult
            ? source.Result == compare.Result
            : (compare.Value <= source.Value + Settings.ValueResult_ValueTolerance) && (compare.Value >= source.Value - Settings.ValueResult_ValueTolerance);
    public static bool CompareValue(Value_ source, Value_ compare) => (compare.Value <= source.Value + Settings.Value_ValueTolerance) && (compare.Value >= source.Value - Settings.Value_ValueTolerance);
    public static bool CompareAlarm(Alarm source, Alarm compare) => source.Category == compare.Category && source.Data?.SubAlarm == compare.Data?.SubAlarm;

}

public class SectorDifference
{
    public string Name { get; set; }
    public object Previous { get; set; }
    public object Current { get; set; }

}

public class SectorElements(object previous, object current)
{
    public object Previous { get; set; } = previous;
    public object Current { get; set; } = current;

    public List<SectorDifference> Difference
    {
        get
        {
            List<SectorDifference> differences = new();

            if (Previous is GradeValue previous && Current is GradeValue current)
            {
                if (!SectorDifferences.CompareGradeValue(previous, current))
                {
                    differences.Add(new SectorDifference { Name = current.Name, Previous = previous, Current = current });
                }
                if (!SectorDifferences.CompareGrade(current.Grade, previous.Grade))
                {
                    differences.Add(new SectorDifference { Name = current.Name, Previous = current.Grade, Current = previous.Grade });
                }
            }

            if (Previous is Grade previousGrade && Current is Grade currentGrade)
            {
                if (!SectorDifferences.CompareGrade(currentGrade, previousGrade))
                {
                    differences.Add(new SectorDifference { Name = currentGrade.Name, Previous = previousGrade, Current = currentGrade });
                }
            }

            if (Previous is ValueResult previousValueResult && Current is ValueResult currentValueResult)
            {
                if (!SectorDifferences.CompareValueResult(previousValueResult, currentValueResult))
                {
                    differences.Add(new SectorDifference { Name = currentValueResult.Name, Previous = previousValueResult, Current = currentValueResult });
                }
            }

            return differences.Count > 0 ? differences : null;
        }

    }



}

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
