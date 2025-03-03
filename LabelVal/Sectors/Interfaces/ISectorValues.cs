namespace LabelVal.Sectors.Interfaces;

public interface ISectorValue
{
    string Name { get; set; }
}

public class GradeValue : ISectorValue
{
    public string Name { get; set; }
    public double Value { get; set; }
    public Grade Grade { get; set; }

    public GradeValue(string name, double value, Grade grade)
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
public class Grade : ISectorValue
{
    public string Name { get; set; }
    public double Value { get; set; }
    public string Letter { get; set; }

    public Grade(string name, double value, string letter)
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
public class ValueResult : ISectorValue
{
    public string Name { get; set; }
    public double Value { get; set; }
    public string Result { get; set; }

    public ValueResult(string name, double value, string result)
    {
        Value = value;
        Result = result;
        Name = name;
    }
}
public class Value_ : ISectorValue
{
    public string Name { get; set; }
    public double Value { get; set; }

    public Value_(string name, double value)
    {
        Value = value;
        Name = name;
    }
}
public class Blemish : ISectorValue
{
    public string Name { get; set; }

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

public class Alarm : ISectorValue
{
    public string Name { get; set; } = null;
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
