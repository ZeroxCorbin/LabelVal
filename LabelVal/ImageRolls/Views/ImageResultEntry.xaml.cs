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

    private void ViewModel_BringIntoView() => App.Current.Dispatcher.Invoke(new Action(BringIntoView));

    private void SourceImage_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            _ = ShowImage(((ViewModels.ImageResultEntry)DataContext).SourceImage, null);
    }
    private void SourceImage_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
            e.Handled = true;
    }

    private void V275StoredSectors_Click(object sender, RoutedEventArgs e)
    {
        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
        {
            if (((ViewModels.ImageResultEntry)DataContext).V275ResultRow != null)
            {
                V275StoredTemplateJsonView.Load(((ViewModels.ImageResultEntry)DataContext).V275ResultRow.Template);
                V275StoredReportJsonView.Load(((ViewModels.ImageResultEntry)DataContext).V275ResultRow.Report);
                V275StoredJsonPopup.PlacementTarget = (Button)sender;
                V275StoredJsonPopup.IsOpen = true;
            }
        }
        else
        {
            if (((ViewModels.ImageResultEntry)DataContext).V275StoredSectors.Count > 0)
            {
                V275StoredSectorsDetailsPopup.PlacementTarget = (Button)sender;
                V275StoredSectorsDetailsPopup.IsOpen = true;
            }
        }
    }
    private void V275CurrentSectorDetails_Click(object sender, RoutedEventArgs e)
    {
        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
        {
            if (((ViewModels.ImageResultEntry)DataContext).V275CurrentReport != null)
            {
                V275CurrentReportJsonView.Load(Newtonsoft.Json.JsonConvert.SerializeObject(((ViewModels.ImageResultEntry)DataContext).V275CurrentReport));
                V275CurrentReportJsonPopup.PlacementTarget = (Button)sender;
                V275CurrentReportJsonPopup.IsOpen = true;
            }
        }
        else
        {
            if (((ViewModels.ImageResultEntry)DataContext).V275CurrentSectors.Count > 0)
            {
                V275CurrentSectorsDetailsPopup.PlacementTarget = (Button)sender;
                V275CurrentSectorsDetailsPopup.IsOpen = true;
            }
        }
    }
    private void V275Image_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            _ = ShowImage(((ViewModels.ImageResultEntry)DataContext).V275Image, ((ViewModels.ImageResultEntry)DataContext).V275StoredSectorsImageOverlay);
    }
    private void ScrollV275StoredSectors_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.VerticalChange != 0)
            ScrollV275CurrentSectors.ScrollToVerticalOffset(e.VerticalOffset);
    }
    private void ScrollV275CurrentSectors_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.VerticalChange != 0)
            ScrollV275StoredSectors.ScrollToVerticalOffset(e.VerticalOffset);
    }

    private void V5StoredSectors_Click(object sender, RoutedEventArgs e)
    {
        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
        {
            if (((ViewModels.ImageResultEntry)DataContext).V5ResultRow != null)
            {
                V5StoredTemplateJsonView.Load(((ViewModels.ImageResultEntry)DataContext).V5ResultRow.Template);
                V5StoredReportJsonView.Load(((ViewModels.ImageResultEntry)DataContext).V5ResultRow.Report);
                V5StoredJsonPopup.PlacementTarget = (Button)sender;
                V5StoredJsonPopup.IsOpen = true;
            }
        }
        else
        {
            if (((ViewModels.ImageResultEntry)DataContext).V5StoredSectors.Count > 0)
            {
                V5StoredSectorsDetailsPopup.PlacementTarget = (Button)sender;
                V5StoredSectorsDetailsPopup.IsOpen = true;
            }
        }
    }  
    private void V5CurrentSectorDetails_Click(object sender, RoutedEventArgs e)
    {
        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
        {
            if (((ViewModels.ImageResultEntry)DataContext).V5CurrentReport != null)
            {
                V5CurrentReportJsonView.Load(Newtonsoft.Json.JsonConvert.SerializeObject(((ViewModels.ImageResultEntry)DataContext).V5CurrentReport));
                V5CurrentReportJsonPopup.PlacementTarget = (Button)sender;
                V5CurrentReportJsonPopup.IsOpen = true;
            }
        }
        else
        {
            if (((ViewModels.ImageResultEntry)DataContext).V5CurrentSectors.Count > 0)
            {
                V5CurrentSectorsDetailsPopup.PlacementTarget = (Button)sender;
                V5CurrentSectorsDetailsPopup.IsOpen = true;
            }
        }
    }
    private void V5Image_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            _ = ShowImage(((ViewModels.ImageResultEntry)DataContext).V5Image, ((ViewModels.ImageResultEntry)DataContext).V5StoredSectorsImageOverlay);
    }
    private void ScrollV5StoredSectors_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.VerticalChange != 0)
            ScrollV5CurrentSectors.ScrollToVerticalOffset(e.VerticalOffset);
    }
    private void ScrollV5CurrentSectors_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.VerticalChange != 0)
            ScrollV5StoredSectors.ScrollToVerticalOffset(e.VerticalOffset);
    }

    private bool ShowImage(byte[] image, DrawingImage overlay)
    {
        var dc = new ImageViewerDialogViewModel();

        dc.LoadImage(image, overlay);
        if (dc.Image == null) return false;

        var yourParentWindow = (MainWindowView)Window.GetWindow(this);

        dc.Width = yourParentWindow.ActualWidth - 100;
        dc.Height = yourParentWindow.ActualHeight - 100;

        _ = DialogCoordinator.Instance.ShowMetroDialogAsync(yourParentWindow.DataContext, new ImageViewerDialogView() { DataContext = dc });

        return true;

    }

}
