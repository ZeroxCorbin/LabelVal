using ControlzEx.Theming;
using LabelVal.result.ViewModels;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace LabelVal.Run.Views;

/// <summary>
/// Interaction logic for RunRunView.xaml
/// </summary>
public partial class View : MetroWindow
{
    public static class VisualTreeHelper
    {
        public static Collection<T> GetVisualChildren<T>(DependencyObject current) where T : DependencyObject
        {
            if (current == null)
                return null;

            var children = new Collection<T>();
            GetVisualChildren(current, children);
            return children;
        }
        private static void GetVisualChildren<T>(DependencyObject current, Collection<T> children) where T : DependencyObject
        {
            if (current != null)
            {
                if (current.GetType() == typeof(T))
                    children.Add((T)current);

                for (var i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(current); i++)
                {
                    GetVisualChildren(System.Windows.Media.VisualTreeHelper.GetChild(current, i), children);
                }
            }
        }
    }

    private void btnLightTheme_Click(object sender, RoutedEventArgs e) => ThemeManager.Current.ChangeTheme(App.Current, "Light.Steel");

    private void btnDarkTheme_Click(object sender, RoutedEventArgs e) => ThemeManager.Current.ChangeTheme(App.Current, "Dark.Steel");

    private void btnColorBlind_Click(object sender, RoutedEventArgs e) => App.ChangeColorBlindTheme(!App.Settings.GetValue("App.IsColorBlind", false));

    public View()
    {
        InitializeComponent();

        if (FindResource("LabelsDataList") is CollectionViewSource viewSource)
        {
            viewSource.GroupDescriptions.Clear();
            viewSource.GroupDescriptions.Add(new PropertyGroupDescription(App.Settings.GetValue("LabelsDataList_Group", "Run.LoopCount")));
        }

        if (FindResource("RunEntriesDataList") is CollectionViewSource viewSource1)
        {
            viewSource1.SortDescriptions.RemoveAt(3);

            var order = App.Settings.GetValue("RunEntriesDataList_Sort", System.ComponentModel.ListSortDirection.Descending);

            if (order == System.ComponentModel.ListSortDirection.Ascending)
            {
                var packIconMaterial = new PackIconMaterial()
                {
                    Kind = PackIconMaterialKind.SortClockDescending,
                };
                BtnSortRuns.Content = packIconMaterial;
            }
            else
            {
                var packIconMaterial = new PackIconMaterial()
                {
                    Kind = PackIconMaterialKind.SortClockAscending,
                };
                BtnSortRuns.Content = packIconMaterial;
            }

            viewSource1.SortDescriptions.Add(new System.ComponentModel.SortDescription("TimeDate", order));
        }
    }

    private void BtnSortLabels_Click(object sender, RoutedEventArgs e)
    {
        if (FindResource("LabelsDataList") is CollectionViewSource viewSource)
            if (((PropertyGroupDescription)viewSource.GroupDescriptions[0]).PropertyName == "Run.LoopCount")
            {
                viewSource.GroupDescriptions.Clear();
                viewSource.GroupDescriptions.Add(new PropertyGroupDescription("Run.LabelImageUID"));
                App.Settings.SetValue("LabelsDataList_Group", "Run.LabelImageUID");
            }
            else
            {
                viewSource.GroupDescriptions.Clear();
                viewSource.GroupDescriptions.Add(new PropertyGroupDescription("Run.LoopCount"));
                App.Settings.SetValue("LabelsDataList_Group", "Run.LoopCount");
            }
    }

    private void BtnSortRuns_Click(object sender, RoutedEventArgs e)
    {
        if (FindResource("RunEntriesDataList") is CollectionViewSource viewSource)
            if (viewSource.SortDescriptions[3].Direction == System.ComponentModel.ListSortDirection.Ascending)
            {
                viewSource.SortDescriptions.RemoveAt(3);
                viewSource.SortDescriptions.Add(new System.ComponentModel.SortDescription("TimeDate", System.ComponentModel.ListSortDirection.Descending));
                App.Settings.SetValue("RunEntriesDataList_Sort", System.ComponentModel.ListSortDirection.Descending);

                var packIconMaterial = new PackIconMaterial()
                {
                    Kind = PackIconMaterialKind.SortClockAscending,

                };
                BtnSortRuns.Content = packIconMaterial;
            }
            else
            {
                viewSource.SortDescriptions.RemoveAt(3);
                viewSource.SortDescriptions.Add(new System.ComponentModel.SortDescription("TimeDate", System.ComponentModel.ListSortDirection.Ascending));
                App.Settings.SetValue("RunEntriesDataList_Sort", System.ComponentModel.ListSortDirection.Ascending);
                var packIconMaterial = new PackIconMaterial()
                {
                    Kind = PackIconMaterialKind.SortClockDescending,
                };
                BtnSortRuns.Content = packIconMaterial;
            }
    }

    private void BtnExpandAll_Click(object sender, RoutedEventArgs e)
    {
        var collection = VisualTreeHelper.GetVisualChildren<Expander>(RunList);
        foreach (var expander in collection)
        {
            expander.IsExpanded = true;
        }
    }

    private void BtnCollapseAll_Click(object sender, RoutedEventArgs e)
    {
        var collection = VisualTreeHelper.GetVisualChildren<Expander>(RunList);
        foreach (var expander in collection)
        {
            expander.IsExpanded = false;
        }
    }

    private void Expander_Loaded_1(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Expander expander)
        {
            if (expander.Content is ItemsPresenter ip)
                if (ip.DataContext is CollectionViewGroup collection)
                {
                    var errorCount = 0;
                    foreach (LabelViewModel item in collection.Items)
                    {
                        if (expander.Header is Grid grid1)
                        {
                            if (grid1.FindChild<TextBlock>("LabelSectors") is var res)
                            {
                                res.Text = item.LabelSectors.Count.ToString();
                            }

                            if (grid1.FindChild<TextBlock>("RepeatSectors") is var res1)
                            {
                                res1.Text = item.RepeatSectors.Count.ToString();
                            }
                        }

                        foreach (var diff in item.DiffSectors)
                        {
                            if (diff.IsNotEmpty)
                            {
                                errorCount++;
                            }
                        }
                    }

                    if (expander.Header is Grid grid)
                    {
                        if (grid.FindChild<TextBlock>("HasErrors") is var res)
                        {
                            if (errorCount > 0)
                                res.Text = "Dissimilar";
                        }

                        if (grid.FindChild<TextBlock>("DissimilarSectors") is var res1)
                        {
                            res1.Text = errorCount.ToString();
                        }
                    }
                }
        }
    }

    private void btnCompareSettings_Click(object sender, RoutedEventArgs e) => CompareSettings.IsOpen = true;

    private void MetroWindow_Unloaded(object sender, RoutedEventArgs e) => DialogParticipation.SetRegister(this, null);
}
