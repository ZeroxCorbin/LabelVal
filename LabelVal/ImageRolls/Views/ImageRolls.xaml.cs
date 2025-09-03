using LabelVal.ImageRolls.ViewModels;
using MahApps.Metro.Controls.Dialogs;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

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

                _viewModel.PropertyChanged += ViewModel_PropertyChanged;

                var ir = App.Settings.GetValue<ImageRoll>(nameof(ViewModels.ImageRolls.SelectedImageRoll));

                _viewModel.SelectedFixedImageRoll = ir != null ? _viewModel.FixedImageRolls.FirstOrDefault((e) => e.UID == ir.UID) : null;
                _viewModel.SelectedUserImageRoll = ir != null ? _viewModel.UserImageRolls.FirstOrDefault((e) => e.UID == ir.UID) : null;


            };

        TabCtlUserIr.Loaded += (s, e) =>
        {
            if (_viewModel.SelectedUserImageRoll != null)
            {
                var item = TabCtlUserIr.Items.OfType<CollectionViewGroup>().FirstOrDefault((e1) => e1.Items.Contains(_viewModel.SelectedUserImageRoll));
                if (item != null)
                {
                    TabCtlUserIr.SelectedItem = item;
                }
            }
        };

        TabCtlFixedIr.Loaded += (s, e) =>
        {
            if (_viewModel.SelectedFixedImageRoll != null)
            {
                var item = TabCtlFixedIr.Items.OfType<CollectionViewGroup>().FirstOrDefault((e1) => e1.Items.Contains(_viewModel.SelectedFixedImageRoll));
                if (item != null)
                {
                    TabCtlFixedIr.SelectedItem = item;
                }
            }
        };
    }

    private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModels.ImageRolls.RefreshView))
        {
            Refresh();
        }
    }

    private void UserControl_Unloaded(object sender, RoutedEventArgs e) => DialogParticipation.SetRegister(this, null);

    private List<ListView> fixedLists = [];
    private List<ListView> userLists = [];

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

    private bool userChanging = false;
    private void ListViewUser_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (userChanging)
            return;

        if (sender is not ListView lst)
            return;

        if (lst.SelectedItem is not ViewModels.ImageRoll ir)
            return;

        if (Utilities.VisualTreeHelp.GetVisualParent<TabControl>(lst) is not TabControl tab)
            return;

        try
        {
            userChanging = true;
            foreach (var l in userLists)
            {
                if (l != lst)
                    l.SelectedItem = null;
            }
            foreach (var l in fixedLists)
            {
                if (l != lst)
                    l.SelectedItem = null;
            }
            ((ViewModels.ImageRolls)tab.DataContext).SelectedUserImageRoll = ir;
        }
        finally
        {
            userChanging = false;
        }
    }

    private bool fixedChanging = false;
    private void ListViewFixed_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (fixedChanging)
            return;

        if (sender is not ListView lst)
            return;

        if (lst.SelectedItem is not ViewModels.ImageRoll ir)
            return;

        if (Utilities.VisualTreeHelp.GetVisualParent<TabControl>(lst) is not TabControl tab)
            return;

        try
        {
            fixedChanging = true;
            foreach (var l in userLists)
            {
                if (l != lst)
                    l.SelectedItem = null;
            }
            foreach (var l in fixedLists)
            {
                if (l != lst)
                    l.SelectedItem = null;
            }
            ((ViewModels.ImageRolls)tab.DataContext).SelectedFixedImageRoll = ir;
        }
        finally
        {
            fixedChanging = false;
        }
    }

    public void Refresh() { if (FindResource("UserImageRolls") is CollectionViewSource cvs) { cvs.View?.Refresh(); } }

    private void btnOpenImageRollsLocation(object sender, RoutedEventArgs e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
    {
        FileName = $"{App.UserImageRollsRoot}\\",
        UseShellExecute = true,
        Verb = "open"
    });
}
