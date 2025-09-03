using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelVal.Utilities;
public class BitmapHeaderViewer
{
    public static void ViewBitmapHeader(string path)
    {
        var bitmap = BitmapUtilities.LoadBitmap(path);
        var header = new StringBuilder();
        header.AppendLine($"Path: {path}");

        header.AppendLine($"Width: {bitmap.Width}");
        header.AppendLine($"Height: {bitmap.Height}");
        header.AppendLine($"Horizontal Resolution: {bitmap.HorizontalResolution}");
        header.AppendLine($"Vertical Resolution: {bitmap.VerticalResolution}");
        header.AppendLine($"PixelFormat: {bitmap.PixelFormat}");
        header.AppendLine($"RawFormat: {bitmap.RawFormat}");
        header.AppendLine($"Size: {bitmap.Size}");

        header.AppendLine($"Physical Dimension Width:  {bitmap.PhysicalDimension.Width}");
        header.AppendLine($"Physical Dimension Height: {bitmap.PhysicalDimension.Height}");
    }
}
