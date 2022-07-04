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

        private void RepeatImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                ShowImage(((RunLabelControlViewModel)DataContext).Run.RepeatImage);
        }

        private bool ShowImage(byte[] image)
        {
            var dc = new ImageViewerDialogViewModel();

            dc.CreateImage(image);
            if (dc.RepeatImage == null) return false;

            RunView yourParentWindow = (RunView)Window.GetWindow(this);

            dc.Width = yourParentWindow.ActualWidth - 100;
            dc.Height = yourParentWindow.ActualHeight - 100;

            MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance.ShowMetroDialogAsync(yourParentWindow.DataContext, new ImageViewerDialogView() { DataContext = dc });

            return true;
        }

        private void LabelImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                ShowImage(((RunLabelControlViewModel)DataContext).Run.LabelImage);
        }

    }
}
