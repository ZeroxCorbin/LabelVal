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
/// Interaction logic for ImageResultsManager.xaml
/// </summary>
public partial class ImageResultsManager : UserControl
{
    private ViewModels.ImageResultsManager _viewModel;
    private bool _isSnapping;
    private bool _isLoaded;

    public ImageResultsManager()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is ViewModels.ImageResultsManager oldVm)
        {
            oldVm.ImageResultsEntries.CollectionChanged -= OnImageResultsEntriesChanged;
        }
        if (e.NewValue is ViewModels.ImageResultsManager newVm)
        {
            newVm.ImageResultsEntries.CollectionChanged += OnImageResultsEntriesChanged;
            _viewModel = newVm;
        }
    }

    private void OnImageResultsEntriesChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
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

    private async void ImageResultsScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        e.Handled = true;
        await SnapScroll(e.Delta < 0);
    }

    private async void ImageResultsScrollViewer_PreviewKeyDown(object sender, KeyEventArgs e)
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
        if (_viewModel?.ImageResultsEntries.Any() != true || _isSnapping)
        {
            return;
        }

        if (ImageResultsScrollViewer is not ScrollViewer scrollViewer || scrollViewer.Content is not ItemsControl itemsControl)
        {
            return;
        }

        _isSnapping = true;

        try
        {
            var sortedEntries = itemsControl.Items.OfType<ViewModels.ImageResultEntry>().ToList();
            if (!sortedEntries.Any()) return;

            ViewModels.ImageResultEntry currentItem = _viewModel.TopmostItem ?? sortedEntries.FirstOrDefault();
            if (currentItem == null) return;

            var currentIndex = sortedEntries.IndexOf(currentItem);
            if (currentIndex < 0)
            {
                ViewModels.ImageResultEntry currentVisualItem = FindBestCandidate(scrollViewer, itemsControl);
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
                ViewModels.ImageResultEntry nextItem = sortedEntries[nextIndex];
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

    private ViewModels.ImageResultEntry FindBestCandidate(ScrollViewer scrollViewer, ItemsControl itemsControl)
    {
        const double scrollTolerance = 1.0;
        ViewModels.ImageResultEntry bestCandidate = null;

        var visibleItems = new List<(ViewModels.ImageResultEntry item, double top)>();
        foreach (var item in itemsControl.Items)
        {
            if (item is not ViewModels.ImageResultEntry imageResultEntry) continue;
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

    private void ImageResultsScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if ((!_isLoaded && e.ExtentHeight > 0) || (_viewModel.SelectedImageRoll.RollType == ImageRolls.ViewModels.ImageRollTypes.Database && _viewModel.SelectedImageRoll.ImageCount == 0))
        {
            _isLoaded = true;
            // Use Dispatcher to send message after rendering is complete
            _ = Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ContextIdle, new System.Action(() => WeakReferenceMessenger.Default.Send(new ImageResultsRenderedMessage())));
        }

        if (_isSnapping || _viewModel?.ImageResultsEntries.Any() != true)
            return;

        var scrollViewer = (ScrollViewer)sender;
        if (scrollViewer.Content is not ItemsControl itemsControl)
            return;

        ViewModels.ImageResultEntry bestCandidate = FindBestCandidate(scrollViewer, itemsControl);

        if (bestCandidate != null && _viewModel.TopmostItem != bestCandidate)
        {
            _viewModel.TopmostItem = bestCandidate;
            ImageRollThumbnails.SelectedItem = bestCandidate;
            ImageRollThumbnails.ScrollIntoView(bestCandidate);
        }
    }
}