using CommunityToolkit.Mvvm.ComponentModel;

namespace LabelVal.Results.Databases;
public partial class Lock : ObservableObject
{
    [ObservableProperty] private bool isPerminent;
}
