using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using LabelVal.Messages;
using LabelVal.Utilities;
using Newtonsoft.Json;
using System;
using System.Drawing.Printing;
using System.IO;
using System.Windows.Media.Imaging;

namespace LabelVal.ImageRolls.ViewModels;

[JsonObject(MemberSerialization.OptIn)]
public partial class ImageEntry : ObservableRecipient, IRecipient<PrinterMessages.SelectedPrinterChanged>
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    [JsonProperty]
    public string Name
    {
        get => string.IsNullOrEmpty(name) ? System.IO.Path.GetFileNameWithoutExtension(Path) : name;
        set => SetProperty(ref name, value);
    }
    private string name;


    [JsonProperty] public string Path { get; private set; }
    [JsonProperty] public string Comment { get; private set; }

    public BitmapImage Image { get; private set; }
    public BitmapImage ImageLow { get; private set; }

    [JsonProperty] public string UID { get; private set; }

    [JsonProperty]
    public byte[] ImageBytes
    {
        get => GetPngBytes();
        set
        {
            Image = BitmapImageUtilities.CreateBitmap(value);
            ImageLow = BitmapImageUtilities.CreateBitmap(value, 400);

            UID = BitmapImageUtilities.ImageUID(Image);
        }
    }

    [ObservableProperty][property: JsonProperty] int targetDpiWidth;
    [ObservableProperty][property: JsonProperty] int targetDpiHeight;

    [JsonProperty] public double ImageWidth { get; private set; }
    [JsonProperty] public double ImageHeight { get; private set; }
    [JsonProperty] public long ImageTotalPixels { get; private set; }
    [JsonProperty] public double V52ImageTotalPixelDeviation { get; private set; }


    [ObservableProperty] PrinterSettings selectedPrinter;
    partial void OnSelectedPrinterChanged(PrinterSettings value) => InitPrinterVariables();

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

    public ImageEntry(byte[] image, int targetDpiWidth, PrinterSettings selectedPrinter, int targetDpiHeight = 0, string comment = null)
    {
        TargetDpiWidth = targetDpiWidth;
        TargetDpiHeight = targetDpiHeight != 0 ? targetDpiHeight : targetDpiWidth;

        Image = BitmapImageUtilities.CreateBitmap(image);
        ImageLow = BitmapImageUtilities.CreateBitmap(image, 400);
        UID = ImageUtilities.ImageUID(image);

        Comment = comment;

        ImageWidth = Math.Round(Image.PixelWidth / Image.DpiX, 2);
        ImageHeight = Math.Round(Image.PixelHeight / Image.DpiY, 2);
        ImageTotalPixels = Image.PixelWidth * Image.PixelHeight;

        V52ImageTotalPixelDeviation = 5488640 - ImageTotalPixels;

        SelectedPrinter = selectedPrinter;

        IsActive = true;
    }

    public ImageEntry Clone() => new ImageEntry(GetBitmapBytes(), TargetDpiWidth, SelectedPrinter, TargetDpiHeight, Comment);

    public void Receive(PrinterMessages.SelectedPrinterChanged message) => SelectedPrinter = message.Value;

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


    public byte[] GetBitmapBytes(int dpi = 0)
    {
        var tDpi = dpi == 0 ? TargetDpiWidth : dpi;

        var bmp = BitmapImageUtilities.ImageToBytesBMP(Image);

        ImageUtilities.SetBitmapDPI(bmp, tDpi);

        return bmp;
    }

    public byte[] GetPngBytes()
    {
        var bmp = BitmapImageUtilities.ImageToBytesPNG(Image);
        return bmp;
    }
}
