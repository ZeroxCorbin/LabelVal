using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

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

            PngBitmapEncoder encoder = new PngBitmapEncoder();
            using (var ms = new System.IO.MemoryStream(img))
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    encoder.Frames.Add(BitmapFrame.Create(ms));
                    encoder.Save(stream);
                    stream.Close();

                    return stream.ToArray();

                }
            }
        }

        public static byte[] ConvertToBmp(byte[] img)
        {
            BmpBitmapEncoder encoder = new BmpBitmapEncoder();
            using (var ms = new System.IO.MemoryStream(img))
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    encoder.Frames.Add(BitmapFrame.Create(ms));
                    encoder.Save(stream);
                    stream.Close();

                    return stream.ToArray();

                }
            }
        }

        public static BitmapImage CreateBitmap(byte[] data)
        {
            if (data == null || data.Length < 2)
                return null;

            BitmapImage img = new BitmapImage();

            using (MemoryStream memStream = new MemoryStream(data))
            {
                img.BeginInit();
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.StreamSource = memStream;
                //img.DecodePixelWidth = 400;
                img.EndInit();
                img.Freeze();

            }
            return img;
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


        private static int GetDPI(int headerValue) => (int)Math.Round(headerValue / InchesPerMeter);
        private static int SetDPI(int dpi) => (int)Math.Round(dpi * InchesPerMeter);

        public static string ImageUID(byte[] image)
        {
            try
            {
                using (SHA256 md5 = SHA256.Create())
                {
                    return BitConverter.ToString(md5.ComputeHash(image)).Replace("-", String.Empty);
                }

            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

    }
}
