using BarcodeVerification.lib.Common;
using CommunityToolkit.Mvvm.ComponentModel;
using LabelVal.Utilities;
using Newtonsoft.Json;
using SQLite;
using System.ComponentModel;
using System.Drawing.Printing;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Media.Imaging;

namespace LabelVal.ImageRolls.ViewModels;

/// <summary>
/// Represents a single image entry within an image roll.
/// It handles image data, properties, and interactions.
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
public partial class ImageEntry : ObservableObject
{
    #region Events

    /// <summary>
    /// Occurs when a save operation is requested for this image entry.
    /// </summary>
    public event Action<ImageEntry> SaveRequested;

    #endregion

    #region Backing Fields

    private string name;
    private BitmapImage _image;

    #endregion

    #region Properties

    /// <summary>
    /// Gets a unique identifier for the image based on its content.
    /// </summary>
    public static string GetUID(byte[] bytes) => BitConverter.ToString(SHA256.HashData(bytes)).Replace("-", string.Empty);

    /// <summary>
    /// The unique identifier for the image.
    /// </summary>
    [JsonProperty]
    [Indexed(Name = "CompositeKey", Order = 1, Unique = true)]
    public string UID { get; set; }

    /// <summary>
    /// The unique identifier of the roll this image belongs to.
    /// </summary>
    [JsonProperty]
    [Indexed(Name = "CompositeKey", Order = 2, Unique = true)]
    public string RollUID { get; set; }

    /// <summary>
    /// Gets or sets the name of the image.
    /// </summary>
    [JsonProperty]
    public string Name
    {
        get => name;
        set
        {
            if (SetProperty(ref name, value))
                SaveRequested?.Invoke(this);
        }
    }

    /// <summary>
    /// Gets or sets the display order of the image within the roll.
    /// </summary>
    [ObservableProperty]
    [property: JsonProperty]
    private int order = -1;
    partial void OnOrderChanged(int value) => SaveRequested?.Invoke(this);

    /// <summary>
    /// Gets or sets a value indicating whether this is a placeholder image.
    /// </summary>
    [JsonProperty]
    public bool IsPlaceholder { get; set; } = false;

    /// <summary>
    /// Holds new data for the image, typically from a device, before processing.
    /// </summary>
    [Ignore]
    public object NewData { get; set; }

    /// <summary>
    /// The file path of the image, if sourced from a file.
    /// </summary>
    [JsonProperty]
    public string Path { get; set; }

    /// <summary>
    /// Gets or sets a comment for the image.
    /// </summary>
    [JsonProperty]
    public string Comment { get; set; }

    /// <summary>
    /// The raw, original image data.
    /// </summary>
    private byte[] OriginalImage { get; set; }

    /// <summary>
    /// Gets the full-resolution BitmapImage. It is loaded on-demand.
    /// </summary>
    [Ignore]
    public BitmapImage Image
    {
        get
        {
            if (_image == null)
            {
                if (OriginalImage != null)
                {
                    _image = BitmapHelpers.CreateBitmapImage(OriginalImage);
                }
                else if (File.Exists(Path))
                {
                    _image = BitmapHelpers.LoadBitmapImage(Path);
                }
            }
            return _image;
        }
        set => SetProperty(ref _image, value);
    }

    /// <summary>
    /// Gets the low-resolution BitmapImage for quick display.
    /// </summary>
    [Ignore]
    public BitmapImage ImageLow { get; set; }

    /// <summary>
    /// Gets or sets the raw byte data of the image, used for serialization.
    /// </summary>
    [JsonProperty]
    public byte[] ImageBytes
    {
        get => OriginalImage;
        set
        {
            OriginalImage = value;
            if (OriginalImage != null)
            {
                ImageLow = BitmapHelpers.CreateBitmapImage(OriginalImage, decodePixelWidth: 200);
                (ImageWidth, ImageHeight) = BitmapHelpers.GetImageDimensions(OriginalImage);
                ImageTotalPixels = (long)ImageWidth * (long)ImageHeight;
            }
        }
    }

    /// <summary>
    /// Gets the image data as a byte array, suitable for saving.
    /// </summary>
    [Ignore]
    public byte[] BitmapBytes => BitmapHelpers.ImageToBytes(Image);

    /// <summary>
    /// Gets or sets the DPI of the image along the X-axis.
    /// </summary>
    [ObservableProperty]
    private double imageDpiX;

    /// <summary>
    /// Gets or sets the DPI of the image along the Y-axis.
    /// </summary>
    [ObservableProperty]
    private double imageDpiY;

    /// <summary>
    /// The width of the image in pixels.
    /// </summary>
    [JsonProperty]
    public double ImageWidth;

    /// <summary>
    /// The height of the image in pixels.
    /// </summary>
    [JsonProperty]
    public double ImageHeight;

    /// <summary>
    /// Gets the width of the image in inches.
    /// </summary>
    [Ignore]
    public double ImageInchesWidth => ImageDpiX > 0 ? ImageWidth / ImageDpiX : 0;

