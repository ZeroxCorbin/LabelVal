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
using LabelVal.RunViewModels;
using LabelVal.WindowViews;

namespace LabelVal.RunViews
{
    /// <summary>
    /// Interaction logic for RunLabelControlView.xaml
    /// </summary>
    public partial class RunLabelControlView : UserControl
    {
        public RunLabelControlView()
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
                if (((RunLabelControlViewModel)DataContext).Run.LabelImage != null)
                    ShowImage(((RunLabelControlViewModel)DataContext).Run.LabelImage, null);
                else
                    ShowImage(((RunLabelControlViewModel)DataContext).Run.RepeatGoldenImage, null);
            }
        }
        private void RepeatImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                ShowImage(((RunLabelControlViewModel)DataContext).Run.RepeatImage, ((RunLabelControlViewModel)DataContext).RepeatOverlay);
        }

        private bool ShowImage(byte[] image, DrawingImage overlay)
        {
            var dc = new ImageViewerDialogViewModel();

            dc.CreateImage(image, overlay);
            if (dc.RepeatImage == null) return false;

            RunView yourParentWindow = (RunView)Window.GetWindow(this);

            dc.Width = yourParentWindow.ActualWidth - 100;
            dc.Height = yourParentWindow.ActualHeight - 100;

            MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance.ShowMetroDialogAsync(yourParentWindow.DataContext, new ImageViewerDialogView() { DataContext = dc });

            return true;
        }

        private void LabelSectors_Click(object sender, RoutedEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                if (((RunLabelControlViewModel)DataContext).Run != null)
                {
                    LabelJobJsonView.Load(((RunLabelControlViewModel)DataContext).Run.LabelTemplate);
                    LabelResultJsonView.Load(((RunLabelControlViewModel)DataContext).Run.LabelReport);
                    LabelJsonPopup.PlacementTarget = (Button)sender;
                    LabelJsonPopup.IsOpen = true;
                }
            }
            else
            {
                if (((RunLabelControlViewModel)DataContext).LabelSectors.Count > 0)
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
                if (((RunLabelControlViewModel)DataContext).Run.RepeatReport != null)
                {
                    RepeatResultJsonView.Load(((RunLabelControlViewModel)DataContext).Run.RepeatReport);
                    RepeatJsonPopup.PlacementTarget = (Button)sender;
                    RepeatJsonPopup.IsOpen = true;
                }
            }
            else
            {
                if (((RunLabelControlViewModel)DataContext).RepeatSectors.Count > 0)
                {
                    RepeatSectorsDetailsPopup.PlacementTarget = (Button)sender;
                    RepeatSectorsDetailsPopup.IsOpen = true;
                }

            }
        }
    }
}
