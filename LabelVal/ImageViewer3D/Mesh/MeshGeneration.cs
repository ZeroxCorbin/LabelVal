using HelixToolkit.Wpf.SharpDX;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static LabelVal.ImageViewer3D.Mesh.MeshGeneration;

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

        public static MeshGeometry3D CreateSurfaceMeshGeometry3D(byte[] image, bool whiteFront)
        {
            var points = new List<Vertex>();
            // The offset to the start of the pixel data is at byte 10 to 13
            var pixelDataOffset = BitConverter.ToInt32(image, 10);
            // The width of the image is at byte 18 to 21
            var width = BitConverter.ToInt32(image, 18);
            // The height of the image is at byte 22 to 25
            var height = BitConverter.ToInt32(image, 22);

            // Calculate the row size with padding
            var rowSize = (width + 3) & ~3; // Align to 4-byte boundary

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    double z = whiteFront ? image[pixelDataOffset + y * rowSize + x] : -image[pixelDataOffset + y * rowSize + x]; // Use pixel value as Z position
                    points.Add(new Vertex(x, y, z)); // Keep the Y-coordinate unchanged
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
                textureCoordinates[i] = new Vector2((float)point.X / (width - 1), 1.0f - (float)point.Y / (height - 1)); // Flip only the texture coordinates
            });

            // Generate indices for triangles
            for (var y = 0; y < height - 1; y++)
            {
                for (var x = 0; x < width - 1; x++)
                {
                    var topLeft = y * width + x;
                    var topRight = topLeft + 1;
                    var bottomLeft = topLeft + width;
                    var bottomRight = bottomLeft + 1;

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
            meshGeometry.Normals = meshGeometry.CalculateNormals();

            return meshGeometry;
        }

        //public static MeshGeometry3D CreateSurfaceMeshGeometry3D_AllSurfaces(byte[] image)
        //{
        //    var points = new List<Vertex>();
        //    // The offset to the start of the pixel data is at byte 10 to 13
        //    int pixelDataOffset = BitConverter.ToInt32(image, 10);

        //    // The width of the image is at byte 18 to 21
        //    int width = BitConverter.ToInt32(image, 18);

        //    // The height of the image is at byte 22 to 25
        //    int height = BitConverter.ToInt32(image, 22);

        //    // Calculate the row size with padding
        //    int rowSize = (width + 3) & ~3; // Align to 4-byte boundary

        //    for (int y = 0; y < height; y++)
        //    {
        //        for (int x = 0; x < width; x++)
        //        {
        //            double z = -image[pixelDataOffset + y * rowSize + x];
        //            points.Add(new Vertex(x, y, z));
        //        }
        //    }

        //    var meshGeometry = new MeshGeometry3D();

        //    // Create arrays for positions and texture coordinates
        //    var positions = new Vector3[points.Count];
        //    var textureCoordinates = new Vector2[points.Count];
        //    var indices = new List<int>();

        //    // Fill positions and texture coordinates
        //    Parallel.For(0, points.Count, i =>
        //    {
        //        var point = points[i];
        //        positions[i] = new Vector3((float)point.X, (float)point.Y, (float)point.Z);
        //        textureCoordinates[i] = new Vector2((float)point.X / (width - 1), (float)point.Y / (height - 1));
        //    });

        //    // Generate indices for triangles
        //    for (int y = 0; y < height - 1; y++)
        //    {
        //        for (int x = 0; x < width - 1; x++)
        //        {
        //            int topLeft = y * width + x;
        //            int topRight = topLeft + 1;
        //            int bottomLeft = topLeft + width;
        //            int bottomRight = bottomLeft + 1;

        //            // First triangle
        //            indices.Add(topLeft);
        //            indices.Add(bottomLeft);
        //            indices.Add(topRight);

        //            // Second triangle
        //            indices.Add(topRight);
        //            indices.Add(bottomLeft);
        //            indices.Add(bottomRight);
        //        }
        //    }

        //    // Assign the arrays to the meshGeometry
        //    meshGeometry.Positions = new Vector3Collection(positions);
        //    meshGeometry.TextureCoordinates = new Vector2Collection(textureCoordinates);
        //    meshGeometry.Indices = new IntCollection(indices);
        //    meshGeometry.Normals = meshGeometry.CalculateNormals();
        //    return meshGeometry;
        //}

        //public static MeshGeometry3D CreateSolidBodyMeshGeometry3D(byte[] image)
        //{
        //    var points = new List<Vertex>();
        //    // The offset to the start of the pixel data is at byte 10 to 13
        //    int pixelDataOffset = BitConverter.ToInt32(image, 10);

        //    // The width of the image is at byte 18 to 21
        //    int width = BitConverter.ToInt32(image, 18);

        //    // The height of the image is at byte 22 to 25
        //    int height = BitConverter.ToInt32(image, 22);

        //    // Calculate the row size with padding
        //    int rowSize = (width + 3) & ~3; // Align to 4-byte boundary

        //    for (int y = 0; y < height; y++)
        //    {
        //        for (int x = 0; x < width; x++)
        //        {
        //            double z = -image[pixelDataOffset + y * rowSize + x];
        //            points.Add(new Vertex(x, y, z));
        //        }
        //    }

        //    var meshGeometry = new MeshGeometry3D();

        //    // Create arrays for positions and texture coordinates
        //    var positions = new List<Vector3>();
        //    var textureCoordinates = new List<Vector2>();
        //    var indices = new List<int>();

        //    // Fill positions and texture coordinates for the top surface
        //    for (int i = 0; i < points.Count; i++)
        //    {
        //        var point = points[i];
        //        positions.Add(new Vector3((float)point.X, (float)point.Y, (float)point.Z));
        //        textureCoordinates.Add(new Vector2((float)point.X / (width - 1), (float)point.Y / (height - 1)));
        //    }

        //    // Fill positions and texture coordinates for the bottom surface
        //    for (int i = 0; i < points.Count; i++)
        //    {
        //        var point = points[i];
        //        positions.Add(new Vector3((float)point.X, (float)point.Y, 0)); // Z = 0 for the bottom surface
        //        textureCoordinates.Add(new Vector2((float)point.X / (width - 1), (float)point.Y / (height - 1)));
        //    }

        //    // Generate indices for the top surface
        //    for (int y = 0; y < height - 1; y++)
        //    {
        //        for (int x = 0; x < width - 1; x++)
        //        {
        //            int topLeft = y * width + x;
        //            int topRight = topLeft + 1;
        //            int bottomLeft = topLeft + width;
        //            int bottomRight = bottomLeft + 1;

        //            // First triangle
        //            indices.Add(topLeft);
        //            indices.Add(bottomLeft);
        //            indices.Add(topRight);

        //            // Second triangle
        //            indices.Add(topRight);
        //            indices.Add(bottomLeft);
        //            indices.Add(bottomRight);
        //        }
        //    }

        //    // Generate indices for the bottom surface
        //    int offset = points.Count;
        //    for (int y = 0; y < height - 1; y++)
        //    {
        //        for (int x = 0; x < width - 1; x++)
        //        {
        //            int topLeft = y * width + x + offset;
        //            int topRight = topLeft + 1;
        //            int bottomLeft = topLeft + width;
        //            int bottomRight = bottomLeft + 1;

        //            // First triangle
        //            indices.Add(topLeft);
        //            indices.Add(topRight);
        //            indices.Add(bottomLeft);

        //            // Second triangle
        //            indices.Add(topRight);
        //            indices.Add(bottomRight);
        //            indices.Add(bottomLeft);
        //        }
        //    }

        //    // Generate indices for the side faces
        //    for (int y = 0; y < height - 1; y++)
        //    {
        //        for (int x = 0; x < width - 1; x++)
        //        {
        //            int topLeft = y * width + x;
        //            int topRight = topLeft + 1;
        //            int bottomLeft = topLeft + width;
        //            int bottomRight = bottomLeft + 1;

        //            int topLeftBottom = topLeft + offset;
        //            int topRightBottom = topRight + offset;
        //            int bottomLeftBottom = bottomLeft + offset;
        //            int bottomRightBottom = bottomRight + offset;

        //            // Side face 1
        //            indices.Add(topLeft);
        //            indices.Add(bottomLeft);
        //            indices.Add(topLeftBottom);

        //            indices.Add(topLeftBottom);
        //            indices.Add(bottomLeft);
        //            indices.Add(bottomLeftBottom);

        //            // Side face 2
        //            indices.Add(topRight);
        //            indices.Add(topRightBottom);
        //            indices.Add(bottomRight);

        //            indices.Add(topRightBottom);
        //            indices.Add(bottomRightBottom);
        //            indices.Add(bottomRight);
        //        }
        //    }

        //    // Assign the arrays to the meshGeometry
        //    meshGeometry.Positions = new Vector3Collection(positions);
        //    meshGeometry.TextureCoordinates = new Vector2Collection(textureCoordinates);
        //    meshGeometry.Indices = new IntCollection(indices);
        //    meshGeometry.Normals = meshGeometry.CalculateNormals();
        //    return meshGeometry;
        //}

        //public static void CalculateNormalsIn(MeshGeometry3D meshGeometry)
        //{
        //    var normals = new Vector3Collection(meshGeometry.Positions.Count);
        //    for (int i = 0; i < meshGeometry.Positions.Count; i++)
        //    {
        //        normals.Add(new Vector3(0, 0, 0));
        //    }

        //    for (int i = 0; i < meshGeometry.Indices.Count; i += 3)
        //    {
        //        int index1 = meshGeometry.Indices[i];
        //        int index2 = meshGeometry.Indices[i + 1];
        //        int index3 = meshGeometry.Indices[i + 2];

        //        var p1 = meshGeometry.Positions[index1];
        //        var p2 = meshGeometry.Positions[index2];
        //        var p3 = meshGeometry.Positions[index3];

        //        var normal = Vector3.Cross(p2 - p1, p3 - p1);
        //        normal.Normalize();

        //        normals[index1] += normal;
        //        normals[index2] += normal;
        //        normals[index3] += normal;
        //    }

        //    for (int i = 0; i < normals.Count; i++)
        //    {
        //        normals[i].Normalize();
        //    }

        //    meshGeometry.Normals = normals;
        //}
        //public static void CalculateNormalsOut(MeshGeometry3D meshGeometry)
        //{
        //    var normals = new Vector3Collection(meshGeometry.Positions.Count);
        //    for (int i = 0; i < meshGeometry.Positions.Count; i++)
        //    {
        //        normals.Add(new Vector3(0, 0, 0));
        //    }

        //    for (int i = 0; i < meshGeometry.Indices.Count; i += 3)
        //    {
        //        int index1 = meshGeometry.Indices[i];
        //        int index2 = meshGeometry.Indices[i + 1];
        //        int index3 = meshGeometry.Indices[i + 2];

        //        var p1 = meshGeometry.Positions[index1];
        //        var p2 = meshGeometry.Positions[index2];
        //        var p3 = meshGeometry.Positions[index3];

        //        var normal = Vector3.Cross(p2 - p1, p3 - p1);
        //        normal.Normalize();

        //        normals[index1] += normal;
        //        normals[index2] += normal;
        //        normals[index3] += normal;
        //    }

        //    for (int i = 0; i < normals.Count; i++)
        //    {
        //        normals[i].Normalize();
        //        normals[i] = -normals[i]; // Flip the normal direction
        //    }

        //    meshGeometry.Normals = normals;
        //}
        //public static void CalculateNormalsAndReverse(MeshGeometry3D meshGeometry)
        //{
        //    var normals = new Vector3Collection(meshGeometry.Positions.Count);
        //    for (int i = 0; i < meshGeometry.Positions.Count; i++)
        //    {
        //        normals.Add(new Vector3(0, 0, 0));
        //    }

        //    for (int i = 0; i < meshGeometry.Indices.Count; i += 3)
        //    {
        //        int index1 = meshGeometry.Indices[i];
        //        int index2 = meshGeometry.Indices[i + 1];
        //        int index3 = meshGeometry.Indices[i + 2];

        //        var p1 = meshGeometry.Positions[index1];
        //        var p2 = meshGeometry.Positions[index2];
        //        var p3 = meshGeometry.Positions[index3];

        //        var normal = Vector3.Cross(p2 - p1, p3 - p1);
        //        normal.Normalize();

        //        normals[index1] += normal;
        //        normals[index2] += normal;
        //        normals[index3] += normal;

        //        // Calculate and add the normal for the opposite direction
        //        var reverseNormal = -normal;
        //        normals[index1] += reverseNormal;
        //        normals[index2] += reverseNormal;
        //        normals[index3] += reverseNormal;
        //    }

        //    for (int i = 0; i < normals.Count; i++)
        //    {
        //        normals[i].Normalize();
        //    }

        //    meshGeometry.Normals = normals;
        //}
    }
}