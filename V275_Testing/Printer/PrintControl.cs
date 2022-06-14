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
        private string Data;

        private int index;

        public void Print(string imagePath, int count, string printerName, string data)
        {
            ImagePath = imagePath;
            Count = count;
            Data = data;

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
            using (System.Drawing.Image img = System.Drawing.Image.FromFile(ImagePath))
            {
                e.Graphics.DrawImage(img, new Point(0, 0));
                if (!string.IsNullOrEmpty(Data))
                {
                    SizeF dataLength = e.Graphics.MeasureString(Data, new Font("Arial", 8));
                    e.Graphics.DrawString(Data, new Font("Arial", 8), Brushes.Black, new Point(e.PageBounds.Width - (int)dataLength.Width - 10, 10));
                }

            }

            if (index++ < Count)
                e.HasMorePages = true;
        }
    }
}
