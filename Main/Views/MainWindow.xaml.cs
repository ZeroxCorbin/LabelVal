using CommunityToolkit.Mvvm.Input;
using ControlzEx.Theming;
using MahApps.Metro.Controls;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace LabelVal.Main.Views;

public partial class MainWindow : MetroWindow
{
    public ICommand OpenRunWindowCommand { get; }

    private Run.Views.MainWindow RunWindow;
    private WindowState _lastWindowState = WindowState.Normal;
    private WindowState _currentWindowState = WindowState.Normal;

    public MainWindow()
    {
        InitializeComponent();
        this.DpiChanged += MainWindow_DpiChanged;

        ((ViewModels.MainWindow)this.DataContext).DPIChangedMessage = new ViewModels.DPIChangedMessage(VisualTreeHelper.GetDpi(this));

        OpenRunWindowCommand = new RelayCommand(() =>
        {
            if (RunWindow != null)
            {
                if (_currentWindowState == WindowState.Minimized)
                    RunWindow.WindowState = _lastWindowState;

                RunWindow.Activate();
                return;
            }
            RunWindow = new Run.Views.MainWindow();
            RunWindow.Closed += (s, e) => RunWindow = null;
            RunWindow.StateChanged += (s, e) =>
            {
                _lastWindowState = _currentWindowState;
                _currentWindowState = RunWindow.WindowState;
            };
            RunWindow.Owner = this;
            RunWindow.Show();
        });
    }

    public void ClearSelectedMenuItem() => ((ViewModels.MainWindow)this.DataContext).SetDeafultMenuItem();

    private void MainWindow_DpiChanged(object sender, DpiChangedEventArgs e) => ((ViewModels.MainWindow)this.DataContext).DPIChangedMessage = new ViewModels.DPIChangedMessage(e.NewDpi);
    private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e) { }

    private void btnLightTheme_Click(object sender, RoutedEventArgs e) => ThemeManager.Current.ChangeTheme(Application.Current, "Light.Steel");
    private void btnDarkTheme_Click(object sender, RoutedEventArgs e) => ThemeManager.Current.ChangeTheme(Application.Current, "Dark.Steel");
    private void btnSyncOSTheme_Click(object sender, RoutedEventArgs e) => ThemeManager.Current.SyncTheme(ThemeSyncMode.SyncAll);
    private void btnColorBlind_Click(object sender, RoutedEventArgs e) => App.ChangeColorBlindTheme(!App.Settings.GetValue("App.IsColorBlind", false));

    private void btnShowSettings_Click(object sender, RoutedEventArgs e) => ApplicationSettings.IsOpen = true;


    private void hamMenu_ItemClick(object sender, ItemClickEventArgs args)
    {
        if (args.ClickedItem is Main.ViewModels.HamburgerMenuItem menuItem && menuItem.IsNotSelectable)
        {
            hamMenu.IsPaneOpen = true;
            args.Handled = true;
        }
    }

    private bool wasOpen = false;
    private bool openOneShot = false;
    private void hamMenu_LayoutUpdated(object sender, System.EventArgs e)
    {
        double maxWidth = 280;
        var res = Utilities.VisualTreeHelp.GetVisualChildren<ListBoxItem>(hamMenu);
        if (res == null)
            return;

        if (hamMenu.IsPaneOpen && !wasOpen)
        {
            openOneShot = true;
            wasOpen = true;
        }
        else if (!hamMenu.IsPaneOpen)
            wasOpen = false;

        if (openOneShot)
        {
            hamMenu.Focus();
            hamMenu.Items.Refresh();
        }


        foreach (var item in res)
        {
            maxWidth = Math.Max(maxWidth, item.DesiredSize.Width);
        }

        if (hamMenu.IsPaneOpen)
            hamMenu.OpenPaneLength = maxWidth + 1;
        else
            hamMenu.OpenPaneLength = 0;

        openOneShot = false;
    }
}
