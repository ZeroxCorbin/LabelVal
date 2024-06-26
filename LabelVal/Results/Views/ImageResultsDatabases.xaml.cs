using System.Windows.Controls;

namespace LabelVal.Results.Views;
/// <summary>
/// Interaction logic for ImageResultsDatabasesView.xaml
/// </summary>
public partial class ImageResultsDatabases : UserControl
{
    public ImageResultsDatabases() => InitializeComponent();

    private void btnLockImageResultsDatabase_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if(sender is Button button && button.DataContext is Databases.ImageResults ir)
            ir.IsLocked = !ir.IsLocked;
    }
}
