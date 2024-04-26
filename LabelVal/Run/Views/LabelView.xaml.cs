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
using LabelVal.result.ViewModels;
using LabelVal.Run.ViewModels;
using LabelVal.WindowViews;

namespace LabelVal.Run.Views
{
    /// <summary>
    /// Interaction logic for RunLabelControlView.xaml
    /// </summary>
    public partial class LabelView : UserControl
    {
        public LabelView()
        {
            InitializeComponent();
        }

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
            {
                if (((LabelViewModel)DataContext).Result.LabelImage != null)
                    ShowImage(((LabelViewModel)DataContext).Result.LabelImage, null);
                else
                    ShowImage(((LabelViewModel)DataContext).Result.RepeatGoldenImage, null);
            }
        }
        private void RepeatImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                ShowImage(((LabelViewModel)DataContext).Result.RepeatImage, ((LabelViewModel)DataContext).RepeatOverlay);
        }

        private bool ShowImage(byte[] image, DrawingImage overlay)
        {
            var dc = new ImageViewerDialogViewModel();

            dc.CreateImage(image, overlay);
            if (dc.RepeatImage == null) return false;

            Run.Views.View yourParentWindow = (Run.Views.View)Window.GetWindow(this);

            dc.Width = yourParentWindow.ActualWidth - 100;
            dc.Height = yourParentWindow.ActualHeight - 100;

            MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance.ShowMetroDialogAsync(yourParentWindow.DataContext, new ImageViewerDialogView() { DataContext = dc });

            return true;
        }

        private void LabelSectors_Click(object sender, RoutedEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                if (((LabelViewModel)DataContext).Result != null)
                {
                    LabelJobJsonView.Load(((LabelViewModel)DataContext).Result.LabelTemplate);
                    LabelResultJsonView.Load(((LabelViewModel)DataContext).Result.LabelReport);
                    LabelJsonPopup.PlacementTarget = (Button)sender;
                    LabelJsonPopup.IsOpen = true;
                }
            }
            else
            {
                if (((LabelViewModel)DataContext).LabelSectors.Count > 0)
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
                if (((LabelViewModel)DataContext).Result.RepeatReport != null)
                {
                    RepeatResultJsonView.Load(((LabelViewModel)DataContext).Result.RepeatReport);
                    RepeatJsonPopup.PlacementTarget = (Button)sender;
                    RepeatJsonPopup.IsOpen = true;
                }
            }
            else
            {
                if (((LabelViewModel)DataContext).RepeatSectors.Count > 0)
                {
                    RepeatSectorsDetailsPopup.PlacementTarget = (Button)sender;
                    RepeatSectorsDetailsPopup.IsOpen = true;
                }

            }
        }
    }
}
