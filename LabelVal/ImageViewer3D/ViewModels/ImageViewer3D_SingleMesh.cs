using CommunityToolkit.Mvvm.ComponentModel;
using HelixToolkit.Wpf.SharpDX;
using LabelVal.ImageViewer3D.Mesh;
using LabelVal.Utilities;
using LibImageUtilities.ImageTypes;
using LibImageUtilities.ImageTypes.Bmp;
using LibImageUtilities.ImageTypes.Png;
using SharpDX;
using System;
using System.Collections.Generic;
using System.IO;
using Color = SharpDX.Color;

namespace LabelVal.ImageViewer3D.ViewModels
{
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

        [ObservableProperty] double width;
        partial void OnWidthChanged(double value) => Width3DWindow = Width - 100;
        [ObservableProperty] double height;
        partial void OnHeightChanged(double value) => Height3DWindow = Height - 100;

        [ObservableProperty] double width3DWindow;
        [ObservableProperty] double height3DWindow;

        public System.Windows.Media.Imaging.BitmapImage Image { get; }
        public List<System.Windows.Media.Media3D.Vector3D> DirectionalLightDirections { get; } = [];

        [ObservableProperty] private Color4 directionalLightColor;
        [ObservableProperty] private Color4 ambientLightColor;

        [ObservableProperty] MeshGeometry3D meshGeometry;
        [ObservableProperty] PhongMaterial meshMaterial;
        [ObservableProperty] private System.Windows.Media.Media3D.Transform3D meshTransform;

        [ObservableProperty] private LineGeometry3D normalLines;

        private byte[] BitmapArray { get; }
        private byte[] OriginalImageArray { get; }

        public ImageViewer3D_SingleMesh(byte[] image)
        {
            var format = image.GetImagePixelFormat();
            // Convert the image to a 8bpp indexed bmp, if needed. This will be used to generate the mesh
            BitmapArray = format != System.Drawing.Imaging.PixelFormat.Format8bppIndexed
                ? image.GetBmp(System.Drawing.Imaging.PixelFormat.Format8bppIndexed)
                : image.GetBmp();

            //The material needs a 32 Bpp image
            OriginalImageArray = LibImageUtilities.ImageTypes.Png.Utilities.GetPng(image, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            // Convert the image to a BitmapImage for display
            Image = Utilities.BitmapImageUtilities.CreateBitmapImage(image);

            EffectsManager = new DefaultEffectsManager();

            int width = Image.PixelWidth;
            int height = Image.PixelHeight;

            // camera setup
            // Set up the default perspective camera
            this.defaultPerspectiveCamera = new PerspectiveCamera
            {
                Position = new System.Windows.Media.Media3D.Point3D(width / 2, height / 2, Math.Max(width, height) * 2), // Zoom out by setting Z to a larger value
                LookDirection = new System.Windows.Media.Media3D.Vector3D(0, 0, -Math.Max(width, height) * 2), // Look towards the model plane
                UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 1, 0),
                NearPlaneDistance = 0,
                FarPlaneDistance = 50000000,
                FieldOfView = 45 // Adjust the field of view if necessary
            }; this.defaultOrthographicCamera = new OrthographicCamera
            {
                Position = new System.Windows.Media.Media3D.Point3D(width / 2, height / 2, Math.Max(width, height) * 2), // Zoom out by setting Z to a larger value
                LookDirection = new System.Windows.Media.Media3D.Vector3D(0, 0, -Math.Max(width, height) * 2), // Look towards the model plane
                UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 1, 0),
                NearPlaneDistance = 0,
                FarPlaneDistance = 50000000,
                Width = Math.Max(width, height) * 2 // Adjust the width to ensure the model is visible
            };
            this.Camera = this.defaultOrthographicCamera;

            // setup lighting            
            SetupLighting();

            BuildImageMesh();
        }

        private void SetupLighting()
        {
            //this.AmbientLightColor = new Color4(1.0f, 1.0f, 1.0f, 1.0f); // Slight ambient light

            this.DirectionalLightColor = Color.White;
            // Add light directions for all quadrants
            DirectionalLightDirections.Add(new System.Windows.Media.Media3D.Vector3D(1, 1, 1));
            DirectionalLightDirections.Add(new System.Windows.Media.Media3D.Vector3D(-1, 1, 1));
            DirectionalLightDirections.Add(new System.Windows.Media.Media3D.Vector3D(1, -1, 1));
            DirectionalLightDirections.Add(new System.Windows.Media.Media3D.Vector3D(-1, -1, 1));
            DirectionalLightDirections.Add(new System.Windows.Media.Media3D.Vector3D(1, 1, -1));
            DirectionalLightDirections.Add(new System.Windows.Media.Media3D.Vector3D(-1, 1, -1));
            DirectionalLightDirections.Add(new System.Windows.Media.Media3D.Vector3D(1, -1, -1));
            DirectionalLightDirections.Add(new System.Windows.Media.Media3D.Vector3D(-1, -1, -1));

        }
        private void BuildImageMesh()
        {
            MeshMaterial = new PhongMaterial
            {
                DiffuseColor = Color.White, // Set the material color here
                DiffuseMap = new MemoryStream(OriginalImageArray),
            };
            
            MeshGeometry = MeshGeneration.CreateSurfaceMeshGeometry3D(BitmapArray, WhiteFront);
            MeshTransform = new System.Windows.Media.Media3D.TranslateTransform3D(0, 0, 0);

            // Add normal visualization
            AddNormalVisualization();
        }
        private void AddNormalVisualization()
        {
            NormalLines = VisualizeNormals(MeshGeometry);
        }
        public static LineGeometry3D VisualizeNormals(MeshGeometry3D meshGeometry)
        {
            var lineBuilder = new LineBuilder();
            for (int i = 0; i < meshGeometry.Positions.Count; i++)
            {
                var position = meshGeometry.Positions[i];
                var normal = meshGeometry.Normals[i];
                lineBuilder.AddLine(position, position + normal * 0.5f); // Scale for visibility
            }

            return lineBuilder.ToLineGeometry3D();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
