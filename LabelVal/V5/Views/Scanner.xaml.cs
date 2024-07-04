using LabelVal.Dialogs;
using LabelVal.V5.ViewModels;
using LabelVal.WindowViews;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LabelVal.V5.Views
{
    /// <summary>
    /// Interaction logic for Scanner.xaml
    /// </summary>
    public partial class Scanner : UserControl
    {
        //private string? CodeType { get => App.Settings.GetValue<string>(nameof(TestViewModel.TestSettings.CodeType)); set => App.Settings.SetValue(nameof(TestViewModel.TestSettings.CodeType), value); }
        //private string? ExpectedOutDataUTF8 { get => App.Settings.GetValue<string>(nameof(TestViewModel.TestSettings.ExpectedOutDataUTF8)); set => App.Settings.SetValue(nameof(TestViewModel.TestSettings.ExpectedOutDataUTF8), value); }

        public Scanner()
        {
            InitializeComponent();
        }

        private void SourceImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && e.ClickCount == 2)
                ShowImage(((ViewModels.Scanner)DataContext).Image, ((ViewModels.Scanner)DataContext).ImageOverlay, ((ViewModels.Scanner)DataContext).ImageFocusRegionOverlay);
        }

        private bool ShowImage(byte[] image, DrawingImage overlay, DrawingImage overlay1)
        {
            var dc = new ImageViewerDialogViewModel();

            dc.LoadImage(image, [overlay, overlay1]);
            if (dc.Image == null) return false;

            MainWindowView yourParentWindow = (MainWindowView)Window.GetWindow(this);

            dc.Width = yourParentWindow.ActualWidth - 100;
            dc.Height = yourParentWindow.ActualHeight - 100;

            DialogCoordinator.Instance.ShowMetroDialogAsync(yourParentWindow.DataContext, new ImageViewerDialogView() { DataContext = dc });

            return true;

        }

        private bool ShowImage(BitmapImage image, DrawingImage overlay, DrawingImage overlay1)
        {
            var dc = new ImageViewerDialogViewModel();

            dc.LoadImage(image, [overlay, overlay1]);
            if (dc.Image == null) return false;

            MainWindowView yourParentWindow = (MainWindowView)Window.GetWindow(this);

            dc.Width = yourParentWindow.ActualWidth - 100;
            dc.Height = yourParentWindow.ActualHeight - 100;

            DialogCoordinator.Instance.ShowMetroDialogAsync(yourParentWindow.DataContext, new ImageViewerDialogView() { DataContext = dc });

            return true;

        }


        private void btnResetImageView_Click(object sender, RoutedEventArgs e) => ZoomBorder.Reset();

        private void btnSaveImage_Click(object sender, RoutedEventArgs e)
        {
            string path;
            if ((path = Utilities.FileUtilities.SaveFileDialog("", "PNG|*.png", "Save Image")) != "")
            {
                try
                {
                    System.IO.File.WriteAllBytes(path, ((ViewModels.Scanner)DataContext).RawImage);
                }
                catch (Exception ex) { }

            }
        }

        private void btnShowSettings_Click(object sender, RoutedEventArgs e) => drwSettings.IsTopDrawerOpen = !drwSettings.IsTopDrawerOpen;

        private void btnOpenInBrowser_Click(object sender, RoutedEventArgs e)
        {
            var v275 = $"http://{((ViewModels.Scanner)DataContext).Host}:{((ViewModels.Scanner)DataContext).Port}";
            var ps = new ProcessStartInfo(v275)
            {
                UseShellExecute = true,
                Verb = "open"
            };
            _ = Process.Start(ps);
        }

        //private void btnSetResults_Click(object sender, RoutedEventArgs e)
        //{
        //    CodeType = ((Scanner)DataContext).Results[0]["type"].ToString();
        //    ExpectedOutDataUTF8 = ((Scanner)DataContext).Results[0]["dataUTF8"].ToString();
        //}
    }
}
