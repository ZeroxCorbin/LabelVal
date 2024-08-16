using System;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Reflection.Metadata;
using System.Windows.Media.Imaging;
using System.Linq;
using Org.BouncyCastle.Tls;
using static LabelVal.Utilities.ImageUtilities;

namespace LabelVal.Utilities
{
    public static class ImageUtilities
    { 
        private const double InchesPerMeter = 39.3701;
        
        public class DPI
        {
            public int X { get; set; }
            public int Y { get; set; }
        }

        /// <summary>
        /// Get PNG image from PNG or BMP image.
        /// Copies DPI, PixelFormat, and metadata if converted from BMP.
        /// </summary>
        /// <param name="img"></param>
        /// <returns>Converted BMP or original image</returns>
        public static byte[] GetPng(byte[] img)
        {
            if (IsPng(img))
            {
                return img;
            }

            using var ms = new MemoryStream(img);
            using var bitmap = new Bitmap(ms);
            using var stream = new MemoryStream();

            var dpiX = bitmap.HorizontalResolution;
            var dpiY = bitmap.VerticalResolution;

            bitmap.SetResolution(dpiX, dpiY);
            bitmap.Save(stream, ImageFormat.Png);
            return stream.ToArray();
        }
        /// <summary>
        /// Get PNG image from PNG or BMP image. Sets the DPI in the PNG image, if needed.
        /// Copies PixelFormat and metadata if converted from BMP.
        /// </summary>
        /// <param name="img"></param>
        /// <param name="dpiX"></param>
        /// <param name="dpiY"></param>
        /// <returns>Converted BMP or updated orginal</returns>
        /// <exception cref="ArgumentException"></exception>
        public static byte[] GetPng(byte[] img, int dpiX, int dpiY = 0)
        {
            if (dpiX <= 0)
            {
                throw new ArgumentException("DPI value must be greater than zero.");
            }

            if (dpiY <= 0)
            {
                dpiY = dpiX; // Use dpiX if dpiY is not provided or invalid
            }

            if (IsPng(img))
            {
                var dpi = GetPngDPI(img);
                if (dpi.X == dpiX && dpi.Y == dpiY)
                {
                    return img;
                }
                else
                {
                    return SetPngDPI(img, dpiX, dpiY); ;
                }
            }

            using var ms = new MemoryStream(img);
            using var bitmap = new Bitmap(ms);
            using var stream = new MemoryStream();

            bitmap.SetResolution(dpiX, dpiY);
            bitmap.Save(stream, ImageFormat.Png);
            return stream.ToArray();
        }

        /// <summary>
        /// Get the DPI of a PNG or BMP image.
        /// </summary>
        /// <param name="image"></param>
        /// <returns>DPI</returns>
        /// <exception cref="ArgumentException"></exception>
        public static DPI GetImageDPI(byte[] image)
        {
            if (IsPng(image))
            {
                return GetPngDPI(image);
            }
            else if (IsBmp(image))
            {
                return GetBitmapDPI(image);
            }
            else
            {
                throw new ArgumentException("Unsupported image format.");
            }
        }
        /// <summary>
        /// Set the header DPI of a PNG or BMP image.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="dpiX"></param>
        /// <param name="dpiY"></param>
        /// <exception cref="ArgumentException"></exception>
        public static byte[] SetImageDPI(byte[] image, int dpiX, int dpiY = 0)
        {
            if (dpiX <= 0)
            {
                throw new ArgumentException("DPI value must be greater than zero.");
            }

            if (dpiY <= 0)
            {
                dpiY = dpiX; // Use dpiX if dpiY is not provided or invalid
            }

            if (IsPng(image))
            {
                return SetPngDPI(image, dpiX, dpiY);
            }
            else if (IsBmp(image))
            {
                return SetBitmapDPI(image, dpiX, dpiY);
            }
            else
            {
                throw new ArgumentException("Unsupported image format.");
            }
        }

