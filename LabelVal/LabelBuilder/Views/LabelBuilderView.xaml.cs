using LabelVal.Main.Views;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LabelVal.LabelBuilder.Views;

/// <summary>
/// Interaction logic for LabelBuilderView.xaml
/// </summary>
public partial class LabelBuilderView : UserControl
{
    public LabelBuilderView() => InitializeComponent();

    private void btnCollapseContent(object sender, RoutedEventArgs e) => ((MainWindow)Application.Current.MainWindow).ClearSelectedMenuItem();

    private void OnExpanderHeaderClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border b && b.TemplatedParent is Expander exp)
        {
            exp.IsExpanded = !exp.IsExpanded;
            e.Handled = true;
        }
    }
}
