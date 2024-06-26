using System.Windows;
using System.Windows.Controls;

namespace LabelVal.LVS_95xx.Views;
/// <summary>
/// Interaction logic for Verifier.xaml
/// </summary>
public partial class Verifier : UserControl
{
    public Verifier()
    {
        InitializeComponent();
    }

    private void btnShowSettings_Click(object sender, RoutedEventArgs e) => drwSettings.IsTopDrawerOpen = !drwSettings.IsTopDrawerOpen;
}
