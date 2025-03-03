namespace LabelVal.Sectors.Classes;

public class ModuleData
{
    public int[] ModuleModulation { get; set; }
    public int[] ModuleReflectance { get; set; }

    public int QuietZone { get; set; }

    public int NumRows { get; set; }
    public int NumColumns { get; set; }

    public double CosAngle0 { get; set; }
    public double CosAngle1 { get; set; }

    public double SinAngle0 { get; set; }
    public double SinAngle1 { get; set; }

    public double DeltaX { get; set; }
    public double DeltaY { get; set; }

    public double Xne { get; set; }
    public double Yne { get; set; }

    public double Xnw { get; set; }
    public double Ynw { get; set; }

    public double Xsw { get; set; }
    public double Ysw { get; set; }
}
