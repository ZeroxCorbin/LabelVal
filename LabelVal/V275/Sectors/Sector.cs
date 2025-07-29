using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.GS1;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabelVal.Sectors.Extensions;
using LabelVal.Sectors.Interfaces;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;

namespace LabelVal.V275.Sectors;

public partial class Sector : ObservableObject, ISector
{
    public Devices Device { get; } = Devices.V275;
    public string Version { get; }

    public ISectorTemplate Template { get; }
    public ISectorReport Report { get; }

    public ISectorParameters SectorDetails { get; }
    public bool IsWarning { get; }
    public bool IsError { get; }

    public ApplicationStandards DesiredApplicationStandard { get; }
    public ObservableCollection<GradingStandards> DesiredGradingStandards { get; } = new ObservableCollection<GradingStandards>();
    public GS1Tables DesiredGS1Table { get; }

    public bool IsWrongStandard
    {
        get
        {
            if (DesiredApplicationStandard == ApplicationStandards.None && DesiredGradingStandards.Count == 0)
                return false;

            bool found = false;
            foreach (var gradingStandard in DesiredGradingStandards)
            {
                if (gradingStandard == Report.GradingStandard)
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

    public Sector(JObject template, JObject report, GradingStandards[] gradingStandards, ApplicationStandards appStandard, GS1Tables table, string version)
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

        Template = new SectorTemplate(template, version);
        Report = new SectorReport(report, Template);
        SectorDetails = new SectorParameters(this);

        foreach (Alarm alm in SectorDetails.Alarms)
        {
            if (alm.Category == AvaailableAlarmCategories.Warning)
                IsWarning = true;

            if (alm.Category == AvaailableAlarmCategories.Error)
                IsError = true;
        }
    }

    [RelayCommand]
    private void CopyToClipBoard(int rollID) => this.GetSectorReport(rollID.ToString(), true);

}
