using CommunityToolkit.Mvvm.ComponentModel;
using HelixToolkit.Wpf.SharpDX;
using LabelVal.ImageViewer3D.Mesh;
using SharpDX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Color = SharpDX.Color;
using Color4 = SharpDX.Color4;
using Point3D = System.Windows.Media.Media3D.Point3D;
using Vector3 = SharpDX.Vector3;
using Vector3D = System.Windows.Media.Media3D.Vector3D;

namespace LabelVal.ImageViewer3D.ViewModels
{
    public partial class ImageViewer3D_new : BaseViewModel
    {
        [ObservableProperty] private byte[] originalImage;
        [ObservableProperty] private byte[] indexedColorPallet;
        [ObservableProperty] Dictionary<byte, Color4> colorPallet;
        [ObservableProperty] private byte[] bytes;

        public Matrix[] ModelInstances { get; private set; }
        public InstanceParameter[] InstanceParam { get; private set; }

        public Vector3D DirectionalLightDirection { get; private set; }
        public Color4 DirectionalLightColor { get; set; }
        public Color4 AmbientLightColor { get; private set; }

        [ObservableProperty] MeshGeometry3D meshGeometry;
        [ObservableProperty] PhongMaterial material;

        public ImageViewer3D_new(byte[] image)
        {
            Title = "Instancing Demo";
            EffectsManager = new DefaultEffectsManager();

            //// setup lighting            
            this.AmbientLightColor = Color4.White;
            this.DirectionalLightColor = Color4.White;
            this.DirectionalLightDirection = new Vector3D(-0, -0, -200);

            // The width of the image is at byte 18 to 21
            int width = BitConverter.ToInt32(image, 18);
            // The height of the image is at byte 22 to 25
            int height = BitConverter.ToInt32(image, 22);

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

            BuildImageMesh(image);
        }

        private void BuildImageMesh(byte[] image)
        {
            var bitmapImage = Utilities.BitmapImageUtilities.CreateBitmapImage(image);
            if (bitmapImage == null)
                return;

            var textureModel = CreateTextureModel(bitmapImage);
            if (textureModel == null)
                return;

            Material = new PhongMaterial
            {
                DiffuseColor = Color.White, // Set the material color here
                DiffuseMap = textureModel
            };

            var mesh = new MeshGeneration();
            MeshGeometry = mesh.CreateSurfaceMeshGeometry3D(image);
        }
        private static TextureModel CreateTextureModel(BitmapImage bitmapImage)
        {
            var stream = BitmapImageToStream(bitmapImage);
            return TextureModel.Create(stream);
        }
        private static MemoryStream BitmapImageToStream(BitmapImage bitmapImage)
        {
            MemoryStream ms = new MemoryStream();
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
            encoder.Save(ms);
            ms.Position = 0;
            return ms;
        }

        private void BuildVisuals(byte[] image)
        {

            // The offset to the start of the pixel data is at byte 10 to 13
            int pixelDataOffset = BitConverter.ToInt32(image, 10);

            // The width of the image is at byte 18 to 21
            int width = BitConverter.ToInt32(image, 18);

            // The height of the image is at byte 22 to 25
            int height = BitConverter.ToInt32(image, 22);

            // Calculate the row size with padding
            int rowSize = (width + 3) & ~3; // Align to 4-byte boundary

            instances.Clear();
            parameters.Clear();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var z = image[pixelDataOffset + y * rowSize + x];
                    //if (z == 255) continue;
                    var material = ColorPallet[z];

                    var matrix = Matrix.Translation(new Vector3(x, y, -z));
                    //var color = new Color4(1, 1, 1, 1);//new Color4((float)Math.Abs(i) / num, (float)Math.Abs(j) / num, (float)Math.Abs(i + j) / (2 * num), 1);
                    //var offset = new Vector2(x, y);
                    parameters.Add(new InstanceParameter() { DiffuseColor = material });
                    instances.Add(matrix);

                }
            }

            InstanceParam = parameters.ToArray();
            ModelInstances = instances.ToArray();
            SubTitle = "Number of Instances: " + parameters.Count.ToString();
        }

        public bool LoadImage(byte[] image)
        {
            OriginalImage = LibImageUtilities.ImageTypes.Bmp.Utilities.GetBmp(image);

            var form = LibImageUtilities.ImageTypes.Bmp.Utilities.GetPixelFormat(OriginalImage);
            if (form != System.Drawing.Imaging.PixelFormat.Format8bppIndexed)
                return false;

            IndexedColorPallet = LibImageUtilities.ImageTypes.Bmp.Utilities.GetIndexedColorPallet(OriginalImage);
            if (IndexedColorPallet.Length != 1024)
                return false;

            ColorPallet = GetColorPallet();
            if (ColorPallet.Count != 256)
                return false;

            Bytes = LibImageUtilities.ImageTypes.Bmp.Utilities.GetImageData(OriginalImage);

            // BuildVisuals(OriginalImage);

            return true;
        }
        private Dictionary<byte, Color4> GetColorPallet()
        {
            var pallet = new Dictionary<byte, Color4>();
            for (int i = 0; i < 256; i++)
            {
                var r = IndexedColorPallet[i * 4 + 2] / 255.0f;
                var g = IndexedColorPallet[i * 4 + 1] / 255.0f;
                var b = IndexedColorPallet[i * 4] / 255.0f;
                var color4 = new Color4(r, g, b, 1.0f);
                pallet.Add((byte)i, color4);
            }
            return pallet;
        }

        const int num = 40;
        List<Matrix> instances = new List<Matrix>(num * 2);
        List<Matrix> selectedLineInstances = new List<Matrix>();
        List<InstanceParameter> parameters = new List<InstanceParameter>(num * 2);

        public void OnMouseLeftButtonDownHandler(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //if (EnableAnimation) { return; }
            //var viewport = sender as Viewport3DX;
            //if (viewport == null) { return; }
            //var point = e.GetPosition(viewport);
            //var hitTests = viewport.FindHits(point);
            //if (hitTests.Count > 0)
            //{
            //    foreach (var hit in hitTests)
            //    {
            //        if (hit.ModelHit is InstancingMeshGeometryModel3D)
            //        {
            //            var index = (int)hit.Tag;
            //            InstanceParam[index].EmissiveColor = InstanceParam[index].EmissiveColor != Colors.Yellow.ToColor4() ? Colors.Yellow.ToColor4() : Colors.Black.ToColor4();
            //            InstanceParam = (InstanceParameter[])InstanceParam.Clone();
            //            break;
            //        }
            //        else if (hit.ModelHit is LineGeometryModel3D)
            //        {
            //            var index = (int)hit.Tag;
            //            SelectedLineInstances = new Matrix[] { ModelInstances[index] };
            //            break;
            //        }
            //    }
            //}
        }

        protected override void Dispose(bool disposing)
        {
            //timer.Stop();
            //timer.Tick -= Timer_Tick;
            base.Dispose(disposing);
        }
    }
}
