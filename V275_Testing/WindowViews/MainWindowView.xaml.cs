using ControlzEx.Theming;
using MahApps.Metro.Controls;
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
using V275_Testing.RunViews;

namespace V275_Testing.WindowViews
{
    /// <summary>
    /// Interaction logic for MainWindowView.xaml
    /// </summary>
    public partial class MainWindowView : MetroWindow
    {
        RunView win;

        public MainWindowView()
        {
            InitializeComponent();
        }

        private void btnLightTheme_Click(object sender, RoutedEventArgs e) => ThemeManager.Current.ChangeTheme(App.Current, "Light.Steel");

        private void btnDarkTheme_Click(object sender, RoutedEventArgs e) => ThemeManager.Current.ChangeTheme(App.Current, "Dark.Steel");

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            win = new RunView();
            win.Show();
            win.Closed += Win_Closed;
        }

        private void Win_Closed(object sender, EventArgs e)
        {
            ((RunView)sender).Closed -= Win_Closed;

            GC.Collect();
        }
    }
}
