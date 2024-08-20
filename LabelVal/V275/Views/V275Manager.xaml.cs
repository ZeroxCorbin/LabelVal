using LabelVal.Main.Views;
using System.Windows;
using System.Windows.Controls;

namespace LabelVal.V275.Views
{
    /// <summary>
    /// Interaction logic for V275Manager.xaml
    /// </summary>
    public partial class V275Manager : UserControl
    {
        public V275Manager()
        {
            InitializeComponent();
        }

        private void btnCollapseContent(object sender, RoutedEventArgs e) => ((MainWindow)App.Current.MainWindow).ClearSelectedMenuItem();

        private void btnShowDetails_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
