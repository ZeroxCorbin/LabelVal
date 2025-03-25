using CommunityToolkit.Mvvm.ComponentModel;
using HelixToolkit.Wpf.SharpDX;
using LabelVal.ImageViewer3D.Models;
using SharpDX;
using System;
using System.Collections.Generic;

namespace LabelVal.ImageViewer3D.ViewModels;


public partial class ImageViewer3D_IndividualMeshs : BaseViewModel
{
    [ObservableProperty] double width;
    [ObservableProperty] double height;

    [ObservableProperty] private byte[] originalImage;
    [ObservableProperty] private byte[] indexedColorPallet;
    [ObservableProperty] Dictionary<byte, PhongMaterial> colorPallet;
    [ObservableProperty] private byte[] bytes;

    public MeshGeometry3D MeshGeometry { get; set; }
    public PhongMaterial Material { get; set; }

    public List<Shape> Items { get; } = new List<Shape>();

    public ImageViewer3D_IndividualMeshs(byte[] image)
    {
        EffectsManager = new DefaultEffectsManager();

            LoadImage(image);

    }
    public bool LoadImage(byte[] image)
    {
        OriginalImage = ImageUtilities.lib.Core.Bmp.Utilities.GetBmp(image);

        var form = ImageUtilities.lib.Core.Bmp.Utilities.GetPixelFormat(OriginalImage);
        if (form != System.Drawing.Imaging.PixelFormat.Format8bppIndexed)
            return false;

        IndexedColorPallet = ImageUtilities.lib.Core.Bmp.Utilities.GetIndexedColorPallet(OriginalImage);
        if (IndexedColorPallet.Length != 1024)
            return false;

        ColorPallet = GetColorPallet();
        if (ColorPallet.Count != 256)
            return false;

        Bytes = ImageUtilities.lib.Core.Bmp.Utilities.GetImageData(OriginalImage);

        BuildVisuals(OriginalImage);

        return true;
    }

    private Dictionary<byte, PhongMaterial> GetColorPallet()
    {
        var pallet = new Dictionary<byte, PhongMaterial>();
        for (int i = 0; i < 256; i++)
        {
            var r = IndexedColorPallet[i * 4 + 2] / 255.0f;
            var g = IndexedColorPallet[i * 4 + 1] / 255.0f;
            var b = IndexedColorPallet[i * 4] / 255.0f;
            var color4 = new Color4(r, g, b, 1.0f);
            pallet.Add((byte)i, new PhongMaterial { DiffuseColor = color4 });
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
                var material = ColorPallet[z];

                Items.Add(new Cube() { Transform = new System.Windows.Media.Media3D.TranslateTransform3D(x, y, z), Material = material });
            }
        }
    }
}
