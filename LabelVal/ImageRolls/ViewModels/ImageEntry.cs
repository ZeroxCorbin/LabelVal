using CommunityToolkit.Mvvm.ComponentModel;
using LabelVal.Utilities;
using Newtonsoft.Json;
using System;
using System.Drawing.Printing;
using System.IO;
using System.Windows.Media.Imaging;

namespace LabelVal.ImageRolls.ViewModels;

[JsonObject(MemberSerialization.OptIn)]
public partial class ImageEntry : ObservableObject
{
    public string ToJSON() => JsonConvert.SerializeObject(this);

    [JsonProperty]
    public string Name
    {
        get => string.IsNullOrEmpty(name) ? System.IO.Path.GetFileNameWithoutExtension(Path) : name;
        set => SetProperty(ref name, value);
    }
    private string name;

    [ObservableProperty][property: JsonProperty] private int order = -1;
    [JsonProperty] public bool IsPlaceholder { get; set; }
    [property: SQLite.Ignore] public string NewType { get; set; }

    [JsonProperty] public string Path { get; set; }
    [JsonProperty] public string Comment { get; set; }

    [property: SQLite.Ignore] public BitmapImage Image { get; private set; }
    [property: SQLite.Ignore] public BitmapImage ImageLow { get; private set; }

    [JsonProperty][SQLite.PrimaryKey] public string UID { get; set; }
    [JsonProperty] public string RollUID { get; set; }

    [JsonProperty]
    public byte[] ImageBytes
    {
        get => GetPngBytes();
        set
        {
            Image = BitmapImageUtilities.CreateBitmapImage(value);
            ImageLow = BitmapImageUtilities.CreateBitmapImage(value, 400);

            //UID = BitmapImageUtilities.ImageUID(Image);
        }
    }

    [ObservableProperty][property: JsonProperty] private int targetDpiWidth;
    [ObservableProperty][property: JsonProperty] private int targetDpiHeight;

    [JsonProperty] public double ImageWidth { get; set; }
    [JsonProperty] public double ImageHeight { get; set; }
    [JsonProperty] public long ImageTotalPixels { get; set; }

    [ObservableProperty][property: SQLite.Ignore] private double v52ImageTotalPixelDeviation;

    [ObservableProperty][property: SQLite.Ignore] private double printerWidth;
    [ObservableProperty][property: SQLite.Ignore] private double printerHeight;

    [ObservableProperty][property: SQLite.Ignore] private int printerPixelWidth;
    [ObservableProperty][property: SQLite.Ignore] private int printerPixelHeight;
    [ObservableProperty][property: SQLite.Ignore] private long printerTotalPixels;

    [ObservableProperty][property: SQLite.Ignore] private double deviationWidth;
    [ObservableProperty][property: SQLite.Ignore] private double deviationHeight;
    [ObservableProperty][property: SQLite.Ignore] private double deviationWidthPercent;
    [ObservableProperty][property: SQLite.Ignore] private double deviationHeightPercent;

    [ObservableProperty][property: SQLite.Ignore] private double printer2ImageTotalPixelDeviation;

    public ImageEntry() { }
    public ImageEntry(string rollUID, string path, int targetDpiWidth, int targetDpiHeight)
    {
        RollUID = rollUID;

        Path = path;
        TargetDpiHeight = targetDpiHeight;
        TargetDpiWidth = targetDpiWidth;

        Image = BitmapImageUtilities.LoadBitmapImage(Path);
        ImageLow = BitmapImageUtilities.LoadBitmapImage(Path, 400);
        UID = BitmapImageUtilities.ImageUID(Image);

        string cmt = Path.Replace(System.IO.Path.GetExtension(Path), ".txt");
        if (File.Exists(cmt))
            Comment = File.ReadAllText(cmt);

        ImageWidth = Math.Round(Image.PixelWidth / Image.DpiX, 2);
        ImageHeight = Math.Round(Image.PixelHeight / Image.DpiY, 2);
        ImageTotalPixels = Image.PixelWidth * Image.PixelHeight;
    }
    //This should only be used for a placeholder image
    public ImageEntry(string rollUID, byte[] placeholderImage)
    {
        Image = BitmapImageUtilities.CreateBitmapImage(placeholderImage);
        UID = ImageUtilities.ImageUID(placeholderImage);
        RollUID = rollUID;
        IsPlaceholder = true;
    }
    public ImageEntry(string rollUID, byte[] image, int targetDpiWidth, int targetDpiHeight = 0, string comment = null)
    {
        TargetDpiWidth = targetDpiWidth;
        TargetDpiHeight = targetDpiHeight != 0 ? targetDpiHeight : targetDpiWidth;

        Image = BitmapImageUtilities.CreateBitmapImage(image);
        ImageLow = BitmapImageUtilities.CreateBitmapImage(image, 400);
        UID = ImageUtilities.ImageUID(image);
        RollUID = rollUID;

        Comment = comment;

        ImageWidth = Math.Round(Image.PixelWidth / Image.DpiX, 2);
        ImageHeight = Math.Round(Image.PixelHeight / Image.DpiY, 2);
        ImageTotalPixels = Image.PixelWidth * Image.PixelHeight;
    }

    public ImageEntry Clone() => new(RollUID, GetBitmapBytes(), TargetDpiWidth, TargetDpiHeight, Comment);

    public void InitPrinterVariables(PrinterSettings printer)
    {
        PrinterWidth = Math.Round(printer.DefaultPageSettings.PrintableArea.Width / 100, 2);
        PrinterHeight = Math.Round(printer.DefaultPageSettings.PrintableArea.Height / 100, 2);

        PrinterPixelWidth = (int)Math.Round(printer.DefaultPageSettings.PrintableArea.Width / 100 * printer.DefaultPageSettings.PrinterResolution.X, 0);
        PrinterPixelHeight = (int)Math.Round(printer.DefaultPageSettings.PrintableArea.Height / 100 * printer.DefaultPageSettings.PrinterResolution.Y, 0);
        PrinterTotalPixels = PrinterPixelWidth * PrinterPixelHeight;

        DeviationWidth = PrinterPixelWidth - Image.PixelWidth;
        DeviationHeight = PrinterPixelHeight - Image.PixelHeight;
        DeviationWidthPercent = Math.Round(DeviationWidth / Image.PixelWidth * 100, 2);
        DeviationHeightPercent = Math.Round(DeviationHeight / Image.PixelHeight * 100, 2);

        Printer2ImageTotalPixelDeviation = PrinterTotalPixels - ImageTotalPixels;

        V52ImageTotalPixelDeviation = 5488640 - ImageTotalPixels;
    }

    public byte[] GetBitmapBytes(int dpi = 0)
    {
        if (Image == null)
            return null;

        int tDpi = dpi == 0 ? TargetDpiWidth : dpi;

        byte[] bmp = BitmapImageUtilities.ImageToBytes(Image, false);

        if (dpi != 0)
            ImageUtilities.SetBitmapDPI(bmp, (int)Image.DpiX);

        return bmp;
    }

    public byte[] GetPngBytes() => Image != null ? BitmapImageUtilities.ImageToBytes(Image) : null;
}
