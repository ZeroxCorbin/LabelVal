using System.Windows.Controls;

namespace LabelVal.Results.Views;
/// <summary>
/// Interaction logic for ImageResults.xaml
/// </summary>
public partial class ImageResultsManager : UserControl
{
    private ViewModels.ImageResultsManager _viewModel;
    public ImageResultsManager()
    {
        InitializeComponent();

        DataContextChanged += (s, e) =>
        {
            if (e.NewValue is ViewModels.ImageResultsManager vm)
            {
                _viewModel = vm;
            }
        };

        App.Settings.PropertyChanged += Settings_PropertyChanged;
    }

    private void ImageResultsManager_DataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e) => throw new NotImplementedException();

    private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {

    }
    private void JsonDrawer_DrawerOpened(object sender, MaterialDesignThemes.Wpf.DrawerOpenedEventArgs e)
    {
        if (tiReport.IsSelected && !tiReport.IsEnabled)
        {
            tiTemplate.IsSelected = true;
        }
        if (tiTemplate.IsSelected && !tiTemplate.IsEnabled)
        {
            tiReport.IsSelected = true;
        }
    }

    private void btnRightSideBar_Click(object sender, System.Windows.RoutedEventArgs e) => JsonDrawer.IsRightDrawerOpen = !JsonDrawer.IsRightDrawerOpen;

}
