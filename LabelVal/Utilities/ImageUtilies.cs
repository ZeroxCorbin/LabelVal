using System;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Reflection.Metadata;
using System.Windows.Media.Imaging;

namespace LabelVal.Utilities
{
    public static class ImageUtilities
    {
        private const double InchesPerMeter = 39.3701;

        public static int GetBitmapDPI(byte[] image)
        {
            var value = BitConverter.ToInt32(image, 38);
            var res = GetDPI(value);
            return res;
        }

        //Set DPI for 95xx systems
        public static void SetBitmapDPI(byte[] image, int dpi)
        {
            var value = BitConverter.GetBytes(SetDPI(dpi));

            int i = 38;
            foreach (byte b in value)
                image[i++] = b;

            foreach (byte b in value)
                image[i++] = b;
        }

      //Seek #FileIndex, 7   ' position of balance information
      //Get #FileIndex, , BalanceInfo
      //If BalanceInfo = Asc("C") Or BalanceInfo = Asc("3") Then
      //  ColorFlag = Chr(BalanceInfo)
      //End If
        public static void SetBitmapColorFlag(byte[] image, char balanceInfo)
        {
            var value = BitConverter.GetBytes(balanceInfo);

            int i = 7;
            foreach (byte b in value)
                image[i++] = b;
        }

        public static byte[] ConvertToPng(byte[] img, int dpi, double angle)
        {
            if (dpi > 0)
                SetBitmapDPI(img, dpi);

            System.Windows.Media.Imaging.PngBitmapEncoder encoder = new();
            using var ms = new System.IO.MemoryStream(img);
            using MemoryStream stream = new();

            // Create a BitmapImage from the byte array
            var bitmapImage = new System.Windows.Media.Imaging.BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = ms;
            bitmapImage.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            bitmapImage.Freeze();

            // Apply rotation if angle is not zero
            if (angle != 0)
            {
                var transformedBitmap = new System.Windows.Media.Imaging.TransformedBitmap(
                    bitmapImage, new System.Windows.Media.RotateTransform(angle));
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(transformedBitmap));
            }
            else
            {
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmapImage));
            }

            encoder.Save(stream);
            stream.Close();

            return stream.ToArray();
        }
        public static byte[] ConvertToPng(byte[] img, double angle = 0)
        {
  
                System.Windows.Media.Imaging.PngBitmapEncoder encoder = new();
                using var ms = new System.IO.MemoryStream(img);
                using MemoryStream stream = new();

                // Create a BitmapImage from the byte array
                var bitmapImage = new System.Windows.Media.Imaging.BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = ms;
                bitmapImage.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                // Apply rotation if angle is not zero
                if (angle != 0)
                {
                    var transformedBitmap = new System.Windows.Media.Imaging.TransformedBitmap(
                        bitmapImage, new System.Windows.Media.RotateTransform(angle));
                    encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(transformedBitmap));
                }
                else
                {
                    encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmapImage));
                }

                encoder.Save(stream);
                stream.Close();

                return stream.ToArray();
            
        }
        public static byte[] ConvertToPng(byte[] img, int dpi)
        {
            if (IsPng(img))
                return img;
            
            using var ms = new MemoryStream(img);
            using var bitmap = new Bitmap(ms);
            using var stream = new MemoryStream();

            bitmap.SetResolution(dpi, dpi);
            bitmap.Save(stream, ImageFormat.Png);
            return stream.ToArray();
        }
        public static byte[] ConvertToPng(byte[] img)
        {
            if (IsPng(img))
            {
                return img;
            }

            using var ms = new MemoryStream(img);
            using var bitmap = new Bitmap(ms);
            using var stream = new MemoryStream();

            bitmap.Save(stream, ImageFormat.Png);
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
        public static byte[] ConvertToBmp(byte[] img, int dpi)
        {
            System.Windows.Media.Imaging.BmpBitmapEncoder encoder = new();
            using var ms = new System.IO.MemoryStream(img);
            using MemoryStream stream = new();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(ms));
            encoder.Save(stream);
            stream.Close();

            byte[] ret = stream.ToArray();

            if (dpi > 0)
                SetBitmapDPI(ret, dpi);

            return ret;
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

        private static bool IsPng(byte[] img)
        {
            if (img.Length < 8)
            {
                return false;
            }

            return img[0] == 0x89 &&
                   img[1] == 0x50 &&
                   img[2] == 0x4E &&
                   img[3] == 0x47 &&
                   img[4] == 0x0D &&
                   img[5] == 0x0A &&
                   img[6] == 0x1A &&
                   img[7] == 0x0A;
        }

        private static bool IsBmp(byte[] img)
        {
            if (img.Length < 2)
            {
                return false;
            }

            return img[0] == 0x42 && img[1] == 0x4D; // 'B' and 'M' in ASCII
        }

    }
}
