using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HelixToolkit.Wpf.SharpDX;
using ImageUtilities.lib.Wpf;
using LabelVal.ImageViewer3D.Mesh;
using SharpDX;
using System.Drawing.Imaging;
using System.IO;

namespace LabelVal.ImageViewer3D.ViewModels;

public partial class ImageViewer3D_SingleMesh : BaseViewModel
{
    [ObservableProperty] private bool whiteFront = App.Settings.GetValue(nameof(WhiteFront), false, true);
    partial void OnWhiteFrontChanged(bool value)
    {
        App.Settings.SetValue(nameof(WhiteFront), value);
        BuildImageMesh();
    }

    [ObservableProperty] private bool showWireframe = App.Settings.GetValue(nameof(ShowWireframe), false, true);
    partial void OnShowWireframeChanged(bool value) => App.Settings.SetValue(nameof(ShowWireframe), value);

    [ObservableProperty] private bool showNormals = App.Settings.GetValue(nameof(ShowNormals), false, true);
    partial void OnShowNormalsChanged(bool value) => App.Settings.SetValue(nameof(ShowNormals), value);

    [ObservableProperty] private double width;
    partial void OnWidthChanged(double value) => Width3DWindow = Width - 100;
    [ObservableProperty] private double height;
    partial void OnHeightChanged(double value) => Height3DWindow = Height - 100;

    [ObservableProperty] private double width3DWindow;
    [ObservableProperty] private double height3DWindow;

    public System.Windows.Media.Imaging.BitmapImage Image { get; }
    public List<System.Windows.Media.Media3D.Vector3D> DirectionalLightDirections { get; } = [];

    [ObservableProperty] private System.Windows.Media.Color directionalLightColor;
    [ObservableProperty] private System.Windows.Media.Color ambientLightColor;

    [ObservableProperty] private MeshGeometry3D meshGeometry;
    [ObservableProperty] private PhongMaterial meshMaterial;
    [ObservableProperty] private System.Windows.Media.Media3D.Transform3D meshTransform;

    [ObservableProperty] private LineGeometry3D normalLines;
    public MeshGeometry3D BoxModel { get; private set; }

    private byte[] BitmapArray { get; }
    private byte[] OriginalImageArray { get; }

    private byte[] SobelImageArray { get; }

    [ObservableProperty] private Vector3? constraintPlaneYz_AxisX = new Vector3(1, 0, 0);
    [ObservableProperty] private Vector3? constraintPlaneZx_AxisY = new Vector3(0, 1, 0);
    [ObservableProperty] private Vector3? constraintPlaneXy_AxisZ = new Vector3(0, 0, 1);
    [ObservableProperty] private Vector3? constrainDimension;

    [ObservableProperty] private bool enablePlaneXy = true;
    [ObservableProperty] private Plane planeXy = new(new Vector3(0, 0, 1), 0);
    [ObservableProperty] private PhongMaterial planeMaterialXy = PhongMaterials.Blue;

    [ObservableProperty] private bool enablePlaneYz = true;
    [ObservableProperty] private Plane planeYz = new(new Vector3(1, 0, 0), 0);
    [ObservableProperty] private PhongMaterial planeMaterialYz = PhongMaterials.Red;

    [ObservableProperty] private bool enablePlaneZx = true;
    [ObservableProperty] private Plane planeZx = new(new Vector3(0, 1, 0), 0);
    [ObservableProperty] private PhongMaterial planeMaterialZx = PhongMaterials.Green;

    [ObservableProperty] private int cuttingOperationIndex;
    partial void OnCuttingOperationIndexChanged(int value) => CuttingOperation = (CuttingOperation)value;

    [ObservableProperty] private CuttingOperation cuttingOperation = CuttingOperation.Intersect;

