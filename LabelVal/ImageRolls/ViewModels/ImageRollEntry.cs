using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace LabelVal.ImageRolls.ViewModels;

public partial class ImageRollEntry : ObservableObject
{
    [ObservableProperty] private string name;
    [ObservableProperty] private string path;
    [ObservableProperty] private string dPI;

    [ObservableProperty] private bool isGS1;
    partial void OnIsGS1Changed(bool value) => OnPropertyChanged(nameof(ImagesShareTemplate));

    [ObservableProperty] private string tableID;

    public bool ImagesShareTemplate
    {
        get => !IsGS1 && imagesShareTemplate;
        set => SetProperty(ref imagesShareTemplate, value);
    }
    private bool imagesShareTemplate = true;

    public ObservableCollection<ImageEntry> Images { get; set; } = [];

    public ImageRollEntry() { }

    public ImageRollEntry(string name, string path)
    {
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(path))
            return;

        Name = name;
        Path = path;

        DPI = Path[(Path.LastIndexOf('\\') + 1)..];
        IsGS1 = Name.StartsWith("gs1", System.StringComparison.CurrentCultureIgnoreCase);
        ImagesShareTemplate = !IsGS1;

        if (IsGS1)
        {
            var val = Regex.Match(Name, @"TABLE (\d*\.?\d+)");

            if (val.Groups.Count == 2)
                TableID = val.Groups[1].Value;
        }
    }
}
