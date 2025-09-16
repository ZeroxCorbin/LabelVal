using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibStaticUtilities_IPHostPort;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Media.Imaging;

namespace LabelVal.LabelBuilder.ViewModels;

public partial class DisplayEditorViewModel : ObservableObject
{
    public partial class DisplayEntity : ObservableObject
    {
        [ObservableProperty] private string name;
        [ObservableProperty] private string description;
        [ObservableProperty] private byte[] thumbnail;
        [ObservableProperty] private int size = 512;

        public ObservableCollection<BarcodeEntity> BarcodeEntities { get; set; } = [];

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

        [JsonIgnore]
        public string HTML
        {
            get
            {
                var sb = new StringBuilder();

                _ = sb.AppendLine($"<div class=\"canvas\">");

                foreach (BarcodeEntity entity in BarcodeEntities)
                    _ = sb.AppendLine(entity.HTML);

                _ = sb.AppendLine($"</div>");

                return sb.ToString();
            }
        }
        public DisplayEntity() { }
        public DisplayEntity(string name) => Name = name;

        public void Save() => App.DisplayDatabase.SetValue(Name, this);
        public void Rename(string newName) { _ = App.DisplayDatabase.DeleteSetting(Name); Name = newName; Save(); }
    }

    public partial class BarcodeEntity : ObservableObject
    {
        public partial class CanvasPosition : ObservableObject
        {
            [ObservableProperty] private int top;
            [ObservableProperty] private int left;
            [ObservableProperty] private int zIndex;
            [ObservableProperty] private double scale = 1.0;
        }

        [ObservableProperty] private CanvasPosition position = new();
        [ObservableProperty] private string imagePath;
        [ObservableProperty] private string thumbPath;

        partial void OnImagePathChanged(string value) => Image = CreateBitmap(ImageFileContents);
        partial void OnThumbPathChanged(string value) { OnPropertyChanged(nameof(FileName)); OnPropertyChanged(nameof(ParentDir)); }

        public string FileName => Path.GetFileNameWithoutExtension(ImagePath);
        public string ParentDir => Directory.GetParent(ImagePath).Name;

        [JsonIgnore] public int ID => Position.Top + Position.Left;
        [JsonIgnore] public byte[] ImageFileContents => File.ReadAllBytes(ImagePath);
        [JsonIgnore] public BitmapImage Image { get; private set; }

        [JsonIgnore]
        public string HTMLStyle => $"div.img{ID} {{ position: absolute; top: {Position.Top}px; left: {Position.Left}px; }}\r\n" +
                                  $"img.img{ID} {{ height: {Image.Height * Position.Scale}px; width: {Image.Width * Position.Scale}px; }}";

        [JsonIgnore] public string HTML => $"<div class=\"img{ID}\"><img class=\"img{ID}\" src=\"img{ID}.png\"></div>";

        public BarcodeEntity() { }
        public BarcodeEntity(string path)
        {
            ImagePath = path;
            ThumbPath = Path.Combine(Path.GetDirectoryName(ImagePath), Path.GetFileNameWithoutExtension(ImagePath) + ".thumb");

            //Image = ImageUtilities.CreateBitmap(ImageFileContents);

            if (!File.Exists(ThumbPath))
                _ = CreateThumbnailImage(ImagePath, ThumbPath);
        }

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
        private FileInfo CreateThumbnailImage(string imageFileName, string thumbnailFileName)
        {
            const int thumbnailSize = 150;
            using (var image = System.Drawing.Image.FromFile(imageFileName))
            {
                var imageHeight = Image.Height;
                var imageWidth = Image.Width;
                if (imageHeight > imageWidth)
                {
                    imageWidth = (int)((float)imageWidth / (float)imageHeight * thumbnailSize);
                    imageHeight = thumbnailSize;
                }
                else
                {
                    imageHeight = (int)((float)imageHeight / (float)imageWidth * thumbnailSize);
                    imageWidth = thumbnailSize;
                }

                using Image thumb = image.GetThumbnailImage((int)imageWidth, (int)imageHeight, () => false, IntPtr.Zero);
                thumb.Save(thumbnailFileName);
            }

            return new FileInfo(thumbnailFileName);
        }
    }

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

