using CommunityToolkit.Mvvm.ComponentModel;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LabelVal.Dialogs;

public partial class ImageViewerDialogViewModel : ObservableObject
{
    [ObservableProperty] private double width;
    [ObservableProperty] private double height;
    [ObservableProperty] private BitmapImage image;
    [ObservableProperty] private DrawingImage overlay;
    [ObservableProperty] private DrawingImage overlay1;

    public void LoadImage(byte[] image, DrawingImage overlay, DrawingImage overlay1)
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

        Overlay = overlay;
        Overlay1 = overlay1;
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

        Overlay = overlay;
    }

    public void LoadImage(BitmapImage image, DrawingImage overlay, DrawingImage overlay1)
    {
        Image = image;
        Overlay = overlay;
        Overlay1 = overlay1;
    }
    public void LoadImage(BitmapImage image, DrawingImage overlay)
    {
        Image = image;
        Overlay = overlay;
    }
}
