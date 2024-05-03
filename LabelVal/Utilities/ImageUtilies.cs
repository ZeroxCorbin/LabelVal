using System;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;

namespace LabelVal.Utilities
{
    public static class ImageUtilities
    {
        private const double InchesPerMeter = 39.3701;

        public static int GetImageDPI(byte[] image)
        {
            var value = BitConverter.ToInt32(image, 38);
            var res = GetDPI(value);
            return res;
        }

        public static void SetImageDPI(byte[] image, int dpi)
        {
            var value = BitConverter.GetBytes(SetDPI(dpi));

            int i = 38;
            foreach (byte b in value)
                image[i++] = b;

            foreach (byte b in value)
                image[i++] = b;
        }

        public static byte[] ConvertToPng(byte[] img, int dpi)
        {
            if (dpi > 0)
                SetImageDPI(img, dpi);

            System.Windows.Media.Imaging.PngBitmapEncoder encoder = new();

            using var ms = new System.IO.MemoryStream(img);
            using MemoryStream stream = new();

            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(ms));
            encoder.Save(stream);
            stream.Close();

            return stream.ToArray();
        }

        public static byte[] ConvertToPng(byte[] img)
        {
            System.Windows.Media.Imaging.PngBitmapEncoder encoder = new();
            using var ms = new System.IO.MemoryStream(img);
            using MemoryStream stream = new();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(ms));
            encoder.Save(stream);
            stream.Close();

            return stream.ToArray();
        }



        public static byte[] AddOverlayPNG(byte[] img, byte[] overlay)
        {

            var dg = new System.Windows.Media.DrawingGroup();

            var renderBitmap1 = CreateBitmap(img);
            var renderBitmap2 = CreateBitmap(overlay);

            var id1 = new System.Windows.Media.ImageDrawing(renderBitmap1, new System.Windows.Rect(0, 0, renderBitmap1.Width, renderBitmap1.Height));
            var id2 = new System.Windows.Media.ImageDrawing(renderBitmap2, new System.Windows.Rect(0, 0, renderBitmap2.Width, renderBitmap2.Height));

            dg.Children.Add(id1);
            dg.Children.Add(id2);

            var combinedImg = new System.Windows.Media.Imaging.RenderTargetBitmap(
                (int)renderBitmap1.Width,
                (int)renderBitmap1.Height, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);

            var dv = new System.Windows.Media.DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                dc.DrawDrawing(dg);
            }

            combinedImg.Render(dv);

            System.Windows.Media.Imaging.PngBitmapEncoder encoder = new();
            using MemoryStream stream = new();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(combinedImg));

            encoder.Save(stream);
            stream.Close();

            return stream.ToArray();

        }

        public static byte[] ConvertToBmp(byte[] img)
        {
            System.Windows.Media.Imaging.BmpBitmapEncoder encoder = new();
            using var ms = new System.IO.MemoryStream(img);
            using MemoryStream stream = new();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(ms));
            encoder.Save(stream);
            stream.Close();

            return stream.ToArray();
        }

        public static System.Windows.Media.Imaging.BitmapImage CreateBitmap(byte[] data, int decodePixelWidth = 0)
        {
            if (data == null || data.Length < 2)
                return null;

            try
            {
                System.Windows.Media.Imaging.BitmapImage img = new();

                using (MemoryStream memStream = new(data))
                {
                    img.BeginInit();
                    img.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    img.StreamSource = memStream;
                    img.DecodePixelWidth = decodePixelWidth;
                    img.EndInit();
                    img.Freeze();

                }
                return img;
            }
            catch { }

            return null;
        }

        public static byte[] ImageToBytes(System.Drawing.Image image)
        {
            System.Drawing.ImageConverter converter = new();
            return (byte[])converter.ConvertTo(image, typeof(byte[]));
        }

        private static byte[] imageToBytes(System.Windows.Media.ImageSource imageSource)
        {
            System.Windows.Media.Imaging.PngBitmapEncoder encoder = new();
            byte[] bytes = null;
            var bitmapSource = imageSource as System.Windows.Media.Imaging.BitmapSource;

            if (bitmapSource != null)
            {
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmapSource));

                using var stream = new MemoryStream();
                encoder.Save(stream);
                bytes = stream.ToArray();
            }

            return bytes;
        }

        public static byte[] ImageToBytes(this System.Windows.Media.DrawingImage source)
        {
            var drawingVisual = new System.Windows.Media.DrawingVisual();
            var drawingContext = drawingVisual.RenderOpen();
            drawingContext.DrawImage(source, new System.Windows.Rect(new System.Windows.Point(0, 0), new System.Windows.Size(source.Width, source.Height)));
            drawingContext.Close();

            var bmp = new System.Windows.Media.Imaging.RenderTargetBitmap((int)source.Width, (int)source.Height, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            bmp.Render(drawingVisual);
            return imageToBytes(bmp);
        }

        public static System.Windows.Media.Imaging.BitmapSource ToBitmapSource(this System.Windows.Media.DrawingImage source)
        {
            var drawingVisual = new System.Windows.Media.DrawingVisual();
            var drawingContext = drawingVisual.RenderOpen();
            drawingContext.DrawImage(source, new System.Windows.Rect(new System.Windows.Point(0, 0), new System.Windows.Size(source.Width, source.Height)));
            drawingContext.Close();

            var bmp = new System.Windows.Media.Imaging.RenderTargetBitmap((int)source.Width, (int)source.Height, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            bmp.Render(drawingVisual);
            return bmp;
        }

        private static int GetDPI(int headerValue) => (int)Math.Round(headerValue / InchesPerMeter);
        private static int SetDPI(int dpi) => (int)Math.Round(dpi * InchesPerMeter);

        public static string ImageUID(byte[] image)
        {
            try
            {
                using SHA256 md5 = SHA256.Create();
                return BitConverter.ToString(md5.ComputeHash(image)).Replace("-", String.Empty);

            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public static void RedrawFiducial(string path, bool is300)
        {
            // load your photo
            using (FileStream fs = new FileStream(path, FileMode.Open))
            {
                Bitmap photo = (Bitmap)Bitmap.FromStream(fs);
                fs.Close();

                Bitmap newmap = new Bitmap(photo.Width, photo.Height);
                newmap.SetResolution(photo.HorizontalResolution, photo.VerticalResolution);
                //if (photo.Height != 2400)
                //    File.AppendAllText($"{UserDataDirectory}\\Small Images List", Path.GetFileName(path));

                //if (is300)
                //{//600 DPI
                //    if ((photo.Height > 2400 && photo.Height != 4800) || photo.Height < 2000)
                //        return;
                //}
                //else
                //{//300 DPI
                //    if ((photo.Height > 1200) || photo.Height < 1000)
                //        return;
                //}

                using (var graphics = Graphics.FromImage(newmap))
                {
                    graphics.DrawImage(photo, 0, 0, photo.Width, photo.Height);

                    if (is300)
                    {//300 DPI
                        graphics.FillRectangle(Brushes.White, 0, 976, 150, photo.Height - 976);
                        graphics.FillRectangle(Brushes.Black, 15, 975, 45, 45);
                    }
                    else
                    {
                        graphics.FillRectangle(Brushes.White, 0, 1900, 195, photo.Height - 1900);
                        graphics.FillRectangle(Brushes.Black, 30, 1950, 90, 90);
                    }

                    newmap.Save(path, ImageFormat.Png);
                }
            }

        }

    }
}
