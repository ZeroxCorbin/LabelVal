using System;
using System.Collections.Generic;
using System.IO;
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
using LabelVal.WindowViewModels;
using MahApps.Metro.Controls.Dialogs;

namespace LabelVal.WindowViews
{
    /// <summary>
    /// Interaction logic for LabelControlView.xaml
    /// </summary>
    public partial class LabelControlView : UserControl
    {
        public LabelControlView()
        {
            InitializeComponent();

        }

        LabelControlViewModel viewModel;
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            viewModel = (LabelControlViewModel)DataContext;
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
            App.Current.Dispatcher.Invoke(new Action(() => this.BringIntoView()));
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
                ShowImage(((LabelControlViewModel)DataContext).LabelImage, null);
        }
        private void RepeatImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                ShowImage(((LabelControlViewModel)DataContext).RepeatImage, ((LabelControlViewModel)DataContext).RepeatOverlay);
        }

        private bool ShowImage(byte[] image, DrawingImage overlay)
        {
            var dc = new ImageViewerDialogViewModel();

            dc.CreateImage(image, overlay);
            if (dc.RepeatImage == null) return false;

            MainWindowView yourParentWindow = (MainWindowView)Window.GetWindow(this);

            dc.Width = yourParentWindow.ActualWidth - 100;
            dc.Height = yourParentWindow.ActualHeight - 100;

            DialogCoordinator.Instance.ShowMetroDialogAsync(yourParentWindow.DataContext, new ImageViewerDialogView() { DataContext = dc });

            return true;

        }

        private void LabelSectors_Click(object sender, RoutedEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                if (((LabelControlViewModel)DataContext).CurrentRow != null)
                {
                    LabelJobJsonView.Load(((LabelControlViewModel)DataContext).CurrentRow.LabelTemplate);
                    LabelResultJsonView.Load(((LabelControlViewModel)DataContext).CurrentRow.LabelReport);
                    LabelJsonPopup.PlacementTarget = (Button)sender;
                    LabelJsonPopup.IsOpen = true;
                }
            }
            else
            {
                if (((LabelControlViewModel)DataContext).LabelSectors.Count > 0)
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
                if (((LabelControlViewModel)DataContext).RepeatReport != null)
                {
                    RepeatResultJsonView.Load(Newtonsoft.Json.JsonConvert.SerializeObject(((LabelControlViewModel)DataContext).RepeatReport));
                    RepeatJsonPopup.PlacementTarget = (Button)sender;
                    RepeatJsonPopup.IsOpen = true;
                }
            }
            else
            {
                if (((LabelControlViewModel)DataContext).RepeatSectors.Count > 0)
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
}
