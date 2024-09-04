using HelixToolkit.Wpf.SharpDX;
using SharpDX;
using SharpDX.Direct2D1.Effects;
using System;
using System.Collections.Generic;
using System.Linq;
using Media3D = System.Windows.Media.Media3D;
using Point3D = System.Windows.Media.Media3D.Point3D;
using Vector3D = System.Windows.Media.Media3D.Vector3D;
using Transform3D = System.Windows.Media.Media3D.Transform3D;
using TranslateTransform3D = System.Windows.Media.Media3D.TranslateTransform3D;
using Color = SharpDX.Color;
using Plane = SharpDX.Plane;
using Vector3 = SharpDX.Vector3;
using Colors = System.Windows.Media.Colors;
using Color4 = SharpDX.Color4;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using LabelVal.ImageViewer3D.Models;
using static V5_REST_Lib.Models.Meta;
using System.Windows;

namespace LabelVal.ImageViewer3D.ViewModels
{
    public partial class ImageViewer3D_new : BaseViewModel
    {
        [ObservableProperty] private byte[] originalImage;
        [ObservableProperty] private byte[] indexedColorPallet;
        [ObservableProperty] Dictionary<byte, Color4> colorPallet;
        [ObservableProperty] private byte[] bytes;

        public MeshGeometry3D Model { get; private set; }
        //public LineGeometry3D Lines { get; private set; }
        public LineGeometry3D Grid { get; private set; }
        public Matrix[] ModelInstances { get; private set; }

        //public Matrix[] SelectedLineInstances { private set; get; }

        public InstanceParameter[] InstanceParam { get; private set; }

        //public BillboardSingleImage3D BillboardModel { private set; get; }
        //public Matrix[] BillboardInstances { private set; get; }

        //public BillboardInstanceParameter[] BillboardInstanceParams { private set; get; }

        public PhongMaterial ModelMaterial { get; private set; }
        public Transform3D ModelTransform { get; private set; }

        public Vector3D DirectionalLightDirection { get; private set; }
        public Color4 DirectionalLightColor { get; set; }
        public Color4 AmbientLightColor { get; private set; }
        public TextureModel Texture { private set; get; }
        public bool EnableAnimation { set; get; }

