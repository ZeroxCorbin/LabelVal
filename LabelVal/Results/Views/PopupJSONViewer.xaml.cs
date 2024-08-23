using System.Windows.Controls;

namespace LabelVal.Results.Views;
/// <summary>
/// Interaction logic for PopupJSONViewer.xaml
/// </summary>
public partial class PopupJSONViewer : UserControl
{
    public PopupJSONViewer()
    {
        InitializeComponent();
    }

    private void btnClose(object sender, System.Windows.RoutedEventArgs e)
    {
        Popup.IsOpen = false;   
    }
}
