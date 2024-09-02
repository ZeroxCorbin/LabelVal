using CommunityToolkit.Mvvm.ComponentModel;
using HelixToolkit.Wpf.SharpDX;
using LabelVal.ImageViewer3D.Models;
using LabelVal.Utilities;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Windows.Media.Media3D;

namespace LabelVal.ImageViewer3D.ViewModels;


public partial class ImageViewer3D : BaseViewModel
{
    [ObservableProperty] double width;
    [ObservableProperty] double height;

    [ObservableProperty] private byte[] originalImage;
    [ObservableProperty] private byte[] indexedColorPallet;
    [ObservableProperty] Dictionary<byte, Color4> colorPallet;
    [ObservableProperty] private byte[] bytes;

    public List<Shape> Items { get; } = new List<Shape>();

    public ImageViewer3D()
    {
        EffectsManager = new DefaultEffectsManager();

    }
    public bool LoadImage(byte[] image)
    {
        OriginalImage = LibImageUtilities.ImageUtilities_BMP.GetBmp(image);

        var form = LibImageUtilities.ImageUtilities_BMP.GetBmpPixelFormat(OriginalImage);
        if (form != System.Drawing.Imaging.PixelFormat.Format8bppIndexed)
            return false;

        IndexedColorPallet = LibImageUtilities.ImageUtilities_BMP.ExtractBitmapIndexedColorPallet(OriginalImage);
        if (IndexedColorPallet.Length != 1024)
            return false;

        ColorPallet = GetColorPallet();
        if (ColorPallet.Count != 256)
            return false;

        Bytes = LibImageUtilities.ImageUtilities_BMP.ExtractBitmapData(OriginalImage);

        BuildVisuals(OriginalImage);

        return true;
    }

    private Dictionary<byte, Color4> GetColorPallet()
    {
        var pallet = new Dictionary<byte, Color4>();
        for (int i = 0; i < 256; i++)
        {
            var r = IndexedColorPallet[i * 4 + 2] / 255.0f;
            var g = IndexedColorPallet[i * 4 + 1] / 255.0f;
            var b = IndexedColorPallet[i * 4] / 255.0f;
            var color4 = new Color4(r, g, b, 1.0f);
            pallet.Add((byte)i, color4);
        }
        return pallet;
    }


    private void BuildVisuals(byte[] image)
    {
        Items.Clear();

        // The offset to the start of the pixel data is at byte 10 to 13
        int pixelDataOffset = BitConverter.ToInt32(image, 10);

        // The width of the image is at byte 18 to 21
        int width = BitConverter.ToInt32(image, 18);

        // The height of the image is at byte 22 to 25
        int height = BitConverter.ToInt32(image, 22);

        // Calculate the row size with padding
        int rowSize = (width + 3) & ~3; // Align to 4-byte boundary

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var z = image[pixelDataOffset + y * rowSize + x];
                if (z == 255) continue;
                var color = ColorPallet[z];
                var material = new PhongMaterial { DiffuseColor = color };
                Items.Add(new Cube() { Transform = new TranslateTransform3D(x, y, z), Material = material });
            }
        }
    }
}
