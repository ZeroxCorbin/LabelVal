using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.Extensions;
using BarcodeVerification.lib.GS1;
using BarcodeVerification.lib.ISO.ParameterTypes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabelVal.Sectors.Extensions;
using LabelVal.Sectors.Interfaces;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.Windows;

namespace LabelVal.V5.Sectors;

public partial class Sector : ObservableObject, ISector, IDisposable
{
    public Devices Device { get; } = Devices.V5;
    public string Version { get; }

    public ISectorTemplate Template { get; }
    public ISectorReport Report { get; }

    public ISectorParameters SectorDetails { get; }
    public ObservableCollection<IParameterValue> FocusedParameters { get; } = [];
    public bool IsWarning { get; }
    public bool IsError { get; }

    public ApplicationStandards DesiredApplicationStandard { get; }
    public ObservableCollection<GradingStandards> DesiredGradingStandards { get; } = [];
    public GS1Tables DesiredGS1Table { get; }

    public bool IsWrongStandard
    {
        get
        {
            var found = false;
            foreach (var gradingStandard in DesiredGradingStandards)
            {
                if (gradingStandard == Report.GradingStandard)
                {
                    found = true;
                    break;
                }
                else if (gradingStandard == GradingStandards.None)
                {
                    found = true;
                    break;
                }
            }

            return (DesiredApplicationStandard == ApplicationStandards.None && !found) || ((DesiredApplicationStandard != ApplicationStandards.None || !found) && (DesiredApplicationStandard != Report.ApplicationStandard || !found));

        }
    }

    public bool IsFocused { get; set; }
    public bool IsMouseOver { get; set; }

    public bool ShowApplicationParameters
    {
        get => App.Settings.GetValue(nameof(ShowApplicationParameters), true, true);
        set
        {
            if (App.Settings.GetValue(nameof(ShowApplicationParameters), true, true) == value) return;
            App.Settings.SetValue(nameof(ShowApplicationParameters), value);
        }
    }

    public bool ShowGradingParameters
    {
        get => App.Settings.GetValue(nameof(ShowGradingParameters), true, true);
        set
        {
            if (App.Settings.GetValue(nameof(ShowGradingParameters), true, true) == value) return;
            App.Settings.SetValue(nameof(ShowGradingParameters), value);
        }
    }

    public bool ShowSymbologyParameters
    {
        get => App.Settings.GetValue(nameof(ShowSymbologyParameters), true, true);
        set
        {
            if (App.Settings.GetValue(nameof(ShowSymbologyParameters), true, true) == value) return;
            App.Settings.SetValue(nameof(ShowSymbologyParameters), value);
        }
    }

    private List<Parameters> _selectedParameters => App.Settings.GetValue("SelectedParameters", new List<Parameters>(), true);

    private bool disposedValue;

    public Sector(JObject report, JObject template, GradingStandards[] gradingStandards, ApplicationStandards appStandard, GS1Tables table, string version)
    {
        Version = version;

        DesiredApplicationStandard = appStandard;
        if (gradingStandards != null && gradingStandards.Length > 0)
        {
            foreach (var standard in gradingStandards)
            {
                DesiredGradingStandards.Add(standard);
            }
        }
        DesiredGS1Table = table;

        var toolUid = report.GetParameter<string>("toolUid");
        if (string.IsNullOrWhiteSpace(toolUid))
        {
            toolUid = $"SymbologyTool_{report.GetParameter<int>("toolSlot")}";
        }

        Template = new SectorTemplate(report, template, toolUid, version);
        Report = new SectorReport(report, Template, table);
        SectorDetails = new SectorDetails(this);

        foreach (var alm in SectorDetails.Alarms)
        {
            if (alm.Category == AvaailableAlarmCategories.Warning)
                IsWarning = true;

            if (alm.Category == AvaailableAlarmCategories.Error)
                IsError = true;
        }

        UpdateFocusedParameters();

        App.Settings.PropertyChanged += Settings_PropertyChanged;
    }

    private void Settings_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ShowApplicationParameters))
            OnPropertyChanged(nameof(ShowApplicationParameters));
        if (e.PropertyName == nameof(ShowGradingParameters))
            OnPropertyChanged(nameof(ShowGradingParameters));
        if (e.PropertyName == nameof(ShowSymbologyParameters))
            OnPropertyChanged(nameof(ShowSymbologyParameters));

        if (e.PropertyName == "SelectedParameters")
            _ = Application.Current.Dispatcher.BeginInvoke(() => UpdateFocusedParameters());

    }

    private void UpdateFocusedParameters()
    {
        var lst = _selectedParameters.ToList();
        //for any selected parameters, show them in the focused parameters list. remove any that are not selected. Do not clear the list.
        foreach (var parameter in lst)
        {
            if (!FocusedParameters.Any(p => p.Parameter == parameter))
            {
                var found = SectorDetails.Parameters.FirstOrDefault(p => p.Parameter == parameter);
                if (found != null)
                {
                    FocusedParameters.Add(found);
                }
                else
                {
                    ParameterHandling.AddParameter(parameter, Report.Symbology, FocusedParameters, Report.Original, Template.Original);
                }
            }
        }
        for (var i = FocusedParameters.Count - 1; i >= 0; i--)
        {
            var focusedParam = FocusedParameters[i];
            if (!lst.Contains(focusedParam.Parameter))
            {
                FocusedParameters.RemoveAt(i);
            }
        }
    }

    [RelayCommand]
    private void CopyToClipBoard(int rollID) => this.GetDelimetedSectorReport(rollID.ToString(), true);

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // Unsubscribe from the static event here
                App.Settings.PropertyChanged -= Settings_PropertyChanged;
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
