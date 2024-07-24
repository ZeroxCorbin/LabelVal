using ControlzEx.Theming;
using MahApps.Metro.Controls;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LabelVal.Main.Views;

public partial class MainWindow : MetroWindow
{
    public MainWindow()
    {
        InitializeComponent();
        this.DpiChanged += MainWindow_DpiChanged;

        ((ViewModels.MainWindow)this.DataContext).DPIChangedMessage = new ViewModels.DPIChangedMessage(VisualTreeHelper.GetDpi(this));
    }

    public void ClearSelectedMenuItem() => ((ViewModels.MainWindow)this.DataContext).SelectedMenuItem = null;

    private void MainWindow_DpiChanged(object sender, DpiChangedEventArgs e) => ((ViewModels.MainWindow)this.DataContext).DPIChangedMessage = new ViewModels.DPIChangedMessage(e.NewDpi);

    private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e) { }

    private void btnLightTheme_Click(object sender, RoutedEventArgs e) => ThemeManager.Current.ChangeTheme(App.Current, "Light.Steel");
    private void btnDarkTheme_Click(object sender, RoutedEventArgs e) => ThemeManager.Current.ChangeTheme(App.Current, "Dark.Steel");
    private void btnSyncOSTheme_Click(object sender, RoutedEventArgs e) => ThemeManager.Current.SyncTheme(ThemeSyncMode.SyncAll);
    private void btnColorBlind_Click(object sender, RoutedEventArgs e) => App.ChangeColorBlindTheme(!App.Settings.GetValue("App.IsColorBlind", false));

    private void btnShowSettings_Click(object sender, RoutedEventArgs e) => ApplicationSettings.IsOpen = true;

    private void btnShowInfo_Click(object sender, RoutedEventArgs e) => popupInfo.IsOpen = true;
    private void btnShowError_Click(object sender, RoutedEventArgs e) => popupError.IsOpen = true;

    private void hamMenu_ItemClick(object sender, ItemClickEventArgs args)
    {
        if (args.ClickedItem is Main.ViewModels.HamburgerMenuItem menuItem && menuItem.IsNotSelectable)
        {
            hamMenu.IsPaneOpen = true;
            args.Handled = true;
        }
    }

    private bool update = false;
    private void hamMenu_LayoutUpdated(object sender, System.EventArgs e)
    {
        Update();
    }
    private void Update()
    {
        double maxWidth = 280;
        var res = Utilities.VisualTreeHelp.GetVisualChildren<ListBoxItem>(hamMenu);
        if (res == null)
            return;
        
        foreach (var item in res)
            maxWidth = Math.Max(maxWidth, item.DesiredSize.Width);

        hamMenu.OpenPaneLength = maxWidth + 1;
    }

}
