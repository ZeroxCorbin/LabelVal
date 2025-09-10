using System.Windows.Controls;

namespace LabelVal.Sectors.Views;

/// <summary>
/// Interaction logic for CompareSettingsControlView.xaml
/// </summary>
public partial class SectorDifferencesDatabaseSettingsView : UserControl
{
    public Classes.SectorDifferencesDatabaseSettings ViewModel { get; }
    public SectorDifferencesDatabaseSettingsView()
    {
        DataContext = null;
        InitializeComponent();
        ViewModel = App.GetService<Classes.SectorDifferencesDatabaseSettings>();
        Loaded += (s, e) => DataContext = this;
    }
}
