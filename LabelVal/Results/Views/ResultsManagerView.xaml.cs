using CommunityToolkit.Mvvm.Messaging;
using LabelVal.Main.Messages;
using MahApps.Metro.Controls.Dialogs;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace LabelVal.Results.Views;

public partial class ResultsManagerView : UserControl
{
    private ViewModels.ResultsManagerViewModel _viewModel;
    private bool _isSnapping;
    private bool _isLoaded;
    private int _pendingEntryLoads = 0;
    private int _expectedEntryCount = 0;

    #region RelayCommand
    private class RelayCommand : ICommand
    {
        private readonly Action<object> _exec;
        private readonly Func<object, bool> _can;
        public RelayCommand(Action<object> exec, Func<object, bool> can = null) { _exec = exec; _can = can; }
        public bool CanExecute(object p) => _can?.Invoke(p) ?? true;
        public void Execute(object p) => _exec(p);
        public event EventHandler CanExecuteChanged;
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
    #endregion

    #region DependencyProperty Commands
    public ICommand PreviewMouseWheelCommand
    {
        get => (ICommand)GetValue(PreviewMouseWheelCommandProperty);
        set => SetValue(PreviewMouseWheelCommandProperty, value);
    }
    public static readonly DependencyProperty PreviewMouseWheelCommandProperty =
        DependencyProperty.Register(nameof(PreviewMouseWheelCommand), typeof(ICommand), typeof(ResultsManagerView));

    public ICommand PreviewKeyDownCommand
    {
        get => (ICommand)GetValue(PreviewKeyDownCommandProperty);
        set => SetValue(PreviewKeyDownCommandProperty, value);
    }
    public static readonly DependencyProperty PreviewKeyDownCommandProperty =
        DependencyProperty.Register(nameof(PreviewKeyDownCommand), typeof(ICommand), typeof(ResultsManagerView));

    public ICommand ScrollChangedCommand
    {
        get => (ICommand)GetValue(ScrollChangedCommandProperty);
        set => SetValue(ScrollChangedCommandProperty, value);
    }
    public static readonly DependencyProperty ScrollChangedCommandProperty =
        DependencyProperty.Register(nameof(ScrollChangedCommand), typeof(ICommand), typeof(ResultsManagerView));

    public ICommand UnloadedCommand
    {
        get => (ICommand)GetValue(UnloadedCommandProperty);
        set => SetValue(UnloadedCommandProperty, value);
    }
    public static readonly DependencyProperty UnloadedCommandProperty =
        DependencyProperty.Register(nameof(UnloadedCommand), typeof(ICommand), typeof(ResultsManagerView));
    #endregion

    public ResultsManagerView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;

        PreviewMouseWheelCommand = new RelayCommand(e => HandlePreviewMouseWheel(e as MouseWheelEventArgs));
        PreviewKeyDownCommand = new RelayCommand(e => HandlePreviewKeyDown(e as KeyEventArgs));
        ScrollChangedCommand = new RelayCommand(e => HandleScrollChanged(e as ScrollChangedEventArgs));
        UnloadedCommand = new RelayCommand(e => HandleUnloaded());
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is ViewModels.ResultsManagerViewModel oldVm)
            oldVm.ResultssEntries.CollectionChanged -= OnResultssEntriesChanged;

