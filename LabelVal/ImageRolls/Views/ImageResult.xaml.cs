using LabelVal.Dialogs;
using LabelVal.WindowViews;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace LabelVal.ImageRolls.Views;

/// <summary>
/// Interaction logic for LabelControlView.xaml
/// </summary>
public partial class ImageResult : UserControl
{
    public ImageResult() => InitializeComponent();

    private ViewModels.ImageResult viewModel;
    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        viewModel = (ViewModels.ImageResult)DataContext;
        viewModel.BringIntoView += ViewModel_BringIntoView;
    }
    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        DialogParticipation.SetRegister(this, null);

        viewModel.BringIntoView -= ViewModel_BringIntoView;
        viewModel = null;
    }

    private void ViewModel_BringIntoView() => App.Current.Dispatcher.Invoke(new Action(BringIntoView));

    private void ScrollLabelSectors_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.VerticalChange != 0)
            ScrollRepeatSectors.ScrollToVerticalOffset(e.VerticalOffset);
    }
    private void ScrollRepeatSectors_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.VerticalChange != 0)
            ScrollLabelSectors.ScrollToVerticalOffset(e.VerticalOffset);
    }

    private void LabelImage_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            _ = ShowImage(((ViewModels.ImageResult)DataContext).LabelImage, null);
    }
    private void RepeatImage_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            _ = ShowImage(((ViewModels.ImageResult)DataContext).RepeatImage, ((ViewModels.ImageResult)DataContext).RepeatOverlay);
    }

    private bool ShowImage(byte[] image, DrawingImage overlay)
    {
        var dc = new ImageViewerDialogViewModel();

        dc.CreateImage(image, overlay);
        if (dc.RepeatImage == null) return false;

        var yourParentWindow = (MainWindowView)Window.GetWindow(this);

        dc.Width = yourParentWindow.ActualWidth - 100;
        dc.Height = yourParentWindow.ActualHeight - 100;

        _ = DialogCoordinator.Instance.ShowMetroDialogAsync(yourParentWindow.DataContext, new ImageViewerDialogView() { DataContext = dc });

        return true;

    }

    private void LabelSectors_Click(object sender, RoutedEventArgs e)
    {
        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
        {
            if (((ViewModels.ImageResult)DataContext).CurrentRow != null)
            {
                LabelJobJsonView.Load(((ViewModels.ImageResult)DataContext).CurrentRow.LabelTemplate);
                LabelResultJsonView.Load(((ViewModels.ImageResult)DataContext).CurrentRow.LabelReport);
                LabelJsonPopup.PlacementTarget = (Button)sender;
                LabelJsonPopup.IsOpen = true;
            }
        }
        else
        {
            if (((ViewModels.ImageResult)DataContext).LabelSectors.Count > 0)
            {
                LabelSectorsDetailsPopup.PlacementTarget = (Button)sender;
                LabelSectorsDetailsPopup.IsOpen = true;
            }
        }
    }
    private void RepeatSectors_Click(object sender, RoutedEventArgs e)
    {
        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
        {
            if (((ViewModels.ImageResult)DataContext).RepeatReport != null)
            {
                RepeatResultJsonView.Load(Newtonsoft.Json.JsonConvert.SerializeObject(((ViewModels.ImageResult)DataContext).RepeatReport));
                RepeatJsonPopup.PlacementTarget = (Button)sender;
                RepeatJsonPopup.IsOpen = true;
            }
        }
        else
        {
            if (((ViewModels.ImageResult)DataContext).RepeatSectors.Count > 0)
            {
                RepeatSectorsDetailsPopup.PlacementTarget = (Button)sender;
                RepeatSectorsDetailsPopup.IsOpen = true;
            }
        }
    }

    private void LabelImage_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
            e.Handled = true;
    }

    //        JsonViewer.JsonViewer.JsonViewer jsonViewer { get; set; } = null;
    //        private void LabelSectors_Click(object sender, RoutedEventArgs e)
    //        {
    //            if (jsonViewer == null)
    //            {
    //jsonViewer = new JsonViewer.JsonViewer.JsonViewer();
    //                jsonViewer.Clo
    //            }

    //        }
}
