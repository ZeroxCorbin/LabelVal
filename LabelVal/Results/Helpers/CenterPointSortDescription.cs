using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelVal.Results.Helpers
{
    public class CenterPointSortDescription: IComparer<Point>
    {
        public int Compare(Point x, Point y)
        {
            if (x.X == y.X)
            {
                return x.Y.CompareTo(y.Y);
            }
            return x.X.CompareTo(y.X);
        }
    }

    public class PointComparer : IComparer
    {
        public int Compare(object x, object y)
        {
            if (x is Point point1 && y is Point point2)
            {
                // Sort by X coordinate first, then by Y coordinate
                int result = point1.X.CompareTo(point2.X);
                return result != 0 ? result : point1.Y.CompareTo(point2.Y);
            }
            throw new ArgumentException("Objects are not of type Point.");
        }
    }
}
