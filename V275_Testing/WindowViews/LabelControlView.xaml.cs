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
using V275_Testing.WindowViewModels;

namespace V275_Testing.WindowViews
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

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            LabelControlViewModel viewModel = (LabelControlViewModel)DataContext;
            viewModel.BringIntoView += ViewModel_BringIntoView;

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
    }
}
