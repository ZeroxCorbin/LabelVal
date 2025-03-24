using System.Windows.Controls;

namespace LabelVal.Results.Views;
/// <summary>
/// Interaction logic for ImageResults.xaml
/// </summary>
public partial class ImageResults : UserControl
{

    public ImageResults() => InitializeComponent();

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
}
