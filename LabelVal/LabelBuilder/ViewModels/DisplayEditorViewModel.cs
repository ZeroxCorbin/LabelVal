using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibStaticUtilities_IPHostPort;
using Newtonsoft.Json;

namespace LabelVal.LabelBuilder.ViewModels;

/// <summary>
/// ViewModel that manages a collection of user-defined display layouts (DisplayEntity) composed
/// of positioned barcode/image elements (BarcodeEntity). Supports persistence, basic layout construction,
/// IP/port configuration (for an external web server concept), and HTML generation for the current display.
/// </summary>
public partial class DisplayEditorViewModel : ObservableObject
{
    #region Nested Entities

    /// <summary>
    /// Represents a display (canvas) containing multiple placed barcode/image entities.
    /// Provides HTML + CSS aggregation for external rendering / preview.
    /// </summary>
    public partial class DisplayEntity : ObservableObject
    {
        // Source generator creates public properties (Name, Description, Thumbnail, Size) with OnPropertyChanged.
        [ObservableProperty] private string name;
        [ObservableProperty] private string description;
        [ObservableProperty] private byte[] thumbnail;
        [ObservableProperty] private int size = 512; // Canvas width/height (square canvas assumption).

        /// <summary>
        /// Collection of visual elements (barcodes / images) placed on this display.
        /// </summary>
        public ObservableCollection<BarcodeEntity> BarcodeEntities { get; set; } = [];

        /// <summary>
        /// Aggregated CSS for this display and all child entities.
        /// </summary>
        [JsonIgnore]
        public string HTMLStyle
        {
            get
            {
                var sb = new StringBuilder();
                _ = sb.AppendLine($"div.canvas {{ position: relative; width: {Size}px; height: {Size}px; }}");
                foreach (BarcodeEntity entity in BarcodeEntities)
                    _ = sb.AppendLine(entity.HTMLStyle);
                return sb.ToString();
            }
        }

        /// <summary>
        /// Aggregated HTML for the display. Each child entity supplies a <div> wrapper with an <img>.
        /// </summary>
        [JsonIgnore]
        public string HTML
        {
            get
            {
                var sb = new StringBuilder();
                _ = sb.AppendLine("<div class=\"canvas\">");
                foreach (BarcodeEntity entity in BarcodeEntities)
                    _ = sb.AppendLine(entity.HTML);
                _ = sb.AppendLine("</div>");
                return sb.ToString();
            }
        }

        public DisplayEntity() { }
        public DisplayEntity(string name) => Name = name;

        /// <summary>Persists this display in the application display database.</summary>
        public void Save() => App.DisplayDatabase.SetValue(Name, this);

        /// <summary>Renames this display (delete + reinsert).</summary>
        public void Rename(string newName)
        {
            _ = App.DisplayDatabase.DeleteSetting(Name);
            Name = newName;
            Save();
        }
    }

    /// <summary>
    /// Represents a single image (barcode or other) positioned on a display canvas.
    /// Provides HTML + CSS fragments and maintains a WPF <see cref="BitmapImage"/> for UI binding.
    /// </summary>
    public partial class BarcodeEntity : ObservableObject
    {
        /// <summary>
        /// Logical positioning metadata on the display canvas.
        /// </summary>
        public partial class CanvasPosition : ObservableObject
        {
            [ObservableProperty] private int top;
            [ObservableProperty] private int left;
            [ObservableProperty] private int zIndex;
            [ObservableProperty] private double scale = 1.0; // Multiplicative scale factor.
        }

        [ObservableProperty] private CanvasPosition position = new();
        [ObservableProperty] private string imagePath;
        [ObservableProperty] private string thumbPath;

        // When the image path changes, reload the bitmap for UI display.
        partial void OnImagePathChanged(string value) => Image = CreateBitmap(ImageFileContents);

        // Update derived file-related display properties.
        partial void OnThumbPathChanged(string value)
        {
            OnPropertyChanged(nameof(FileName));
            OnPropertyChanged(nameof(ParentDir));
        }

        /// <summary>Base file name (without extension) extracted from ImagePath.</summary>
        public string FileName => Path.GetFileNameWithoutExtension(ImagePath);

        /// <summary>Parent directory name of the image path.</summary>
        public string ParentDir => Directory.GetParent(ImagePath).Name;

        /// <summary>
        /// Naive ID used for HTML/CSS class naming. NOTE: This is not guaranteed unique
        /// (collision possible if different entities share same Top + Left). Consider a GUID if uniqueness matters.
        /// </summary>
        [JsonIgnore] public int ID => Position.Top + Position.Left;

        /// <summary>Raw file bytes of the bound image.</summary>
        [JsonIgnore] public byte[] ImageFileContents => File.ReadAllBytes(ImagePath);