        if (e.NewValue is ViewModels.ResultsManagerViewModel newVm)
        {
            newVm.ResultssEntries.CollectionChanged += OnResultssEntriesChanged;
            _viewModel = newVm;
        }
    }

    private void OnResultssEntriesChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        PrepareForNewRoll();
        AttachEntryLoadedHandlers();
    }

    private void PrepareForNewRoll()
    {
        _expectedEntryCount = _viewModel?.ResultssEntries.Count ?? 0;
        _pendingEntryLoads = _expectedEntryCount;
        _isLoaded = false;
    }

    private void AttachEntryLoadedHandlers()
    {
        if (ResultssScrollViewer.Content is ItemsControl itemsControl)
        {
            itemsControl.ItemContainerGenerator.StatusChanged -= ItemContainerGenerator_StatusChanged;
            itemsControl.ItemContainerGenerator.StatusChanged += ItemContainerGenerator_StatusChanged;
        }
    }

    private void ItemContainerGenerator_StatusChanged(object sender, EventArgs e)
    {
        if (ResultssScrollViewer.Content is ItemsControl itemsControl &&
            itemsControl.ItemContainerGenerator.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
        {
            foreach (var item in itemsControl.Items)
            {
                if (itemsControl.ItemContainerGenerator.ContainerFromItem(item) is FrameworkElement container)
                {
                    container.Loaded -= ResultsEntry_Loaded;
                    container.Loaded += ResultsEntry_Loaded;
                }
            }
        }
    }

    private void ResultsEntry_Loaded(object sender, RoutedEventArgs e)
    {
        if (_pendingEntryLoads > 0)
        {
            _pendingEntryLoads--;
            if (_pendingEntryLoads == 0 && !_isLoaded)
            {
                _isLoaded = true;
                _ = Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ContextIdle,
                    new Action(() => CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default.Send(new ResultssRenderedMessage())));
            }
        }
    }

    #region New Command Handler Methods (formerly event handlers)
    private async void HandlePreviewMouseWheel(MouseWheelEventArgs e)
    {
        if (e == null) return;
        e.Handled = true;
        await SnapScroll(e.Delta < 0);
    }

    private async void HandlePreviewKeyDown(KeyEventArgs e)
    {
        if (e == null) return;
        switch (e.Key)
        {
            case Key.Down:
            case Key.PageDown:
                e.Handled = true;
                await SnapScroll(true);
                break;
            case Key.Up:
            case Key.PageUp:
                e.Handled = true;
                await SnapScroll(false);
                break;
        }
    }

    private void HandleScrollChanged(ScrollChangedEventArgs e)
    {
        if (e == null) return;
        if (_isSnapping || _viewModel?.ResultssEntries.Any() != true) return;

        if (ResultssScrollViewer.Content is not ItemsControl itemsControl) return;

        var bestCandidate = FindBestCandidate(ResultssScrollViewer, itemsControl);
        if (bestCandidate != null && _viewModel.TopmostItem != bestCandidate)
        {
            _viewModel.TopmostItem = bestCandidate;
            ImageRollThumbnails.SelectedItem = bestCandidate;
            ImageRollThumbnails.ScrollIntoView(bestCandidate);
        }
    }

    private void HandleUnloaded()
    {
        DialogParticipation.SetRegister(this, null);
        DataContextChanged -= OnDataContextChanged;
    }
    #endregion

    private async Task SnapScroll(bool scrollDown)
    {
        if (_viewModel?.ResultssEntries.Any() != true || _isSnapping) return;
        if (ResultssScrollViewer is not ScrollViewer scrollViewer || scrollViewer.Content is not ItemsControl itemsControl) return;

        _isSnapping = true;
        try
        {
            var sortedEntries = itemsControl.Items.OfType<ViewModels.ResultsEntry>().ToList();
            if (!sortedEntries.Any()) return;

            var currentItem = _viewModel.TopmostItem ?? sortedEntries.FirstOrDefault();
            if (currentItem == null) return;

            var currentIndex = sortedEntries.IndexOf(currentItem);
            if (currentIndex < 0)
            {
                var currentVisualItem = FindBestCandidate(scrollViewer, itemsControl);
                currentIndex = currentVisualItem != null ? sortedEntries.IndexOf(currentVisualItem) : 0;
                if (currentIndex < 0) currentIndex = 0;
            }

            var nextIndex = currentIndex;
            if (scrollDown)
            {
                if (currentIndex < sortedEntries.Count - 1)
                    nextIndex = currentIndex + 1;
            }
            else
            {
                if (currentIndex > 0)
                    nextIndex = currentIndex - 1;
            }

            if (nextIndex != currentIndex)
            {
                var nextItem = sortedEntries[nextIndex];
                _viewModel.TopmostItem = nextItem;
                nextItem.BringIntoViewHandler();
                ImageRollThumbnails.SelectedItem = nextItem;
                ImageRollThumbnails.ScrollIntoView(nextItem);
            }
        }
        finally
        {
            await Task.Delay(200);
            _isSnapping = false;
        }
    }

    private ViewModels.ResultsEntry FindBestCandidate(ScrollViewer scrollViewer, ItemsControl itemsControl)
    {
        const double scrollTolerance = 1.0;
        ViewModels.ResultsEntry bestCandidate = null;

        var visibleItems = new List<(ViewModels.ResultsEntry item, double top)>();
        foreach (var item in itemsControl.Items)
        {
            if (item is not ViewModels.ResultsEntry entry) continue;
            if (itemsControl.ItemContainerGenerator.ContainerFromItem(item) is not FrameworkElement container) continue;

            GeneralTransform transform = container.TransformToAncestor(scrollViewer);
            var itemTop = transform.Transform(new Point(0, 0)).Y;
            var itemBottom = itemTop + container.ActualHeight;

            if (itemBottom > 0 && itemTop < scrollViewer.ViewportHeight)
                visibleItems.Add((entry, itemTop));
        }

        if (!visibleItems.Any()) return null;

        if (scrollViewer.VerticalOffset >= scrollViewer.ScrollableHeight - scrollTolerance)
            bestCandidate = visibleItems.OrderBy(i => i.top).Last().item;
        else
            bestCandidate = visibleItems.OrderBy(i => Math.Abs(i.top)).First().item;

        return bestCandidate;
    }
}