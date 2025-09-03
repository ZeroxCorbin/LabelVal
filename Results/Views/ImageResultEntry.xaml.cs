using LabelVal.Utilities;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Windows;
using System.Windows.Controls;

namespace LabelVal.Results.Views;

/// <summary>
/// Interaction logic for LabelControlView.xaml
/// </summary>
public partial class ImageResultEntry : UserControl
{
    public ImageResultEntry() => InitializeComponent();

    private ViewModels.ImageResultEntry viewModel;

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        viewModel = (ViewModels.ImageResultEntry)DataContext;
        viewModel.BringIntoView += ViewModel_BringIntoView;


    }
    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        DialogParticipation.SetRegister(this, null);

        viewModel.BringIntoView -= ViewModel_BringIntoView;
        viewModel = null;
    }

    private void ViewModel_BringIntoView()
    {
        // This ensures the item is scrolled to the top of the view.
        var scrollViewer = Utilities.VisualTreeHelp.GetVisualParent<ScrollViewer>(this);
        if (scrollViewer != null)
        {
            // Calculate the offset of the item relative to the ScrollViewer's content.
            var transform = TransformToAncestor(scrollViewer);
            var offset = transform.Transform(new Point(0, 0));

            // Scroll to the calculated offset.
            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + offset.Y);
        }
        else
        {
            // Fallback to the default behavior if the ScrollViewer is not found for some reason.
            Application.Current.Dispatcher.Invoke(new Action(BringIntoView));
        }
    }

    //private void btnMove_Click(object sender, RoutedEventArgs e)
    //{
    //    popMove.PlacementTarget = sender as UIElement;
    //    popMove.Placement = System.Windows.Controls.Primitives.PlacementMode.Center;
    //    popMove.StaysOpen = false;
    //    popMove.IsOpen = true;
    //}

    //private void btnMoveImage_Click(object sender, RoutedEventArgs e)
    //{
    //    popMove.IsOpen = false;
    //    var viewModel = (ViewModels.ImageResultEntry)DataContext;
    //    if (((Button)sender).Tag is string s)
    //        switch (s)
    //        {
    //            case "top":
    //                viewModel.ImageResultsManager.SelectedImageRoll.MoveImageTop(viewModel.SourceImage);
    //                break;
    //            case "up":
    //                viewModel.ImageResultsManager.SelectedImageRoll.MoveImageUp(viewModel.SourceImage);
    //                break;
    //            case "down":
    //                viewModel.ImageResultsManager.SelectedImageRoll.MoveImageDown(viewModel.SourceImage);
    //                break;
    //            case "bottom":
    //                viewModel.ImageResultsManager.SelectedImageRoll.MoveImageBottom(viewModel.SourceImage);
    //                break;
    //        }

    //    BringIntoView();
    //}

    private void btnShowDetailsToggle(object sender, RoutedEventArgs e) => ((ViewModels.ImageResultEntry)DataContext).ShowDetails = !((ViewModels.ImageResultEntry)DataContext).ShowDetails;
}