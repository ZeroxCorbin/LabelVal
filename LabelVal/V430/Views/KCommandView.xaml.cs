using CommunityToolkit.Mvvm.ComponentModel;
using LabelVal.V430.ViewModels;
using OMRON.Reader.SDK.KCommands.Models;
using System.Windows.Controls;
using System.Windows.Data;

namespace LabelVal.V430.Views;
/// <summary>
/// Interaction logic for KCommandView.xaml
/// </summary>
[ObservableObject]
public partial class KCommandView : UserControl
{
    public V430ViewModel ViewModel { get; }

    [ObservableProperty] private string? _searchKeyword;
    partial void OnSearchKeywordChanged(string? value) => ((CollectionViewSource)this.Resources["FieldsCollection"]).View.Refresh();

    public KCommandView()
    {
        InitializeComponent();
        ViewModel = App.GetService<V430ViewModel>()!;
        Loaded += (s, e) => DataContext = this.ViewModel;
    }

    private void DemoItemsFilter(object sender, FilterEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(SearchKeyword))
        {
            e.Accepted = true;
            return;
        }

        e.Accepted = e.Item is Field item && (item.Title.ToLower().Contains(SearchKeyword!.ToLower()) || item.Cmd.ToLower().Contains(SearchKeyword!.ToLower()));
    }
}
