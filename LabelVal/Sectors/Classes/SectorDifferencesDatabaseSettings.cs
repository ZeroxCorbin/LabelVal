using BarcodeVerification.lib.ISO.ParameterTypes;
using Newtonsoft.Json;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using System.IO;
using Microsoft.Win32;
using CommunityToolkit.Mvvm.Input;

namespace LabelVal.Sectors.Classes;

public partial class SectorDifferencesDatabaseSettings : ObservableObject
{
    [ObservableProperty]
    private GradeValueCompareSettings gradeValueCompareSettings = new();

    [ObservableProperty]
    private GradeCompareSettings gradeCompareSettings= new();

    [ObservableProperty]
    private ValuePassFailCompareSettings valuePassFailCompareSettings = new();

    [ObservableProperty]
    private ValueDoubleCompareSettings valueDoubleCompareSettings = new();

    [ObservableProperty]
    private PassFailCompareSettings passFailCompareSettings = new();

    [ObservableProperty]
    private GS1DecodeCompareSettings gs1DecodeCompareSettings = new();

    [ObservableProperty]
    private OverallGradeCompareSettings overallGradeCompareSettings = new();

    [ObservableProperty]
    private ValueStringCompareSettings valueStringCompareSettings = new();

    [ObservableProperty]
    private MissingCompareSettings missingCompareSettings = new();

    partial void OnGradeValueCompareSettingsChanged(GradeValueCompareSettings oldValue, GradeValueCompareSettings newValue)
    {
        if (oldValue != null)
            oldValue.PropertyChanged -= Child_PropertyChanged;
        if (newValue != null)
            newValue.PropertyChanged += Child_PropertyChanged;
    }

    partial void OnGradeCompareSettingsChanged(GradeCompareSettings oldValue, GradeCompareSettings newValue)
    {
        if (oldValue != null)
            oldValue.PropertyChanged -= Child_PropertyChanged;
        if (newValue != null)
            newValue.PropertyChanged += Child_PropertyChanged;
    }

    partial void OnValuePassFailCompareSettingsChanged(ValuePassFailCompareSettings oldValue, ValuePassFailCompareSettings newValue)
    {
        if (oldValue != null)
            oldValue.PropertyChanged -= Child_PropertyChanged;
        if (newValue != null)
            newValue.PropertyChanged += Child_PropertyChanged;
    }

    partial void OnValueDoubleCompareSettingsChanged(ValueDoubleCompareSettings oldValue, ValueDoubleCompareSettings newValue)
    {
        if (oldValue != null)
            oldValue.PropertyChanged -= Child_PropertyChanged;
        if (newValue != null)
            newValue.PropertyChanged += Child_PropertyChanged;
    }

    partial void OnPassFailCompareSettingsChanged(PassFailCompareSettings oldValue, PassFailCompareSettings newValue)
    {
        if (oldValue != null)
            oldValue.PropertyChanged -= Child_PropertyChanged;
        if (newValue != null)
            newValue.PropertyChanged += Child_PropertyChanged;
    }

    partial void OnGs1DecodeCompareSettingsChanged(GS1DecodeCompareSettings oldValue, GS1DecodeCompareSettings newValue)
    {
        if (oldValue != null)
            oldValue.PropertyChanged -= Child_PropertyChanged;
        if (newValue != null)
            newValue.PropertyChanged += Child_PropertyChanged;
    }

    partial void OnOverallGradeCompareSettingsChanged(OverallGradeCompareSettings oldValue, OverallGradeCompareSettings newValue)
    {
        if (oldValue != null)
            oldValue.PropertyChanged -= Child_PropertyChanged;
        if (newValue != null)
            newValue.PropertyChanged += Child_PropertyChanged;
    }

    partial void OnValueStringCompareSettingsChanged(ValueStringCompareSettings oldValue, ValueStringCompareSettings newValue)
    {
        if (oldValue != null)
            oldValue.PropertyChanged -= Child_PropertyChanged;
        if (newValue != null)
            newValue.PropertyChanged += Child_PropertyChanged;
    }

    partial void OnMissingCompareSettingsChanged(MissingCompareSettings oldValue, MissingCompareSettings newValue)
    {
        if (oldValue != null)
            oldValue.PropertyChanged -= Child_PropertyChanged;
        if (newValue != null)
            newValue.PropertyChanged += Child_PropertyChanged;
    }

    public SectorDifferencesDatabaseSettings()
    {
        GradeValueCompareSettings.PropertyChanged += Child_PropertyChanged;
        GradeCompareSettings.PropertyChanged += Child_PropertyChanged;
        ValuePassFailCompareSettings.PropertyChanged += Child_PropertyChanged;
        ValueDoubleCompareSettings.PropertyChanged += Child_PropertyChanged;
        PassFailCompareSettings.PropertyChanged += Child_PropertyChanged;
        Gs1DecodeCompareSettings.PropertyChanged += Child_PropertyChanged;
        OverallGradeCompareSettings.PropertyChanged += Child_PropertyChanged;
        ValueStringCompareSettings.PropertyChanged += Child_PropertyChanged;
        MissingCompareSettings.PropertyChanged += Child_PropertyChanged;
    }

    private void Child_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        App.Settings.SetValue(nameof(SectorDifferencesDatabaseSettings), this);
    }


    public void SaveToFile(string path)
    {
        var json = JsonConvert.SerializeObject(this, Formatting.Indented);
        File.WriteAllText(path, json);
    }


    public void LoadFromFile(string path)
    {
        SectorDifferencesDatabaseSettings loaded;
        if (!File.Exists(path))
        {
            loaded = new SectorDifferencesDatabaseSettings();
        }
        else
        {
            var json = File.ReadAllText(path);
            loaded = JsonConvert.DeserializeObject<SectorDifferencesDatabaseSettings>(json)
                     ?? new SectorDifferencesDatabaseSettings();
        }

        GradeValueCompareSettings = loaded.GradeValueCompareSettings;
        GradeCompareSettings = loaded.GradeCompareSettings;
        ValuePassFailCompareSettings = loaded.ValuePassFailCompareSettings;
        ValueDoubleCompareSettings = loaded.ValueDoubleCompareSettings;
        PassFailCompareSettings = loaded.PassFailCompareSettings;
        Gs1DecodeCompareSettings = loaded.Gs1DecodeCompareSettings;
        OverallGradeCompareSettings = loaded.OverallGradeCompareSettings;
        ValueStringCompareSettings = loaded.ValueStringCompareSettings;
        MissingCompareSettings = loaded.MissingCompareSettings;
    }

    [RelayCommand]
    // New: Save with file dialog
    public void SaveToFileWithDialog()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            DefaultExt = "json",
            FileName = "SectorDifferencesDatabaseSettings.json"
        };
        if (dialog.ShowDialog() == true)
        {
            SaveToFile(dialog.FileName);
        }
    }

    [RelayCommand]
    // New: Load with file dialog
    public void LoadFromFileWithDialog()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            DefaultExt = "json"
        };
        if (dialog.ShowDialog() == true)
        {
            LoadFromFile(dialog.FileName);
        }
    }
}