using LabelVal.Results.Views;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LabelVal.Sectors.Views;

public partial class Sector : UserControl
{
    private Results.ViewModels.ImageResultEntry ImageResultEntry { get; set; }
    private ViewModels.Sector ThisSector { get; set; }
    public string SectorName => ThisSector.Template.Username;
    public System.Drawing.Point SectorCenterPoint => ThisSector.Template.CenterPoint;
    public string GroupName { get; private set; }

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
            ImageResultEntry = (Results.ViewModels.ImageResultEntry)list.DataContext;
        }
        else
        {
            var itmc = Utilities.VisualTreeHelp.GetVisualParent<ItemsControl>(this);
            if (itmc != null)
            {
                GroupName = itmc.Tag.ToString();
                ImageResultEntry = (Results.ViewModels.ImageResultEntry)itmc.DataContext;
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
        {
            ShowAllLikeSectors();
        }
        else
        {
            ShowSectorDetails();
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
                break;
            case "l95xxStored":
                ImageResultEntry.L95xxFocusedStoredSector = (ViewModels.Sector)this.DataContext;
                break;
            case "l95xxCurrent":
                ImageResultEntry.L95xxFocusedCurrentSector = (ViewModels.Sector)this.DataContext;
                break;
        }
    }

    private void ShowSameNameSector(string targetGroup)
    {
        var ire = Utilities.VisualTreeHelp.GetVisualParent<ImageResultEntry_V275>(this);
        if (ire != null)
        {
            var sectors = Utilities.VisualTreeHelp.GetVisualChildren<Sector>(ire);
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
