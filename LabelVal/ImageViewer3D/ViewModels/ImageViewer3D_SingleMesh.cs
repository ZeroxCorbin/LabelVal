using CommunityToolkit.Mvvm.ComponentModel;
using HelixToolkit.Wpf.SharpDX;
using LabelVal.ImageViewer3D.Mesh;
using LabelVal.Utilities;
using SharpDX;
using System;
using System.IO;
using Color = SharpDX.Color;
using Point3D = System.Windows.Media.Media3D.Point3D;
using Vector3D = System.Windows.Media.Media3D.Vector3D;

namespace LabelVal.ImageViewer3D.ViewModels
{
    public partial class ImageViewer3D_SingleMesh : BaseViewModel
    {
        [ObservableProperty] private Vector3D directionalLightDirection;
        [ObservableProperty] private Color4 directionalLightColor;
        [ObservableProperty] private Color4 ambientLightColor;

        [ObservableProperty] MeshGeometry3D meshGeometry;
        [ObservableProperty] PhongMaterial material;
        [ObservableProperty] private System.Windows.Media.Media3D.Transform3D meshTransform;

        public ImageViewer3D_SingleMesh(byte[] image)
        {
            var bmp = LibImageUtilities.ImageTypes.Bmp.Utilities.GetBmp(image, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);

            this.Title = "ImageViewDemo";
            this.SubTitle = "WPF & SharpDX";

            EffectsManager = new DefaultEffectsManager();

            // The width of the image is at byte 18 to 21
            int width = BitConverter.ToInt32(bmp, 18);
            // The height of the image is at byte 22 to 25
            int height = BitConverter.ToInt32(bmp, 22);

            // camera setup
            // Set up the default perspective camera
            this.defaultPerspectiveCamera = new PerspectiveCamera
            {
                Position = new Point3D(width / 2, height / 2, Math.Max(width, height) * 2), // Zoom out by setting Z to a larger value
                LookDirection = new Vector3D(0, 0, -Math.Max(width, height) * 2), // Look towards the model plane
                UpDirection = new Vector3D(0, 1, 0),
                NearPlaneDistance = 0.5,
                FarPlaneDistance = 50000000,
                FieldOfView = 45 // Adjust the field of view if necessary
            }; this.defaultOrthographicCamera = new OrthographicCamera
            {
                Position = new Point3D(width / 2, height / 2, Math.Max(width, height) * 2), // Zoom out by setting Z to a larger value
                LookDirection = new Vector3D(0, 0, -Math.Max(width, height) * 2), // Look towards the model plane
                UpDirection = new Vector3D(0, 1, 0),
                NearPlaneDistance = 0,
                FarPlaneDistance = 50000000,
                Width = Math.Max(width, height) * 2 // Adjust the width to ensure the model is visible
            };
            this.Camera = this.defaultOrthographicCamera;

            // setup lighting            
            SetupLighting();

            this.MeshTransform = new System.Windows.Media.Media3D.TranslateTransform3D(-0, -0, -0);
            this.MeshTransform = new System.Windows.Media.Media3D.TranslateTransform3D(0, 0, 0);

            BuildImageMesh(image, bmp);
        }

        private void SetupLighting()
        {
            this.AmbientLightColor = new Color4(1.0f, 1.0f, 1.0f, 1.0f); // Slight ambient light
            this.DirectionalLightColor = Color.White;
            this.DirectionalLightDirection = new Vector3D(1, 1, 1); // Off-axis directional light
        }

        private void BuildImageMesh(byte[] image, byte[] bmp)
        {
           Material = new PhongMaterial
            {
                DiffuseColor = Color.White, // Set the material color here
                DiffuseMap = new MemoryStream(image),
                
            };

            MeshGeometry = MeshGeneration.CreateSurfaceMeshGeometry3D(bmp);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
