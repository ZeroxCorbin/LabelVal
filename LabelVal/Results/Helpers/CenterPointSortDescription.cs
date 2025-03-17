using System;
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
}
