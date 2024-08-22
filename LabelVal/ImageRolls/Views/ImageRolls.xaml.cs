
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LabelVal.ImageRolls.Views;
/// <summary>
/// Interaction logic for ImageRolls.xaml
/// </summary>
public partial class ImageRolls : UserControl
{
    public ImageRolls()
    {
        InitializeComponent();
    }

    List<ListView> fixedLists = new List<ListView>();
    List<ListView> userLists = new List<ListView>();

    private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var par = Utilities.VisualTreeHelp.GetVisualParent<TabControl>(sender as ListView);
        if(par == null)
            return;

        foreach(var l in fixedLists)
        {
            if(l == null)
            {
                fixedLists.Remove(l);
                continue;
            }

            if(l != sender)
                l.SelectedItem = null;
        }

        if ((ViewModels.ImageRollEntry)((ListView)sender).SelectedItem != null)
            foreach (var l in userLists)
            {
                if (l == null)
                {
                    userLists.Remove(l);
                    continue;
                }

                if (l != sender)
                    l.SelectedItem = null;
            }

        ((ViewModels.ImageRolls)par.DataContext).SelectedImageRoll = (ViewModels.ImageRollEntry)((ListView)sender).SelectedItem;
    }

    private void ListView_Loaded(object sender, RoutedEventArgs e)
    {
        if(fixedLists.Contains((ListView)sender))
            return;
        fixedLists.Add((ListView)sender);
    }

    private void ListViewUser_Loaded(object sender, RoutedEventArgs e)
    {
        if(userLists.Contains((ListView)sender))
            return;
        userLists.Add((ListView)sender);
    }

    private void ListViewUser_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var par = Utilities.VisualTreeHelp.GetVisualParent<TabControl>(sender as ListView);
        if (par == null)
            return;

        foreach (var l in userLists)
        {
            if (l == null)
            {
                userLists.Remove(l);
                continue;
            }

            if (l != sender)
                l.SelectedItem = null;
        }

        if((ViewModels.ImageRollEntry)((ListView)sender).SelectedItem != null)
            foreach (var l in fixedLists)
            {
                if (l == null)
                {
                    fixedLists.Remove(l);
                    continue;
                }

                if (l != sender)
                    l.SelectedItem = null;
            }

        ((ViewModels.ImageRolls)par.DataContext).SelectedUserImageRoll = (ViewModels.ImageRollEntry)((ListView)sender).SelectedItem;

    }

    private void btnOpenImageRollsLocation(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
        {
            FileName = $"{App.UserImageRollsRoot}\\",
            UseShellExecute = true,
            Verb = "open"
        });
    }
}
