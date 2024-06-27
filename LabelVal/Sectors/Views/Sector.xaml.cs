using LabelVal.Results.Views;
using System.Windows;
using System.Windows.Controls;

namespace LabelVal.Sectors.Views;

public partial class Sector : UserControl
{
    private Results.ViewModels.ImageResultEntry Vm { get; set; }
    public string SectorName => ((ViewModels.Sector)DataContext).Template.Username;
    public string GroupName { get; private set; }

    public bool IsSectorFocused => GroupName switch
    {
        "v275Stored" => Vm.V275FocusedStoredSector != null,
        "v275Current" => Vm.V275FocusedCurrentSector != null,
        "v5Stored" => Vm.V5FocusedStoredSector != null,
        "v5Current" => Vm.V5FocusedCurrentSector != null,
        "l95xxStored" => Vm.L95xxFocusedStoredSector != null,
        "l95xxCurrent" => Vm.L95xxFocusedCurrentSector != null,
        _ => false,
    };

    public Sector()
    {
        InitializeComponent();
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        GetSecotorDetails();
    }

    private void GetSecotorDetails()
    {
        var list = Utilities.VisualTreeHelp.GetVisualParent<ListView>(this);
        if (list != null)
        {
            GroupName = list.Tag.ToString();
            Vm = (Results.ViewModels.ImageResultEntry)list.DataContext;
        }
        else
        {
            var itmc = Utilities.VisualTreeHelp.GetVisualParent<ItemsControl>(this);
            if (itmc != null)
            {
                GroupName = itmc.Tag.ToString();
                Vm = (Results.ViewModels.ImageResultEntry)itmc.DataContext;
            }
        }
    }

    private void btnGS1DecodeText_Click(object sender, RoutedEventArgs e)
    {
        popGS1DecodeText.IsOpen = true;
    }

    private void btnOverallGrade_Click(object sender, RoutedEventArgs e)
    {
        ShowSectorDetails();
    }

    public void ShowSectorDetails()
    {
        if (Vm == null)
            return;

        //This lets us know which sector is being focused on
        switch (GroupName)
        {
            case "v275Stored":
                Vm.V275FocusedStoredSector = (ViewModels.Sector)this.DataContext;
                break;
            case "v275Current":
                Vm.V275FocusedCurrentSector = (ViewModels.Sector)this.DataContext;
                ShowSameNameSector("v275Stored");
                break;
            case "v5Stored":
                Vm.V5FocusedStoredSector = (ViewModels.Sector)this.DataContext;
                break;
            case "v5Current":
                Vm.V5FocusedCurrentSector = (ViewModels.Sector)this.DataContext;
                break;
            case "l95xxStored":
                Vm.L95xxFocusedStoredSector = (ViewModels.Sector)this.DataContext;
                break;
            case "l95xxCurrent":
                Vm.L95xxFocusedCurrentSector = (ViewModels.Sector)this.DataContext;
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

}
