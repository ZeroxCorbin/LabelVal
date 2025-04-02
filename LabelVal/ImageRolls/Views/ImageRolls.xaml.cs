using LabelVal.ImageRolls.ViewModels;
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

                ImageRoll ir = App.Settings.GetValue<ImageRoll>(nameof(ViewModels.ImageRolls.SelectedImageRoll));

                _viewModel.SelectedUserImageRoll = null;
                _viewModel.SelectedFixedImageRoll = null;

                _viewModel.SelectedFixedImageRoll = ir != null ? _viewModel.FixedImageRolls.FirstOrDefault((e) => e.UID == ir.UID) : null;
                _viewModel.SelectedUserImageRoll = ir != null ? _viewModel.UserImageRolls.FirstOrDefault((e) => e.UID == ir.UID) : null;

                
            };

        tabCtlUserIR.Loaded += (s, e) =>
        {
            if(_viewModel.SelectedUserImageRoll != null)
            {
                var item = tabCtlUserIR.Items.OfType<CollectionViewGroup>().FirstOrDefault((e1) => e1.Items.Contains(_viewModel.SelectedUserImageRoll));
                if(item != null)
                {
                    tabCtlUserIR.SelectedItem = item;
                }
            }
        };

        tabCtlFixedIR.Loaded += (s, e) =>
        {
            if (_viewModel.SelectedFixedImageRoll != null)
            {
                var item = tabCtlFixedIR.Items.OfType<CollectionViewGroup>().FirstOrDefault((e1) => e1.Items.Contains(_viewModel.SelectedFixedImageRoll));
                if (item != null)
                {
                    tabCtlFixedIR.SelectedItem = item;
                }
            }
        };
    }

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

        if(lst.SelectedItem is not ViewModels.ImageRoll ir)
            return;

        if (Utilities.VisualTreeHelp.GetVisualParent<TabControl>(lst) is not TabControl tab)
            return;

        try
        {
            userChanging = true;
            foreach (ListView l in userLists)
            {
                if (l != lst)
                    l.SelectedItem = null;
            }
            foreach (ListView l in fixedLists)
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
            foreach (ListView l in userLists)
            {
                if (l != lst)
                    l.SelectedItem = null;
            }
            foreach (ListView l in fixedLists)
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

    private void btnOpenImageRollsLocation(object sender, RoutedEventArgs e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
    {
        FileName = $"{App.UserImageRollsRoot}\\",
        UseShellExecute = true,
        Verb = "open"
    });
}
