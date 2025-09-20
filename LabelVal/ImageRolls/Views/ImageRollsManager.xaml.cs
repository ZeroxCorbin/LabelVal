using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using LabelVal.ImageRolls.ViewModels;

namespace LabelVal.ImageRolls.Views;

/// <summary>
/// WPF <see cref="UserControl"/> that displays and manages a grouped TreeView of <see cref="ImageRoll"/> items.
/// Core responsibilities:
///  - Keep TreeView selection synchronized with <see cref="ViewModels.ImageRollsManager.ActiveImageRoll"/>.
///  - React robustly to grouping/regrouping, collection mutations, and container virtualization timing.
///  - Highlight ancestor group nodes for the currently selected (leaf) <see cref="ImageRoll"/>.
///  - Handle dynamic regroup when properties affecting grouping change (Name, StandardGroup, SectorType, ImageType).
///  - Provide resilient, retry-based selection logic safe against generator latency and layout cycles.
/// Design notes:
///  - Selection sync is tokenized (_activeRollSelectionToken) to avoid stale async attempts.
///  - Retrying uses escalating Dispatcher priorities to balance responsiveness and correctness.
///  - Group highlighting is implemented via an attached DP (IsAncestorOfSelected) for styling.
/// </summary>
public partial class ImageRollsManager : UserControl
{
    #region Fields / Constants

    // Tracks which group TreeViewItem ancestors are currently highlighted (so we can clear flags efficiently).
    private readonly List<TreeViewItem> _highlightedGroupAncestors = new();

    private ViewModels.ImageRollsManager _viewModel;
    private bool _initialSelectionApplied;

    // For monitoring CollectionView regroup events caused by changes to grouping keys.
    private ICollectionView? _groupingCollectionView;
    private INotifyCollectionChanged? _groupingNotifier;

    // Token invalidation mechanism for async selection attempts.
    private int _activeRollSelectionToken;
    private const int MaxActiveRollSelectionRetries = 8;

    // Track which ImageRoll instances we've subscribed to for PropertyChanged (so we can detach cleanly).
    private readonly HashSet<ImageRoll> _wiredRolls = new();

    // Property names that affect WPF CollectionView grouping (adjust if grouping changes in XAML).
    private static readonly HashSet<string> _groupingPropertyNames =
        new(["StandardGroup", "Name", "SectorType", "ImageType"], StringComparer.Ordinal);

    #endregion

    #region Ctor / Initialization

