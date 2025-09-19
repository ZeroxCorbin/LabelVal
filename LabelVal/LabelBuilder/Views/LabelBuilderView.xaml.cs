using System;
using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.Input;
using MahApps.Metro.Controls;
using LabelVal.Theme;
using LabelVal.Main.Views;

namespace LabelVal.LabelBuilder.Views;

public partial class LabelBuilderView : MetroWindow
{
    public LabelBuilderView() => InitializeComponent();

    private void btnCollapseContent(object sender, RoutedEventArgs e) =>
        ((MainWindow)Application.Current.MainWindow).ClearSelectedMenuItem();

    [RelayCommand]
    private void ExpanderHeaderClick(Border b)
    {
        if (b.TemplatedParent is Expander exp)
            exp.IsExpanded = !exp.IsExpanded;
    }

    private void btnLightTheme_Click(object sender, RoutedEventArgs e) => ThemeSupport.ApplyTheme("Light.Steel");
    private void btnDarkTheme_Click(object sender, RoutedEventArgs e) => ThemeSupport.ApplyTheme("Dark.Steel");
    private void btnSyncOSTheme_Click(object sender, RoutedEventArgs e) => ThemeSupport.ApplyTheme(ThemeSupport.SystemSyncSentinel);

    private void btnColorBlind_Click(object sender, RoutedEventArgs e)
    {
        var currentType = App.Settings.GetValue("App.ColorBlindnessType", ColorBlindnessType.None);
        var nextType = (ColorBlindnessType)(((int)currentType + 1) % Enum.GetValues(typeof(ColorBlindnessType)).Length);
        App.ChangeColorBlindTheme(nextType);
    }
}
