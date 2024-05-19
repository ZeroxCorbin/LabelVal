using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LabelVal.Dialogs;

public partial class ImageViewerDialogViewModel : ObservableObject
{
    [ObservableProperty] private double width;
    [ObservableProperty] private double height;

    [ObservableProperty] private BitmapImage image;
    public List<DrawingImage> Overlays { get; set; } = new();

    public void LoadImage(byte[] image, List<DrawingImage> overlays)
    {
        if (image == null || image.Length < 2)
            return;

        Image = new BitmapImage();
        using (MemoryStream memStream = new(image))
        {
            Image.BeginInit();
            Image.CacheOption = BitmapCacheOption.OnLoad;
            Image.StreamSource = memStream;
            Image.EndInit();
            Image.Freeze();
        }

        foreach (var overlay in overlays)
            Overlays.Add(overlay);
    }

    public void LoadImage(byte[] image, DrawingImage overlay)
    {
        if (image == null || image.Length < 2)
            return;

        Image = new BitmapImage();
        using (MemoryStream memStream = new(image))
        {
            Image.BeginInit();
            Image.CacheOption = BitmapCacheOption.OnLoad;
            Image.StreamSource = memStream;
            Image.EndInit();
            Image.Freeze();
        }

        Overlays.Add(overlay);
    }

    public void LoadImage(BitmapImage image, List<DrawingImage> overlays)
    {
        Image = image;
        foreach (var overlay in overlays)
            Overlays.Add(overlay);
    }
    public void LoadImage(BitmapImage image, DrawingImage overlay)
    {
        Image = image;
        Overlays.Add(overlay);
    }

}
