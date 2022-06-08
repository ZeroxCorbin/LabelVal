using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V275_Testing.Printer
{
    public class PrintControl
    {

        private int Count { get; set; }
        private string ImagePath { get; set; }

        private int index;

        public void Print(string imagePath, int count, string printerName)
        {
            ImagePath = imagePath;
            Count = count;

            index = 1;

            using (PrintDocument pd = new PrintDocument())
            {
                pd.PrintPage += PrintPage;
                pd.PrinterSettings.PrinterName = printerName;
                pd.Print();
            }
        }

        private void PrintPage(object o, PrintPageEventArgs e)
        {
            //e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            //e.Graphics.InterpolationMode = InterpolationMode.High;
           //e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;



            using (System.Drawing.Image img = System.Drawing.Image.FromFile(ImagePath))
            {
                Size s = new Size(((int)(img.Width / img.HorizontalResolution) * 300), ((int)(img.Height / img.VerticalResolution) * 300));

            //Rectangle rect = new Rectangle(x, y, thumbSize.Width, thumbSize.Height);
            //g.DrawImage(mg, rect, 0, 0, mg.Width, mg.Height, GraphicsUnit.Pixel);
            //    Bitmap bit = new Bitmap(img, );
                //Rectangle rectangle = new Rectangle(0,0, ((int)(img.Width / img.HorizontalResolution) * 300), ((int)(img.Height / img.VerticalResolution) * 300));
                System.Drawing.Point loc = new System.Drawing.Point(0, 0);
                e.Graphics.DrawImage(img, new Point(0,0));
            }

            if (index++ < Count)
                e.HasMorePages = true;
        }
    }
}
