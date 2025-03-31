using System.Windows.Controls;

namespace LabelVal.Results.Views;
/// <summary>
/// Interaction logic for ImageResults.xaml
/// </summary>
public partial class ImageResultsManager : UserControl
{

    public ImageResultsManager()
    {
        InitializeComponent();

        App.Settings.PropertyChanged += Settings_PropertyChanged;
    }

    private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {

    }
    private void JsonDrawer_DrawerOpened(object sender, MaterialDesignThemes.Wpf.DrawerOpenedEventArgs e)
    {
        if(tiReport.IsSelected && !tiReport.IsEnabled)
        {
            tiTemplate.IsSelected = true;
        }
        if(tiTemplate.IsSelected && !tiTemplate.IsEnabled)
        {
            tiReport.IsSelected = true;
        }
    }

    private void btnRightSideBar_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        JsonDrawer.IsRightDrawerOpen = !JsonDrawer.IsRightDrawerOpen;
    }
}
