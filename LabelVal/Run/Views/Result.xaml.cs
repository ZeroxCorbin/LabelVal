using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LabelVal.Dialogs;
using LabelVal.Run.ViewModels;
using LabelVal.WindowViews;

namespace LabelVal.Run.Views
{
    /// <summary>
    /// Interaction logic for RunLabelControlView.xaml
    /// </summary>
    public partial class Result : UserControl
    {
        public Result()
        {
            InitializeComponent();
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

        private void SourceImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (((ViewModels.Result)DataContext).ResultEntry.LabelImage != null)
                    ShowImage(((ViewModels.Result)DataContext).ResultEntry.LabelImage, null);
                else
                    ShowImage(((ViewModels.Result)DataContext).ResultEntry.RepeatGoldenImage, null);
            }
        }
        private void RepeatImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                ShowImage(((ViewModels.Result)DataContext).ResultEntry.RepeatImage, ((ViewModels.Result)DataContext).V275SectorsImageOverlay);
        }

        private bool ShowImage(byte[] image, DrawingImage overlay)
        {
            var dc = new ImageViewerDialogViewModel();

            dc.LoadImage(image, overlay);
            if (dc.Image == null) return false;

            Views.Run yourParentWindow = (Views.Run)Window.GetWindow(this);

            dc.Width = yourParentWindow.ActualWidth - 100;
            dc.Height = yourParentWindow.ActualHeight - 100;

            MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance.ShowMetroDialogAsync(yourParentWindow.DataContext, new ImageViewerDialogView() { DataContext = dc });

            return true;
        }

        private void V275StoredSectors_Click(object sender, RoutedEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                if (((ViewModels.Result)DataContext).ResultEntry != null)
                {
                    LabelJobJsonView.JSON = ((ViewModels.Result)DataContext).ResultEntry.LabelTemplate;
                    LabelResultJsonView.JSON = ((ViewModels.Result)DataContext).ResultEntry.LabelReport;
                    LabelJsonPopup.PlacementTarget = (Button)sender;
                    LabelJsonPopup.IsOpen = true;
                }
            }
            else
            {
                if (((ViewModels.Result)DataContext).V275StoredSectors.Count > 0)
                {
                    V275StoredSectorsDetailsPopup.PlacementTarget = (Button)sender;
                    V275StoredSectorsDetailsPopup.IsOpen = true;
                }

            }
        }
        private void V275CurrentSectors_Click(object sender, RoutedEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                if (((ViewModels.Result)DataContext).ResultEntry.RepeatReport != null)
                {
                    RepeatResultJsonView.JSON = ((ViewModels.Result)DataContext).ResultEntry.RepeatReport;
                    RepeatJsonPopup.PlacementTarget = (Button)sender;
                    RepeatJsonPopup.IsOpen = true;
                }
            }
            else
            {
                if (((ViewModels.Result)DataContext).V275CurrentSectors.Count > 0)
                {
                    V275CurrentSectorsDetailsPopup.PlacementTarget = (Button)sender;
                    V275CurrentSectorsDetailsPopup.IsOpen = true;
                }

            }
        }
    }
}
