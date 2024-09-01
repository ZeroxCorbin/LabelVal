using CommunityToolkit.Mvvm.ComponentModel;
using HelixToolkit.Wpf;
using LabelVal.Utilities;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace LabelVal.ImageViewer3D.ViewModels;
public partial class ImageViewer3D : ObservableObject
{
    [ObservableProperty] private byte[] originalImage;
    [ObservableProperty] private byte[] indexedColorPallet;
    [ObservableProperty] Dictionary<byte, Brush> colorPallet;
    [ObservableProperty] private byte[] bytes;
    public ObservableCollection<Visual3D> Visuals { get; } = [];
    [ObservableProperty] private PerspectiveCamera camera = new() { Position = new Point3D(850.0, 850.0, -366.0), LookDirection = new Vector3D(-850.0, -850.0, 366.0), UpDirection = new Vector3D(-0.2, -0.2, -0.94) };

    public bool LoadImage(byte[] image)
    {
        OriginalImage = ImageUtilities.GetBmp(image);

        var form = ImageUtilities.GetBmpPixelFormat(OriginalImage);
        if (form != System.Drawing.Imaging.PixelFormat.Format8bppIndexed)
            return false;

        IndexedColorPallet = ImageUtilities.ExtractBitmapIndexedColorPallet(OriginalImage);
        if (IndexedColorPallet.Length != 1024)
            return false;

        ColorPallet = GetColorPallet();
        if (ColorPallet.Count != 256)
            return false;

        Bytes = ImageUtilities.ExtractBitmapData(ImageUtilities.GetBmp(image));

        return true;
    }

    private Dictionary<byte, Brush> GetColorPallet()
    {
        var pallet = new Dictionary<byte, Brush>();
        for (int i = 0; i < 256; i++)
        {
            var color = Color.FromRgb(IndexedColorPallet[i * 4 + 2], IndexedColorPallet[i * 4 + 1], IndexedColorPallet[i * 4]);
            pallet.Add((byte)i, new SolidColorBrush(color));
        }
        return pallet;
    }

    private void BuildVisuals()
    {
        Visuals.Clear();

        for (int i = 0; i < Bytes.Length; i++)
        {
            var x = i % 512;
            var y = i / 512;
            var z = Bytes[i];
            var color = ColorPallet[z];
            var visual = new CubeVisual3D()
            {
                Center = new Point3D(x, y, z / 2.0),
                SideLength = 1,
                Material = new DiffuseMaterial(color)
            };
            Visuals.Add(visual);
        }
    }
}
