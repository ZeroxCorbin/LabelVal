using LabelVal.ImageRolls.Services;
using LabelVal.ImageRolls.ViewModels;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace LabelVal.ImageRolls.Views;

public partial class ImageRollsManager : UserControl
{
    private ViewModels.ImageRollsManager _viewModel;
    private readonly SelectionService _selectionService = new();
    private readonly DispatcherTimer _layoutDebounce;
    private bool _initialSelectionApplied;

    public ImageRollsManager()
    {
        InitializeComponent();

        DataContextChanged += (s, e) =>
        {
            if (_viewModel is not null)
            {
                _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
                if (_viewModel.AllImageRolls is INotifyCollectionChanged oldObs)
                    oldObs.CollectionChanged -= AllImageRolls_CollectionChanged;
            }

            _viewModel = DataContext as ViewModels.ImageRollsManager;
            if (_viewModel == null)
                return;

            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            if (_viewModel.AllImageRolls is INotifyCollectionChanged obs)
                obs.CollectionChanged += AllImageRolls_CollectionChanged;

            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, ApplyInitialSelectionIfNeeded);
        };
    }

    private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModels.ImageRollsManager.ActiveImageRoll))
        {
            if (!_initialSelectionApplied)
                Dispatcher.BeginInvoke(DispatcherPriority.Background, ApplyInitialSelectionIfNeeded);
        }
    }

    private void AllImageRolls_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (!_initialSelectionApplied && _viewModel?.ActiveImageRoll != null)
            Dispatcher.BeginInvoke(DispatcherPriority.Background, ApplyInitialSelectionIfNeeded);
    }

    // TreeView SelectedItem -> ActiveImageRoll
    private void TreeAllImageRolls_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is ImageRoll roll && _viewModel is not null && !ReferenceEquals(_viewModel.ActiveImageRoll, roll))
            _viewModel.ActiveImageRoll = roll;
    }

    private void TreeAllImageRolls_Loaded(object sender, RoutedEventArgs e)
    {
        ApplyInitialSelectionIfNeeded();
    }

    private void ApplyInitialSelectionIfNeeded()
    {
        if (_initialSelectionApplied || _viewModel?.ActiveImageRoll == null || TreeAllImageRolls == null)
            return;

        TreeAllImageRolls.UpdateLayout();

        if (ExpandPathAndSelect(_viewModel.ActiveImageRoll))
        {
            _initialSelectionApplied = true;
        }
        else
        {
            // Retry once containers are realized
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, () =>
            {
                if (!_initialSelectionApplied && ExpandPathAndSelect(_viewModel.ActiveImageRoll))
                    _initialSelectionApplied = true;
            });
        }
    }

    /// <summary>
    /// Expands only the ancestor chain for the target roll and selects it.
    /// No other branches are expanded.
    /// </summary>
    private bool ExpandPathAndSelect(ImageRoll target) => FindAndSelect(TreeAllImageRolls, target);

    private bool FindAndSelect(ItemsControl parent, ImageRoll target)
    {
        int count = parent.Items.Count;
        for (int i = 0; i < count; i++)
        {
            var item = parent.Items[i];
            var container = parent.ItemContainerGenerator.ContainerFromIndex(i) as TreeViewItem;
            if (container == null)
                continue;

            if (ReferenceEquals(item, target))
            {
                container.IsSelected = true;
                container.BringIntoView();
                return true;
            }

            if (container.Items.Count > 0)
            {
                bool wasExpanded = container.IsExpanded;
                container.IsExpanded = true;
                container.UpdateLayout();

                if (FindAndSelect(container, target))
                    return true;

                if (!wasExpanded)
                    container.IsExpanded = false;
            }
        }
        return false;
    }

    private void btnOpenImageRollsLocation(object sender, RoutedEventArgs e) =>
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
        {
            FileName = $"{App.UserImageRollsRoot}\\",
            UseShellExecute = true,
            Verb = "open"
        });
}