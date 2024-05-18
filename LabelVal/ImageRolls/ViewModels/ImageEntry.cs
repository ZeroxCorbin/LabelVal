using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using LabelVal.Messages;
using LabelVal.Utilities;
using System;
using System.Drawing.Printing;
using System.IO;
using System.Windows.Media.Imaging;

namespace LabelVal.ImageRolls.ViewModels;
public partial class ImageEntry : ObservableRecipient, IRecipient<PrinterMessages.SelectedPrinterChanged>
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    public string Name
    {
        get => string.IsNullOrEmpty(name) ? System.IO.Path.GetFileNameWithoutExtension(Path) : name;
        set => SetProperty(ref name, value);
    }
    private string name;

    public string Path { get; }
    public BitmapImage Image { get; }
    public BitmapImage ImageLow { get; }
    public string Comment { get; }

    [ObservableProperty] int targetDpiWidth;
    [ObservableProperty] int targetDpiHeight;

    public double ImageWidth { get; }
    public double ImageHeight { get; }
    public long ImageTotalPixels { get; }
    public double V52ImageTotalPixelDeviation { get; private set; }

    [ObservableProperty] private double printerWidth;
    [ObservableProperty] private double printerHeight;

    [ObservableProperty] private int printerPixelWidth;
    [ObservableProperty] private int printerPixelHeight;
    [ObservableProperty] private long printerTotalPixels;

    [ObservableProperty] private double deviationWidth;
    [ObservableProperty] private double deviationHeight;
    [ObservableProperty] private double deviationWidthPercent;
    [ObservableProperty] private double deviationHeightPercent;

    [ObservableProperty] private double printer2ImageTotalPixelDeviation;


    public string UID { get; }


    [ObservableProperty] PrinterSettings selectedPrinter;
    partial void OnSelectedPrinterChanged(PrinterSettings value) => InitPrinterVariables();

    public ImageEntry() { IsActive = true; }  

    public ImageEntry(string path, int targetDpiWidth, int targetDpiHeight, PrinterSettings selectedPrinter)
    {
        Path = path;
        TargetDpiHeight = targetDpiHeight;
        TargetDpiWidth = targetDpiWidth;

        Image = BitmapImageUtilities.LoadBitmap(Path);
        ImageLow = BitmapImageUtilities.LoadBitmap(Path, 400);
        UID = BitmapImageUtilities.ImageUID(Image);

        var cmt = Path.Replace(System.IO.Path.GetExtension(Path), ".txt");
        if (File.Exists(cmt))
            Comment = File.ReadAllText(cmt);

        ImageWidth = Math.Round(Image.PixelWidth / Image.DpiX, 2);
        ImageHeight = Math.Round(Image.PixelHeight / Image.DpiY, 2);
        ImageTotalPixels = Image.PixelWidth * Image.PixelHeight;

        V52ImageTotalPixelDeviation = 5488640 - ImageTotalPixels;

        SelectedPrinter = selectedPrinter;

        IsActive = true;
    }

    private void InitPrinterVariables()
    {
        PrinterWidth = Math.Round(SelectedPrinter.DefaultPageSettings.PrintableArea.Width / 100, 2);
        PrinterHeight = Math.Round(SelectedPrinter.DefaultPageSettings.PrintableArea.Height / 100, 2);    

        PrinterPixelWidth = (int)Math.Round((SelectedPrinter.DefaultPageSettings.PrintableArea.Width / 100) * SelectedPrinter.DefaultPageSettings.PrinterResolution.X, 0);
        PrinterPixelHeight = (int)Math.Round((SelectedPrinter.DefaultPageSettings.PrintableArea.Height / 100) * SelectedPrinter.DefaultPageSettings.PrinterResolution.Y, 0);
        PrinterTotalPixels = PrinterPixelWidth * PrinterPixelHeight;

        DeviationWidth = PrinterPixelWidth - Image.PixelWidth;
        DeviationHeight = PrinterPixelHeight - Image.PixelHeight;
        DeviationWidthPercent = Math.Round((DeviationWidth / Image.PixelWidth) * 100, 2);
        DeviationHeightPercent = Math.Round((DeviationHeight / Image.PixelHeight) * 100, 2);

        Printer2ImageTotalPixelDeviation = PrinterTotalPixels - ImageTotalPixels;
    }
    
    public ImageEntry(byte[] image, string comment, int targetDpiWidth, int targetDpiHeight, PrinterSettings selectedPrinter)
    {
        SelectedPrinter = selectedPrinter;

        Image = BitmapImageUtilities.CreateBitmap(image);
        ImageLow = BitmapImageUtilities.CreateBitmap(image, 400);
        UID = ImageUtilities.ImageUID(image);

        Comment = comment;
        TargetDpiWidth = targetDpiWidth;
        TargetDpiHeight = targetDpiHeight;
    }

    public void Receive(PrinterMessages.SelectedPrinterChanged message) => SelectedPrinter = message.Value;

   //public byte[] GetBitmapBytes() => BitmapImageUtilities.ImageToBytesBMP(Image);

    public byte[] GetBitmapBytes(int dpi = 0)
    {
        var tDpi = dpi == 0 ? TargetDpiWidth : dpi;

        var bmp = BitmapImageUtilities.ImageToBytesBMP(Image);

        ImageUtilities.SetBitmapDPI(bmp, tDpi);

        return bmp;
    }
}
