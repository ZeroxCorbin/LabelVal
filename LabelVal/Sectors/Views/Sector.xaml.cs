using LabelVal.Results.Views;
using LabelVal.Sectors.Interfaces;
using LabelVal.Sectors.Extensions;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LabelVal.Sectors.Views;

public partial class Sector : UserControl
{
    public static readonly DependencyProperty HideErrorsWarningsProperty =
                            DependencyProperty.Register(
                            nameof(HideErrorsWarnings),
                            typeof(bool),
                            typeof(Sector),
                            new FrameworkPropertyMetadata(App.Settings.GetValue<bool>(nameof(HideErrorsWarnings)), FrameworkPropertyMetadataOptions.AffectsMeasure));

    public bool HideErrorsWarnings
    {
        get => (bool)GetValue(HideErrorsWarningsProperty);
        set => SetValue(HideErrorsWarningsProperty, value);
    }

    private ISector ThisSector { get; set; }
    public string SectorName => ThisSector.Template.Username;
    public System.Drawing.Point SectorCenterPoint => ThisSector.Report.CenterPoint;
    public string GroupName { get; private set; }


    private Results.ViewModels.IResultsDeviceEntry ResultsEntry { get; set; }

    private PopupGS1DecodeText popGS1DecodeText = new();

    public bool IsSectorFocused => GroupName switch
    {
        "V275Stored" => ResultsEntry.FocusedStoredSector != null,
        "V275Current" => ResultsEntry.FocusedCurrentSector != null,
        "V5Stored" => ResultsEntry.FocusedStoredSector != null,
        "V5Current" => ResultsEntry.FocusedCurrentSector != null,
        "L95Stored" => ResultsEntry.FocusedStoredSector != null,
        "L95Current" => ResultsEntry.FocusedCurrentSector != null,
        _ => false,
    };

    public Sector()
    {
        InitializeComponent();

        HideErrorsWarnings = App.Settings.GetValue<bool>(nameof(HideErrorsWarnings));
        App.Settings.PropertyChanged += Settings_PropertyChanged;
    }

