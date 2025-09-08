using CommunityToolkit.Mvvm.Messaging;
using LabelVal.ImageRolls.Services;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.Main.Messages;
using MahApps.Metro.Controls.Dialogs;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;

namespace LabelVal.ImageRolls.Views;

public partial class ImageRolls : UserControl
{
    private ViewModels.ImageRolls _viewModel;
    private readonly SelectionService _selectionService = new();

    public ImageRolls()
    {
        InitializeComponent();

        DataContextChanged += (s, e) =>
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }

            _viewModel = e.NewValue as ViewModels.ImageRolls;
            if (_viewModel == null)
                return;

            _viewModel.PropertyChanged += ViewModel_PropertyChanged;

            ImageRoll ir = App.Settings.GetValue<ImageRoll>(nameof(ViewModels.ImageRolls.SelectedImageRoll));

            _viewModel.SelectedFixedImageRoll = ir != null ? _viewModel.FixedImageRolls.FirstOrDefault((roll) => roll.UID == ir.UID) : null;
            _viewModel.SelectedUserImageRoll = ir != null ? _viewModel.UserImageRolls.FirstOrDefault((roll) => roll.UID == ir.UID) : null;

            if (ir != null && _viewModel.SelectedFixedImageRoll == null && _viewModel.SelectedUserImageRoll == null)
                App.Settings.SetValue(nameof(ViewModels.ImageRolls.SelectedImageRoll), null);
          
            if(ir == null || ir.ImageEntries.Count == 0)
                _ = Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => WeakReferenceMessenger.Default.Send(new CloseSplashScreenMessage(true))));
        };

        TabCtlUserIr.Loaded += (s, e) =>
        {
            if (_viewModel.SelectedUserImageRoll != null)
            {
                CollectionViewGroup item = TabCtlUserIr.Items.OfType<CollectionViewGroup>().FirstOrDefault((e1) => e1.Items.Contains(_viewModel.SelectedUserImageRoll));
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
                CollectionViewGroup item = TabCtlFixedIr.Items.OfType<CollectionViewGroup>().FirstOrDefault((e1) => e1.Items.Contains(_viewModel.SelectedFixedImageRoll));
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

    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        DialogParticipation.SetRegister(this, null);
        if (sender is ListView lst)
        {
            _selectionService.UnregisterListView(lst);
        }
    }

    private void ListView_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is ListView lst)
        {
            _selectionService.RegisterListView(lst);
        }
    }

    private void ListViewUser_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is ListView lst)
        {
            _selectionService.RegisterListView(lst);
        }
    }

    private void ListViewUser_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_viewModel == null || _viewModel.IsLoading || (e.RemovedItems.Count > 0 && e.AddedItems.Count == 0))
        {
            return;
        }

        if (sender is not ListView lst) return;

        _selectionService.NotifySelectionChanged(lst);

        if (lst.SelectedItem is not ViewModels.ImageRoll ir)
        {
            return;
        }

        if (Utilities.VisualTreeHelp.GetVisualParent<TabControl>(lst) is not TabControl tab) return;

        ((ViewModels.ImageRolls)tab.DataContext).SelectedUserImageRoll = ir;
    }

    private void ListViewFixed_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_viewModel == null || _viewModel.IsLoading || (e.RemovedItems.Count > 0 && e.AddedItems.Count == 0))
        {
            return;
        }

        if (sender is not ListView lst) return;

        _selectionService.NotifySelectionChanged(lst);

        if (lst.SelectedItem is not ViewModels.ImageRoll ir)
        {
            return;
        }

        if (Utilities.VisualTreeHelp.GetVisualParent<TabControl>(lst) is not TabControl tab) return;

        ((ViewModels.ImageRolls)tab.DataContext).SelectedFixedImageRoll = ir;
    }

    public void Refresh() { if (FindResource("UserImageRolls") is CollectionViewSource cvs) { cvs.View?.Refresh(); } }

    private void btnOpenImageRollsLocation(object sender, RoutedEventArgs e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
    {
        FileName = $"{App.UserImageRollsRoot}\\",
        UseShellExecute = true,
        Verb = "open"
    });
}