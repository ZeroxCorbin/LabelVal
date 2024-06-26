using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;
using System.Collections.ObjectModel;

namespace LabelVal.Utilities
{
    public static class VisualTreeHelp
    {
        public static T GetVisual<T>(DependencyObject root, bool forward = false) where T : Visual
        {
            if (forward)
            {
                return GetVisualChild<T>(root) as T;
            }
            else
            {
                var res = GetVisualChild<T>(root) as T;
                if (res == null)
                {
                    Visual v = (Visual)VisualTreeHelper.GetParent(root);
                    if (v is not T parent)
                        parent = GetVisual<T>(v);

                    return parent;
                }
                else
                    return res;
            }
        }

        public static T GetVisualChild<T>(DependencyObject parent) where T : Visual
        {
            T child = default(T);

            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++)
            {
                Visual v = (Visual)VisualTreeHelper.GetChild(parent, i);
                child = v as T;
                if (child == null)
                {
                    child = GetVisualChild<T>(v);
                }
                if (child != null)
                {
                    break;
                }
            }
            return child;
        }
        public static T GetVisualParent<T>(DependencyObject child, int level = 0) where T : Visual
        {
            T parent = default(T);

            if(child == null)
                return null; 
            
            Visual v = (Visual)VisualTreeHelper.GetParent(child);
            parent = v as T;
            if (parent == null || level > 0)
            {
                if (parent != null)
                    level--;

                parent = GetVisualParent<T>(v, level);
            }

            return parent;
        }

        public static Collection<T> GetVisualChildren<T>(DependencyObject current) where T : DependencyObject
        {
            if (current == null)
                return null;

            var children = new Collection<T>();
            GetVisualChildren(current, children);
            return children;
        }
        private static void GetVisualChildren<T>(DependencyObject current, Collection<T> children) where T : DependencyObject
        {
            if (current != null)
            {
                if (current.GetType() == typeof(T))
                    children.Add((T)current);

                for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(current); i++)
                {
                    GetVisualChildren(System.Windows.Media.VisualTreeHelper.GetChild(current, i), children);
                }
            }
        }
    }
}
