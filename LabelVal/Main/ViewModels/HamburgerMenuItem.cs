namespace LabelVal.Main.ViewModels;
public class HamburgerMenuItem
{
    public string Label { get; set; }
    public object Content { get; set; }
    public bool IsNotSelectable { get; set; }
    public bool OpensWindow { get; set; } // When true the item behaves like a command and can open its own window (not stay selected).
}
