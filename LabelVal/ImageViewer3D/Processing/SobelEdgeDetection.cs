using LabelVal.Utilities;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

namespace LabelVal.ImageViewer3D.Processing
{
    public class SobelEdgeDetection
    {

        public static byte[] ApplySobelEdgeDetection3ByteColor(byte[] input, int width, int height)
        {
            byte[] output = new byte[input.Length];

            int[] sobelX = new int[] { -1, 0, 1, -2, 0, 2, -1, 0, 1 };
            int[] sobelY = new int[] { -1, -2, -1, 0, 0, 0, 1, 2, 1 };

            int stride = width * 3;
            int offset = stride + 3;

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    int gx = 0;
                    int gy = 0;

                    int index = (y * stride) + (x * 3);

                    for (int i = -1; i <= 1; i++)
                    {
                        for (int j = -1; j <= 1; j++)
                        {
                            int index2 = ((y + i) * stride) + ((x + j) * 3);
                            int index3 = (i + 1) * 3 + (j + 1);

                            gx += input[index2] * sobelX[index3];
                            gy += input[index2] * sobelY[index3];
                        }
                    }

                    int magnitude = (int)Math.Sqrt(gx * gx + gy * gy);

                    if (magnitude > 255)
                    {
                        magnitude = 255;
                    }
                    else if (magnitude < 0)
                    {
                        magnitude = 0;
                    }

                    output[index] = (byte)magnitude;
                    output[index + 1] = (byte)magnitude;
                    output[index + 2] = (byte)magnitude;
                }
            }

            return output;
        }

        public static byte[] ApplySobelEdgeDetection1ByteColor(byte[] input, int width, int height)
        {
            byte[] output = new byte[input.Length];

            int[] sobelX = new int[] { -1, 0, 1, -2, 0, 2, -1, 0, 1 };
            int[] sobelY = new int[] { -1, -2, -1, 0, 0, 0, 1, 2, 1 };

            int stride = width;
            int offset = stride + 1;

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    int gx = 0;
                    int gy = 0;

                    int index = (y * stride) + x;

                    for (int i = -1; i <= 1; i++)
                    {
                        for (int j = -1; j <= 1; j++)
                        {
                            int index2 = ((y + i) * stride) + (x + j);
                            int index3 = (i + 1) * 3 + (j + 1);

                            gx += input[index2] * sobelX[index3];
                            gy += input[index2] * sobelY[index3];
                        }
                    }

                    int magnitude = (int)Math.Sqrt(gx * gx + gy * gy);

                    if (magnitude > 255)
                    {
                        magnitude = 255;
                    }
                    else if (magnitude < 0)
                    {
                        magnitude = 0;
                    }

                    output[index] = (byte)magnitude;
                }
            }

            return output;
        }

        public static byte[] CreateBitmapFromEdgeDetection3ByteColor(byte[] edgeData, int width, int height)
        {
            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);

            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, bitmap.PixelFormat);
            IntPtr ptr = bitmapData.Scan0;

            System.Runtime.InteropServices.Marshal.Copy(edgeData, 0, ptr, edgeData.Length);

            bitmap.UnlockBits(bitmapData);

            return bitmap.GetBytes();
        }

        public static byte[] CreateBitmapFromEdgeDetection1ByteColor(byte[] edgeData, int width, int height)
        {
            // Create a WriteableBitmap
            System.Windows.Media.Imaging.WriteableBitmap writeableBitmap = new System.Windows.Media.Imaging.WriteableBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Gray8, null);

            // Copy the pixel data to the WriteableBitmap
            writeableBitmap.WritePixels(new System.Windows.Int32Rect(0, 0, width, height), edgeData, width, 0);

            // Convert WriteableBitmap to Bitmap
            Bitmap bitmap;
            using (MemoryStream stream = new MemoryStream())
            {
                System.Windows.Media.Imaging.BitmapEncoder encoder = new System.Windows.Media.Imaging.BmpBitmapEncoder();
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(writeableBitmap));
                encoder.Save(stream);
                bitmap = new Bitmap(stream);
            }

            // Set the grayscale palette
            ColorPalette palette = bitmap.Palette;
            for (byte i = 0; i <= byte.MaxValue; i++)
            {
                palette.Entries[i] = System.Drawing.Color.FromArgb(i, i, i);
            }
            bitmap.Palette = palette;

            return bitmap.GetBytes();
        }
    }
}
