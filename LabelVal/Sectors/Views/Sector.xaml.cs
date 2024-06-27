using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LabelVal.Sectors.Views;

/// <summary>
/// Interaction logic for SectorControlView.xaml
/// </summary>
public partial class Sector : UserControl
{
    public Sector()
    {
        InitializeComponent();
    }

    private void btnGS1DecodeText_Click(object sender, RoutedEventArgs e)
    {
        popGS1DecodeText.IsOpen = true;
    }

    private void btnOverallGrade_Click(object sender, RoutedEventArgs e)
    {
        Results.ViewModels.ImageResultEntry vm = null;
        string str = string.Empty;
        var list = Utilities.VisualTreeHelp.GetVisualParent<ListView>(this);
        if (list != null)
        {
            str = list.Tag.ToString();
            vm = (Results.ViewModels.ImageResultEntry)list.DataContext;
        }
        else
        {
            var itmc = Utilities.VisualTreeHelp.GetVisualParent<ItemsControl>(this);
            if (itmc != null)
            {
                str = itmc.Tag.ToString();
                vm = (Results.ViewModels.ImageResultEntry)itmc.DataContext;
            }
        }

        if (vm == null)
            return;

        switch (str)
        {
            case "v275Stored":
                vm.V275FocusedStoredSector = (ViewModels.Sector)this.DataContext;
                break;
            case "v275Current":
                vm.V275FocusedCurrentSector = (ViewModels.Sector)this.DataContext;
                break;
            case "v5Stored":
                vm.V5FocusedStoredSector = (ViewModels.Sector)this.DataContext;
                break;
            case "v5Current":
                vm.V5FocusedCurrentSector = (ViewModels.Sector)this.DataContext;
                break;
            case "l95xxStored":
                vm.L95xxFocusedStoredSector = (ViewModels.Sector)this.DataContext;
                break;
            case "l95xxCurrent":
                vm.L95xxFocusedCurrentSector = (ViewModels.Sector)this.DataContext;
                break;
        }
    }
}
