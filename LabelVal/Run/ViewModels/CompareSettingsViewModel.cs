using CommunityToolkit.Mvvm.ComponentModel;
using LabelVal.Sectors.ViewModels;

namespace LabelVal.Run.ViewModels;

internal class CompareSettingsViewModel : ObservableObject
{
    private Sectors.ViewModels.SectorDifferencesSettings Settings { get; } = new Sectors.ViewModels.SectorDifferencesSettings();

    public bool Grade_UseGradeLetter { get => Settings.Grade_UseGradeLetter; set { Settings.Grade_UseGradeLetter = value; OnPropertyChanged("Grade_UseGradeLetter"); } }
    public double Grade_GradeValueTolerance { get => Settings.Grade_GradeValueTolerance; set { Settings.Grade_GradeValueTolerance = value; OnPropertyChanged("Grade_GradeValueTolerance"); } }

    public bool GradeValue_UseGradeLetter { get => Settings.GradeValue_UseGradeLetter; set { Settings.GradeValue_UseGradeLetter = value; OnPropertyChanged("GradeValue_UseGradeLetter"); } }
    public double GradeValue_GradeValueTolerance { get => Settings.GradeValue_GradeValueTolerance; set { Settings.GradeValue_GradeValueTolerance = value; OnPropertyChanged("GradeValue_GradeValueTolerance"); } }
    public bool GradeValue_UseValue { get => Settings.GradeValue_UseValue; set { Settings.GradeValue_UseValue = value; OnPropertyChanged("GradeValue_UseValue"); } }
    public double GradeValue_ValueTolerance { get => Settings.GradeValue_ValueTolerance; set { Settings.GradeValue_ValueTolerance = value; OnPropertyChanged("GradeValue_ValueTolerance"); } }

    public bool ValueResult_UseResult { get => Settings.ValueResult_UseResult; set { Settings.ValueResult_UseResult = value; OnPropertyChanged("ValueResult_UseResult"); } }
    public double ValueResult_ValueTolerance { get => Settings.ValueResult_ValueTolerance; set { Settings.ValueResult_ValueTolerance = value; OnPropertyChanged("ValueResult_ValueTolerance"); } }

    public double Value_ValueTolerance { get => Settings.Value_ValueTolerance; set { Settings.Value_ValueTolerance = value; OnPropertyChanged("Value_ValueTolerance"); } }
}