        /// <summary>Loaded WPF bitmap for display binding.</summary>
        [JsonIgnore] public BitmapImage Image { get; private set; }

        /// <summary>CSS snippet for this entity's container and scaled image.</summary>
        [JsonIgnore]
        public string HTMLStyle =>
            $"div.img{ID} {{ position: absolute; top: {Position.Top}px; left: {Position.Left}px; z-index: {Position.ZIndex}; }}\r\n" +
            $"img.img{ID} {{ height: {Image.Height * Position.Scale}px; width: {Image.Width * Position.Scale}px; }}";

        /// <summary>HTML snippet for this entity (container div + img tag).</summary>
        [JsonIgnore]
        public string HTML => $"<div class=\"img{ID}\"><img class=\"img{ID}\" src=\"img{ID}.png\"></div>";

        public BarcodeEntity() { }

        /// <summary>
        /// Initializes with an image path and ensures a thumbnail exists.
        /// </summary>
        public BarcodeEntity(string path)
        {
            ImagePath = path;
            ThumbPath = Path.Combine(Path.GetDirectoryName(ImagePath),
                Path.GetFileNameWithoutExtension(ImagePath) + ".thumb");

            // Load the primary bitmap now.
            Image = CreateBitmap(ImageFileContents);

            if (!File.Exists(ThumbPath))
                _ = CreateThumbnailImage(ImagePath, ThumbPath);
        }

        /// <summary>
        /// Creates a WPF frozen BitmapImage from raw bytes.
        /// </summary>
        private BitmapImage CreateBitmap(byte[] imageData)
        {
            using var ms = new MemoryStream(imageData);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = ms;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }

        /// <summary>
        /// Creates a simple thumbnail file on disk.
        /// </summary>
        private FileInfo CreateThumbnailImage(string imageFileName, string thumbnailFileName)
        {
            const int thumbnailSize = 150;
            using (var image = System.Drawing.Image.FromFile(imageFileName))
            {
                // NOTE: The previous implementation referenced the WPF Image property (possibly null / different dimension source).
                // Using the System.Drawing.Image instance here is intentional.
                var imageHeight = image.Height;
                var imageWidth = image.Width;

                if (imageHeight > imageWidth)
                {
                    imageWidth = (int)(imageWidth / (float)imageHeight * thumbnailSize);
                    imageHeight = thumbnailSize;
                }
                else
                {
                    imageHeight = (int)(imageHeight / (float)imageWidth * thumbnailSize);
                    imageWidth = thumbnailSize;
                }

                using Image thumb = image.GetThumbnailImage(imageWidth, imageHeight, () => false, IntPtr.Zero);
                thumb.Save(thumbnailFileName);
            }

            return new FileInfo(thumbnailFileName);
        }
    }

    #endregion

    #region Settings / Configuration

    /// <summary>
    /// IP address used by the (commented out) web server component. Defaults to first local IPv4 (Ethernet) or 127.0.0.1.
    /// </summary>
    public string DPS_ListenIPAddress
    {
        get
        {
            string val;
            if (!string.IsNullOrEmpty(val = App.Settings.GetValue<string>("DPS_ListenIPAddress", null)))
                return val;

            var first = IPHost.GetAllLocalIPv4(System.Net.NetworkInformation.NetworkInterfaceType.Ethernet).FirstOrDefault();
            return App.Settings.GetValue("DPS_ListenIPAddress", string.IsNullOrEmpty(first) ? "127.0.0.1" : first, true);
        }
        set
        {
            App.Settings.SetValue("DPS_ListenIPAddress", value);
            OnPropertyChanged();
            // Potential restart hook: RestartWebServer();
        }
    }

    /// <summary>
    /// Port number bound by the (inactive) web server concept.
    /// </summary>
    public int DPS_ListenPort
    {
        get => App.Settings.GetValue("DPS_ListenPort", 10001, true);
        set
        {
            App.Settings.SetValue("DPS_ListenPort", value);
            OnPropertyChanged();
            // Potential restart hook: RestartWebServer();
        }
    }

    /// <summary>
    /// Toggles UI visualization aids (e.g., showing bounding boxes around barcode elements).
    /// </summary>
    public bool ShowBarcodeBorders
    {
        get => App.Settings.GetValue("DisplayEditorViewModel_ShowBarcodeBorders", true, true);
        set
        {
            App.Settings.SetValue("DisplayEditorViewModel_ShowBarcodeBorders", value);
            OnPropertyChanged();
        }
    }

    #endregion

    #region Collections & State

    /// <summary>
    /// Local machine IP addresses for user selection (populated via GetLocalIP()).
    /// </summary>
    public ObservableCollection<string> IPAddresses { get; } = [];

