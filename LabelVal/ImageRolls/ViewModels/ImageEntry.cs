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

    public double ImageWidth => Math.Round(Image.PixelWidth / Image.DpiX, 2);
    public double ImageHeight => Math.Round(Image.PixelHeight / Image.DpiY, 2);
    public long ImageTotalPixels => Image.PixelWidth * Image.PixelHeight;

    public double PrinterWidth => Math.Round(SelectedPrinter.DefaultPageSettings.PrintableArea.Width / 100, 2);
    public double PrinterHeight => Math.Round(SelectedPrinter.DefaultPageSettings.PrintableArea.Height / 100, 2);

    public int PrinterPixelWidth => (int)Math.Round((SelectedPrinter.DefaultPageSettings.PrintableArea.Width / 100) * SelectedPrinter.DefaultPageSettings.PrinterResolution.X, 0);
    public int PrinterPixelHeight => (int)Math.Round((SelectedPrinter.DefaultPageSettings.PrintableArea.Height / 100) * SelectedPrinter.DefaultPageSettings.PrinterResolution.Y, 0);
    public long PrinterTotalPixels => PrinterPixelWidth * PrinterPixelHeight;

    public double DeviationWidth => PrinterPixelWidth - Image.PixelWidth;
    public double DeviationHeight => PrinterPixelHeight - Image.PixelHeight;

    public double DeviationWidthPercent => Math.Round((DeviationWidth / Image.PixelWidth) * 100, 2);
    public double DeviationHeightPercent => Math.Round((DeviationHeight / Image.PixelHeight) * 100, 2);

    public double Printer2ImageTotalPixelDeviation => PrinterTotalPixels - ImageTotalPixels;

    public double V52ImageTotalPixelDeviation => 5488640 - ImageTotalPixels;

    public string UID { get; }


    [ObservableProperty] PrinterSettings selectedPrinter;
    partial void OnSelectedPrinterChanged(PrinterSettings value)
    {
        OnPropertyChanged(nameof(PrinterWidth));
        OnPropertyChanged(nameof(PrinterHeight));

        OnPropertyChanged(nameof(PrinterPixelWidth)); 
        OnPropertyChanged(nameof(PrinterPixelHeight));
        OnPropertyChanged(nameof(PrinterTotalPixels));

        OnPropertyChanged(nameof(DeviationWidth));
        OnPropertyChanged(nameof(DeviationHeight));
    }

    public ImageEntry() { IsActive = true; }  

    public ImageEntry(string path, int targetDpiWidth, int targetDpiHeight, PrinterSettings selectedPrinter)
    {
        SelectedPrinter = selectedPrinter;

        Path = path;
        TargetDpiHeight = targetDpiHeight;
        TargetDpiWidth = targetDpiWidth;

        Image = BitmapImageUtilities.LoadBitmap(Path);
        ImageLow = BitmapImageUtilities.LoadBitmap(Path, 400);
        UID = BitmapImageUtilities.ImageUID(Image);

        var cmt = Path.Replace(System.IO.Path.GetExtension(Path), ".txt");
        if (File.Exists(cmt))
            Comment = File.ReadAllText(cmt);
        
        IsActive = true;
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
