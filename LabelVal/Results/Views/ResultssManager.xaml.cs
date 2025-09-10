using CommunityToolkit.Mvvm.Messaging;
using LabelVal.Main.Messages;
using MahApps.Metro.Controls.Dialogs;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace LabelVal.Results.Views;

/// <summary>
/// Interaction logic for (ResultssManager.xaml
/// </summary>
public partial class ResultssManager : UserControl
{
    private ViewModels.ResultsManager _viewModel;
    private bool _isSnapping;
    private bool _isLoaded;

    public ResultssManager()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is ViewModels.ResultsManager oldVm)
        {
            oldVm.ResultssEntries.CollectionChanged -= OnResultssEntriesChanged;
        }
        if (e.NewValue is ViewModels.ResultsManager newVm)
        {
            newVm.ResultssEntries.CollectionChanged += OnResultssEntriesChanged;
            _viewModel = newVm;
        }
    }

    private void OnResultssEntriesChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
        {
            _isLoaded = false;
        }
    }

    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        DialogParticipation.SetRegister(this, null);
        DataContextChanged -= OnDataContextChanged;
    }

    private async void ResultssScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        e.Handled = true;
        await SnapScroll(e.Delta < 0);
    }

    private async void ResultssScrollViewer_PreviewKeyDown(object sender, KeyEventArgs e)
    {
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

    private async Task SnapScroll(bool scrollDown)
    {
        if (_viewModel?.ResultssEntries.Any() != true || _isSnapping)
        {
            return;
        }

        if (ResultssScrollViewer is not ScrollViewer scrollViewer || scrollViewer.Content is not ItemsControl itemsControl)
        {
            return;
        }

        _isSnapping = true;

        try
        {
            var sortedEntries = itemsControl.Items.OfType<ViewModels.ResultsEntry>().ToList();
            if (!sortedEntries.Any()) return;

            ViewModels.ResultsEntry currentItem = _viewModel.TopmostItem ?? sortedEntries.FirstOrDefault();
            if (currentItem == null) return;

            var currentIndex = sortedEntries.IndexOf(currentItem);
            if (currentIndex < 0)
            {
                ViewModels.ResultsEntry currentVisualItem = FindBestCandidate(scrollViewer, itemsControl);
                currentIndex = currentVisualItem != null ? sortedEntries.IndexOf(currentVisualItem) : 0;
                if (currentIndex < 0) currentIndex = 0;
            }

            var nextIndex = currentIndex;

            if (scrollDown) // Scrolling down
            {
                if (currentIndex < sortedEntries.Count - 1)
                {
                    nextIndex = currentIndex + 1;
                }
            }
            else // Scrolling up
            {
                if (currentIndex > 0)
                {
                    nextIndex = currentIndex - 1;
                }
            }

            if (nextIndex != currentIndex)
            {
                ViewModels.ResultsEntry nextItem = sortedEntries[nextIndex];
                _viewModel.TopmostItem = nextItem;
                nextItem.BringIntoViewHandler();
                ImageRollThumbnails.SelectedItem = nextItem;
                ImageRollThumbnails.ScrollIntoView(nextItem);
            }
        }
        finally
        {
            await Task.Delay(200); // Debounce to prevent rapid scrolling
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
            if (item is not ViewModels.ResultsEntry imageResultEntry) continue;
            if (itemsControl.ItemContainerGenerator.ContainerFromItem(item) is not FrameworkElement container) continue;

            GeneralTransform transform = container.TransformToAncestor(scrollViewer);
            var itemTop = transform.Transform(new Point(0, 0)).Y;
            var itemBottom = itemTop + container.ActualHeight;

            if (itemBottom > 0 && itemTop < scrollViewer.ViewportHeight)
            {
                visibleItems.Add((imageResultEntry, itemTop));
            }
        }

        if (!visibleItems.Any()) return null;

        // If scrolled to the bottom, the best candidate is the last visible item.
        if (scrollViewer.VerticalOffset >= scrollViewer.ScrollableHeight - scrollTolerance)
        {
            bestCandidate = visibleItems.OrderBy(i => i.top).Last().item;
        }
        else
        {
            // Otherwise, find the item closest to the top of the viewport.
            bestCandidate = visibleItems.OrderBy(i => Math.Abs(i.top)).First().item;
        }

        return bestCandidate;
    }

    private void ResultssScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if ((!_isLoaded && e.ExtentHeight > 0) || _viewModel.ActiveImageRoll != null && (_viewModel.ActiveImageRoll.RollType == ImageRolls.ViewModels.ImageRollTypes.Database && _viewModel.ActiveImageRoll.ImageCount == 0))
        {
            _isLoaded = true;
            // Use Dispatcher to send message after rendering is complete
            _ = Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ContextIdle, new System.Action(() => WeakReferenceMessenger.Default.Send(new ResultssRenderedMessage())));
        }

        if (_isSnapping || _viewModel?.ResultssEntries.Any() != true)
            return;

        var scrollViewer = (ScrollViewer)sender;
        if (scrollViewer.Content is not ItemsControl itemsControl)
            return;

        ViewModels.ResultsEntry bestCandidate = FindBestCandidate(scrollViewer, itemsControl);

        if (bestCandidate != null && _viewModel.TopmostItem != bestCandidate)
        {
            _viewModel.TopmostItem = bestCandidate;
            ImageRollThumbnails.SelectedItem = bestCandidate;
            ImageRollThumbnails.ScrollIntoView(bestCandidate);
        }
    }
}