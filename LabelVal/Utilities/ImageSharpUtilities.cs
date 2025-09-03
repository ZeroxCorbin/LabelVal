using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace LabelVal.Utilities
{
    class ImageSharpUtilities
    {
        private const double InchesPerMeter = 39.3701;
        private const int PngSignatureLength = 8;

        public static byte[] GetPng(byte[] img)
        {
            using var image = Image.Load(img);
            if (image.Metadata.DecodedImageFormat is PngFormat)
            {
                return img;
            }

            using var ms = new MemoryStream();
            image.Save(ms, new PngEncoder());
            return ms.ToArray();
        }

        public static byte[] GetPng(byte[] img, int dpiX, int dpiY = 0)
        {
            using var image = Image.Load(img);
            bool needsUpdate = false;
            if (image.Metadata.HorizontalResolution != dpiX)
            {
                image.Metadata.HorizontalResolution = dpiX;
                needsUpdate = true;
            }
            if (image.Metadata.VerticalResolution != (dpiY == 0 ? dpiX : dpiY))
            {
                image.Metadata.VerticalResolution = dpiY == 0 ? dpiX : dpiY;
                needsUpdate = true;
            }

            if (image.Metadata.DecodedImageFormat is not PngFormat)
            {
                needsUpdate = true;
            }

            if (needsUpdate)
            {
                using var ms = new MemoryStream();
                image.Save(ms, new PngEncoder());
                return ms.ToArray();
            }
            return img;
        }

        public static byte[] GetPng<TPixel>(byte[] img) where TPixel : unmanaged, IPixel<TPixel>
        {
            using var image = Image.Load<TPixel>(img);
            if (image.Metadata.DecodedImageFormat is PngFormat)
            {
                return img;
            }

            using var ms = new MemoryStream();
            image.Save(ms, new PngEncoder());
            return ms.ToArray();
        }

        public static byte[] GetBmp(byte[] img)
        {
            using var image = Image.Load(img);
            if (image.Metadata.DecodedImageFormat is BmpFormat)
            {
                return img;
            }

            using var ms = new MemoryStream();
            image.SaveAsBmp(ms);
            return ms.ToArray();
        }

        public static byte[] GetBmp(byte[] img, int dpiX, int dpiY = 0)
        {
            using var image = Image.Load(img);
            bool needsUpdate = false;
            if (image.Metadata.HorizontalResolution != dpiX)
            {
                image.Metadata.HorizontalResolution = dpiX;
                needsUpdate = true;
            }
            if (image.Metadata.VerticalResolution != (dpiY == 0 ? dpiX : dpiY))
            {
                image.Metadata.VerticalResolution = dpiY == 0 ? dpiX : dpiY;
                needsUpdate = true;
            }

            if (image.Metadata.DecodedImageFormat is not BmpFormat)
            {
                needsUpdate = true;
            }

            if (needsUpdate)
            {
                using var ms = new MemoryStream();
                image.SaveAsBmp(ms);
                return ms.ToArray();
            }
            return img;
        }

        public static DPI GetImageDPI(byte[] image)
        {
            using var img = Image.Load(image);
            return new DPI
            {
                X = (int)img.Metadata.HorizontalResolution,
                Y = (int)img.Metadata.VerticalResolution
            };
        }

        public static byte[] SetImageDPI(byte[] image, int dpiX, int dpiY = 0)
        {
            using var img = Image.Load(image);
            bool needsUpdate = false;
            if (img.Metadata.HorizontalResolution != dpiX)
            {
                img.Metadata.HorizontalResolution = dpiX;
                needsUpdate = true;
            }
            if (img.Metadata.VerticalResolution != (dpiY == 0 ? dpiX : dpiY))
            {
                img.Metadata.VerticalResolution = dpiY == 0 ? dpiX : dpiY;
                needsUpdate = true;
            }

            if (needsUpdate)
            {
                using var ms = new MemoryStream();
                img.Save(ms, img.Metadata.DecodedImageFormat);
                return ms.ToArray();
            }
            return image;
        }

        public static string GetImageUID(byte[] image)
        {
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(image);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        public static string GetImageDataUID(byte[] image)
        {
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(image);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        private static bool IsPng(byte[] img)
        {
            return img.Take(PngSignatureLength).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        }

        private static bool IsBmp(byte[] img)
        {
            return img.Length > 2 && img[0] == 'B' && img[1] == 'M';
        }

        private static DPI GetBitmapDPI(byte[] image)
        {
            using var img = Image.Load(image);
            return new DPI
            {
                X = (int)img.Metadata.HorizontalResolution,
                Y = (int)img.Metadata.VerticalResolution
            };
        }

        public static PixelTypeInfo GetBmpPixelFormat(byte[] image)
        {
            using var img = Image.Load(image);
            return img.PixelType;
        }

        public static byte[] SetBitmapDPI(byte[] image, int dpiX, int dpiY)
        {
            using var img = Image.Load(image);
            bool needsUpdate = false;
            if (img.Metadata.HorizontalResolution != dpiX)
            {
                img.Metadata.HorizontalResolution = dpiX;
                needsUpdate = true;
            }
            if (img.Metadata.VerticalResolution != dpiY)
            {
                img.Metadata.VerticalResolution = dpiY;
                needsUpdate = true;
            }

            if (needsUpdate)
            {
                using var ms = new MemoryStream();
                img.SaveAsBmp(ms);
                return ms.ToArray();
            }
            return image;
        }

        private static byte[] SetBmpPixelFormat<TPixel>(byte[] image) where TPixel : unmanaged, IPixel<TPixel>
        {
            using var img = Image.Load(image);
            var convertedImage = img.CloneAs<TPixel>();
            using var ms = new MemoryStream();
            convertedImage.SaveAsBmp(ms);
            return ms.ToArray();
        }

        private static byte[] ExtractBitmapData(byte[] image)
        {
            using var img = Image.Load(image);
            using var ms = new MemoryStream();
            img.SaveAsBmp(ms);
            return ms.ToArray();
        }

        private static DPI GetPngDPI(byte[] image)
        {
            using var img = Image.Load(image);
            return new DPI
            {
                X = (int)img.Metadata.HorizontalResolution,
                Y = (int)img.Metadata.VerticalResolution
            };
        }

        public static PixelTypeInfo GetPngPixelFormat(byte[] image)
        {
            using var img = Image.Load(image);
            return img.PixelType;
        }

        private static byte[] SetPngDPI(byte[] image, int dpiX, int dpiY)
        {
            using var img = Image.Load(image);
            bool needsUpdate = false;
            if (img.Metadata.HorizontalResolution != dpiX)
            {
                img.Metadata.HorizontalResolution = dpiX;
                needsUpdate = true;
            }
            if (img.Metadata.VerticalResolution != dpiY)
            {
                img.Metadata.VerticalResolution = dpiY;
                needsUpdate = true;
            }

            if (needsUpdate)
            {
                using var ms = new MemoryStream();
                img.Save(ms, new PngEncoder());
                return ms.ToArray();
            }
            return image;
        }

        public static byte[] SetPngPixelFormat<TPixel>(byte[] image) where TPixel : unmanaged, IPixel<TPixel>
        {
            using var img = Image.Load(image);
            var convertedImage = img.CloneAs<TPixel>();
            using var ms = new MemoryStream();
            convertedImage.Save(ms, new PngEncoder());
            return ms.ToArray();
        }

        private static byte[] ExtractPngData(byte[] image)
        {
            using var img = Image.Load(image);
            using var ms = new MemoryStream();
            img.Save(ms, new PngEncoder());
            return ms.ToArray();
        }

        private static int DotsPerInch(int dpm)
        {
            return (int)(dpm / InchesPerMeter);
        }

        private static int DotsPerMeter(int dpi)
        {
            return (int)(dpi * InchesPerMeter);
        }

        public static void RedrawFiducial(string path, bool is300)
        {
            // Implementation for RedrawFiducial
        }
    }

    class DPI
    {
        public int X { get; set; }
        public int Y { get; set; }
    }
}
