using LabelVal.Sectors.Interfaces;

namespace LabelVal.Sectors.Classes;

public class SectorElement(string name, object previous, object current)
{
    public string Name { get; } = name;
    public object Previous { get; } = previous;
    public object Current { get; } = current;

    public List<SectorDifference> Difference
    {
        get
        {
            List<SectorDifference> differences = [];

            //if (Previous is GradeValue previous && Current is GradeValue current)
            //{
            //    if (!SectorDifferences.CompareGradeValue(previous, current))
            //        differences.Add(new SectorDifference { Name = current.Name, Previous = previous, Current = current });
            //    if (!SectorDifferences.CompareGrade(current.Grade, previous.Grade))
            //        differences.Add(new SectorDifference { Name = current.Name, Previous = current.Grade, Current = previous.Grade });
            //}

            //if (Previous is Grade previousGrade && Current is Grade currentGrade)
            //    if (!SectorDifferences.CompareGrade(currentGrade, previousGrade))
            //        differences.Add(new SectorDifference { Name = currentGrade.Name, Previous = previousGrade, Current = currentGrade });

            //if (Previous is ValueResult previousValueResult && Current is ValueResult currentValueResult)
            //    if (!SectorDifferences.CompareValueResult(previousValueResult, currentValueResult))
            //        differences.Add(new SectorDifference { Name = currentValueResult.Name, Previous = previousValueResult, Current = currentValueResult });

            //if (Previous is Value_ previousValue && Current is Value_ currentValue)
            //    if (!SectorDifferences.CompareValue(previousValue, currentValue))
            //        differences.Add(new SectorDifference { Name = currentValue.Name, Previous = previousValue, Current = currentValue });

            if (Previous is Alarm previousAlarm && Current is Alarm currentAlarm)
                if (!SectorDifferences.CompareAlarm(previousAlarm, currentAlarm))
                    differences.Add(new SectorDifference { Name = currentAlarm.Name, Previous = previousAlarm, Current = currentAlarm });

            return differences.Count > 0 ? differences : null;
        }
    }
}