    private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "HideErrorsWarnings")
            HideErrorsWarnings = App.Settings.GetValue<bool>(nameof(HideErrorsWarnings));

    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        ThisSector = (ISector)DataContext;

        GetSecotorDetails();
    }
    private void GetSecotorDetails()
    {
        var list = Utilities.VisualTreeHelp.GetVisualParent<ListView>(this);
        if (list != null)
        {
            GroupName = list.Tag.ToString();
            ResultsEntry = (Results.ViewModels.IResultsDeviceEntry)list.DataContext;
        }
        else
        {
            var itmc = Utilities.VisualTreeHelp.GetVisualParent<ItemsControl>(this);
            if (itmc != null)
            {
                GroupName = itmc.Tag.ToString();
                ResultsEntry = (Results.ViewModels.IResultsDeviceEntry)itmc.DataContext;
            }
        }
    }

    private void btnGS1DecodeText_Click(object sender, RoutedEventArgs e)
    {
        popGS1DecodeText.Popup.PlacementTarget = gs1AiTextPopAnchor;
        popGS1DecodeText.Popup.IsOpen = true;
    }

    private void btnOverallGrade_Click(object sender, RoutedEventArgs e)
    {
        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            ShowAllLikeSectors();
        else
            ShowSectorDetails();
    }
    private void ShowAllLikeSectors()
    {
        var ire = Utilities.VisualTreeHelp.GetVisualParent<ResultsEntry>(this);
        if (ire != null)
        {
            var sectors = Utilities.VisualTreeHelp.GetVisualChildren<Sector>(ire);
            foreach (var s in sectors)
            {
                var vm = (ISector)s.DataContext;

                if (vm.Report.CenterPoint.FallsWithin(ThisSector))
                    s.ShowSectorDetails();
            }
        }
    }

    public void ShowSectorDetails()
    {
        if (ResultsEntry == null)
            return;

        //This lets us know which sector is being focused on
        switch (GroupName)
        {
            case "V275Stored":
               var devices =  ResultsEntry.ResultsEntry.ResultsDeviceEntries.Where(x => x.Device == Results.ViewModels.ResultsEntryDevices.V275);
                foreach (var device in devices)
                {
                    if (device.FocusedStoredSector != null)
                        device.FocusedStoredSector.IsFocused = false;
                    device.FocusedStoredSector = (ISector)this.DataContext;
                    device.FocusedStoredSector.IsFocused = true;
                    Application.Current.Dispatcher.BeginInvoke(() => ((Results.ViewModels.IResultsDeviceEntry)device).RefreshStoredOverlay());
                }
                break;
            case "V275Current":
                devices = ResultsEntry.ResultsEntry.ResultsDeviceEntries.Where(x => x.Device == Results.ViewModels.ResultsEntryDevices.V275);
                foreach (var device in devices)
                {
                    if (device.FocusedCurrentSector != null)
                        device.FocusedCurrentSector.IsFocused = false;
                    device.FocusedCurrentSector = (ISector)this.DataContext;
                    device.FocusedCurrentSector.IsFocused = true;
                    Application.Current.Dispatcher.BeginInvoke(() => ((Results.ViewModels.IResultsDeviceEntry)device).RefreshCurrentOverlay());
                }
                break;
            case "V5Stored":
                devices = ResultsEntry.ResultsEntry.ResultsDeviceEntries.Where(x => x.Device == Results.ViewModels.ResultsEntryDevices.V5);
                foreach (var device in devices)
                {
                    if (device.FocusedStoredSector != null)
                        device.FocusedStoredSector.IsFocused = false;
                    device.FocusedStoredSector = (ISector)this.DataContext;
                    device.FocusedStoredSector.IsFocused = true;
                    Application.Current.Dispatcher.BeginInvoke(() => ((Results.ViewModels.IResultsDeviceEntry)device).RefreshStoredOverlay());
                }
                break;
            case "V5Current":
                devices = ResultsEntry.ResultsEntry.ResultsDeviceEntries.Where(x => x.Device == Results.ViewModels.ResultsEntryDevices.V5);
                foreach (var device in devices)
                {
                    if (device.FocusedCurrentSector != null)
                        device.FocusedCurrentSector.IsFocused = false;
                    device.FocusedCurrentSector = (ISector)this.DataContext;
                    device.FocusedCurrentSector.IsFocused = true;
                    Application.Current.Dispatcher.BeginInvoke(() => ((Results.ViewModels.IResultsDeviceEntry)device).RefreshCurrentOverlay());
                }
                break;
            case "L95Stored":
                devices = ResultsEntry.ResultsEntry.ResultsDeviceEntries.Where(x => x.Device == Results.ViewModels.ResultsEntryDevices.L95);
                foreach (var device in devices)
                {
                    if (device.FocusedStoredSector != null)
                        device.FocusedStoredSector.IsFocused = false;
                    device.FocusedStoredSector = (ISector)this.DataContext;
                    device.FocusedStoredSector.IsFocused = true;
                    Application.Current.Dispatcher.BeginInvoke(() => ((Results.ViewModels.IResultsDeviceEntry)device).RefreshStoredOverlay());
                }
                break;
            case "L95Current":
                devices = ResultsEntry.ResultsEntry.ResultsDeviceEntries.Where(x => x.Device == Results.ViewModels.ResultsEntryDevices.L95);
                foreach (var device in devices)
                {
                    if (device.FocusedCurrentSector != null)
                        device.FocusedCurrentSector.IsFocused = false;
                    device.FocusedCurrentSector = (ISector)this.DataContext;
                    device.FocusedCurrentSector.IsFocused = true;
                    Application.Current.Dispatcher.BeginInvoke(() => ((Results.ViewModels.IResultsDeviceEntry)device).RefreshCurrentOverlay());
                }
                break;
        }


    }

}