    /// <summary>
    /// Gets the height of the image in inches.
    /// </summary>
    [Ignore]
    public double ImageInchesHeight => ImageDpiY > 0 ? ImageHeight / ImageDpiY : 0;

    /// <summary>
    /// The total number of pixels in the image.
    /// </summary>
    [JsonProperty]
    public long ImageTotalPixels;

    /// <summary>
    /// The bit depth of the image.
    /// </summary>
    [JsonProperty]
    public int ImageBitDepth;

    /// <summary>
    /// The pixel format of the image.
    /// </summary>
    [JsonProperty]
    public System.Windows.Media.PixelFormat ImagePixelFormat;

    #region Printer Specific Properties

    [ObservableProperty]
    [property: Ignore]
    private double v52ImageTotalPixelDeviation;

    [ObservableProperty]
    [property: Ignore]
    private double printerWidth;

    [ObservableProperty]
    [property: Ignore]
    private double printerHeight;

    [ObservableProperty]
    [property: Ignore]
    private int printerPixelWidth;

    [ObservableProperty]
    [property: Ignore]
    private int printerPixelHeight;

    [ObservableProperty]
    [property: Ignore]
    private long printerTotalPixels;

    [ObservableProperty]
    [property: Ignore]
    private double deviationWidth;

    [ObservableProperty]
    [property: Ignore]
    private double deviationHeight;

    [ObservableProperty]
    [property: Ignore]
    private double deviationWidthPercent;

    [ObservableProperty]
    [property: Ignore]
    private double deviationHeightPercent;

    [ObservableProperty]
    [property: Ignore]
    private double printer2ImageTotalPixelDeviation;

    #endregion

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageEntry"/> class.
    /// Required for deserialization.
    /// </summary>
    public ImageEntry() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageEntry"/> class from a file path.
    /// </summary>
    /// <param name="rollUID">The UID of the parent image roll.</param>
    /// <param name="path">The path to the image file.</param>
    public ImageEntry(string rollUID, string path)
    {
        RollUID = rollUID;
        Path = path;
        Name = System.IO.Path.GetFileNameWithoutExtension(path);
        OriginalImage = File.ReadAllBytes(path);
        UID = GetUID(OriginalImage);
        ImageLow = BitmapHelpers.LoadBitmapImage(path, 200);
        (ImageWidth, ImageHeight) = BitmapHelpers.GetImageDimensions(OriginalImage);
        ImageTotalPixels = (long)ImageWidth * (long)ImageHeight;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageEntry"/> class from a byte array.
    /// </summary>
    /// <param name="rollUID">The UID of the parent image roll.</param>
    /// <param name="image">The raw image data.</param>
    /// <param name="imageDpi">The DPI of the image.</param>
    /// <param name="comment">An optional comment.</param>
    public ImageEntry(string rollUID, byte[] image, int imageDpi = 0, string comment = null)
    {
        RollUID = rollUID;
        OriginalImage = image;
        UID = GetUID(OriginalImage);
        ImageLow = BitmapHelpers.CreateBitmapImage(OriginalImage, decodePixelWidth: 200);
        (ImageWidth, ImageHeight) = BitmapHelpers.GetImageDimensions(OriginalImage);
        ImageTotalPixels = (long)ImageWidth * (long)ImageHeight;
        Comment = comment;
        if (imageDpi > 0)
            ImageDpiX = imageDpi;
    }

    #endregion

    /// <summary>
    /// Creates a shallow copy of the current <see cref="ImageEntry"/>.
    /// </summary>
    /// <returns>A new <see cref="ImageEntry"/> instance with the same values.</returns>
    public ImageEntry Clone() => (ImageEntry)MemberwiseClone();

    /// <summary>
    /// Initializes printer-related properties based on the provided printer settings.
    /// </summary>
    /// <param name="printer">The printer settings to use.</param>
    public void InitPrinterVariables(PrinterSettings printer)
    {
        if (printer == null) return;

        PrinterWidth = printer.DefaultPageSettings.PaperSize.Width / 100.0;
        PrinterHeight = printer.DefaultPageSettings.PaperSize.Height / 100.0;
        PrinterPixelWidth = printer.DefaultPageSettings.PaperSize.Width * printer.DefaultPageSettings.PrinterResolution.X / 100;
        PrinterPixelHeight = printer.DefaultPageSettings.PaperSize.Height * printer.DefaultPageSettings.PrinterResolution.Y / 100;
        PrinterTotalPixels = (long)PrinterPixelWidth * PrinterPixelHeight;

        DeviationWidth = ImageWidth - PrinterPixelWidth;
        DeviationHeight = ImageHeight - PrinterPixelHeight;
        DeviationWidthPercent = ImageWidth / PrinterPixelWidth;
        DeviationHeightPercent = ImageHeight / PrinterPixelHeight;

        //V52ImageTotalPixelDeviation = ImageTotalPixels - GlobalAppSettings.Instance.V5TotalPixels;
        Printer2ImageTotalPixelDeviation = ImageTotalPixels - PrinterTotalPixels;
    }
}