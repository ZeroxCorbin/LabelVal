using HelixToolkit.Wpf.SharpDX;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelVal.ImageViewer3D.Views;
public class CutPlaneBoxIndicator
{
    private MeshGeometryModel3D edgeHandle;
    private MeshGeometryModel3D cornerHandle;
    private Plane cutPlane;
    private Vector3 modelSize;

    public CutPlaneBoxIndicator(MeshGeometryModel3D edgeHandle, MeshGeometryModel3D cornerHandle)
    {
        this.edgeHandle = edgeHandle;
        this.cornerHandle = cornerHandle;
    }

    public void Update(Plane cutPlane, Vector3 modelSize)
    {
        this.cutPlane = cutPlane;
        this.modelSize = modelSize;
        UpdateBoxGeometry();
    }

    private void UpdateBoxGeometry()
    {
        Vector3 planeNormal = cutPlane.Normal;
        Vector3 boxSize = CalculateBoxSize();
        Matrix rotation = CalculateRotationMatrix(planeNormal);

        // Create corner instances
        cornerHandle.Instances = new[]
        {
            CreateCornerMatrix(new Vector3(0, 0, 0), rotation),
            CreateCornerMatrix(new Vector3(boxSize.X, 0, 0), rotation),
            CreateCornerMatrix(new Vector3(boxSize.X, boxSize.Y, 0), rotation),
            CreateCornerMatrix(new Vector3(0, boxSize.Y, 0), rotation),
            CreateCornerMatrix(new Vector3(0, 0, boxSize.Z), rotation),
            CreateCornerMatrix(new Vector3(boxSize.X, 0, boxSize.Z), rotation),
            CreateCornerMatrix(new Vector3(boxSize.X, boxSize.Y, boxSize.Z), rotation),
            CreateCornerMatrix(new Vector3(0, boxSize.Y, boxSize.Z), rotation)
        };

        // Create edge instances
        edgeHandle.Instances = new[]
        {
            CreateEdgeMatrix(new Vector3(0, 0, 0), new Vector3(boxSize.X, 0, 0), rotation),
            CreateEdgeMatrix(new Vector3(boxSize.X, 0, 0), new Vector3(boxSize.X, boxSize.Y, 0), rotation),
            CreateEdgeMatrix(new Vector3(boxSize.X, boxSize.Y, 0), new Vector3(0, boxSize.Y, 0), rotation),
            CreateEdgeMatrix(new Vector3(0, boxSize.Y, 0), new Vector3(0, 0, 0), rotation),
            CreateEdgeMatrix(new Vector3(0, 0, boxSize.Z), new Vector3(boxSize.X, 0, boxSize.Z), rotation),
            CreateEdgeMatrix(new Vector3(boxSize.X, 0, boxSize.Z), new Vector3(boxSize.X, boxSize.Y, boxSize.Z), rotation),
            CreateEdgeMatrix(new Vector3(boxSize.X, boxSize.Y, boxSize.Z), new Vector3(0, boxSize.Y, boxSize.Z), rotation),
            CreateEdgeMatrix(new Vector3(0, boxSize.Y, boxSize.Z), new Vector3(0, 0, boxSize.Z), rotation),
            CreateEdgeMatrix(new Vector3(0, 0, 0), new Vector3(0, 0, boxSize.Z), rotation),
            CreateEdgeMatrix(new Vector3(boxSize.X, 0, 0), new Vector3(boxSize.X, 0, boxSize.Z), rotation),
            CreateEdgeMatrix(new Vector3(boxSize.X, boxSize.Y, 0), new Vector3(boxSize.X, boxSize.Y, boxSize.Z), rotation),
            CreateEdgeMatrix(new Vector3(0, boxSize.Y, 0), new Vector3(0, boxSize.Y, boxSize.Z), rotation)
        };
    }

    private Vector3 CalculateBoxSize()
    {
        // Calculate box size based on model size and cut plane orientation
        // This is a simplified example; you may need to adjust this based on your specific requirements
        return new Vector3(
            Math.Abs(Vector3.Dot(cutPlane.Normal, Vector3.UnitX)) < 0.01f ? modelSize.X : modelSize.Y,
            Math.Abs(Vector3.Dot(cutPlane.Normal, Vector3.UnitY)) < 0.01f ? modelSize.Y : modelSize.Z,
            Math.Abs(Vector3.Dot(cutPlane.Normal, Vector3.UnitZ)) < 0.01f ? modelSize.Z : modelSize.X
        );
    }

    private Matrix CalculateRotationMatrix(Vector3 normal)
    {
        // Calculate rotation matrix to align the box with the cut plane
        Vector3 up = Vector3.UnitY;
        if (Math.Abs(Vector3.Dot(normal, up)) > 0.99f)
        {
            up = Vector3.UnitZ;
        }
        Vector3 right = Vector3.Cross(up, normal);
        up = Vector3.Cross(normal, right);
        return Matrix.LookAtLH(Vector3.Zero, normal, up);
    }

    private Matrix CreateCornerMatrix(Vector3 position, Matrix rotation)
    {
        return Matrix.Scaling(0.1f) * Matrix.Translation(position) * rotation;
    }

    private Matrix CreateEdgeMatrix(Vector3 start, Vector3 end, Matrix rotation)
    {
        Vector3 edge = end - start;
        float length = edge.Length();
        Vector3 direction = Vector3.Normalize(edge);
        Vector3 scale = new Vector3(length, 0.05f, 0.05f);

        Quaternion rotationQuat = Quaternion.RotationMatrix(Matrix.LookAtLH(Vector3.Zero, direction, Vector3.UnitY));

        return Matrix.Scaling(scale) * Matrix.RotationQuaternion(rotationQuat) * Matrix.Translation(start) * rotation;
    }
}
