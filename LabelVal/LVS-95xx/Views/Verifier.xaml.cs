using System.Windows;
using System.Windows.Controls;

namespace LabelVal.LVS_95xx.Views;

public partial class Verifier : UserControl
{
    public static readonly DependencyProperty IsDockedProperty =
        DependencyProperty.Register(
        nameof(IsDocked),
        typeof(bool),
        typeof(Verifier),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public bool IsDocked
    {
        get => (bool)GetValue(IsDockedProperty);
        set => SetValue(IsDockedProperty, value);
    }

    public Verifier() => InitializeComponent();

    private void btnShowSettings_Click(object sender, RoutedEventArgs e) => drwSettings.IsTopDrawerOpen = !drwSettings.IsTopDrawerOpen;

    private void drwSettings_DrawerClosing(object sender, MaterialDesignThemes.Wpf.DrawerClosingEventArgs e) =>
        ((ViewModels.Verifier)this.DataContext).Manager.SaveCommand.Execute(null);

    private void btnUnselect(object sender, RoutedEventArgs e)
    {

    }
}
