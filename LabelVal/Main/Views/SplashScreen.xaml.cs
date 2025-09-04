using LabelVal.Main.ViewModels;
using System.Windows;

namespace LabelVal.Main.Views;

/// <summary>
/// Interaction logic for SplashScreen.xaml
/// </summary>
public partial class SplashScreen : Window
{
    public SplashScreen()
    {
        InitializeComponent();
        if (DataContext is SplashScreenViewModel vm)
        {
            vm.RequestClose = Close;
        }
    }
}