    public ImageViewer3D_SingleMesh(byte[] image)
    {
        OriginalImageArray = Utilities.DotImagingUtilities.GetBmp(image, PixelFormat.Format32bppArgb);
        BitmapArray = Utilities.DotImagingUtilities.GetBmp(image, PixelFormat.Format8bppIndexed);

        // Convert the image to a BitmapImage for display
        Image = BitmapHelpers.CreateBitmapImage(image);

        EffectsManager = new DefaultEffectsManager();

        var width = Image.PixelWidth;
        var height = Image.PixelHeight;

        // camera setup
        // Set up the default perspective camera
        defaultPerspectiveCamera = new PerspectiveCamera
        {
            Position = new System.Windows.Media.Media3D.Point3D(width / 2, height / 2, Math.Max(width, height) * 2), // Zoom out by setting Z to a larger value
            LookDirection = new System.Windows.Media.Media3D.Vector3D(0, 0, -Math.Max(width, height) * 2), // Look towards the model plane
            UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 1, 0),
            NearPlaneDistance = 0,
            FarPlaneDistance = 50000000,
            FieldOfView = 45 // Adjust the field of view if necessary
        }; defaultOrthographicCamera = new OrthographicCamera
        {
            Position = new System.Windows.Media.Media3D.Point3D(width / 2, height / 2, Math.Max(width, height) * 2), // Zoom out by setting Z to a larger value
            LookDirection = new System.Windows.Media.Media3D.Vector3D(0, 0, -Math.Max(width, height) * 2), // Look towards the model plane
            UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 1, 0),
            NearPlaneDistance = 0,
            FarPlaneDistance = 50000000,
            Width = Math.Max(width, height) * 2 // Adjust the width to ensure the model is visible
        };
        Camera = defaultOrthographicCamera;

        // setup lighting            
        SetupLighting();

        BuildImageMesh();
    }

    [RelayCommand]
    private void ResetCuttingPlanes()
    {
        PlaneXy = new Plane(new Vector3(0, 0, 1), WhiteFront ? 0 : -255);
        PlaneYz = new Plane(new Vector3(1, 0, 0), 0);
        PlaneZx = new Plane(new Vector3(0, 1, 0), 0);
    }

    private void SetupLighting()
    {
        //this.AmbientLightColor = new Color4(1.0f, 1.0f, 1.0f, 1.0f); // Slight ambient light

        DirectionalLightColor = System.Windows.Media.Colors.LightGray;
        // Add light directions for all quadrants
        // Add light directions for all quadrants
        DirectionalLightDirections.Add(new System.Windows.Media.Media3D.Vector3D(1, 1, 1));  // Quadrant 1
        DirectionalLightDirections.Add(new System.Windows.Media.Media3D.Vector3D(-1, 1, 1)); // Quadrant 2
        DirectionalLightDirections.Add(new System.Windows.Media.Media3D.Vector3D(1, -1, 1)); // Quadrant 3
        DirectionalLightDirections.Add(new System.Windows.Media.Media3D.Vector3D(-1, -1, 1)); // Quadrant 4
        DirectionalLightDirections.Add(new System.Windows.Media.Media3D.Vector3D(1, 1, -1));  // Quadrant 5
        DirectionalLightDirections.Add(new System.Windows.Media.Media3D.Vector3D(-1, 1, -1)); // Quadrant 6
        DirectionalLightDirections.Add(new System.Windows.Media.Media3D.Vector3D(1, -1, -1)); // Quadrant 7
        DirectionalLightDirections.Add(new System.Windows.Media.Media3D.Vector3D(-1, -1, -1)); // Quadrant 8

        // Add light directions from the center of each face of the cube
        DirectionalLightDirections.Add(new System.Windows.Media.Media3D.Vector3D(1, 0, 0));  // Right face
        DirectionalLightDirections.Add(new System.Windows.Media.Media3D.Vector3D(-1, 0, 0)); // Left face
        DirectionalLightDirections.Add(new System.Windows.Media.Media3D.Vector3D(0, 1, 0));  // Top face
        DirectionalLightDirections.Add(new System.Windows.Media.Media3D.Vector3D(0, -1, 0)); // Bottom face
        DirectionalLightDirections.Add(new System.Windows.Media.Media3D.Vector3D(0, 0, 1));  // Front face
        DirectionalLightDirections.Add(new System.Windows.Media.Media3D.Vector3D(0, 0, -1)); // Back face
    }
    private void BuildImageMesh()
    {
        MeshMaterial = new PhongMaterial
        {
            DiffuseColor = Color4.White, // Set the material color here
            DiffuseMap = new MemoryStream(OriginalImageArray),
            AmbientColor = Color4.White,
        };

        MeshGeometry = MeshGeneration.CreateSurfaceMeshGeometry3D(BitmapArray, WhiteFront);
        MeshTransform = new System.Windows.Media.Media3D.TranslateTransform3D(0, 0, 0);

        ConstrainDimension = new Vector3(Image.PixelWidth, Image.PixelHeight, WhiteFront ? -255 : 255);

        PlaneXy = new Plane(new Vector3(0, 0, 1), WhiteFront ? PlaneXy.D != 0 ? PlaneXy.D + 255 : 0 : PlaneXy.D != 0 ? PlaneXy.D + -255 : -255);
        // Add normal visualization
        AddNormalVisualization();
    }
    private void AddNormalVisualization() => NormalLines = VisualizeNormals(MeshGeometry);
    public static LineGeometry3D VisualizeNormals(MeshGeometry3D meshGeometry)
    {
        var lineBuilder = new LineBuilder();
        for (var i = 0; i < meshGeometry.Positions.Count; i++)
        {
            var position = meshGeometry.Positions[i];
            var normal = meshGeometry.Normals[i];
            lineBuilder.AddLine(position, position + (normal * 0.5f)); // Scale for visibility
        }

        return lineBuilder.ToLineGeometry3D();
    }

    protected override void Dispose(bool disposing) => base.Dispose(disposing);
}
