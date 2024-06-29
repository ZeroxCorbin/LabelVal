using LabelVal.WindowViewModels;
using LabelVal.WindowViews;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace LabelVal.V275.Views;
/// <summary>
/// Interaction logic for V275NodesView.xaml
/// </summary>
public partial class V275 : UserControl
{
    public V275() {
        //Register KeyUpEvent to all TextBox elements
        EventManager.RegisterClassHandler(typeof(TextBox),
            TextBox.KeyUpEvent,
            new System.Windows.Input.KeyEventHandler(TextBox_KeyUp));

        InitializeComponent();
    }

    //Trigger binding update when enter key is pressed
    private void TextBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key != System.Windows.Input.Key.Enter) return;

        TextBox tBox = (TextBox)sender;
        DependencyProperty prop = TextBox.TextProperty;

        BindingExpression binding = BindingOperations.GetBindingExpression(tBox, prop);
        if (binding != null)
        {
            binding.UpdateSource();
        }

        e.Handled = true;
    }

    public void btnShowDetails_Click(object sender, RoutedEventArgs e)
    {
        var view = ((MainWindowView)App.Current.MainWindow).NodeDetails;
        var vm = ((MainWindowView)App.Current.MainWindow).DataContext as MainWindowViewModel;
        if (view.LeftDrawerContent == null)
        {
            var details = new Views.NodeDetails();
            details.DataContext = vm.NodeDetails;
            view.LeftDrawerContent = details;
        }
        view.IsLeftDrawerOpen = !view.IsLeftDrawerOpen;
     }
     
    private void btnShowSettings_Click(object sender, RoutedEventArgs e) => drwSettings.IsTopDrawerOpen = !drwSettings.IsTopDrawerOpen;

    private void btnOpenInBrowser_Click(object sender, RoutedEventArgs e)
    {
        var v275 = $"http://{((ViewModels.V275)DataContext).V275_Host}:{((ViewModels.V275)DataContext).V275_SystemPort}";
        var ps = new ProcessStartInfo(v275)
        {
            UseShellExecute = true,
            Verb = "open"
        };
        _ = Process.Start(ps);
    }
}
