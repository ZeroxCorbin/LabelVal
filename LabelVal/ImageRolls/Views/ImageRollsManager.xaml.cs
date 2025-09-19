using CommunityToolkit.Mvvm.Messaging;
using LabelVal.ImageRolls.Services;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.Main.Messages;
using MahApps.Metro.Controls.Dialogs;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using System;
using System.Linq;

namespace LabelVal.ImageRolls.Views;

public partial class ImageRollsManager : UserControl
{
    private ViewModels.ImageRollsManager _viewModel;
    private readonly SelectionService _selectionService = new();
    private readonly DispatcherTimer _layoutDebounce;

    public ImageRollsManager()
    {
        InitializeComponent();

        _layoutDebounce = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(60)
        };
        _layoutDebounce.Tick += (_, __) =>
        {
            _layoutDebounce.Stop();
            UpdateRowHeights();
        };

        DataContextChanged += (s, e) =>
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }

            _viewModel = DataContext as ViewModels.ImageRollsManager;
            if (_viewModel == null)
                return;

            _viewModel.PropertyChanged += ViewModel_PropertyChanged;

            ImageRoll ir = App.Settings.GetValue<ImageRoll>(nameof(ViewModels.ImageRollsManager.ActiveImageRoll));

            _viewModel.SelectedFixedImageRoll = ir != null ? _viewModel.FixedImageRolls.FirstOrDefault((roll) => roll.UID == ir.UID) : null;
            _viewModel.SelectedUserImageRoll = ir != null ? _viewModel.UserImageRolls.FirstOrDefault((roll) => roll.UID == ir.UID) : null;

            if (ir != null && _viewModel.SelectedFixedImageRoll == null && _viewModel.SelectedUserImageRoll == null)
                App.Settings.SetValue(nameof(ViewModels.ImageRollsManager.ActiveImageRoll), null);

            if (ir == null || (ir.RollType == ImageRollTypes.Database && ir.ImageCount == 0))
                _ = Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => WeakReferenceMessenger.Default.Send(new CloseSplashScreenMessage(true))));
        };

        TabCtlUserIr.Loaded += (s, e) =>
        {
            if (_viewModel?.SelectedUserImageRoll != null)
            {
                CollectionViewGroup item = TabCtlUserIr.Items.OfType<CollectionViewGroup>()
                    .FirstOrDefault((e1) => e1.Items.Contains(_viewModel.SelectedUserImageRoll));
                if (item != null)
                {
                    TabCtlUserIr.SelectedItem = item;
                }
            }
        };

        TabCtlFixedIr.Loaded += (s, e) =>
        {
            if (_viewModel?.SelectedFixedImageRoll != null)
            {
                CollectionViewGroup item = TabCtlFixedIr.Items.OfType<CollectionViewGroup>()
                    .FirstOrDefault((e1) => e1.Items.Contains(_viewModel.SelectedFixedImageRoll));
                if (item != null)
                {
                    TabCtlFixedIr.SelectedItem = item;
                }
            }
        };
    }

    private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModels.ImageRollsManager.RefreshView))
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

        _viewModel.SelectedUserImageRoll = ir;
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

        _viewModel.SelectedFixedImageRoll = ir;
    }

    public void Refresh()
    {
        if (FindResource("UserImageRolls") is CollectionViewSource cvs)
        {
            cvs.View?.Refresh();
        }
        if (FindResource("AllImageRolls") is CollectionViewSource cvsAll)
        {
            cvsAll.View?.Refresh();
        }
        Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(UpdateRowHeights));
    }

    private void btnOpenImageRollsLocation(object sender, RoutedEventArgs e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
    {
        FileName = $"{App.UserImageRollsRoot}\\",
        UseShellExecute = true,
        Verb = "open"
    });

    private ListView FindListViewInTemplate(DependencyObject parent)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, i);
            if (child is ListView lv)
                return lv;
            ListView result = FindListViewInTemplate(child);
            if (result != null)
                return result;
        }
        return null;
    }

    private ListView FindUserImageRollsListView()
    {
        for (var i = 0; i < TabCtlUserIr.Items.Count; i++)
        {
            if (TabCtlUserIr.ItemContainerGenerator.ContainerFromIndex(i) is TabItem tabItem)
            {
                ContentPresenter contentPresenter = FindVisualChild<ContentPresenter>(tabItem);
                if (contentPresenter != null)
                {
                    ListView listView = FindVisualChild<ListView>(contentPresenter);
                    if (listView != null)
                    {
                        if (listView.ItemsSource is CollectionViewGroup group &&
                            group.Items.Count > 0 &&
                            group.Items[0].GetType().Name.Contains("ImageRoll"))
                        {
                            return listView;
                        }
                        return listView;
                    }
                }
            }
        }
        return null;
    }

    private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent == null) return null;
        var count = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < count; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, i);
            if (child is T tChild)
                return tChild;
            T result = FindVisualChild<T>(child);
            if (result != null)
                return result;
        }
        return null;
    }

    private void RootGrid_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateRowHeights();
    }

    private void RootGrid_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Debounce rapid resize events
        if (!_layoutDebounce.IsEnabled)
            _layoutDebounce.Start();
    }

    private void UpdateRowHeights()
    {
        double available = ActualHeight;
        if (available <= 0 || ActualWidth <= 0)
            return;

        bool forcedAuto = false;
        if (RowUser.Height.GridUnitType != GridUnitType.Auto)
        {
            RowUser.Height = GridLength.Auto;
            forcedAuto = true;
        }
        if (RowFixed.Height.GridUnitType != GridUnitType.Auto)
        {
            RowFixed.Height = GridLength.Auto;
            forcedAuto = true;
        }
        if (forcedAuto)
            UpdateLayout();

        double naturalUser = UserPanel.ActualHeight;
        double naturalFixed = FixedPanel.ActualHeight;
        double total = naturalUser + naturalFixed;

        if (total <= available)
            return;

        bool userIsLarger = naturalUser >= naturalFixed;
        double smallNatural = userIsLarger ? naturalFixed : naturalUser;

        if (smallNatural >= available * 0.9)
        {
            SetStar(RowUser, 1);
            SetStar(RowFixed, 1);
            return;
        }

        if (userIsLarger)
        {
            SetStar(RowUser, 1);
            SetAuto(RowFixed);
        }
        else
        {
            SetAuto(RowUser);
            SetStar(RowFixed, 1);
        }
    }

    private static void SetStar(RowDefinition row, double weight)
    {
        if (row.Height.GridUnitType != GridUnitType.Star || !DoubleEquals(row.Height.Value, weight))
            row.Height = new GridLength(weight, GridUnitType.Star);
    }

    private static void SetAuto(RowDefinition row)
    {
        if (row.Height.GridUnitType != GridUnitType.Auto)
            row.Height = GridLength.Auto;
    }

    private static bool DoubleEquals(double a, double b) => Math.Abs(a - b) < 0.0001;

    private void MeasurePanel(FrameworkElement panel)
    {
        var width = RootGrid.ActualWidth > 0 ? RootGrid.ActualWidth : double.PositiveInfinity;
        panel.Measure(new Size(width, double.PositiveInfinity));
    }

    private void TreeAllImageRolls_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (_viewModel == null) return;
        if (e.NewValue is ImageRoll roll)
        {
            // Selecting roll updates unified selection
            if (_viewModel.SelectedAllImageRoll != roll)
                _viewModel.SelectedAllImageRoll = roll;
        }
    }
}