    public ImageRollsManager()
    {
        InitializeComponent();

        // Listen globally for expansion to re-highlight or reselect as needed.
        AddHandler(TreeViewItem.ExpandedEvent, new RoutedEventHandler(OnTreeViewItemExpanded), handledEventsToo: true);

        // Re-wire whenever DataContext changes.
        DataContextChanged += (_, _) =>
        {
            DetachViewModelEvents();

            _viewModel = DataContext as ViewModels.ImageRollsManager;
            if (_viewModel == null) return;

            AttachViewModelEvents();
            WireAllCurrentRolls();
            HookGroupingEvents();

            // Defer initial selection until layout / containers are ready.
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, ApplyInitialSelectionIfNeeded);
        };
    }

    #endregion

    #region ViewModel Wiring

    private void AttachViewModelEvents()
    {
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;

        if (_viewModel.AllImageRolls is INotifyCollectionChanged obs)
            obs.CollectionChanged += AllImageRolls_CollectionChanged;
    }

    private void DetachViewModelEvents()
    {
        if (_viewModel is null) return;

        _viewModel.PropertyChanged -= ViewModel_PropertyChanged;

        if (_viewModel.AllImageRolls is INotifyCollectionChanged oldObs)
            oldObs.CollectionChanged -= AllImageRolls_CollectionChanged;

        UnwireAllRolls();
    }

    #endregion

    #region ImageRoll Wiring (PropertyChanged tracking)

    private void WireAllCurrentRolls()
    {
        if (_viewModel == null) return;
        foreach (var r in _viewModel.AllImageRolls)
            WireRoll(r);
    }

    private void WireRoll(ImageRoll? roll)
    {
        if (roll == null || _wiredRolls.Contains(roll)) return;
        roll.PropertyChanged += Roll_PropertyChanged;
        _wiredRolls.Add(roll);
    }

    private void UnwireRoll(ImageRoll? roll)
    {
        if (roll == null) return;
        if (_wiredRolls.Remove(roll))
            roll.PropertyChanged -= Roll_PropertyChanged;
    }

    private void UnwireAllRolls()
    {
        foreach (var r in _wiredRolls)
            r.PropertyChanged -= Roll_PropertyChanged;
        _wiredRolls.Clear();
    }

    /// <summary>
    /// If the active roll changes a property affecting grouping, regroup may relocate it; we then resync selection.
    /// Selection is deferred until after regroup completes (Background priority).
    /// </summary>
    private void Roll_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not ImageRoll roll || !_wiredRolls.Contains(roll))
            return;

        if (ReferenceEquals(sender, _viewModel?.ActiveImageRoll) &&
            _groupingPropertyNames.Contains(e.PropertyName))
        {
            // Wait for CollectionView to complete regroup before attempting to re-find containers.
            Dispatcher.BeginInvoke(DispatcherPriority.Background,
                () => ScheduleActiveRollSyncWithContainerRealization(fullRestart: true));
        }
    }

    #endregion

    #region ViewModel Event Handling

    private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModels.ImageRollsManager.ActiveImageRoll))
        {
            if (_viewModel?.ActiveImageRoll is null)
            {
                ClearHighlightedGroups();
                ClearTreeViewSelection();
                _initialSelectionApplied = false;
                return;
            }

            // Defer to allow UI to realize containers after data changes.
            Dispatcher.BeginInvoke(DispatcherPriority.Background, SyncActiveRollSelection);
        }
    }

    private void AllImageRolls_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        // Maintain wiring for new / removed rolls.
        if (e.Action is NotifyCollectionChangedAction.Add or NotifyCollectionChangedAction.Replace)
            if (e.NewItems != null)
                foreach (ImageRoll r in e.NewItems)
                    WireRoll(r);

        if (e.Action is NotifyCollectionChangedAction.Remove or NotifyCollectionChangedAction.Replace)
            if (e.OldItems != null)
                foreach (ImageRoll r in e.OldItems)
                    UnwireRoll(r);

        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            UnwireAllRolls();
            WireAllCurrentRolls();
        }

        // Re-evaluate selection if active roll exists.
        if (_viewModel?.ActiveImageRoll != null)
            Dispatcher.BeginInvoke(DispatcherPriority.Background, SyncActiveRollSelection);
    }

    #endregion

    #region Selection Synchronization (Token + Retry Mechanism)

    /// <summary>
    /// Unified entrypoint to re-sync TreeView selection to ActiveImageRoll.
    /// Uses a token to invalidate prior async attempts.
    /// </summary>
    private void SyncActiveRollSelection()
    {
        if (_viewModel?.ActiveImageRoll == null || TreeAllImageRolls == null)
            return;

        int token = ++_activeRollSelectionToken;
        AttemptSelectActiveRoll(token, attempt: 0);
    }

    /// <summary>
    /// Attempt selection; escalates through several dispatcher priorities while containers/layout become available.
    /// </summary>
    private void AttemptSelectActiveRoll(int token, int attempt)
    {
        if (token != _activeRollSelectionToken) return;
        if (_viewModel?.ActiveImageRoll == null || TreeAllImageRolls == null) return;

        if (!EnsureRootContainersReady(() => AttemptSelectActiveRoll(token, attempt)))
            return; // Will continue once root generator signals ready.

        // Only run UpdateLayout if previous attempt failed (avoid unnecessary layout passes).
        bool foundPreviously = attempt > 0 && TrySelectAndHighlightActiveRoll();
        if (!foundPreviously)
            TreeAllImageRolls.UpdateLayout();

        if (TrySelectAndHighlightActiveRoll())
        {
            _initialSelectionApplied = true;
            return;
        }

        if (attempt >= MaxActiveRollSelectionRetries)
            return;

        var nextPriority = attempt switch
        {
            < 2 => DispatcherPriority.Background,
            < 5 => DispatcherPriority.ContextIdle,
            _ => DispatcherPriority.Render
        };

        Dispatcher.BeginInvoke(nextPriority, () => AttemptSelectActiveRoll(token, attempt + 1));
    }

    /// <summary>
    /// Ensures root containers exist; if not, registers a handler to retry when generated.
    /// </summary>
    private bool EnsureRootContainersReady(Action retry)
    {
        var gen = TreeAllImageRolls!.ItemContainerGenerator;
        if (gen.Status == GeneratorStatus.ContainersGenerated)
            return true;

        void Handler(object? _, EventArgs _2)
        {
            if (gen.Status == GeneratorStatus.ContainersGenerated)
            {
                gen.StatusChanged -= Handler;
                Dispatcher.BeginInvoke(DispatcherPriority.Background, retry);
            }
        }

        gen.StatusChanged += Handler;
        return false;
    }

    /// <summary>
    /// Core selection + highlight attempt (single pass).
    /// </summary>
    private bool TrySelectAndHighlightActiveRoll()
    {
        var target = _viewModel?.ActiveImageRoll;
        if (target == null) return false;

        bool selected = ExpandPathAndSelect(target);

        var container = TreeAllImageRolls != null
            ? FindContainerForItem(TreeAllImageRolls, target)
            : null;

        if (container != null)
        {
            if (!container.IsSelected)
                container.IsSelected = true;
            else
                HighlightLeafAndAncestors(container);
        }

        return container != null && selected;
    }

    private void ClearTreeViewSelection()
    {
        if (TreeAllImageRolls == null) return;
        var selected = GetCurrentlySelectedContainer();
        if (selected != null)
            selected.IsSelected = false;
    }

    #endregion

    #region TreeView Selection Utilities

    private TreeViewItem? GetCurrentlySelectedContainer() =>
        FindSelectedContainer(TreeAllImageRolls);

    /// <summary>
    /// Depth-first search for currently selected <see cref="TreeViewItem"/>.
    /// </summary>
    private TreeViewItem? FindSelectedContainer(ItemsControl parent)
    {
        if (parent == null) return null;

        int count = parent.Items.Count;
        for (int i = 0; i < count; i++)
        {
            var container = parent.ItemContainerGenerator.ContainerFromIndex(i) as TreeViewItem;
            if (container == null) continue;

            if (container.IsSelected)
                return container;

            if (container.Items.Count > 0 && container.IsExpanded)
            {
                var child = FindSelectedContainer(container);
                if (child != null)
                    return child;
            }
        }
        return null;
    }

    /// <summary>
    /// Expands group path and selects the target <see cref="ImageRoll"/> if found.
    /// </summary>
    private bool ExpandPathAndSelect(ImageRoll target) =>
        FindAndSelect(TreeAllImageRolls, target);

    // Helper: checks (data level) whether a CollectionViewGroup subtree contains the target roll.
    private static bool GroupContainsTarget(CollectionViewGroup group, ImageRoll target)
    {
        foreach (var item in group.Items)
        {
            if (ReferenceEquals(item, target))
                return true;
        }
        foreach (var item in group.Items)
        {
            if (item is CollectionViewGroup sub && GroupContainsTarget(sub, target))
                return true;
        }
        return false;
    }

    // Replace the existing FindAndSelect with this version.
    // Only expands the single group chain that actually contains the target (data-driven),
    // avoiding temporary expansion of unrelated sibling groups.
    private bool FindAndSelect(ItemsControl parent, ImageRoll target)
    {
        int count = parent.Items.Count;
        for (int i = 0; i < count; i++)
        {
            var item = parent.Items[i];

            // Direct leaf at this level
            if (ReferenceEquals(item, target))
            {
                if (parent.ItemContainerGenerator.ContainerFromIndex(i) is TreeViewItem leaf)
                {
                    if (!leaf.IsSelected)
                        leaf.IsSelected = true;
                    leaf.BringIntoView();
                    return true;
                }
                // Container not yet realized: let retry loop handle later
                return false;
            }

            // If this is a group (CollectionViewGroup) use data-level containment test
            if (item is CollectionViewGroup grp)
            {
                // Skip entire branch if the group does not contain the target
                if (!GroupContainsTarget(grp, target))
                    continue;

                // We now know the target lies somewhere under this group.
                var container = parent.ItemContainerGenerator.ContainerFromIndex(i) as TreeViewItem;
                if (container == null)
                    return false; // Containers for this level not ready yet; retry later.

                if (!container.IsExpanded)
                {
                    container.IsExpanded = true;
                    // Do not recurse in same pass; children creation may be deferred (MaterialDesign)
                    return false;
                }

                // Children are (hopefully) realized; descend
                if (FindAndSelect(container, target))
                    return true;

                // If recursion failed (children not yet realized), abort this pass
                return false;
            }
        }
        return false;
    }

    /// <summary>
    /// Attempts to locate a realized container for a given item by descending expanded branches.
    /// </summary> 
    private TreeViewItem? FindContainerForItem(ItemsControl parent, object item)
    {
        var direct = parent.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
        if (direct != null)
            return direct;

        int count = parent.Items.Count;
        for (int i = 0; i < count; i++)
        {
            var childContainer = parent.ItemContainerGenerator.ContainerFromIndex(i) as TreeViewItem;
            if (childContainer == null) continue;

            if (childContainer.Items.Count > 0 && childContainer.IsExpanded)
            {
                var match = FindContainerForItem(childContainer, item);
                if (match != null)
                    return match;
            }
        }
        return null;
    }

    #endregion

    #region Event Handlers (TreeView & Selection)

    private void TreeAllImageRolls_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is ImageRoll roll &&
            _viewModel is not null &&
            !ReferenceEquals(_viewModel.ActiveImageRoll, roll))
        {
            _viewModel.ActiveImageRoll = roll;
        }
    }

    private void TreeAllImageRolls_Loaded(object sender, RoutedEventArgs e)
    {
        HookGroupingEvents();
        ApplyInitialSelectionIfNeeded();
    }

    /// <summary>
    /// Applies the initial selection only once when ActiveImageRoll is known and UI is ready.
    /// </summary>
    private void ApplyInitialSelectionIfNeeded()
    {
        if (_initialSelectionApplied ||
            _viewModel?.ActiveImageRoll == null ||
            TreeAllImageRolls == null)
            return;

        SyncActiveRollSelection();
    }

    #endregion

    #region Group & Leaf Interaction / Highlighting

    /// <summary>
    /// Style selector used in XAML to differentiate between group nodes and leaf nodes (ImageRolls).
    /// </summary>
    public class ImageRollsTreeViewItemStyleSelector : StyleSelector
    {
        public Style GroupStyle { get; set; }
        public Style ItemStyle { get; set; }

        public override Style SelectStyle(object item, DependencyObject container) =>
            item is CollectionViewGroup ? GroupStyle : ItemStyle;
    }

    /// <summary>
    /// Toggle expand/collapse when clicking on a group row background (excluding expander glyph or nested leaves).
    /// </summary>
    private void GroupTreeViewItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not TreeViewItem groupItem) return;
        if (groupItem.DataContext is not CollectionViewGroup) return;

        var origin = e.OriginalSource as DependencyObject;

        var descendant = FindAncestor<System.Windows.Controls.TreeViewItem>(origin, stopAt: groupItem);
        if (descendant != null && descendant != groupItem)
            return; // Click was on a nested leaf.

        if (IsInExpander(origin))
            return; // Let expander toggle behave normally.

        groupItem.IsExpanded = !groupItem.IsExpanded;
        e.Handled = true;
    }

    /// <summary>
    /// Prevent groups from becoming selected (selection should stay on leaves).
    /// </summary>
    private void GroupTreeViewItem_Selected(object sender, RoutedEventArgs e)
    {
        if (sender is TreeViewItem tvi && tvi.DataContext is CollectionViewGroup)
        {
            tvi.IsSelected = false;
            e.Handled = true;
        }
    }

    /// <summary>
    /// When a leaf node is selected, highlight its ancestor groups.
    /// </summary>
    private void LeafTreeViewItem_Selected(object sender, RoutedEventArgs e)
    {
        if (sender is not TreeViewItem leaf ||
            leaf.DataContext is not ImageRoll)
            return;

        HighlightLeafAndAncestors(leaf);
    }

    /// <summary>
    /// Handles expansion of group nodes to re-apply highlighting or reselect the active roll if necessary.
    /// </summary>
    private void OnTreeViewItemExpanded(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not TreeViewItem group ||
            group.DataContext is not CollectionViewGroup)
            return;

        var active = _viewModel?.ActiveImageRoll;
        if (active == null) return;

        if (TreeAllImageRolls?.SelectedItem is ImageRoll currentLeaf &&
            ReferenceEquals(currentLeaf, active))
        {
            var container = FindContainerForItem(TreeAllImageRolls, active);
            if (container != null)
                HighlightLeafAndAncestors(container);
            return;
        }

        if (TreeAllImageRolls?.SelectedItem is CollectionViewGroup)
            TryReselectActiveAfterExpand(group, active);
    }

    /// <summary>
    /// Retries selection of the active leaf after a group expansion (containers may not be realized immediately).
    /// </summary>
    private void TryReselectActiveAfterExpand(TreeViewItem expandedGroupContainer, ImageRoll active)
    {
        int attempts = 0;
        const int maxAttempts = 4;

        void Attempt()
        {
            attempts++;

            var leafContainer =
                FindContainerForItem(expandedGroupContainer, active)
                ?? (TreeAllImageRolls != null ? FindContainerForItem(TreeAllImageRolls, active) : null);

            if (leafContainer != null)
            {
                if (!leafContainer.IsSelected)
                    leafContainer.IsSelected = true;
                else
                    HighlightLeafAndAncestors(leafContainer);
            }
            else if (attempts < maxAttempts)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)Attempt);
            }
        }

        Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)Attempt);
    }

    #endregion

    #region Ancestor Highlight Attached Property

    public static readonly DependencyProperty IsAncestorOfSelectedProperty =
        DependencyProperty.RegisterAttached(
            "IsAncestorOfSelected",
            typeof(bool),
            typeof(ImageRollsManager),
            new FrameworkPropertyMetadata(false));

    public static bool GetIsAncestorOfSelected(DependencyObject obj) =>
        (bool)obj.GetValue(IsAncestorOfSelectedProperty);

    public static void SetIsAncestorOfSelected(DependencyObject obj, bool value) =>
        obj.SetValue(IsAncestorOfSelectedProperty, value);

    private void ClearHighlightedGroups()
    {
        foreach (var tvi in _highlightedGroupAncestors)
            SetIsAncestorOfSelected(tvi, false);
        _highlightedGroupAncestors.Clear();
    }

    /// <summary>
    /// Walks visual tree upward from the selected leaf, marking ancestor group nodes for styling.
    /// </summary>
    private void HighlightLeafAndAncestors(TreeViewItem leaf)
    {
        ClearHighlightedGroups();

        DependencyObject current = leaf;
        while ((current = VisualTreeHelper.GetParent(current)) != null)
        {
            if (current is TreeViewItem tvi &&
                tvi.DataContext is CollectionViewGroup)
            {
                SetIsAncestorOfSelected(tvi, true);
                _highlightedGroupAncestors.Add(tvi);
            }
            if (current is System.Windows.Controls.TreeView)
                break;
        }
    }

    #endregion

    #region Grouping (CollectionView Monitoring)

    /// <summary>
    /// Hooks CollectionChanged for the current CollectionView used by the TreeView (for regroup / reset detection).
    /// </summary>
    private void HookGroupingEvents()
    {
        if (TreeAllImageRolls?.ItemsSource == null)
            return;

        var view = CollectionViewSource.GetDefaultView(TreeAllImageRolls.ItemsSource);
        if (ReferenceEquals(view, _groupingCollectionView))
            return;

        if (_groupingNotifier != null)
            _groupingNotifier.CollectionChanged -= OnGroupingCollectionChanged;

        _groupingCollectionView = view;
        _groupingNotifier = view as INotifyCollectionChanged;

        if (_groupingNotifier != null)
            _groupingNotifier.CollectionChanged += OnGroupingCollectionChanged;
    }

    /// <summary>
    /// Invoked when grouping structure changes (Reset/Move/Add/Remove) — triggers robust reselection.
    /// </summary>
    private void OnGroupingCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_viewModel?.ActiveImageRoll == null)
            return;

        // After regroup, containers are recreated; schedule a full restart of selection sync.
        ScheduleActiveRollSyncWithContainerRealization(fullRestart: true);
    }

    /// <summary>
    /// Schedules selection after root container generator readiness. Optionally invalidates prior attempts.
    /// </summary>
    private void ScheduleActiveRollSyncWithContainerRealization(bool fullRestart)
    {
        if (fullRestart)
            _activeRollSelectionToken++;

        if (TreeAllImageRolls == null)
            return;

        if (TreeAllImageRolls.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background, SyncActiveRollSelection);
        }
        else
        {
            void Handler(object? _, EventArgs _2)
            {
                if (TreeAllImageRolls.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
                {
                    TreeAllImageRolls.ItemContainerGenerator.StatusChanged -= Handler;
                    Dispatcher.BeginInvoke(DispatcherPriority.Background, SyncActiveRollSelection);
                }
            }
            TreeAllImageRolls.ItemContainerGenerator.StatusChanged += Handler;
        }
    }

    #endregion

    #region Visual Helpers

    /// <summary>
    /// Finds first ancestor of type <typeparamref name="TAncestor"/> above 'start' stopping at 'stopAt'.
    /// </summary>
    private static TAncestor? FindAncestor<TAncestor>(DependencyObject? start, System.Windows.Controls.TreeViewItem? stopAt)
        where TAncestor : DependencyObject
    {
        var current = start;
        while (current != null && current != stopAt)
        {
            if (current is TAncestor match)
                return match;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    /// <summary>
    /// Returns true if the visual chain includes an Expander ToggleButton (Expander glyph area).
    /// </summary>
    private static bool IsInExpander(DependencyObject? start)
    {
        var current = start;
        while (current != null)
        {
            if (current is ToggleButton tb && tb.Name == "Expander")
                return true;
            current = VisualTreeHelper.GetParent(current);
        }
        return false;
    }

    #endregion

    #region Commands / Buttons

    private void btnOpenImageRollsLocation(object sender, RoutedEventArgs e) =>
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = $"{App.UserImageRollsRoot}\\",
            UseShellExecute = true,
            Verb = "open"
        });

    #endregion
}