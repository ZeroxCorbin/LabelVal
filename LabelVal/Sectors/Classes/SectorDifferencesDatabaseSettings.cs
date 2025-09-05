using BarcodeVerification.lib.ISO.ParameterTypes;
using System.ComponentModel;

namespace LabelVal.Sectors.Classes;

public class SectorDifferencesDatabaseSettings
{
    public static SectorDifferencesDatabaseSettings Instance { get; } = new();

    private GradeValueCompareSettings _gradeValueCompareSettings;
    private GradeCompareSettings _gradeCompareSettings;
    private ValuePassFailCompareSettings _valuePassFailCompareSettings;
    private ValueDoubleCompareSettings _valueDoubleCompareSettings;
    private PassFailCompareSettings _passFailCompareSettings;
    private GS1DecodeCompareSettings _gs1DecodeCompareSettings;
    private OverallGradeCompareSettings _overallGradeCompareSettings;
    private ValueStringCompareSettings _valueStringCompareSettings;
    private MissingCompareSettings _missingCompareSettings;

    public GradeValueCompareSettings GradeValueCompareSettings
    {
        get
        {
            if (_gradeValueCompareSettings == null)
            {
                _gradeValueCompareSettings = App.Settings.GetValue(nameof(GradeValueCompareSettings), new GradeValueCompareSettings());
                _gradeValueCompareSettings.PropertyChanged += (s, e) => App.Settings.SetValue(nameof(GradeValueCompareSettings), _gradeValueCompareSettings);
            }
            return _gradeValueCompareSettings;
        }
    }

    public GradeCompareSettings GradeCompareSettings
    {
        get
        {
            if (_gradeCompareSettings == null)
            {
                _gradeCompareSettings = App.Settings.GetValue(nameof(GradeCompareSettings), new GradeCompareSettings());
                _gradeCompareSettings.PropertyChanged += (s, e) => App.Settings.SetValue(nameof(GradeCompareSettings), _gradeCompareSettings);
            }
            return _gradeCompareSettings;
        }
    }

    public ValuePassFailCompareSettings ValuePassFailCompareSettings
    {
        get
        {
            if (_valuePassFailCompareSettings == null)
            {
                _valuePassFailCompareSettings = App.Settings.GetValue(nameof(ValuePassFailCompareSettings), new ValuePassFailCompareSettings());
                _valuePassFailCompareSettings.PropertyChanged += (s, e) => App.Settings.SetValue(nameof(ValuePassFailCompareSettings), _valuePassFailCompareSettings);
            }
            return _valuePassFailCompareSettings;
        }
    }

    public ValueDoubleCompareSettings ValueDoubleCompareSettings
    {
        get
        {
            if (_valueDoubleCompareSettings == null)
            {
                _valueDoubleCompareSettings = App.Settings.GetValue(nameof(ValueDoubleCompareSettings), new ValueDoubleCompareSettings());
                _valueDoubleCompareSettings.PropertyChanged += (s, e) => App.Settings.SetValue(nameof(ValueDoubleCompareSettings), _valueDoubleCompareSettings);
            }
            return _valueDoubleCompareSettings;
        }
    }

    public PassFailCompareSettings PassFailCompareSettings
    {
        get
        {
            if (_passFailCompareSettings == null)
            {
                _passFailCompareSettings = App.Settings.GetValue(nameof(PassFailCompareSettings), new PassFailCompareSettings());
                _passFailCompareSettings.PropertyChanged += (s, e) => App.Settings.SetValue(nameof(PassFailCompareSettings), _passFailCompareSettings);
            }
            return _passFailCompareSettings;
        }
    }

    public GS1DecodeCompareSettings GS1DecodeCompareSettings
    {
        get
        {
            if (_gs1DecodeCompareSettings == null)
            {
                _gs1DecodeCompareSettings = App.Settings.GetValue(nameof(GS1DecodeCompareSettings), new GS1DecodeCompareSettings());
                _gs1DecodeCompareSettings.PropertyChanged += (s, e) => App.Settings.SetValue(nameof(GS1DecodeCompareSettings), _gs1DecodeCompareSettings);
            }
            return _gs1DecodeCompareSettings;
        }
    }

    public OverallGradeCompareSettings OverallGradeCompareSettings
    {
        get
        {
            if (_overallGradeCompareSettings == null)
            {
                _overallGradeCompareSettings = App.Settings.GetValue(nameof(OverallGradeCompareSettings), new OverallGradeCompareSettings());
                _overallGradeCompareSettings.PropertyChanged += (s, e) => App.Settings.SetValue(nameof(OverallGradeCompareSettings), _overallGradeCompareSettings);
            }
            return _overallGradeCompareSettings;
        }
    }

    public ValueStringCompareSettings ValueStringCompareSettings
    {
        get
        {
            if (_valueStringCompareSettings == null)
            {
                _valueStringCompareSettings = App.Settings.GetValue(nameof(ValueStringCompareSettings), new ValueStringCompareSettings());
                _valueStringCompareSettings.PropertyChanged += (s, e) => App.Settings.SetValue(nameof(ValueStringCompareSettings), _valueStringCompareSettings);
            }
            return _valueStringCompareSettings;
        }
    }

    public MissingCompareSettings MissingCompareSettings
    {
        get
        {
            if (_missingCompareSettings == null)
            {
                _missingCompareSettings = App.Settings.GetValue(nameof(MissingCompareSettings), new MissingCompareSettings());
                _missingCompareSettings.PropertyChanged += (s, e) => App.Settings.SetValue(nameof(MissingCompareSettings), _missingCompareSettings);
            }
            return _missingCompareSettings;
        }
    }
}