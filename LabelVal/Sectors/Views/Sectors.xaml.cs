using MahApps.Metro.Controls.Dialogs;
using System.Windows;
using System.Windows.Controls;

namespace LabelVal.Sectors.Views;

/// <summary>
/// Interaction logic for SectorControlView.xaml
/// </summary>
public partial class Sectors : UserControl
{
    public Sectors() => InitializeComponent();
    private void Button_Click(object sender, RoutedEventArgs e) => popSymbolDetails.IsOpen = true;

    private void Show95xxCompare_Click(object sender, RoutedEventArgs e)
    {
        //LVS_95xx.LVS95xx_SerialPortView sp = new LVS_95xx.LVS95xx_SerialPortView(this.DataContext);

        var dc = new LVS_95xx.LVS95xx_SerialPortViewModel(DataContext);

        var yourParentWindow = Window.GetWindow(this);

        dc.Width = yourParentWindow.ActualWidth - 200;
        dc.Height = yourParentWindow.ActualHeight - 200;

        _ = DialogCoordinator.Instance.ShowMetroDialogAsync(yourParentWindow.DataContext, new LVS_95xx.LVS95xx_SerialPortView() { DataContext = dc });

    }

    private void btnGS1DecodeText_Click(object sender, RoutedEventArgs e) => popGS1DecodeText.IsOpen = true;
}