        private DispatcherTimer timer = new DispatcherTimer();
        private Random rnd = new Random();
        private float aniX = 0;
        private float aniY = 0;
        private float aniZ = 0;
        private bool aniDir = true;
        public ImageViewer3D_new(byte[] image)
        {
            Title = "Instancing Demo";
            EffectsManager = new DefaultEffectsManager();
            // camera setup
            //Camera = new PerspectiveCamera { Position = new Point3D(40, 40, 40), LookDirection = new Vector3D(-40, -40, -40), UpDirection = new Vector3D(0, 1, 0), FarPlaneDistance = 5000000, NearPlaneDistance = 0 };

            //// setup lighting            
            this.AmbientLightColor = Color4.Black;
            //this.DirectionalLightColor = Colors.White;
            //this.DirectionalLightDirection = new Vector3D(-2, -5, -2);
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
            
            // setup lighting            
            this.AmbientLightColor = new Color4(50f, 50f, 50f, 50f);
            this.DirectionalLightColor = Color.White;
            this.DirectionalLightDirection = new Vector3D(-0, -0, -200);

            // scene model3d
            var b1 = new MeshBuilder(true, true, true);
            b1.AddBox(new Vector3(0, 0, 0), 1, 1, 255 / 4, BoxFaces.All);
            Model = b1.ToMeshGeometry3D();
            //for (int i = 0; i < Model.TextureCoordinates.Count; ++i)
            //{
            //    var tex = Model.TextureCoordinates[i];
            //    Model.TextureCoordinates[i] = new Vector2(tex.X * 0.5f, tex.Y * 0.5f);
            //}

            //var l1 = new LineBuilder();
            //l1.AddBox(new Vector3(0, 0, 0), 1.1, 1.1, 1.1);
            //Lines = l1.ToLineGeometry3D();
            //Lines.Colors = new Color4Collection(Enumerable.Repeat(Colors.White.ToColor4(), Lines.Positions.Count));
            // model trafo
            ModelTransform = Media3D.Transform3D.Identity;// new Media3D.RotateTransform3D(new Media3D.AxisAngleRotation3D(new Vector3D(0, 0, 1), 45));

            // model material
            ModelMaterial = PhongMaterials.White;
            //ModelMaterial.DiffuseMap = TextureModel.Create(new System.Uri(@"TextureCheckerboard2.jpg", System.UriKind.RelativeOrAbsolute).ToString());
            //ModelMaterial.NormalMap = TextureModel.Create(new System.Uri(@"TextureCheckerboard2_dot3.jpg", System.UriKind.RelativeOrAbsolute).ToString());

            //BillboardModel = new BillboardSingleImage3D(ModelMaterial.DiffuseMap, 20, 20);
            //Texture = TextureModel.Create("Cubemap_Grandcanyon.dds");
            LoadImage(image);
            //timer.Interval = TimeSpan.FromMilliseconds(30);
            //timer.Tick += Timer_Tick;
            //timer.Start();
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

                    var matrix = Matrix.Translation(new Vector3(x, y, -z ));
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

            BuildVisuals(OriginalImage);

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

        //private void Timer_Tick(object sender, EventArgs e)
        //{
        //    if (!EnableAnimation) { return; }
        //    CreateModels();
        //}

        const int num = 40;
        List<Matrix> instances = new List<Matrix>(num * 2);
        List<Matrix> selectedLineInstances = new List<Matrix>();
        List<InstanceParameter> parameters = new List<InstanceParameter>(num * 2);

        //List<Matrix> billboardinstances = new List<Matrix>(num * 2);
        //List<BillboardInstanceParameter> billboardParams = new List<BillboardInstanceParameter>(num * 2);

        private void CreateModels()
        {
            instances.Clear();
            parameters.Clear();

            if (aniDir)
            {
                aniX += 0.1f;
                aniY += 0.2f;
                aniZ += 0.3f;
            }
            else
            {
                aniX -= 0.1f;
                aniY -= 0.2f;
                aniZ -= 0.3f;
            }

            if (aniX > 15)
            {
                aniDir = false;
            }
            else if (aniX < -15)
            {
                aniDir = true;
            }

            for (int i = -num - (int)aniX; i < num + aniX; i++)
            {
                for (int j = -num - (int)aniX; j < num + aniX; j++)
                {
                    var matrix = Matrix.RotationAxis(new Vector3(0, 1, 0), aniX * Math.Sign(j))
                        * Matrix.Translation(new Vector3(i * 1.2f + Math.Sign(i), j * 1.2f + Math.Sign(j), i * j / 2.0f));
                    var color = new Color4(1, 1, 1, 1);//new Color4((float)Math.Abs(i) / num, (float)Math.Abs(j) / num, (float)Math.Abs(i + j) / (2 * num), 1);
                    //  var emissiveColor = new Color4( rnd.NextFloat(0,1) , rnd.NextFloat(0, 1), rnd.NextFloat(0, 1), rnd.NextFloat(0, 0.2f));
                    var k = Math.Abs(i + j) % 4;
                    Vector2 offset;
                    if (k == 0)
                    {
                        offset = new Vector2(aniX, 0);
                    }
                    else if (k == 1)
                    {
                        offset = new Vector2(0.5f + aniX, 0);
                    }
                    else if (k == 2)
                    {
                        offset = new Vector2(0.5f + aniX, 0.5f);
                    }
                    else
                    {
                        offset = new Vector2(aniX, 0.5f);
                    }

                    parameters.Add(new InstanceParameter() { DiffuseColor = color, TexCoordOffset = offset });
                    instances.Add(matrix);
                }
            }
            InstanceParam = parameters.ToArray();
            ModelInstances = instances.ToArray();
            SubTitle = "Number of Instances: " + parameters.Count.ToString();

            //if (BillboardInstances == null)
            //{
            //    for (int i = 0; i < 2 * num; ++i)
            //    {
            //        billboardParams.Add(new BillboardInstanceParameter()
            //        { TexCoordOffset = new Vector2(1f / 6 * rnd.Next(0, 6), 1f / 6 * rnd.Next(0, 6)), TexCoordScale = new Vector2(1f / 6, 1f / 6) });
            //        billboardinstances.Add(Matrix.Scaling(rnd.NextFloat(0.5f, 4f), rnd.NextFloat(0.5f, 3f), rnd.NextFloat(0.5f, 3f))
            //            * Matrix.Translation(new Vector3(rnd.NextFloat(0, 100), rnd.NextFloat(0, 100), rnd.NextFloat(-50, 50))));
            //    }
            //    BillboardInstanceParams = billboardParams.ToArray();
            //    BillboardInstances = billboardinstances.ToArray();
            //}
            //else
            //{
            //    for (int i = 0; i < billboardinstances.Count; ++i)
            //    {
            //        var current = billboardinstances[i];
            //        current.M41 += i % 3 == 0 ? aniX / 50 : -aniX / 50;
            //        current.M42 += i % 4 == 0 ? aniY / 50 : -aniY / 30;
            //        current.M43 += i % 5 == 0 ? aniZ / 100 : -aniZ / 50;
            //        billboardinstances[i] = current;
            //    }
            //    BillboardInstances = billboardinstances.ToArray();
            //}
        }

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
