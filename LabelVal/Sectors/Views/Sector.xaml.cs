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

    private void Show95xxCompare_Click(object sender, RoutedEventArgs e)
    {
        //LVS_95xx.LVS95xx_SerialPortView sp = new LVS_95xx.LVS95xx_SerialPortView(this.DataContext);

        //var dc = new LVS_95xx.ViewModels.Verifier();

        //var yourParentWindow = Window.GetWindow(this);

        //dc.Width = yourParentWindow.ActualWidth - 200;
        //dc.Height = yourParentWindow.ActualHeight - 200;

        //_ = DialogCoordinator.Instance.ShowMetroDialogAsync(yourParentWindow.DataContext, new LVS_95xx.LVS95xx_SerialPortView() { DataContext = dc });

        //L95xxComparePopup.PlacementTarget = (Button)sender;
        //L95xxComparePopup.IsOpen = true;
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

        //popSymbolDetails.HorizontalOffset = 0;
        //popSymbolDetails.VerticalOffset = 0;
        //popSymbolDetails.PlacementTarget = null;
        ////popSymbolDetails.Placement = System.Windows.Controls.Primitives.PlacementMode.Center;
        //popSymbolDetails.IsOpen = true;
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        btnOverallGrade.LayoutUpdated += BtnOverallGrade_LayoutUpdated;
    }

    private void BtnOverallGrade_LayoutUpdated(object sender, System.EventArgs e)
    {
        if (!popSymbolDetails.IsOpen)
            return;

        // Calculate the button's position relative to the window
        var transform = btnOverallGrade.TransformToAncestor(this);
        Point relativePosition = transform.Transform(new Point(0, 0));

        // Convert the position to screen coordinates
        Point screenPosition = this.PointToScreen(relativePosition);

        // Assuming yourPopup is the popup you want to move
        // Adjust the popup's position based on the button's screen position
        //popSymbolDetails.HorizontalOffset = popSymbolDetails.HorizontalOffset + 1;
        //popSymbolDetails.HorizontalOffset = popSymbolDetails.HorizontalOffset - 1;

        popSymbolDetails.HorizontalOffset = screenPosition.X;
        popSymbolDetails.VerticalOffset = screenPosition.Y;
    }
}
