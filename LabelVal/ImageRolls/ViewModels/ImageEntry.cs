using CommunityToolkit.Mvvm.ComponentModel;
using LabelVal.Utilities;

namespace LabelVal.ImageRolls.ViewModels;
public partial class ImageEntry : ObservableObject
{
    public string Name
    {
        get => string.IsNullOrEmpty(name) ? System.IO.Path.GetFileName(Path) : name;
        set => SetProperty(ref name, value);
    }
    private string name;

    public string Path { get; set; }

    [ObservableProperty] byte[] image;

    [ObservableProperty] int pixelWidth;
    [ObservableProperty] int pixelHeight;

    [ObservableProperty] double pageWidth;
    [ObservableProperty] double pageHeight;

    [ObservableProperty] double targetDpiWidth;
    [ObservableProperty] double targetDpiHeight;

    public double DpiWidth => PixelWidth / (PageWidth / 100);
    public double DpiHeight => PixelHeight / (PageHeight / 100);

    public string UID => ImageUtilities.ImageUID(Image);

}