           // RestartWebServer();
        }
    }
    public int DPS_ListenPort
    {
        get => App.Settings.GetValue("DPS_ListenPort", 10001, true);
        set
        {
            App.Settings.SetValue("DPS_ListenPort", value);
            OnPropertyChanged();

           // RestartWebServer();
        }
    }

    public bool ShowBarcodeBorders
    {
        get => App.Settings.GetValue("DisplayEditorViewModel_ShowBarcodeBorders", true, true);
        set
        {
            App.Settings.SetValue("DisplayEditorViewModel_ShowBarcodeBorders", value);
            OnPropertyChanged();
        }
    }

    public ObservableCollection<string> IPAddresses { get; } = [];

    [ObservableProperty] private string statusMessage;

    public ObservableCollection<BarcodeEntity> BarcodeEntities { get; } = [];

    public ObservableCollection<DisplayEntity> DisplayEntities { get; } = [];

    [ObservableProperty] private DisplayEntity selectedDisplay;
    //partial void OnSelectedDisplayChanged(DisplayEntity value) => WebServerController.Display = value;

    [ObservableProperty] private BarcodeEntity selectedBarcodeEntity;

    //public string WebServerURL => WebServerController.URL;
    //private WebServerController WebServerController { get; set; }

    public DisplayEditorViewModel()
    { LoadBarcodeLibraryFiles(); 
        LoadDisplayEntities(); 
       // RestartWebServer(); 
        //GetLocalIP(); 
    }

    [RelayCommand]
    private void AddBarcodeToCanvas(BarcodeEntity ent) { var entNew = new BarcodeEntity(ent.ImagePath); entNew.Position.Top = SelectedDisplay.BarcodeEntities.Count(); entNew.Position.Left = SelectedDisplay.BarcodeEntities.Count(); SelectedDisplay?.BarcodeEntities.Add(entNew); }

    [RelayCommand]
    private void RemoveBarcodeFromCanvas(BarcodeEntity ent) => SelectedDisplay?.BarcodeEntities.Remove(ent);

    [RelayCommand]
    private void LoadBarcodeLibraryFiles()
    {
        BarcodeEntities.Clear();

        //FindFiles(App.BarcodeLibraryDirectoryPath);
    }
    [RelayCommand]
    private void NewDisplayEntity() => DisplayEntities.Add(new DisplayEntity(DateTime.Now.Ticks.ToString()));
    [RelayCommand]
    private void SaveDisplayEntities()
    {
        foreach (DisplayEntity entity in DisplayEntities)
            App.DisplayDatabase.SetValue(entity.Name, entity);
    }
    [RelayCommand]
    private void RemoveDisplayCanvas(DisplayEntity ent)
    {
        _ = App.DisplayDatabase.DeleteSetting(ent.Name);

        _ = (DisplayEntities?.Remove(ent));
    }
    private void LoadDisplayEntities()
    {
        DisplayEntities.Clear();

        foreach (DisplayEntity dis in App.DisplayDatabase.GetAllValues<DisplayEntity>())
            DisplayEntities.Add(dis);
    }

    //private void RestartWebServer()
    //{
    //    WebServerController?.Stop();
    //    WebServerController = new WebServerController(DPS_ListenIPAddress, DPS_ListenPort)
    //    {
    //        Display = SelectedDisplay
    //    };
    //    WebServerController?.Start();

    //    OnPropertyChanged(nameof(WebServerURL));
    //}
    private void FindFiles(string root)
    {
        foreach (var dir in Directory.EnumerateDirectories(root))
            FindFiles(dir);

        foreach (var file in Directory.EnumerateFiles(root))
            if (Path.GetExtension(file) is ".png" or ".bmp")
                BarcodeEntities.Add(new BarcodeEntity(file));

    }

    private void GetLocalIP()
    {
        IPAddresses.Clear();

        IPAddresses.Add("127.0.0.1");
        foreach (var ip in LibStaticUtilities_IPHostPort.IPHost.GetAllLocalIPv4(System.Net.NetworkInformation.NetworkInterfaceType.Ethernet))
            IPAddresses.Add(ip);
    }
}
