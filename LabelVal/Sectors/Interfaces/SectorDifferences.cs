namespace LabelVal.Sectors.Interfaces;

public class SectorDifferences
{
    public static SectorDifferencesSettings Settings { get; } = new SectorDifferencesSettings();

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

        Compare(differences.GradeValues, previous.GradeValues, current.GradeValues);
        Compare(differences.ValueResults, previous.ValueResults, current.ValueResults);
        Compare(differences.Gs1ValueResults, previous.Gs1ValueResults, current.Gs1ValueResults);
        Compare(differences.Gs1Grades, previous.Gs1Grades, current.Gs1Grades);
        Compare(differences.Values, previous.Values, current.Values);
        Compare(differences.Alarms, previous.Alarms, current.Alarms);
        Compare(differences.Blemishes, previous.Blemishes, current.Blemishes);

        return differences;

    }

    private static void Compare(List<SectorElements> differences, IEnumerable<ISectorValue> previous, IEnumerable<ISectorValue> current)
    {
        foreach (var pre in previous)
        {
            var cur = current.FirstOrDefault(x => x.Name == pre.Name);
            if (cur != null)
            {
                if (new SectorElements(pre.Name, pre, cur).Difference != null)
                    differences.Add(new SectorElements(pre.Name, pre, cur));
            }
            else
                differences.Add(new SectorElements(pre.Name, pre, null));

        }
        foreach (var cur in current)
        {
            var pre = previous.FirstOrDefault(x => x.Name == cur.Name);
            if (pre != null)
            {
                if (differences.Any(x => x.Name == pre.Name))
                    continue;
                if (new SectorElements(cur.Name, pre, cur).Difference != null)
                    differences.Add(new SectorElements(cur.Name, pre, cur));
            }
            else
                differences.Add(new SectorElements(cur.Name, null, cur));
        }
    }


    public List<SectorElements> Others { get; } = [];
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

public class SectorElements(string name, object previous, object current)
{
    public string Name { get; } = name;
    public object Previous { get; } = previous;
    public object Current { get; } = current;

    public List<SectorDifference> Difference
    {
        get
        {
            List<SectorDifference> differences = [];

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

            if (Previous is Value_ previousValue && Current is Value_ currentValue)
            {
                if (!SectorDifferences.CompareValue(previousValue, currentValue))
                {
                    differences.Add(new SectorDifference { Name = currentValue.Name, Previous = previousValue, Current = currentValue });
                }
            }

            if (Previous is Alarm previousAlarm && Current is Alarm currentAlarm)
            {
                if (!SectorDifferences.CompareAlarm(previousAlarm, currentAlarm))
                {
                    differences.Add(new SectorDifference { Name = currentAlarm.Name, Previous = previousAlarm, Current = currentAlarm });
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
