using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace LabelVal.Run.Views;

public partial class RunDatabases : UserControl
{
    private List<ListView> runEntryLists = [];

    public RunDatabases() => InitializeComponent();

    //private void ListViewUser_Loaded(object sender, RoutedEventArgs e)
    //{

    //}

    //private void ListViewUser_SelectionChanged(object sender, SelectionChangedEventArgs e)
    //{
    //    TabControl par = Utilities.VisualTreeHelp.GetVisualParent<TabControl>(sender as ListView);
    //    if (par == null)
    //        return;

    //    foreach (ListView l in runEntryLists)
    //    {
    //        if (l == null)
    //        {
    //            runEntryLists.Remove(l);
    //            continue;
    //        }

    //        if (l != sender)
    //            l.SelectedItem = null;
    //    }
    //}

    //private void runEntry_Loaded(object sender, RoutedEventArgs e)
    //{
    //    if (runEntryLists.Contains((ListView)sender))
    //        return;

    //    runEntryLists.Add((ListView)sender);
    //}
}
