using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using SQLite;
using System.Drawing.Printing;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LabelVal.ImageRolls.Databases;

[JsonObject(MemberSerialization.OptIn)]
public partial class ImageEntry : ObservableObject
{
    public event Action<ImageEntry> SaveRequested;

    public string Serialize => JsonConvert.SerializeObject(this);

    public static string GetUID(byte[] bytes) => BitConverter.ToString(SHA256.HashData(bytes)).Replace("-", string.Empty);

    [JsonProperty]
    [Indexed(Name = "CompositeKey", Order = 1, Unique = true)]
    public string UID { get; set; }

    [JsonProperty]
    [Indexed(Name = "CompositeKey", Order = 2, Unique = true)]
    public string RollUID { get; set; }

    [JsonProperty]
    public string Name
    {
        get => string.IsNullOrEmpty(name) ? System.IO.Path.GetFileNameWithoutExtension(Path) : name;
        set => SetProperty(ref name, value);
    }
    private string name;

    [ObservableProperty][property: JsonProperty] private int order = -1;
    partial void OnOrderChanged(int value)
    {
        SaveRequested?.Invoke(this);
    }
    [JsonProperty] public bool IsPlaceholder { get; set; }
    [property: SQLite.Ignore] public object NewData { get; set; }

    [JsonProperty] public string Path { get; set; }
    [JsonProperty] public string Comment { get; set; }

    private byte[] OriginalImage { get; set; }

    [property: SQLite.Ignore] public BitmapImage Image { get; private set; }
    [property: SQLite.Ignore] public BitmapImage ImageLow { get; private set; }

    [JsonProperty]
    public byte[] ImageBytes
    {
        get => OriginalImage;
        set
        {
            OriginalImage = value;

            Image = ImageUtilities.lib.Wpf.BitmapImage.CreateBitmapImage(OriginalImage, asPng: true);
            ImageLow = ImageUtilities.lib.Wpf.BitmapImage.CreateBitmapImage(OriginalImage, asPng: true, decodePixelWidth: 400);
        }
    }

    public byte[] BitmapBytes
    {
        get
        {
            using var img = new ImageMagick.MagickImage(OriginalImage);
            return img.ToByteArray(ImageMagick.MagickFormat.Bmp3);
        }
    }

    [ObservableProperty] private double imageDpiX;
    [ObservableProperty] private double imageDpiY;
    [JsonProperty] public double ImageWidth => Math.Round(Image.PixelWidth / Image.DpiX, 2);
    [JsonProperty] public double ImageHeight => Math.Round(Image.PixelHeight / Image.DpiY, 2);
    [JsonProperty] public long ImageTotalPixels => Image.PixelWidth * Image.PixelHeight;
    [JsonProperty] public int ImageBitDepth => Image.Format.BitsPerPixel;
    [JsonProperty] public PixelFormat ImagePixelFormat => Image.Format;

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
    public ImageEntry(string rollUID, string path)
    {
        Path = path;
        using ImageMagick.MagickImage img = new(path);

        ImageBytes = img.ToByteArray(ImageMagick.MagickFormat.Png);

        UID = GetUID(img.GetPixels().ToArray());

        RollUID = rollUID;

        var cmt = Path.Replace(System.IO.Path.GetExtension(Path), ".txt");
        if (File.Exists(cmt))
            Comment = File.ReadAllText(cmt);
    }

    public ImageEntry(string rollUID, byte[] image, int imageDpi = 0, string comment = null)
    {
        using ImageMagick.MagickImage img = new(image);
        if (imageDpi > 0)
        {
            img.Density = new ImageMagick.Density(imageDpi, imageDpi);
            imageDpiX = imageDpi;
            imageDpiY = imageDpi;
        }

        ImageBytes = img.ToByteArray(ImageMagick.MagickFormat.Png);

        UID = GetUID(img.GetPixels().ToArray());

        RollUID = rollUID;

        Comment = comment;


    }

    public ImageEntry Clone() => new(RollUID, ImageBytes, comment: Comment);

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
