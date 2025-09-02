using SharpVectors.Renderers.Wpf;
using System;
using System.Windows.Media;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows;

namespace LabelVal.Utilities;
public static class SharpVectorsSVGUtilities
{
    private static WpfDrawingSettings _wpfSettings = new WpfDrawingSettings() { IncludeRuntime = false, TextAsGeometry = false };

    public static DrawingGroup CreateDrawingGroup(string svgFile, WpfDrawingSettings settings = null)
    {
        var converter = new SharpVectors.Converters.FileSvgReader(settings ?? _wpfSettings);
        return converter.Read(svgFile);
    }
    public static DrawingImage CreateDrawingImage(string svgFile, bool freeze = true)
    {
        var drawingGroup = CreateDrawingGroup(svgFile);
        return CreateDrawingImage(drawingGroup, freeze);
    }
    public static System.Drawing.Bitmap CreateBitmap(string svgFile, int width, int height, double dpiX, double dpiY, WpfDrawingSettings settings = null)
    {
        var drawingGroup = CreateDrawingGroup(svgFile, settings);
        return CreateBitmap(drawingGroup, width, height, dpiX, dpiY);
    }
    public static System.Windows.Media.Imaging.BitmapImage CreateBitmapImage(string svgFile, int width, int height, double dpiX, double dpiY)
    {
        var drawingGroup = CreateDrawingGroup(svgFile);
        return CreateBitmapImage(drawingGroup, width, height, dpiX, dpiY);
    }


    public static DrawingGroup CreateDrawingGroup(Uri svgFile, WpfDrawingSettings settings = null)
    {
        var converter = new SharpVectors.Converters.FileSvgReader(settings ?? _wpfSettings);
        return converter.Read(svgFile);
    }
    public static DrawingImage CreateDrawingImage(Uri svgFile, bool freeze = true)
    {
        var drawingGroup = CreateDrawingGroup(svgFile);
        return CreateDrawingImage(drawingGroup, freeze);
    }
    public static System.Drawing.Bitmap CreateBitmap(Uri svgFile, int width, int height, double dpiX, double dpiY, WpfDrawingSettings settings = null)
    {
        var drawingGroup = CreateDrawingGroup(svgFile, settings);
        return CreateBitmap(drawingGroup, width, height, dpiX, dpiY);
    }
    public static System.Windows.Media.Imaging.BitmapImage CreateBitmapImage(Uri svgFile, int width, int height, double dpiX, double dpiY)
    {
        var drawingGroup = CreateDrawingGroup(svgFile);
        return CreateBitmapImage(drawingGroup, width, height, dpiX, dpiY);
    }

    public static DrawingGroup CreateDrawingGroup(Stream svgStream, WpfDrawingSettings settings = null)
    {
        var converter = new SharpVectors.Converters.FileSvgReader(settings ?? _wpfSettings);
        return converter.Read(svgStream);
    }
    public static DrawingImage CreateDrawingImage(Stream svgStream, bool freeze = true)
    {
        var drawingGroup = CreateDrawingGroup(svgStream);
        return CreateDrawingImage(drawingGroup, freeze);
    }
    public static System.Drawing.Bitmap CreateBitmap(Stream svgStream, int width, int height, double dpiX, double dpiY, WpfDrawingSettings settings = null)
    {
        var drawingGroup = CreateDrawingGroup(svgStream, settings);
        return CreateBitmap(drawingGroup, width, height, dpiX, dpiY);
    }
    public static System.Windows.Media.Imaging.BitmapImage CreateBitmapImage(Stream svgStream, int width, int height, double dpiX, double dpiY)
    {
        var drawingGroup = CreateDrawingGroup(svgStream);
        return CreateBitmapImage(drawingGroup, width, height, dpiX, dpiY);
    }

    public static DrawingGroup CreateDrawingGroup(TextReader textReader, WpfDrawingSettings settings = null)
    {
        var converter = new SharpVectors.Converters.FileSvgReader(settings ?? _wpfSettings);
        return converter.Read(textReader);
    }
    public static DrawingImage CreateDrawingImage(TextReader textReader, bool freeze = true)
    {
        var drawingGroup = CreateDrawingGroup(textReader);
        return CreateDrawingImage(drawingGroup, freeze);
    }
    public static System.Drawing.Bitmap CreateBitmap(TextReader textReader, int width, int height, double dpiX, double dpiY, WpfDrawingSettings settings = null)
    {
        var drawingGroup = CreateDrawingGroup(textReader, settings);
        return CreateBitmap(drawingGroup, width, height, dpiX, dpiY);
    }
    public static System.Windows.Media.Imaging.BitmapImage CreateBitmapImage(TextReader svgStream, int width, int height, double dpiX, double dpiY)
    {
        var drawingGroup = CreateDrawingGroup(svgStream);
        return CreateBitmapImage(drawingGroup, width, height, dpiX, dpiY);
    }

    public static DrawingImage CreateDrawingImage(DrawingGroup drawingGroup, bool freeze = true)
    {
        DrawingImage geometryImage = new(drawingGroup);

        if(freeze)
            geometryImage.Freeze();

        return geometryImage;
    }
    public static System.Drawing.Bitmap CreateBitmap(DrawingGroup drawingGroup, int width, int height, double dpiX, double dpiY)
    {
        var drawingImage = CreateDrawingImage(drawingGroup);
        var drawingVisual = new DrawingVisual();
        using (var drawingContext = drawingVisual.RenderOpen())
        {
            drawingContext.DrawImage(drawingImage, new System.Windows.Rect(0, 0, width, height));
        }

        var bitmap = new System.Drawing.Bitmap(width, height);
        var bitmapSource = new RenderTargetBitmap(width, height, dpiX, dpiY, PixelFormats.Bgra32);
        bitmapSource.Render(drawingVisual);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

        using (var stream = new MemoryStream())
        {
            encoder.Save(stream);
            bitmap = new System.Drawing.Bitmap(stream);
        }

        return bitmap;
    }
    public static System.Windows.Media.Imaging.BitmapImage CreateBitmapImage(DrawingGroup drawingGroup, int width, int height, double dpiX, double dpiY)
    {
        var drawingImage = CreateDrawingImage(drawingGroup);
        var drawingVisual = new DrawingVisual();
        using (var drawingContext = drawingVisual.RenderOpen())
        {
            drawingContext.DrawImage(drawingImage, new System.Windows.Rect(0, 0, width, height));
        }

        var bitmapSource = new RenderTargetBitmap(width, height, dpiX, dpiY, PixelFormats.Bgra32);
        bitmapSource.Render(drawingVisual);

        var bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.StreamSource = new MemoryStream();
        bitmapImage.StreamSource.Position = 0;
        bitmapImage.EndInit();
        bitmapImage.Freeze();

        return bitmapImage;
    }

}
