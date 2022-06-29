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

namespace LabelVal.Dialogs
{
    /// <summary>
    /// Interaction logic for ImageViewerDialog.xaml
    /// </summary>
    public partial class ImageViewerDialogView : MahApps.Metro.Controls.Dialogs.CustomDialog
    {
        public ImageViewerDialogView()
        {
            InitializeComponent();
        }

        private void CustomDialog_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void Close()
        {
            await MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance.HideMetroDialogAsync(this.DataContext, this);
            this.DataContext = null;
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            ZoomBorder.Reset();
        }
    }
}
