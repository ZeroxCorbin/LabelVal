using CommunityToolkit.Mvvm.ComponentModel;
using LabelVal.Sectors.Interfaces;

namespace LabelVal.Run.ViewModels;

internal class CompareSettingsViewModel : ObservableObject
{
    public bool Grade_UseGradeLetter { get => SectorDifferences.Settings.Grade_UseGradeLetter; set { SectorDifferences.Settings.Grade_UseGradeLetter = value; OnPropertyChanged("Grade_UseGradeLetter"); } }
    public double Grade_GradeValueTolerance { get => SectorDifferences.Settings.Grade_GradeValueTolerance; set { SectorDifferences.Settings.Grade_GradeValueTolerance = value; OnPropertyChanged("Grade_GradeValueTolerance"); } }

    public bool GradeValue_UseGradeLetter { get => SectorDifferences.Settings.GradeValue_UseGradeLetter; set { SectorDifferences.Settings.GradeValue_UseGradeLetter = value; OnPropertyChanged("GradeValue_UseGradeLetter"); } }
    public double GradeValue_GradeValueTolerance { get => SectorDifferences.Settings.GradeValue_GradeValueTolerance; set { SectorDifferences.Settings.GradeValue_GradeValueTolerance = value; OnPropertyChanged("GradeValue_GradeValueTolerance"); } }
    public bool GradeValue_UseValue { get => SectorDifferences.Settings.GradeValue_UseValue; set { SectorDifferences.Settings.GradeValue_UseValue = value; OnPropertyChanged("GradeValue_UseValue"); } }
    public double GradeValue_ValueTolerance { get => SectorDifferences.Settings.GradeValue_ValueTolerance; set { SectorDifferences.Settings.GradeValue_ValueTolerance = value; OnPropertyChanged("GradeValue_ValueTolerance"); } }

    public bool ValueResult_UseResult { get => SectorDifferences.Settings.ValueResult_UseResult; set { SectorDifferences.Settings.ValueResult_UseResult = value; OnPropertyChanged("ValueResult_UseResult"); } }
    public double ValueResult_ValueTolerance { get => SectorDifferences.Settings.ValueResult_ValueTolerance; set { SectorDifferences.Settings.ValueResult_ValueTolerance = value; OnPropertyChanged("ValueResult_ValueTolerance"); } }

    public double Value_ValueTolerance { get => SectorDifferences.Settings.Value_ValueTolerance; set { SectorDifferences.Settings.Value_ValueTolerance = value; OnPropertyChanged("Value_ValueTolerance"); } }
}
