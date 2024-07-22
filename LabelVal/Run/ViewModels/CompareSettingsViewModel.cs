using CommunityToolkit.Mvvm.ComponentModel;
using LabelVal.Sectors.Interfaces;

namespace LabelVal.Run.ViewModels;

internal class CompareSettingsViewModel : ObservableObject
{
    public bool Grade_UseGradeLetter { get => ISectorDifferences.Settings.Grade_UseGradeLetter; set { ISectorDifferences.Settings.Grade_UseGradeLetter = value; OnPropertyChanged("Grade_UseGradeLetter"); } }
    public double Grade_GradeValueTolerance { get => ISectorDifferences.Settings.Grade_GradeValueTolerance; set { ISectorDifferences.Settings.Grade_GradeValueTolerance = value; OnPropertyChanged("Grade_GradeValueTolerance"); } }

    public bool GradeValue_UseGradeLetter { get => ISectorDifferences.Settings.GradeValue_UseGradeLetter; set { ISectorDifferences.Settings.GradeValue_UseGradeLetter = value; OnPropertyChanged("GradeValue_UseGradeLetter"); } }
    public double GradeValue_GradeValueTolerance { get => ISectorDifferences.Settings.GradeValue_GradeValueTolerance; set { ISectorDifferences.Settings.GradeValue_GradeValueTolerance = value; OnPropertyChanged("GradeValue_GradeValueTolerance"); } }
    public bool GradeValue_UseValue { get => ISectorDifferences.Settings.GradeValue_UseValue; set { ISectorDifferences.Settings.GradeValue_UseValue = value; OnPropertyChanged("GradeValue_UseValue"); } }
    public double GradeValue_ValueTolerance { get => ISectorDifferences.Settings.GradeValue_ValueTolerance; set { ISectorDifferences.Settings.GradeValue_ValueTolerance = value; OnPropertyChanged("GradeValue_ValueTolerance"); } }

    public bool ValueResult_UseResult { get => ISectorDifferences.Settings.ValueResult_UseResult; set { ISectorDifferences.Settings.ValueResult_UseResult = value; OnPropertyChanged("ValueResult_UseResult"); } }
    public double ValueResult_ValueTolerance { get => ISectorDifferences.Settings.ValueResult_ValueTolerance; set { ISectorDifferences.Settings.ValueResult_ValueTolerance = value; OnPropertyChanged("ValueResult_ValueTolerance"); } }

    public double Value_ValueTolerance { get => ISectorDifferences.Settings.Value_ValueTolerance; set { ISectorDifferences.Settings.Value_ValueTolerance = value; OnPropertyChanged("Value_ValueTolerance"); } }
}
