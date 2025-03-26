using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.ImageRolls.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace LabelVal.ImageRolls.Views;

public partial class ImageRolls : UserControl
{

    private ViewModels.ImageRolls _viewModel;

    public ImageRolls()
    {
        InitializeComponent();

        DataContextChanged += (s, e) =>
            {
                _viewModel = (ViewModels.ImageRolls)DataContext;
                if (_viewModel == null)
                    return;
                //App.Settings.SetValue(nameof(SelectedImageRoll), value);
                var ir = App.Settings.GetValue< ViewModels.ImageRollEntry>("SelectedImageRoll");

                    _viewModel.SelectedUserImageRoll = null;
                    _viewModel.SelectedFixedImageRoll = null;

                    _viewModel.SelectedFixedImageRoll = ir != null ? _viewModel.FixedImageRolls.FirstOrDefault((e) => e.UID == ir.UID) : null;
                    _viewModel.SelectedUserImageRoll = ir != null ? _viewModel.UserImageRolls.FirstOrDefault((e) => e.UID == ir.UID) : null;
                
            };

    }

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

            //((ViewModels.ImageRolls)par.DataContext).SelectedFixedImageRoll = (ViewModels.ImageRollEntry)((ListView)sender).SelectedItem;

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

        //if ((ViewModels.ImageRollEntry)((ListView)sender).SelectedItem != null)
        //    ((ViewModels.ImageRolls)par.DataContext).SelectedUserImageRoll = (ViewModels.ImageRollEntry)((ListView)sender).SelectedItem;

    }

    private void btnOpenImageRollsLocation(object sender, RoutedEventArgs e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
    {
        FileName = $"{App.UserImageRollsRoot}\\",
        UseShellExecute = true,
        Verb = "open"
    });
}
