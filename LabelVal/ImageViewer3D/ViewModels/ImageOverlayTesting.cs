using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HelixToolkit.Wpf.SharpDX;
using SharpDX;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Media3D = System.Windows.Media.Media3D;
using Point3D = System.Windows.Media.Media3D.Point3D;
using Vector3D = System.Windows.Media.Media3D.Vector3D;

namespace LabelVal.ImageViewer3D.ViewModels;
public partial class ImageOverlayTesting : BaseViewModel
{
    [ObservableProperty] private MeshGeometry3D plane;
    [ObservableProperty] private LineGeometry3D grid;
    [ObservableProperty] private PhongMaterial planeMaterial;
    [ObservableProperty] private Color gridColor;
    [ObservableProperty] private Media3D.Transform3D planeTransform;
    [ObservableProperty] private Media3D.Transform3D gridTransform;
    [ObservableProperty] private Vector3D directionalLightDirection;
    [ObservableProperty] private Color4 directionalLightColor;
    [ObservableProperty] private Color4 ambientLightColor;

    //public ExifReader ExifReader { get; private set; }

    public ImageOverlayTesting(byte[] image)
    {
        Title = "ImageViewDemo";
        SubTitle = "WPF & SharpDX";

        EffectsManager = new DefaultEffectsManager();

        // camera setup
        defaultPerspectiveCamera = new PerspectiveCamera { Position = new Point3D(0, 0, 5), LookDirection = new Vector3D(0, 0, -5), UpDirection = new Vector3D(0, 1, 0), NearPlaneDistance = 0.5, FarPlaneDistance = 150 };
        defaultOrthographicCamera = new OrthographicCamera { Position = new Point3D(0, 0, 5), LookDirection = new Vector3D(0, 0, -5), UpDirection = new Vector3D(0, 1, 0), NearPlaneDistance = 0, FarPlaneDistance = 100 };
        Camera = defaultPerspectiveCamera;

        // setup lighting            
        AmbientLightColor = new Color4(0f, 0f, 0f, 0f);
        DirectionalLightColor = Color.White;
        DirectionalLightDirection = new Vector3D(-0, -0, -10);

        // floor plane grid
        Grid = LineBuilder.GenerateGrid(Vector3.UnitZ, -5, 5, -5, 5);
        GridColor = Color.Black;
        GridTransform = new Media3D.TranslateTransform3D(0, 0, 0);

        // plane
        var b2 = new MeshBuilder();
        b2.AddBox(new Vector3(0, 0, 0), 10, 10, 0, BoxFaces.PositiveZ);
        Plane = b2.ToMeshGeometry3D();
        PlaneMaterial = PhongMaterials.Blue;
        PlaneTransform = new Media3D.TranslateTransform3D(-0, -0, -0);
        //this.PlaneMaterial.ReflectiveColor = Color.Black;
        PlaneTransform = new Media3D.TranslateTransform3D(0, 0, 0);

        SetImages(ImageUtilities.lib.Wpf.BitmapImage.CreateBitmapImage(image));
    }

    private void SetImages(BitmapSource img)
    {
        var ratio = img.PixelWidth / (double)img.PixelHeight;
        Media3D.Transform3D transform = Media3D.Transform3D.Identity;

        if (ratio > 1)
        {
            transform = transform.AppendTransform(new Media3D.ScaleTransform3D(ratio, 1.0, 1.0));
            PlaneTransform = transform;
            GridTransform = PlaneTransform;
        }
        else
        {
            transform = transform.AppendTransform(new Media3D.ScaleTransform3D(1.0, 1.0 / ratio, 1.0));
            PlaneTransform = transform;
            GridTransform = PlaneTransform;
        }

        var white = new PhongMaterial()
        {
            DiffuseColor = Color.White,
            AmbientColor = Color.Black,
            ReflectiveColor = Color.Black,
            EmissiveColor = Color.Black,
            SpecularColor = Color.Black,
            DiffuseMap = new MemoryStream(img.ToByteArray()),
        };

        PlaneMaterial = white;
    }

    //private void TryGetExif(string filename)
    //{
    //    try
    //    {
    //        this.ExifReader = new ExifReader(filename);
    //        DateTime dateTime;
    //        this.ExifReader.GetTagValue(ExifTags.DateTime, out dateTime);
    //    }
    //    catch (Exception ex)
    //    {
    //        this.ExifReader = null;
    //    }
    //}
    [RelayCommand]
    private void Open()
    {
        try
        {
            var d = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = "image files|*.jpg; *.png; *.bmp; *.gif",
            };
            if (d.ShowDialog().Value)
            {
                if (File.Exists(d.FileName))
                {
                    var img = new BitmapImage(new Uri(d.FileName, UriKind.RelativeOrAbsolute));
                    //this.TryGetExif(d.FileName);
                    this.SetImages(img);
                    Title = d.FileName;
                }
            }
        }
        catch (Exception ex)
        {
            _ = MessageBox.Show(ex.Message, "File open error!", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
