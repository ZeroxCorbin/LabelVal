using LabelVal.V430.PartNumber.ViewModels;
using System.Windows.Controls;

namespace LabelVal.V430.PartNumber.Views;

/// <summary>
/// Interaction logic for PartNumberView.xaml
/// </summary>
public partial class PartNumberView : UserControl
{
    public PartNumberViewModel ViewModel { get; }
    public PartNumberView()
    {
        InitializeComponent();
        ViewModel = App.GetService<PartNumberViewModel>()!;
        Loaded += (s, e) => DataContext = this;
    }
}
