using ControlzEx.Theming;
using LabelVal.Theme;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System.Windows;

namespace LabelVal.Run.Views;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : MetroWindow
{
    public MainWindow() => InitializeComponent();

    private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e) { }

    private void btnLightTheme_Click(object sender, RoutedEventArgs e) => ThemeSupport.ApplyTheme("Light.Steel");
    private void btnDarkTheme_Click(object sender, RoutedEventArgs e) => ThemeSupport.ApplyTheme("Dark.Steel");
    private void btnSyncOSTheme_Click(object sender, RoutedEventArgs e) => ThemeSupport.ApplyTheme("#SYSTEM#");
    private void btnColorBlind_Click(object sender, RoutedEventArgs e) => App.Settings.GetValue("App.ColorBlindnessType", ColorBlindnessType.None);


    private void MetroWindow_Closed(object sender, System.EventArgs e) => DialogParticipation.SetRegister(this, null);

}
