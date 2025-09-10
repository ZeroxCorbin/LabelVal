using MahApps.Metro.Controls;
using Newtonsoft.Json.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LabelVal.Results.Views;
public partial class SectorsJsonWindow : MetroWindow 
{
    public SectorsJsonWindow()
    {
        InitializeComponent();

        DataContext = this;
    }

    // DependencyProperty for Template (JObject)
    public static readonly DependencyProperty TemplatesProperty =
        DependencyProperty.Register(
            nameof(Templates),
            typeof(JObject),
            typeof(SectorsJsonWindow),
            new PropertyMetadata(null)
        );

    public JObject Templates
    {
        get => (JObject)GetValue(TemplatesProperty);
        set => SetValue(TemplatesProperty, value);
    }

    // DependencyProperty for Reports (JObject)
    public static readonly DependencyProperty ReportsProperty =
        DependencyProperty.Register(
            nameof(Reports),
            typeof(JObject),
            typeof(SectorsJsonWindow),
            new PropertyMetadata(null)
        );

    public JObject Reports
    {
        get => (JObject)GetValue(ReportsProperty);
        set => SetValue(ReportsProperty, value);
    }

    public void Load(JObject templates, JObject reports)
    {
        // You can set the properties here if needed
        Templates = templates;
        Reports = reports;
    }
}