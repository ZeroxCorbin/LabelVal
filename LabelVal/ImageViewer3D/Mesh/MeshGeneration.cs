using HelixToolkit.Wpf.SharpDX;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LabelVal.ImageViewer3D.Mesh
{
    public class MeshGeneration
    {
        public class Vertex
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Z { get; set; }

            public Vertex(double x, double y, double z)
            {
                X = x;
                Y = y;
                Z = z;
            }
        }

        public MeshGeometry3D CreateSurfaceMeshGeometry3D(byte[] image)
        {
            var points = new List<Vertex>();
            // The offset to the start of the pixel data is at byte 10 to 13
            int pixelDataOffset = BitConverter.ToInt32(image, 10);

            // The width of the image is at byte 18 to 21
            int width = BitConverter.ToInt32(image, 18);

            // The height of the image is at byte 22 to 25
            int height = BitConverter.ToInt32(image, 22);

            // Calculate the row size with padding
            int rowSize = (width + 3) & ~3; // Align to 4-byte boundary

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    double z = -image[pixelDataOffset + y * rowSize + x];
                    points.Add(new Vertex(x, y, z));
                }
            }

            var meshGeometry = new MeshGeometry3D();

            // Create arrays for positions and texture coordinates
            var positions = new Vector3[points.Count];
            var textureCoordinates = new Vector2[points.Count];
            var indices = new List<int>();

            // Fill positions and texture coordinates
            Parallel.For(0, points.Count, i =>
            {
                var point = points[i];
                positions[i] = new Vector3((float)point.X, (float)point.Y, (float)point.Z);
                textureCoordinates[i] = new Vector2((float)(point.X / (width - 1)), (float)(point.Y / (height - 1)));
            });

            // Generate indices for triangles
            for (int y = 0; y < height - 1; y++)
            {
                for (int x = 0; x < width - 1; x++)
                {
                    int topLeft = y * width + x;
                    int topRight = topLeft + 1;
                    int bottomLeft = topLeft + width;
                    int bottomRight = bottomLeft + 1;

                    // First triangle
                    indices.Add(topLeft);
                    indices.Add(bottomLeft);
                    indices.Add(topRight);

                    // Second triangle
                    indices.Add(topRight);
                    indices.Add(bottomLeft);
                    indices.Add(bottomRight);
                }
            }

            // Assign the arrays to the meshGeometry
            meshGeometry.Positions = new Vector3Collection(positions);
            meshGeometry.TextureCoordinates = new Vector2Collection(textureCoordinates);
            meshGeometry.Indices = new IntCollection(indices);

            return meshGeometry;
        }
    }
}