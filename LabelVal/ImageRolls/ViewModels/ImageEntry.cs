using CommunityToolkit.Mvvm.ComponentModel;
using LabelVal.Utilities;
using LibImageUtilities.ImageTypes;
using Newtonsoft.Json;
using System.Drawing.Printing;
using System.IO;
using System.Windows.Media.Imaging;

namespace LabelVal.ImageRolls.ViewModels;

[JsonObject(MemberSerialization.OptIn)]
public partial class ImageEntry : ObservableObject
{
    public string Serialize => JsonConvert.SerializeObject(this);

    [JsonProperty]
    public string Name
    {
        get => string.IsNullOrEmpty(name) ? System.IO.Path.GetFileNameWithoutExtension(Path) : name;
        set => SetProperty(ref name, value);
    }
    private string name;

    [ObservableProperty][property: JsonProperty] private int order = -1;
    [JsonProperty] public bool IsPlaceholder { get; set; }
    [property: SQLite.Ignore] public object NewData { get; set; }

    [JsonProperty] public string Path { get; set; }
    [JsonProperty] public string Comment { get; set; }

    private byte[] OriginalImage { get; set; }

    [property: SQLite.Ignore] public BitmapImage Image { get; private set; }
    [property: SQLite.Ignore] public BitmapImage ImageLow { get; private set; }

    [JsonProperty][SQLite.PrimaryKey] public string UID { get; set; }
    [JsonProperty] public string RollUID { get; set; }

    [JsonProperty]
    public byte[] ImageBytes
    {
        get => OriginalImage;
        set
        {
            OriginalImage = value;

            Image = LibImageUtilities.BitmapImage.CreateBitmapImage(OriginalImage);
            ImageLow = LibImageUtilities.BitmapImage.CreateBitmapImage(OriginalImage, 400);
        }
    }

    public byte[] BitmapBytes => LibImageUtilities.ImageTypes.Bmp.Utilities.GetBmp(OriginalImage, (int)Image.DpiX);

    [ObservableProperty][property: JsonProperty] private int targetDpiWidth;
    [ObservableProperty][property: JsonProperty] private int targetDpiHeight;

    [JsonProperty] public double ImageWidth { get; set; }
    [JsonProperty] public double ImageHeight { get; set; }
    [JsonProperty] public long ImageTotalPixels { get; set; }
    public int ImageBitDepth => Image.Format.BitsPerPixel;

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
        ImageBytes = File.ReadAllBytes(path);
        UID = ImageUtilities.GetImageUID(ImageBytes);

        RollUID = rollUID;

        Path = path;

        TargetDpiHeight = targetDpiHeight;
        TargetDpiWidth = targetDpiWidth;

        string cmt = Path.Replace(System.IO.Path.GetExtension(Path), ".txt");
        if (File.Exists(cmt))
            Comment = File.ReadAllText(cmt);

        ImageWidth = Math.Round(Image.PixelWidth / Image.DpiX, 2);
        ImageHeight = Math.Round(Image.PixelHeight / Image.DpiY, 2);
        ImageTotalPixels = Image.PixelWidth * Image.PixelHeight;
    }

    public ImageEntry(string rollUID, byte[] image, int targetDpiWidth, int targetDpiHeight = 0, string comment = null)
    {
        ImageBytes = image;
        UID = ImageUtilities.GetImageUID(ImageBytes);

        RollUID = rollUID;

        Comment = comment;

        TargetDpiWidth = targetDpiWidth;
        TargetDpiHeight = targetDpiHeight != 0 ? targetDpiHeight : targetDpiWidth;

        ImageWidth = Math.Round(Image.PixelWidth / Image.DpiX, 2);
        ImageHeight = Math.Round(Image.PixelHeight / Image.DpiY, 2);
        ImageTotalPixels = Image.PixelWidth * Image.PixelHeight;
    }

    public ImageEntry Clone() => new(RollUID, ImageBytes, TargetDpiWidth, TargetDpiHeight, Comment);

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
}
