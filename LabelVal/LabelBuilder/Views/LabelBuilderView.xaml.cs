using CommunityToolkit.Mvvm.Input;
using LabelVal.Main.Views;
using System.Windows;
using System.Windows.Controls;

namespace LabelVal.LabelBuilder.Views;

/// <summary>
/// Interaction logic for LabelBuilderView.xaml
/// </summary>
public partial class LabelBuilderView : UserControl
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
}
