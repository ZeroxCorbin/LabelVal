using LabelVal.Results.ViewModels;
using LabelVal.Utilities;
using MahApps.Metro.Controls.Dialogs;
using MaterialDesignThemes.Wpf;
using System;
using System.Linq;
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

    public ImageResultsManager()
    {
        InitializeComponent();
        DataContextChanged += ImageResultsManager_DataContextChanged;
    }

    private void ImageResultsManager_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is ViewModels.ImageResultsManager vm)
        {
            _viewModel = vm;
        }
    }

    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        DialogParticipation.SetRegister(this, null);
        DataContextChanged -= ImageResultsManager_DataContextChanged;
    }

    private void btnRightSideBar_Click(object sender, RoutedEventArgs e)
    {
        JsonDrawer.IsRightDrawerOpen = !JsonDrawer.IsRightDrawerOpen;
        btnRightSideBar.LayoutTransform = JsonDrawer.IsRightDrawerOpen ? new RotateTransform(0) : new RotateTransform(180);
    }

    private void JsonDrawer_DrawerOpened(object sender, DrawerOpenedEventArgs e)
    {
        if (_viewModel.FocusedTemplate != null)
            tiTemplate.IsSelected = true;
        else if (_viewModel.FocusedReport != null)
            tiReport.IsSelected = true;
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

            var currentItem = _viewModel.TopmostItem ?? sortedEntries.FirstOrDefault();
            if (currentItem == null) return;

            var currentIndex = sortedEntries.IndexOf(currentItem);
            if (currentIndex < 0)
            {
                var currentVisualItem = FindBestCandidate(scrollViewer, itemsControl);
                currentIndex = currentVisualItem != null ? sortedEntries.IndexOf(currentVisualItem) : 0;
                if (currentIndex < 0) currentIndex = 0;
            }

            int nextIndex = currentIndex;

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
                var nextItem = sortedEntries[nextIndex];
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

            var transform = container.TransformToAncestor(scrollViewer);
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
        if (_isSnapping || _viewModel?.ImageResultsEntries.Any() != true)
            return;

        var scrollViewer = (ScrollViewer)sender;
        if (scrollViewer.Content is not ItemsControl itemsControl)
            return;

        var bestCandidate = FindBestCandidate(scrollViewer, itemsControl);

        if (bestCandidate != null && _viewModel.TopmostItem != bestCandidate)
        {
            _viewModel.TopmostItem = bestCandidate;
            ImageRollThumbnails.SelectedItem = bestCandidate;
            ImageRollThumbnails.ScrollIntoView(bestCandidate);
        }
    }
}