using System.Drawing;
using System.Drawing.Printing;

namespace LabelVal.Printer;

public class Controller
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

        using var pd = new PrintDocument();
        pd.PrintPage += PrintPage;

        pd.PrinterSettings.PrinterName = printerName;
        pd.Print();
    }

    private void PrintPage(object o, PrintPageEventArgs e)
    {
        using (var img = System.Drawing.Image.FromFile(ImagePath))
        {
            if (!string.IsNullOrEmpty(Data))
            {

                using var g = Graphics.FromImage(img);
                var dataLength = g.MeasureString(Data, new Font("Arial", 8));
                g.DrawString(Data, new Font("Arial", 8), Brushes.Black, new Point(img.Width - (int)dataLength.Width - 100, 20));
            }
            e.Graphics.DrawImage(img, new Point(0, 0));
        }

        if (index++ < Count)
            e.HasMorePages = true;
    }
}
