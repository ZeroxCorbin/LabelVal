using Newtonsoft.Json.Linq;
using System.Windows;
using System.Windows.Controls;

namespace LabelVal.Results.Views;
/// <summary>
/// Interaction logic for PopupJSONViewer.xaml
/// </summary>
public partial class PopupJSONViewer : UserControl
{
    //create a dependancy property to bind the json JObject
    public static readonly DependencyProperty JSONProperty = DependencyProperty.Register("JSON", typeof(JObject), typeof(PopupJSONViewer), new PropertyMetadata(null));
    public JObject JSON { get => (JObject)GetValue(JSONProperty); set { SetValue(JSONProperty, value); Viewer1.JSON = value; } }

    public static readonly DependencyProperty JSON1Property = DependencyProperty.Register("JSON1", typeof(JObject), typeof(PopupJSONViewer), new PropertyMetadata(null));
    public JObject JSON1 { get => (JObject)GetValue(JSON1Property); set { SetValue(JSON1Property, value); Viewer2.JSON = value; } }


    public PopupJSONViewer()
    {
        InitializeComponent();
    }

}
