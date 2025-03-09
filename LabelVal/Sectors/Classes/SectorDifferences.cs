using LabelVal.Sectors.Interfaces;

namespace LabelVal.Sectors.Classes;

public class SectorDifferences
{
    public static SectorDifferencesSettings Settings { get; } = new SectorDifferencesSettings();

    public string Name { get; set; }
    public string UserName { get; set; }
    public string SymbolType { get; set; }
    public string Units { get; set; }

    public bool IsSectorMissing { get; set; }
    public string SectorMissingText { get; set; }

    public bool HasDifferences { get; set; }

    public static SectorDifferences? Compare(ISectorDetails previous, ISectorDetails current)
    {

        SectorDifferences differences = new()
        {
            Name = current.Name,
            UserName = current.UserName,
            SymbolType = current.SymbolType,
            Units = current.Units,
        };

        //if(new SectorElement(current.Name, new Value_("Aperture", previous.Sector.Report.Aperture), new Value_("Aperture", current.Sector.Report.Aperture)).Difference != null)
        //    differences.Others.Add(new SectorElement(current.Name, new Value_("Aperture", previous.Sector.Report.Aperture), new Value_("Aperture", current.Sector.Report.Aperture)));

        //if (new SectorElement(current.Name, new Value_("X Dimension", previous.Sector.Report.XDimension), new Value_("X Dimension", current.Sector.Report.XDimension)).Difference != null)
        //    differences.Others.Add(new SectorElement(current.Name, new Value_("X Dimension", previous.Sector.Report.XDimension), new Value_("X Dimension", current.Sector.Report.XDimension)));

        //Compare(differences.GradeValues, previous.GradeValues, current.GradeValues);
        //Compare(differences.ValueResults, previous.ValueResults, current.ValueResults);
        //Compare(differences.Gs1ValueResults, previous.Gs1ValueResults, current.Gs1ValueResults);
        //Compare(differences.Gs1Grades, previous.Gs1Grades, current.Gs1Grades);
        //Compare(differences.Values, previous.Values, current.Values);
        Compare(differences.Alarms, previous.Alarms, current.Alarms);
        Compare(differences.Blemishes, previous.Blemishes, current.Blemishes);

        differences.HasDifferences = differences.Others.Count > 0 || differences.GradeValues.Count > 0 || differences.ValueResults.Count > 0 || differences.Gs1ValueResults.Count > 0 || differences.Gs1Grades.Count > 0 || differences.Values.Count > 0 || differences.Alarms.Count > 0 || differences.Blemishes.Count > 0;

        return differences.HasDifferences ? differences : null;

    }

    private static void Compare(List<SectorElement> differences, IEnumerable<ISectorValue> previous, IEnumerable<ISectorValue> current)
    {
        foreach (ISectorValue pre in previous)
        {
            ISectorValue cur = current.FirstOrDefault(x => x.Name == pre.Name);
            if (cur != null)
            {
                if (new SectorElement(pre.Name, pre, cur).Difference != null)
                    differences.Add(new SectorElement(pre.Name, pre, cur));
            }

            else
                differences.Add(new SectorElement(pre.Name, pre, null));

        }
        foreach (ISectorValue cur in current)
        {
            ISectorValue pre = previous.FirstOrDefault(x => x.Name == cur.Name);
            if (pre != null)
            {
                if (differences.Any(x => x.Name == pre.Name))
                    continue;
                if (new SectorElement(cur.Name, pre, cur).Difference != null)
                    differences.Add(new SectorElement(cur.Name, pre, cur));
            }
            else
                differences.Add(new SectorElement(cur.Name, null, cur));
        }
    }

    public List<SectorElement> Others { get; } = [];
    public List<SectorElement> GradeValues { get; } = [];
    public List<SectorElement> ValueResults { get; } = [];
    public List<SectorElement> Gs1ValueResults { get; } = [];
    public List<SectorElement> Gs1Grades { get; } = [];
    public List<SectorElement> Values { get; } = [];
    public List<SectorElement> Alarms { get; } = [];
    public List<SectorElement> Blemishes { get; } = [];

    //public static bool CompareGrade(Grade source, Grade compare) => Settings.Grade_UseGradeLetter
    //        ? source.Letter == compare.Letter
    //        : compare.Value <= source.Value + Settings.Grade_GradeValueTolerance && compare.Value >= source.Value - Settings.Grade_GradeValueTolerance;
    //public static bool CompareGradeValue(GradeValue source, GradeValue compare) => Settings.GradeValue_UseGradeLetter
    //        ? source.Grade.Letter == compare.Grade.Letter
    //        : Settings.GradeValue_UseValue
    //            ? compare.Value <= source.Value + Settings.GradeValue_ValueTolerance && compare.Value >= source.Value - Settings.GradeValue_ValueTolerance
    //            : compare.Grade.Value <= source.Grade.Value + Settings.GradeValue_GradeValueTolerance && compare.Grade.Value >= source.Grade.Value - Settings.GradeValue_GradeValueTolerance;
    //public static bool CompareValueResult(ValueResult source, ValueResult compare) => Settings.ValueResult_UseResult
    //        ? source.Result == compare.Result
    //        : compare.Value <= source.Value + Settings.ValueResult_ValueTolerance && compare.Value >= source.Value - Settings.ValueResult_ValueTolerance;
    //public static bool CompareValue(Value_ source, Value_ compare) => compare.Value <= source.Value + Settings.Value_ValueTolerance && compare.Value >= source.Value - Settings.Value_ValueTolerance;
    public static bool CompareAlarm(Alarm source, Alarm compare) => source.Category == compare.Category && source.Data?.SubAlarm == compare.Data?.SubAlarm;

}

