using LabelVal.WindowViews;
using System.Windows;
using System.Windows.Controls;

namespace LabelVal.V275.Views;
/// <summary>
/// Interaction logic for V275NodesView.xaml
/// </summary>
public partial class V275 : UserControl
{
    public V275() => InitializeComponent();

    public void btnShowDetails_Click(object sender, RoutedEventArgs e) => ((MainWindowView)App.Current.MainWindow).NodeDetails.IsLeftDrawerOpen = !((MainWindowView)App.Current.MainWindow).NodeDetails.IsLeftDrawerOpen;
}