        /// <summary>
        /// Get the UID of the entire byte array.
        /// </summary>
        /// <param name="image"></param>
        /// <returns>SHA256 Hash string with hyphen removed</returns>
        public static string GetImageUID(byte[] image)
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
        /// <summary>
        /// Get the UID of the image data array only. No header information.
        /// </summary>
        /// <param name="image"></param>
        /// <returns>SHA256 Hash string with hyphen removed</returns>
        public static string GetImageDataUID(byte[] image)
        {
            try
            {
                byte[] imageData;

                if (IsPng(image))
                {
                    imageData = ExtractPngData(image);
                }
                else if (IsBmp(image))
                {
                    imageData = ExtractBitmapData(image);
                }
                else
                {
                    throw new ArgumentException("Unsupported image format.");
                }

                using SHA256 sha256 = SHA256.Create();
                return BitConverter.ToString(sha256.ComputeHash(imageData)).Replace("-", string.Empty);
            }
            catch (Exception)
            {
                return string.Empty;
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

        private static DPI GetBitmapDPI(byte[] image)
        {
            if (image.Length < 54) // Minimum size for BMP with BITMAPINFOHEADER
            {
                throw new ArgumentException("Invalid BMP file.");
            }

            return new DPI
            {
                X = DotsPerInch(BitConverter.ToInt32(image, 38)),
                Y = DotsPerInch(BitConverter.ToInt32(image, 42))
            };
        }
        private static byte[] SetBitmapDPI(byte[] image, int dpiX, int dpiY)
        {
            if (image.Length < 54) // Minimum size for BMP with BITMAPINFOHEADER
            {
                throw new ArgumentException("Invalid BMP file.");
            }

            int dpiXInMeters = DotsPerMeter(dpiX);
            int dpiYInMeters = DotsPerMeter(dpiY);

            // Set the horizontal DPI
            for (int i = 38; i < 42; i++)
            {
                image[i] = BitConverter.GetBytes(dpiXInMeters)[i - 38];
            }

            // Set the vertical DPI
            for (int i = 42; i < 46; i++)
            {
                image[i] = BitConverter.GetBytes(dpiYInMeters)[i - 42];
            }

            return image;
        }
        private static byte[] ExtractBitmapData(byte[] image)
        {
            if (image.Length < 54) // Minimum size for BMP with BITMAPINFOHEADER
            {
                throw new ArgumentException("Invalid BMP file.");
            }

            int dataOffset = BitConverter.ToInt32(image, 10);
            return image[dataOffset..];
        }

        private static DPI GetPngDPI(byte[] image)
        {
            const int PngSignatureLength = 8;
            const int PhysChunkLength = 9;
            const int PhysChunkType = 0x70485973; // 'pHYs' in ASCII

            if (image.Length < PngSignatureLength + PhysChunkLength)
            {
                throw new ArgumentException("Invalid PNG file.");
            }

            using var ms = new MemoryStream(image);
            using var reader = new BinaryReader(ms);

            // Skip the PNG signature
            reader.BaseStream.Seek(PngSignatureLength, SeekOrigin.Begin);

            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                int chunkLength = BitConverter.ToInt32(reader.ReadBytes(4).Reverse().ToArray(), 0);
                int chunkType = BitConverter.ToInt32(reader.ReadBytes(4), 0);

                if (chunkType == PhysChunkType)
                {
                    int pixelsPerUnitX = BitConverter.ToInt32(reader.ReadBytes(4).Reverse().ToArray(), 0);
                    int pixelsPerUnitY = BitConverter.ToInt32(reader.ReadBytes(4).Reverse().ToArray(), 0);
                    byte unitSpecifier = reader.ReadByte();

                    if (unitSpecifier == 1) // Meters
                    {
                        return new DPI()
                        {
                            X = (int)(pixelsPerUnitX * InchesPerMeter),
                            Y = (int)(pixelsPerUnitY * InchesPerMeter)
                        };
                    }
                    else
                    {
                        throw new ArgumentException("Unsupported unit specifier in pHYs chunk.");
                    }
                }
                else
                {
                    // Skip the chunk data and CRC
                    reader.BaseStream.Seek(chunkLength + 4, SeekOrigin.Current);
                }
            }

            throw new ArgumentException("pHYs chunk not found in PNG file.");
        }
        private static byte[] SetPngDPI(byte[] image, int dpiX, int dpiY)
        {
            const int PngSignatureLength = 8;
            const int PhysChunkType = 0x70485973; // 'pHYs' in ASCII

            if (image.Length < PngSignatureLength)
            {
                throw new ArgumentException("Invalid PNG file.");
            }

            using var ms = new MemoryStream(image);
            using var reader = new BinaryReader(ms);
            using var writer = new BinaryWriter(ms);

            // Skip the PNG signature
            reader.BaseStream.Seek(PngSignatureLength, SeekOrigin.Begin);

            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                int chunkLength = BitConverter.ToInt32(reader.ReadBytes(4).Reverse().ToArray(), 0);
                int chunkType = BitConverter.ToInt32(reader.ReadBytes(4), 0);

                if (chunkType == PhysChunkType)
                {
                    ms.Seek(-4, SeekOrigin.Current); // Move back to the start of the chunk data
                    writer.Write(BitConverter.GetBytes(chunkLength).Reverse().ToArray());
                    writer.Write(BitConverter.GetBytes(chunkType));

                    writer.Write(BitConverter.GetBytes((int)(dpiX / InchesPerMeter)).Reverse().ToArray());
                    writer.Write(BitConverter.GetBytes((int)(dpiY / InchesPerMeter)).Reverse().ToArray());
                    writer.Write((byte)1); // Unit specifier: 1 for meters

                    // Skip the CRC
                    reader.BaseStream.Seek(4, SeekOrigin.Current);

                    return image;
                }
                else
                {
                    // Skip the chunk data and CRC
                    reader.BaseStream.Seek(chunkLength + 4, SeekOrigin.Current);
                }
            }

            throw new ArgumentException("pHYs chunk not found in PNG file.");
        }
        private static byte[] ExtractPngData(byte[] image)
        {
            const int pngSignatureLength = 8;
            const int ChunkHeaderLength = 8; // 4 bytes length + 4 bytes type
            const int idatChunkType = 0x49444154; // 'IDAT' in ASCII

            if (image.Length < pngSignatureLength)
            {
                throw new ArgumentException("Invalid PNG file.");
            }

            using var ms = new MemoryStream(image);
            using var reader = new BinaryReader(ms);

            // Skip the PNG signature
            reader.BaseStream.Seek(pngSignatureLength, SeekOrigin.Begin);

            using var dataStream = new MemoryStream();

            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                int chunkLength = BitConverter.ToInt32(reader.ReadBytes(4).Reverse().ToArray(), 0);
                int chunkType = BitConverter.ToInt32(reader.ReadBytes(4), 0);

                if (chunkType == idatChunkType)
                {
                    byte[] chunkData = reader.ReadBytes(chunkLength);
                    dataStream.Write(chunkData, 0, chunkLength);
                }
                else
                {
                    // Skip the chunk data and CRC
                    reader.BaseStream.Seek(chunkLength + 4, SeekOrigin.Current);
                }
            }

            return dataStream.ToArray();
        }

        private static int DotsPerInch(int dpm) => (int)Math.Round(dpm / InchesPerMeter);
        private static int DotsPerMeter(int dpi) => (int)Math.Round(dpi * InchesPerMeter);

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

        public static byte[] RotatePNG(byte[] img, double angle = 0)
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

        //public static byte[] ConvertToBmp(byte[] img)
        //{
        //    System.Windows.Media.Imaging.BmpBitmapEncoder encoder = new();
        //    using var ms = new System.IO.MemoryStream(img);
        //    using MemoryStream stream = new();
        //    encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(ms));
        //    encoder.Save(stream);
        //    stream.Close();

        //    return stream.ToArray();
        //}
        //public static byte[] ConvertToBmp(byte[] img, int dpi)
        //{
        //    System.Windows.Media.Imaging.BmpBitmapEncoder encoder = new();
        //    using var ms = new System.IO.MemoryStream(img);
        //    using MemoryStream stream = new();
        //    encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(ms));
        //    encoder.Save(stream);
        //    stream.Close();

        //    byte[] ret = stream.ToArray();

        //    if (dpi > 0)
        //        SetBitmapDPI(ret, dpi);

        //    return ret;
        //}
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
