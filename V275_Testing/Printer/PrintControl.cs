using System;
using System.Collections.Generic;
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
            using (System.Drawing.Image img = System.Drawing.Image.FromFile(ImagePath))
            {
                System.Drawing.Point loc = new System.Drawing.Point(0, 0);
                e.Graphics.DrawImage(img, loc);
            }

            if (index++ < Count)
                e.HasMorePages = true;

        }



    }
}
