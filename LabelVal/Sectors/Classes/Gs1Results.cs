namespace LabelVal.Sectors.Classes;

public class Gs1Results
{
    public bool Validated { get; set; }
    public string Input { get; set; }
    public string FormattedOut { get; set; }
    public List<string> Fields { get; set; }
    public string Error { get; set; }
}
