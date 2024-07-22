using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace LabelVal.V275.Views
{
    /// <summary>
    /// Interaction logic for V275NodeItemView.xaml
    /// </summary>
    public partial class NodeItem : UserControl
    {
        public static readonly DependencyProperty IsDockedProperty =
            DependencyProperty.Register(
            nameof(IsDocked),
            typeof(bool),
            typeof(NodeItem),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public bool IsDocked
        {
            get => (bool)GetValue(IsDockedProperty);
            set => SetValue(IsDockedProperty, value);
        }

        public NodeItem()
        {
            InitializeComponent();
        }

        private void btnOpenInBrowser_Click(object sender, RoutedEventArgs e)
        {
            var v275 = $"http://{((ViewModels.Node)DataContext).Connection.Commands.Host}:{((ViewModels.Node)DataContext).Connection.Commands.SystemPort}";
            var ps = new ProcessStartInfo(v275)
            {
                UseShellExecute = true,
                Verb = "open"
            };
            _ = Process.Start(ps);
        }
    }
}
