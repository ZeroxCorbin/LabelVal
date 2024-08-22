using LabelVal.Results.Views;
using LabelVal.Sectors.Interfaces;
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
    public System.Drawing.Point SectorCenterPoint => ThisSector.Template.CenterPoint;
    public string GroupName { get; private set; }


    private Results.ViewModels.IImageResultEntry ImageResultEntry { get; set; }

    private PopupGS1DecodeText popGS1DecodeText = new();

    public bool IsSectorFocused => GroupName switch
    {
        "v275Stored" => ImageResultEntry.V275FocusedStoredSector != null,
        "v275Current" => ImageResultEntry.V275FocusedCurrentSector != null,
        "v5Stored" => ImageResultEntry.V5FocusedStoredSector != null,
        "v5Current" => ImageResultEntry.V5FocusedCurrentSector != null,
        "l95xxStored" => ImageResultEntry.L95xxFocusedStoredSector != null,
        "l95xxCurrent" => ImageResultEntry.L95xxFocusedCurrentSector != null,
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
            ImageResultEntry = (Results.ViewModels.IImageResultEntry)list.DataContext;
        }
        else
        {
            var itmc = Utilities.VisualTreeHelp.GetVisualParent<ItemsControl>(this);
            if (itmc != null)
            {
                GroupName = itmc.Tag.ToString();
                ImageResultEntry = (Results.ViewModels.IImageResultEntry)itmc.DataContext;
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
        var ire = Utilities.VisualTreeHelp.GetVisualParent<ImageResultEntry>(this);
        if (ire != null)
        {
            var sectors = Utilities.VisualTreeHelp.GetVisualChildren<Sector>(ire);
            foreach (var s in sectors)
            {
                var vm = (ISector)s.DataContext;

                if (ISector.FallsWithin(ThisSector, vm.Template.CenterPoint))
                    s.ShowSectorDetails();
            }
        }
    }

    public void ShowSectorDetails()
    {
        if (ImageResultEntry == null)
            return;

        //This lets us know which sector is being focused on
        switch (GroupName)
        {
            case "v275Stored":
                if(ImageResultEntry.V275FocusedStoredSector != null)
                    ImageResultEntry.V275FocusedStoredSector.IsFocused = false;
                ImageResultEntry.V275FocusedStoredSector = (ISector)this.DataContext;
                ImageResultEntry.V275FocusedStoredSector.IsFocused = true;
                App.Current.Dispatcher.BeginInvoke(() => ((Results.ViewModels.ImageResultEntry)ImageResultEntry).UpdateV275StoredImageOverlay());
                break;
            case "v275Current":
                if (ImageResultEntry.V275FocusedCurrentSector != null)
                    ImageResultEntry.V275FocusedCurrentSector.IsFocused = false;
                ImageResultEntry.V275FocusedCurrentSector = (ISector)this.DataContext;
                ImageResultEntry.V275FocusedCurrentSector.IsFocused = true;
                App.Current.Dispatcher.BeginInvoke(() => ((Results.ViewModels.ImageResultEntry)ImageResultEntry).UpdateV275CurrentImageOverlay());
                break;
            case "v5Stored":
                if(ImageResultEntry.V5FocusedStoredSector != null)
                    ImageResultEntry.V5FocusedStoredSector.IsFocused = false;
                ImageResultEntry.V5FocusedStoredSector = (ISector)this.DataContext;
                ImageResultEntry.V5FocusedStoredSector.IsFocused = true;
                App.Current.Dispatcher.BeginInvoke(() => ((Results.ViewModels.ImageResultEntry)ImageResultEntry).UpdateV5StoredImageOverlay());
                break;
            case "v5Current":
                if (ImageResultEntry.V5FocusedCurrentSector != null)
                    ImageResultEntry.V5FocusedCurrentSector.IsFocused = false;
                ImageResultEntry.V5FocusedCurrentSector = (ISector)this.DataContext;
                ImageResultEntry.V5FocusedCurrentSector.IsFocused = true;
                App.Current.Dispatcher.BeginInvoke(() => ((Results.ViewModels.ImageResultEntry)ImageResultEntry).UpdateV5CurrentImageOverlay());
                break;
            case "l95xxStored":
                if (ImageResultEntry.L95xxFocusedStoredSector != null)
                    ImageResultEntry.L95xxFocusedStoredSector.IsFocused = false;
                ImageResultEntry.L95xxFocusedStoredSector = (ISector)this.DataContext;
                ImageResultEntry.L95xxFocusedStoredSector.IsFocused = true;
                App.Current.Dispatcher.BeginInvoke(() => ((Results.ViewModels.ImageResultEntry)ImageResultEntry).UpdateL95xxStoredImageOverlay());
                break;
            case "l95xxCurrent":
                if (ImageResultEntry.L95xxFocusedCurrentSector != null)
                    ImageResultEntry.L95xxFocusedCurrentSector.IsFocused = false;
                ImageResultEntry.L95xxFocusedCurrentSector = (ISector)this.DataContext;
                ImageResultEntry.L95xxFocusedCurrentSector.IsFocused = true;
                App.Current.Dispatcher.BeginInvoke(() => ((Results.ViewModels.ImageResultEntry)ImageResultEntry).UpdateL95xxCurrentImageOverlay());
                break;
        }


    }

}
