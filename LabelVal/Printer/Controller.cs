using LabelVal.ImageRolls.ViewModels;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;

namespace LabelVal.Printer;

public class Controller
{
    private byte[] Image;
    private int Count;
    private string Data;

    //public void Print(string imagePath, int count, string printerName, string data)
    //{
    //    Image = File.ReadAllBytes(imagePath);
    //    Count = count;
    //    Data = data;

    //    using var pd = new PrintDocument();
    //    pd.PrintPage += PrintPage;

    //    pd.PrinterSettings.PrinterName = printerName;
    //    pd.Print();
    //}

    public void Print(ImageEntry imageEntry, int count, string printerName, string data)
    {
        Image = imageEntry.ImageBytes;
        Count = count;
        Data = data;

        using var pd = new PrintDocument();
        pd.PrintPage += PrintPage;

        pd.PrinterSettings.PrinterName = printerName;
        pd.Print();
    }


    private void PrintPage(object o, PrintPageEventArgs e)
    {
        using (var img = System.Drawing.Image.FromStream(new MemoryStream(Image)))
        {
            //if (!string.IsNullOrEmpty(Data))
            //{

            //    using var g = Graphics.FromImage(img);
            //    var dataLength = g.MeasureString(Data, new Font("Arial", 8));
            //    g.DrawString(Data, new Font("Arial", 8), Brushes.Black, new Point(img.Width - (int)dataLength.Width - 100, 20));
            //}
            e.Graphics.DrawImage(img, new Point(0, 0));
        }

        if (Count-- > 1)
            e.HasMorePages = true;
    }
}
