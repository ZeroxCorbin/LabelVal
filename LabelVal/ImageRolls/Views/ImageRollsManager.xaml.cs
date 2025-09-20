using LabelVal.ImageRolls.Services;
using LabelVal.ImageRolls.ViewModels;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace LabelVal.ImageRolls.Views;

/// <summary>
/// Code-behind for the ImageRollsManager view.
/// Responsibilities:
///  - Bridge between the TreeView selection and the view model's ActiveImageRoll.
///  - Apply the persisted ActiveImageRoll selection after items are populated/as containers realize.
///  - Handle dynamic collection changes (reloading / caching) and deferred selection.
/// Notes:
///  - Selection is deferred until containers exist (may require two passes due to virtualization).
///  - No business logic lives here; view model retains ownership of data/state.
/// </summary>
public partial class ImageRollsManager : UserControl
{
    #region Fields

    /// <summary>
    /// Backing reference to the strongly-typed view model (DataContext).
    /// </summary>
    private ViewModels.ImageRollsManager _viewModel;

    /// <summary>
    /// Ensures we only attempt to auto-select the ActiveImageRoll once.
    /// </summary>
    private bool _initialSelectionApplied;

    #endregion

    #region Constructor / DataContext Wiring

    public ImageRollsManager()
    {
        InitializeComponent();

        // React to DataContext changes so we can (re)wire events safely.
        DataContextChanged += (s, e) =>
        {
            DetachViewModelEvents();

            _viewModel = DataContext as ViewModels.ImageRollsManager;
            if (_viewModel == null)
                return;

            AttachViewModelEvents();

            // Defer initial selection until after layout & data binding.
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, ApplyInitialSelectionIfNeeded);
        };
    }

    private void AttachViewModelEvents()
    {
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        if (_viewModel.AllImageRolls is INotifyCollectionChanged obs)
            obs.CollectionChanged += AllImageRolls_CollectionChanged;
    }

    private void DetachViewModelEvents()
    {
        if (_viewModel is null)
            return;

        _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        if (_viewModel.AllImageRolls is INotifyCollectionChanged oldObs)
            oldObs.CollectionChanged -= AllImageRolls_CollectionChanged;
    }

    #endregion

    #region ViewModel Event Handling

    /// <summary>
    /// Watches for ActiveImageRoll changes so we can attempt to reflect selection (once) in the TreeView.
    /// </summary>
    private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModels.ImageRollsManager.ActiveImageRoll))
        {
            if (!_initialSelectionApplied)
                Dispatcher.BeginInvoke(DispatcherPriority.Background, ApplyInitialSelectionIfNeeded);
        }
    }

    /// <summary>
    /// If rolls arrive after the initial VM is set (e.g., loaded from cache async), re-attempt selection.
    /// </summary>
    private void AllImageRolls_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (!_initialSelectionApplied && _viewModel?.ActiveImageRoll != null)
            Dispatcher.BeginInvoke(DispatcherPriority.Background, ApplyInitialSelectionIfNeeded);
    }

    #endregion

    #region TreeView Selection Bridging

    /// <summary>
    /// Syncs TreeView SelectedItem -> ViewModel.ActiveImageRoll (user initiated).
    /// </summary>
    private void TreeAllImageRolls_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is ImageRoll roll &&
            _viewModel is not null &&
            !ReferenceEquals(_viewModel.ActiveImageRoll, roll))
        {
            _viewModel.ActiveImageRoll = roll;
        }
    }

    /// <summary>
    /// When the TreeView is loaded we attempt initial selection (may still need a deferred pass).
    /// </summary>
    private void TreeAllImageRolls_Loaded(object sender, RoutedEventArgs e) =>
        ApplyInitialSelectionIfNeeded();

    #endregion

    #region Initial Selection Logic

    /// <summary>
    /// Attempts to locate and select the ActiveImageRoll's container.
    /// Retries once using DispatcherPriority.ContextIdle if containers not yet realized.
    /// </summary>
    private void ApplyInitialSelectionIfNeeded()
    {
        if (_initialSelectionApplied ||
            _viewModel?.ActiveImageRoll == null ||
            TreeAllImageRolls == null)
            return;

        // Ensure containers are generated for current layout pass.
        TreeAllImageRolls.UpdateLayout();

        if (ExpandPathAndSelect(_viewModel.ActiveImageRoll))
        {
            _initialSelectionApplied = true;
        }
        else
        {
            // Retry after idle (virtualization or deferred generation may have delayed container creation).
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, () =>
            {
                if (!_initialSelectionApplied &&
                    ExpandPathAndSelect(_viewModel.ActiveImageRoll))
                {
                    _initialSelectionApplied = true;
                }
            });
        }
    }

    /// <summary>
    /// Expands only the ancestor chain (if hierarchical in the future) and selects the target.
    /// Currently the collection is flat; kept extensible.
    /// </summary>
    private bool ExpandPathAndSelect(ImageRoll target) =>
        FindAndSelect(TreeAllImageRolls, target);

    /// <summary>
    /// Depth-first search of the ItemsControl hierarchy to locate & select the target item.
    /// Only expands containers along the search path; collapses them again if not a match.
    /// </summary>
    private bool FindAndSelect(ItemsControl parent, ImageRoll target)
    {
        int count = parent.Items.Count;
        for (int i = 0; i < count; i++)
        {
            var item = parent.Items[i];
            var container = parent.ItemContainerGenerator.ContainerFromIndex(i) as TreeViewItem;
            if (container == null)
                continue; // Container not yet generated (virtualization) – skip.

            if (ReferenceEquals(item, target))
            {
                container.IsSelected = true;
                container.BringIntoView();
                return true;
            }

            if (container.Items.Count > 0) // Future-proof for hierarchical rolls
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

    #endregion

    #region Actions / External Navigation

    /// <summary>
    /// Opens the root folder where user image roll databases reside in the system file explorer.
    /// </summary>
    private void btnOpenImageRollsLocation(object sender, RoutedEventArgs e) =>
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = $"{App.UserImageRollsRoot}\\",
            UseShellExecute = true,
            Verb = "open"
        });

    #endregion
}