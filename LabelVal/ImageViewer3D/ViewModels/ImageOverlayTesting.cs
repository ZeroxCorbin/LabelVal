using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelVal.ImageViewer3D.ViewModels;
using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

using HelixToolkit.Wpf.SharpDX;

using SharpDX;

using Media3D = System.Windows.Media.Media3D;
using Point3D = System.Windows.Media.Media3D.Point3D;
using Vector3D = System.Windows.Media.Media3D.Vector3D;
using HelixToolkit.Wpf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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
        this.Title = "ImageViewDemo";
        this.SubTitle = "WPF & SharpDX";

        EffectsManager = new DefaultEffectsManager();

        // camera setup
        this.defaultPerspectiveCamera = new PerspectiveCamera { Position = new Point3D(0, 0, 5), LookDirection = new Vector3D(0, 0, -5), UpDirection = new Vector3D(0, 1, 0), NearPlaneDistance = 0.5, FarPlaneDistance = 150 };
        this.defaultOrthographicCamera = new OrthographicCamera { Position = new Point3D(0, 0, 5), LookDirection = new Vector3D(0, 0, -5), UpDirection = new Vector3D(0, 1, 0), NearPlaneDistance = 0, FarPlaneDistance = 100 };
        this.Camera = this.defaultPerspectiveCamera;

        // setup lighting            
        this.AmbientLightColor = new Color4(0f, 0f, 0f, 0f);
        this.DirectionalLightColor = Color.White;
        this.DirectionalLightDirection = new Vector3D(-0, -0, -10);

        // floor plane grid
        this.Grid = LineBuilder.GenerateGrid(Vector3.UnitZ, -5, 5, -5, 5);
        this.GridColor = Color.Black;
        this.GridTransform = new Media3D.TranslateTransform3D(0, 0, 0);

        // plane
        var b2 = new MeshBuilder();
        b2.AddBox(new Vector3(0, 0, 0), 10, 10, 0, BoxFaces.PositiveZ);
        this.Plane = b2.ToMeshGeometry3D();
        this.PlaneMaterial = PhongMaterials.Blue;
        this.PlaneTransform = new Media3D.TranslateTransform3D(-0, -0, -0);
        //this.PlaneMaterial.ReflectiveColor = Color.Black;
        this.PlaneTransform = new Media3D.TranslateTransform3D(0, 0, 0);

        SetImages(LibImageUtilities.BitmapImage.CreateBitmapImage(image));
    }

    private void SetImages(BitmapSource img)
    {
        var ratio = img.PixelWidth / (double)img.PixelHeight;
        var transform = Media3D.Transform3D.Identity;

        if (ratio > 1)
        {
            transform = transform.AppendTransform(new Media3D.ScaleTransform3D(ratio, 1.0, 1.0));
            this.PlaneTransform = transform;
            this.GridTransform = this.PlaneTransform;
        }
        else
        {
            transform = transform.AppendTransform(new Media3D.ScaleTransform3D(1.0, 1.0 / ratio, 1.0));
            this.PlaneTransform = transform;
            this.GridTransform = this.PlaneTransform;
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

        this.PlaneMaterial = white;
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
                    this.Title = d.FileName;
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "File open error!", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
