namespace LabelVal.Sectors.Classes;


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