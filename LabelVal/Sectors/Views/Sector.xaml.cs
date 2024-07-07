using LabelVal.Results.Views;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LabelVal.Sectors.Views;

public partial class Sector : UserControl
{

    private ViewModels.Sector ThisSector { get; set; }
    public string SectorName => ThisSector.Template.Username;
    public System.Drawing.Point SectorCenterPoint => ThisSector.Template.CenterPoint;
    public string GroupName { get; private set; }


    private Results.ViewModels.IImageResultEntry ImageResultEntry { get; set; }

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
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        ThisSector = (ViewModels.Sector)DataContext;

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
        popGS1DecodeText.IsOpen = true;
    }

    private void btnOverallGrade_Click(object sender, RoutedEventArgs e)
    {
        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            ShowAllLikeSectors();
        else
            ShowSectorDetails();
    }

    public void ShowSectorDetails()
    {
        if (ImageResultEntry == null)
            return;

        //This lets us know which sector is being focused on
        switch (GroupName)
        {
            case "v275Stored":
                ImageResultEntry.V275FocusedStoredSector = (ViewModels.Sector)this.DataContext;
                break;
            case "v275Current":
                ImageResultEntry.V275FocusedCurrentSector = (ViewModels.Sector)this.DataContext;
                ShowSameNameSector("v275Stored");
                break;
            case "v5Stored":
                ImageResultEntry.V5FocusedStoredSector = (ViewModels.Sector)this.DataContext;
                break;
            case "v5Current":
                ImageResultEntry.V5FocusedCurrentSector = (ViewModels.Sector)this.DataContext;
                ShowSameNameSector("v5Stored");
                break;
            case "l95xxStored":
                ImageResultEntry.L95xxFocusedStoredSector = (ViewModels.Sector)this.DataContext;
                break;
            case "l95xxCurrent":
                ImageResultEntry.L95xxFocusedCurrentSector = (ViewModels.Sector)this.DataContext;
                ShowSameNameSector("l95xxStored");
                break;
        }
    }

    public void HideSectorDetails()
    {
        if (ImageResultEntry == null)
            return;

        switch (GroupName)
        {
            case "v275Stored":
                ImageResultEntry.V275FocusedStoredSector = null;
                break;
            case "v275Current":
                ImageResultEntry.V275FocusedCurrentSector = null;
                HideSameNameSector("v275Stored");
                break;
            case "v5Stored":
                ImageResultEntry.V5FocusedStoredSector = null;
                break;
            case "v5Current":
                ImageResultEntry.V5FocusedCurrentSector = null;
                HideSameNameSector("v5Stored");
                break;
            case "l95xxStored":
                ImageResultEntry.L95xxFocusedStoredSector = null;
                break;
            case "l95xxCurrent":
                ImageResultEntry.L95xxFocusedCurrentSector = null;
                HideSameNameSector("l95xxStored");
                break;
        }
    }

    private void ShowSameNameSector(string targetGroup)
    {
        Collection<Sector> sectors = null;

        if (targetGroup.StartsWith("v275"))
        {
            var ire = Utilities.VisualTreeHelp.GetVisualParent<ImageResultEntry_V275>(this);
            if (ire != null)
                sectors = Utilities.VisualTreeHelp.GetVisualChildren<Sector>(ire);

        }
        else if (targetGroup.StartsWith("v5"))
        {
            var ire = Utilities.VisualTreeHelp.GetVisualParent<ImageResultEntry_V5>(this);
            if (ire != null)
                sectors = Utilities.VisualTreeHelp.GetVisualChildren<Sector>(ire);

        }
        else if (targetGroup.StartsWith("l95"))
        {
            var ire = Utilities.VisualTreeHelp.GetVisualParent<ImageResultEntry_L95xx>(this);
            if (ire != null)
                sectors = Utilities.VisualTreeHelp.GetVisualChildren<Sector>(ire);

        }

        if(sectors != null)
            foreach (var s in sectors)
            {
                if (s.GroupName == targetGroup && s.SectorName == SectorName)
                {
                    if (!s.IsSectorFocused)
                        s.ShowSectorDetails();
                    break;
                }
            }
    }


    private void HideSameNameSector(string targetGroup)
    {
        Collection<Sector> sectors = null;

        if (targetGroup.StartsWith("v275"))
        {
            var ire = Utilities.VisualTreeHelp.GetVisualParent<ImageResultEntry_V275>(this);
            if (ire != null)
                sectors = Utilities.VisualTreeHelp.GetVisualChildren<Sector>(ire);
        }
        else if (targetGroup.StartsWith("v5"))
        {
            var ire = Utilities.VisualTreeHelp.GetVisualParent<ImageResultEntry_V5>(this);
            if (ire != null)
                sectors = Utilities.VisualTreeHelp.GetVisualChildren<Sector>(ire);
        }
        else if (targetGroup.StartsWith("l95"))
        {
            var ire = Utilities.VisualTreeHelp.GetVisualParent<ImageResultEntry_L95xx>(this);
            if (ire != null)
                sectors = Utilities.VisualTreeHelp.GetVisualChildren<Sector>(ire);
        }

        foreach (var s in sectors)
        {
            if (s.GroupName == targetGroup && s.SectorName == SectorName)
            {
                if (!s.IsSectorFocused)
                    s.HideSectorDetails();
                break;
            }
        }
    }

    private void ShowAllLikeSectors()
    {
        var ire = Utilities.VisualTreeHelp.GetVisualParent<ImageResultEntry>(this);
        if (ire != null)
        {
            var sectors = Utilities.VisualTreeHelp.GetVisualChildren<Sector>(ire);
            foreach (var s in sectors)
            {
                var vm = (ViewModels.Sector)s.DataContext;

                if (LibStaticUtilities.PositionMovement.IsPointWithinCircumference(SectorCenterPoint, 30, vm.Template.CenterPoint))
                {
                    s.ShowSectorDetails();
                }
            }
        }
    }

}
