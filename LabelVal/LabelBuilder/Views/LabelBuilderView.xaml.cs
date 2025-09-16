using CommunityToolkit.Mvvm.Input;
using ControlzEx.Theming;
using LabelVal.Main.Views;
using LabelVal.Theme;
using MahApps.Metro.Controls;
using System.Windows;
using System.Windows.Controls;

namespace LabelVal.LabelBuilder.Views;

/// <summary>
/// Interaction logic for LabelBuilderView.xaml
/// </summary>
public partial class LabelBuilderView : MetroWindow
{
    public LabelBuilderView() => InitializeComponent();

    private void btnCollapseContent(object sender, RoutedEventArgs e) => ((MainWindow)Application.Current.MainWindow).ClearSelectedMenuItem();

    [RelayCommand]
    private void ExpanderHeaderClick(Border b)
    {
        if (b.TemplatedParent is Expander exp)
        {
            exp.IsExpanded = !exp.IsExpanded;
        }
    }

    private void btnLightTheme_Click(object sender, RoutedEventArgs e) => ThemeManager.Current.ChangeTheme(Application.Current, "Light.Steel");
    private void btnDarkTheme_Click(object sender, RoutedEventArgs e) => ThemeManager.Current.ChangeTheme(Application.Current, "Dark.Steel");
    private void btnSyncOSTheme_Click(object sender, RoutedEventArgs e) => ThemeManager.Current.SyncTheme(ThemeSyncMode.SyncAll);
    private void btnColorBlind_Click(object sender, RoutedEventArgs e)
    {
        var currentType = App.Settings.GetValue("App.ColorBlindnessType", ColorBlindnessType.None);
        var nextType = (ColorBlindnessType)(((int)currentType + 1) % Enum.GetValues(typeof(ColorBlindnessType)).Length);
        App.ChangeColorBlindTheme(nextType);
    }
}
