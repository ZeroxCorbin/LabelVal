using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

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
            var v275 = $"http://{((ViewModels.Node)DataContext).Controller.Host}:{((ViewModels.Node)DataContext).Controller.SystemPort}";
            var ps = new ProcessStartInfo(v275)
            {
                UseShellExecute = true,
                Verb = "open"
            };
            _ = Process.Start(ps);
        }

        private void btnUnselect(object sender, RoutedEventArgs e) =>
            ((ViewModels.Node)this.DataContext).Manager.Manager.SelectedDevice = null;
    }
}