    [ObservableProperty] private string statusMessage;

    /// <summary>
    /// Library of discovered barcode/image assets (not yet placed on a display).
    /// </summary>
    public ObservableCollection<BarcodeEntity> BarcodeEntities { get; } = [];

    /// <summary>
    /// Saved / loaded display canvases.
    /// </summary>
    public ObservableCollection<DisplayEntity> DisplayEntities { get; } = [];

    [ObservableProperty] private DisplayEntity selectedDisplay; // Could drive preview / live web server.

    [ObservableProperty] private BarcodeEntity selectedBarcodeEntity;

    #endregion

    #region Constructor / Initialization

    public DisplayEditorViewModel()
    {
        LoadBarcodeLibraryFiles();
        LoadDisplayEntities();
        // RestartWebServer();
        // GetLocalIP();
    }

    #endregion

    #region Commands

    /// <summary>
    /// Adds a new instance of a barcode entity to the currently selected display (basic staggered placement).
    /// </summary>
    [RelayCommand]
    private void AddBarcodeToCanvas(BarcodeEntity ent)
    {
        if (SelectedDisplay is null || ent is null) return;

        var entNew = new BarcodeEntity(ent.ImagePath)
        {
            Position =
            {
                Top = SelectedDisplay.BarcodeEntities.Count,
                Left = SelectedDisplay.BarcodeEntities.Count
            }
        };

        SelectedDisplay.BarcodeEntities.Add(entNew);
    }

    /// <summary>
    /// Removes a barcode entity from the current display.
    /// </summary>
    [RelayCommand]
    private void RemoveBarcodeFromCanvas(BarcodeEntity ent) => SelectedDisplay?.BarcodeEntities.Remove(ent);

    /// <summary>
    /// Loads (or refreshes) the barcode library source list. Currently stubbed (file enumeration disabled).
    /// </summary>
    [RelayCommand]
    private void LoadBarcodeLibraryFiles()
    {
        BarcodeEntities.Clear();
        // Uncomment to scan the configured library path:
        // FindFiles(App.BarcodeLibraryDirectoryPath);
    }

    /// <summary>
    /// Creates a new display with a unique timestamp-based name.
    /// </summary>
    [RelayCommand]
    private void NewDisplayEntity() => DisplayEntities.Add(new DisplayEntity(DateTime.Now.Ticks.ToString()));

    /// <summary>
    /// Persists all current display entities to storage.
    /// </summary>
    [RelayCommand]
    private void SaveDisplayEntities()
    {
        foreach (DisplayEntity entity in DisplayEntities)
            App.DisplayDatabase.SetValue(entity.Name, entity);
    }

    /// <summary>
    /// Removes a display and deletes its persisted representation.
    /// </summary>
    [RelayCommand]
    private void RemoveDisplayCanvas(DisplayEntity ent)
    {
        if (ent is null) return;
        _ = App.DisplayDatabase.DeleteSetting(ent.Name);
        _ = DisplayEntities.Remove(ent);
    }

    #endregion

    #region Private Helpers

    /// <summary>
    /// Loads all persisted display entities from storage.
    /// </summary>
    private void LoadDisplayEntities()
    {
        DisplayEntities.Clear();
        foreach (DisplayEntity dis in App.DisplayDatabase.GetAllValues<DisplayEntity>())
            DisplayEntities.Add(dis);
    }

    // Potential future reactivation of embedded web server:
    // private void RestartWebServer()
    // {
    //     WebServerController?.Stop();
    //     WebServerController = new WebServerController(DPS_ListenIPAddress, DPS_ListenPort)
    //     {
    //         Display = SelectedDisplay
    //     };
    //     WebServerController?.Start();
    //     OnPropertyChanged(nameof(WebServerURL));
    // }

    /// <summary>
    /// Recursively discovers image files (.png/.bmp) and adds them as barcode entities in the library list.
    /// </summary>
    private void FindFiles(string root)
    {
        foreach (var dir in Directory.EnumerateDirectories(root))
            FindFiles(dir);

        foreach (var file in Directory.EnumerateFiles(root))
            if (Path.GetExtension(file) is ".png" or ".bmp")
                BarcodeEntities.Add(new BarcodeEntity(file));
    }

    /// <summary>
    /// Populates local IPv4 addresses (Ethernet) for binding.
    /// </summary>
    private void GetLocalIP()
    {
        IPAddresses.Clear();
        IPAddresses.Add("127.0.0.1");
        foreach (var ip in IPHost.GetAllLocalIPv4(System.Net.NetworkInformation.NetworkInterfaceType.Ethernet))
            IPAddresses.Add(ip);
    }

    #endregion
}
