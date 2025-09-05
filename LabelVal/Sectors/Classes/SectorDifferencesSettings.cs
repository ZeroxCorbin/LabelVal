using BarcodeVerification.lib.ISO.ParameterTypes;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LabelVal.Run.ViewModels;

internal partial class SectorDifferencesSettings : ObservableObject
{
    [ObservableProperty]
    private GradeValueCompareSettings _gradeValueCompareSettings = new();

    [ObservableProperty]
    private GradeCompareSettings _gradeCompareSettings = new();

    [ObservableProperty]
    private ValuePassFailCompareSettings _valuePassFailCompareSettings = new();

    [ObservableProperty]
    private ValueDoubleCompareSettings _valueDoubleCompareSettings = new();

    [ObservableProperty]
    private PassFailCompareSettings _passFailCompareSettings = new();

    [ObservableProperty]
    private GS1DecodeCompareSettings _gS1DecodeCompareSettings = new();

    [ObservableProperty]
    private OverallGradeCompareSettings _overallGradeCompareSettings = new();

    [ObservableProperty]
    private ValueStringCompareSettings _valueStringCompareSettings = new();
}