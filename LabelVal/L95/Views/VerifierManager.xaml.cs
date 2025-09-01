using LabelVal.Main.Views;
using System.Windows;
using System.Windows.Controls;

namespace LabelVal.L95.Views;
/// <summary>
/// Interaction logic for VerifierManager.xaml
/// </summary>
public partial class VerifierManager : UserControl
{
    public VerifierManager() => InitializeComponent();

    private void btnCollapseContent(object sender, RoutedEventArgs e) => ((MainWindow)Application.Current.MainWindow).ClearSelectedMenuItem();
}
