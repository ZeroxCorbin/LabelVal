using System.Windows;
using System.Windows.Controls;

namespace LabelVal.ImageRolls.Views;
/// <summary>
/// Interaction logic for ImageRolls.xaml
/// </summary>
public partial class ImageRolls : UserControl
{
    public ImageRolls() => InitializeComponent();

    private List<ListView> fixedLists = [];
    private List<ListView> userLists = [];

    private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        TabControl par = Utilities.VisualTreeHelp.GetVisualParent<TabControl>(sender as ListView);
        if (par == null)
            return;

        foreach (ListView l in fixedLists)
        {
            if (l == null)
            {
                _ = fixedLists.Remove(l);
                continue;
            }

            if (l != sender)
                l.SelectedItem = null;
        }

        if ((ViewModels.ImageRollEntry)((ListView)sender).SelectedItem != null)
            foreach (ListView l in userLists)
            {
                if (l == null)
                {
                    _ = userLists.Remove(l);
                    continue;
                }

                if (l != sender)
                    l.SelectedItem = null;
            }

            ((ViewModels.ImageRolls)par.DataContext).SelectedFixedImageRoll = (ViewModels.ImageRollEntry)((ListView)sender).SelectedItem;

    }

    private void ListView_Loaded(object sender, RoutedEventArgs e)
    {
        if (fixedLists.Contains((ListView)sender))
            return;
        fixedLists.Add((ListView)sender);
    }

    private void ListViewUser_Loaded(object sender, RoutedEventArgs e)
    {
        if (userLists.Contains((ListView)sender))
            return;
        userLists.Add((ListView)sender);
    }

    private void ListViewUser_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        TabControl par = Utilities.VisualTreeHelp.GetVisualParent<TabControl>(sender as ListView);
        if (par == null)
            return;

        foreach (ListView l in userLists)
        {
            if (l == null)
            {
                _ = userLists.Remove(l);
                continue;
            }

            if (l != sender)
                l.SelectedItem = null;
        }

        if ((ViewModels.ImageRollEntry)((ListView)sender).SelectedItem != null)
            foreach (ListView l in fixedLists)
            {
                if (l == null)
                {
                    _ = fixedLists.Remove(l);
                    continue;
                }

                if (l != sender)
                    l.SelectedItem = null;
            }

        ((ViewModels.ImageRolls)par.DataContext).SelectedUserImageRoll = (ViewModels.ImageRollEntry)((ListView)sender).SelectedItem;

    }

    private void btnOpenImageRollsLocation(object sender, RoutedEventArgs e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
    {
        FileName = $"{App.UserImageRollsRoot}\\",
        UseShellExecute = true,
        Verb = "open"
    });
}
