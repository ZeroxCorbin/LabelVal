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

    [ObservableProperty] int pixelWidth;
    [ObservableProperty] int pixelHeight;

    [ObservableProperty] double pageWidth;
    [ObservableProperty] double pageHeight;

    [ObservableProperty] int targetDpiWidth;
    [ObservableProperty] int targetDpiHeight;

    public int DpiWidth => (int)Math.Round(PixelWidth / (PageWidth / 100));
    public int DpiHeight => (int)Math.Round(PixelHeight / (PageHeight / 100));

    public string UID { get; }

    [ObservableProperty] PrinterSettings selectedPrinter;
    partial void OnSelectedPrinterChanged(PrinterSettings value) => throw new System.NotImplementedException();

    public ImageEntry() { }  

    public ImageEntry(string path, int targetDpiWidth, int targetDpiHeight)
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
    }
    
    public ImageEntry(byte[] image, string comment, double pageWidth, double pageHeight, int targetDpiWidth, int targetDpiHeight)
    {
        Image = BitmapImageUtilities.CreateBitmap(image);
        ImageLow = BitmapImageUtilities.CreateBitmap(image, 400);
        UID = ImageUtilities.ImageUID(image);

        Comment = comment;
        PageWidth = pageWidth;
        PageHeight = pageHeight;
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
