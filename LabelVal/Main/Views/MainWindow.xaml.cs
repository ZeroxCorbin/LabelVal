using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ControlzEx.Theming;
using LabelVal.Main.Messages;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
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

        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        WeakReferenceMessenger.Default.Send(new CloseSplashScreenMessage());

        App.Current.Dispatcher.BeginInvoke(() =>
        {
            this.BringIntoView();
        });
    }
    public void ClearSelectedMenuItem() => ((ViewModels.MainWindow)this.DataContext).SetDeafultMenuItem();

    private void MainWindow_DpiChanged(object sender, DpiChangedEventArgs e) => ((ViewModels.MainWindow)this.DataContext).DPIChangedMessage = new ViewModels.DPIChangedMessage(e.NewDpi);
    private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e) { }

    private void btnLightTheme_Click(object sender, RoutedEventArgs e) => ThemeManager.Current.ChangeTheme(Application.Current, "Light.Steel");
    private void btnDarkTheme_Click(object sender, RoutedEventArgs e) => ThemeManager.Current.ChangeTheme(Application.Current, "Dark.Steel");
    private void btnSyncOSTheme_Click(object sender, RoutedEventArgs e) => ThemeManager.Current.SyncTheme(ThemeSyncMode.SyncAll);
    private void btnColorBlind_Click(object sender, RoutedEventArgs e)
    {
        var currentType = App.Settings.GetValue("App.ColorBlindnessType", ColorBlindnessType.None);
        var nextType = (ColorBlindnessType)(((int)currentType + 1) % Enum.GetValues(typeof(ColorBlindnessType)).Length);
        App.ChangeColorBlindTheme(nextType);
    }

    private void btnShowSettings_Click(object sender, RoutedEventArgs e) => ApplicationSettings.IsOpen = true;

    private void MetroWindow_Closed(object sender, System.EventArgs e) => DialogParticipation.SetRegister(this, null);

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
