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

        private void RepeatImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ShowImage(((LabelControlViewModel)DataContext).RepeatImage);
        }

        private bool ShowImage(byte[] image)
        {
            var dc = new ImageViewerDialogViewModel();

            dc.CreateImage(image);
            if (dc.RepeatImage == null) return false;

            MainWindowView yourParentWindow = (MainWindowView)Window.GetWindow(this);

            dc.Width = yourParentWindow.ActualWidth - 100;
            dc.Height = yourParentWindow.ActualHeight - 100;

            MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance.ShowMetroDialogAsync(yourParentWindow.DataContext, new ImageViewerDialogView() { DataContext = dc });

            return true;
        }

        private void LabelImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ShowImage(((LabelControlViewModel)DataContext).LabelImage);
        }
    }
}
