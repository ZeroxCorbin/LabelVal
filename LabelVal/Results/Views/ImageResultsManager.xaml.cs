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
            const double scrollTolerance = 1.0;
            var sortedEntries = itemsControl.Items.OfType<ViewModels.ImageResultEntry>().ToList();

            var currentItem = _viewModel.TopmostItem ?? sortedEntries.FirstOrDefault();
            if (currentItem == null)
                return;

            var currentIndex = sortedEntries.IndexOf(currentItem);
            int nextIndex = currentIndex;

            if (scrollDown) // Scrolling down
            {
                // Allow scrolling down if we are not at the bottom AND there is a next item.
                if (scrollViewer.VerticalOffset < scrollViewer.ScrollableHeight - scrollTolerance && currentIndex < sortedEntries.Count - 1)
                {
                    nextIndex = currentIndex + 1;
                }
            }
            else // Scrolling up
            {
                // Allow scrolling up if we are not at the top AND there is a previous item.
                if (scrollViewer.VerticalOffset > scrollTolerance && currentIndex > 0)
                {
                    nextIndex = currentIndex - 1;
                }
            }

            if (nextIndex != currentIndex)
            {
                var nextItem = sortedEntries[nextIndex];
                nextItem.BringIntoViewHandler();
                _viewModel.TopmostItem = nextItem;
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

    private void ImageResultsScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (_isSnapping || _viewModel?.ImageResultsEntries.Any() != true)
            return;

        var scrollViewer = (ScrollViewer)sender;
        if (scrollViewer.Content is not ItemsControl itemsControl)
            return;

        const double topAllowance = 1; // 10-pixel allowance
        ViewModels.ImageResultEntry bestCandidate = null;
        double minDistanceToTop = double.MaxValue;

        foreach (var item in itemsControl.Items)
        {
            if (item is not ViewModels.ImageResultEntry imageResultEntry)
                continue;

            var container = itemsControl.ItemContainerGenerator.ContainerFromItem(item) as FrameworkElement;
            if (container != null && container.IsVisible)
            {
                var transform = container.TransformToAncestor(scrollViewer);
                var itemTop = transform.Transform(new Point(0, 0)).Y;
                var itemBottom = itemTop + container.ActualHeight;

                // Check if the item is at least partially visible in the viewport, including the allowance
                if (itemBottom > -topAllowance && itemTop < scrollViewer.ViewportHeight)
                {
                    var distance = Math.Abs(itemTop);
                    // Find the item closest to the top of the viewport (or just above it)
                    if (distance < minDistanceToTop)
                    {
                        minDistanceToTop = distance;
                        bestCandidate = imageResultEntry;
                    }
                }
            }
        }

        if (bestCandidate != null && _viewModel.TopmostItem != bestCandidate)
        {
            _viewModel.TopmostItem = bestCandidate;
            ImageRollThumbnails.SelectedItem = bestCandidate;
            ImageRollThumbnails.ScrollIntoView(bestCandidate);
        }
    }
}