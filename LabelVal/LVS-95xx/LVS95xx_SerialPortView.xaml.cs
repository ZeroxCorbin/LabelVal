using LabelVal.WindowViewModels;
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
using System.Windows.Shapes;

namespace LabelVal.LVS_95xx
{
    /// <summary>
    /// Interaction logic for LVS95xx_SerialPortView.xaml
    /// </summary>
    public partial class LVS95xx_SerialPortView : MahApps.Metro.Controls.Dialogs.CustomDialog
    {
        public LVS95xx_SerialPortView()
        {
            InitializeComponent();
        }

        public LVS95xx_SerialPortView(object sect)
        {
            DataContext = new LVS95xx_SerialPortViewModel(sect);

            InitializeComponent();
        }

        private void CustomDialog_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        private async void Close()
        {
            ((LVS95xx_SerialPortViewModel)this.DataContext).ClosePortCommand.Execute(null);

            await MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance.HideMetroDialogAsync(this.DataContext, this);

            MahApps.Metro.Controls.Dialogs.DialogParticipation.SetRegister(this, null);

            this.DataContext = null;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void CustomDialog_Loaded(object sender, RoutedEventArgs e)
        {
            ((LVS95xx_SerialPortViewModel)this.DataContext).OpenPortCommand.Execute(null);
        }
    }
}
