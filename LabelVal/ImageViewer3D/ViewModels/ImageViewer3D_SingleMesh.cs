using CommunityToolkit.Mvvm.ComponentModel;
using HelixToolkit.Wpf.SharpDX;
using LabelVal.ImageViewer3D.Mesh;
using LabelVal.Utilities;
using SharpDX;
using System;
using System.Collections.Generic;
using System.IO;
using Color = SharpDX.Color;

namespace LabelVal.ImageViewer3D.ViewModels
{
    public partial class ImageViewer3D_SingleMesh : BaseViewModel
    {
        public List<System.Windows.Media.Media3D.Vector3D> DirectionalLightDirections { get; } = [];

        [ObservableProperty] private Color4 directionalLightColor;
        [ObservableProperty] private Color4 ambientLightColor;

        [ObservableProperty] MeshGeometry3D meshGeometry;
        [ObservableProperty] PhongMaterial material;
        [ObservableProperty] private System.Windows.Media.Media3D.Transform3D meshTransform;

        [ObservableProperty] private LineGeometry3D normalLines;

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

            this.MeshTransform = new System.Windows.Media.Media3D.TranslateTransform3D(0, 0, 0);

            BuildImageMesh(image, bmp);
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

        private void BuildImageMesh(byte[] image, byte[] bmp)
        {
            image = LibImageUtilities.ImageTypes.Png.Utilities.GetPng(image, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Material = new PhongMaterial
            {
                DiffuseColor = Color.White, // Set the material color here
                DiffuseMap = new MemoryStream(image),
            };

            MeshGeometry = MeshGeneration.CreateSurfaceMeshGeometry3D(bmp);

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
