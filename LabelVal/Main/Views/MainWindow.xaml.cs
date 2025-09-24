using BarcodeBuilder.lib.Wpf.ViewModels;
using BarcodeBuilder.lib.Wpf.Views;
using CommunityToolkit.Mvvm.Input;
using ControlzEx.Theming;
using LabelVal.Theme;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace LabelVal.Main.Views;

public partial class MainWindow : MetroWindow
{
    public ICommand OpenRunWindowCommand { get; }

    private Run.Views.MainWindow RunWindow;

    // ADD: Label Builder window tracking
    private LabelBuilderView _labelBuilderWindow;

    private WindowState _lastWindowState = WindowState.Normal;
    private WindowState _currentWindowState = WindowState.Normal;

    // Track last real (selectable) menu item so command items can revert
    private ViewModels.HamburgerMenuItem _lastSelectableMenuItem;

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

        // Initialize last selectable item
        _lastSelectableMenuItem = ((ViewModels.MainWindow)DataContext).SelectedMenuItem;
    }

    public void ClearSelectedMenuItem() => ((ViewModels.MainWindow)this.DataContext).SetDeafultMenuItem();

    private void MainWindow_DpiChanged(object sender, DpiChangedEventArgs e) => ((ViewModels.MainWindow)this.DataContext).DPIChangedMessage = new ViewModels.DPIChangedMessage(e.NewDpi);
    private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e) { }

    private void btnLightTheme_Click(object sender, RoutedEventArgs e) => ThemeSupport.ApplyTheme("Light.Steel");
    private void btnDarkTheme_Click(object sender, RoutedEventArgs e) => ThemeSupport.ApplyTheme("Dark.Steel");
    private void btnSyncOSTheme_Click(object sender, RoutedEventArgs e) => ThemeSupport.ApplyTheme("#SYSTEM#");
    private void btnColorBlind_Click(object sender, RoutedEventArgs e)
    {
        var currentType = App.Settings.GetValue("App.ColorBlindnessType", ColorBlindnessType.None);
        var nextType = (ColorBlindnessType)(((int)currentType + 1) % Enum.GetValues(typeof(ColorBlindnessType)).Length);
        ThemeSupport.ApplyColorBlindTheme(nextType);
    }

    private void btnShowSettings_Click(object sender, RoutedEventArgs e) => ApplicationSettings.IsOpen = true;

    private void MetroWindow_Closed(object sender, System.EventArgs e) => DialogParticipation.SetRegister(this, null);

    // NEW: Open (or activate) Label Builder window
    private void OpenLabelBuilderWindow()
    {
        var vm = (ViewModels.MainWindow)DataContext;

        if (_labelBuilderWindow != null)
        {
            if (_labelBuilderWindow.WindowState == WindowState.Minimized)
                _labelBuilderWindow.WindowState = WindowState.Normal;
            _labelBuilderWindow.Activate();
            return;
        }

        _labelBuilderWindow = new LabelBuilderView
        {
            Owner = this
        };

        // Reuse existing ViewModel instance so state is shared
        _labelBuilderWindow.DataContext = vm.LabelBuilderViewModel;

        _labelBuilderWindow.Closed += (s, e) => _labelBuilderWindow = null;
        _labelBuilderWindow.Show();
    }

    private void hamMenu_ItemClick(object sender, ItemClickEventArgs args)
    {
        if (args.ClickedItem is ViewModels.HamburgerMenuItem menuItem)
        {
            var vm = (ViewModels.MainWindow)DataContext;

            // Command-style (window opening) item
            if (menuItem.OpensWindow && menuItem.Content is LabelBuilderViewModel)
            {
                OpenLabelBuilderWindow();

                // Revert selection to last selectable item
                vm.SelectedMenuItem = _lastSelectableMenuItem ?? vm.MenuItems[0];
                args.Handled = true;
                return;
            }

            // Non-selectable (e.g., Printer) keeps pane open but stays selected as current logic
            if (menuItem.IsNotSelectable)
            {
                hamMenu.IsPaneOpen = true;
                // For Printer we allow it to remain showing its inline control, so do not revert
                // Only LabelBuilder (handled above) reverts.
                args.Handled = true;
                return;
            }

            // Update last selectable
            _lastSelectableMenuItem = menuItem;
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