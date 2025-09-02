using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;

namespace LabelVal.Utilities;

public static class VisualTreeHelp
{
    public static T GetVisual<T>(DependencyObject root, bool forward = false) where T : Visual
    {
        if (forward)
        {
            return GetVisualChild<T>(root);
        }
        else
        {
            if (GetVisualChild<T>(root) is not T res)
            {
                var v = (Visual)VisualTreeHelper.GetParent(root);
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
        var child = default(T);

        var numVisuals = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < numVisuals; i++)
        {
            var v = (Visual)VisualTreeHelper.GetChild(parent, i);
            child = v as T;
            child ??= GetVisualChild<T>(v);
            if (child != null)
            {
                break;
            }
        }

        return child;
    }

    public static T GetVisualParent<T>(DependencyObject child, int level = 0) where T : Visual
    {
        if (child == null)
            return null;

        var v = (Visual)VisualTreeHelper.GetParent(child);
        var parent = v as T;
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

        Collection<T> children = [];
        GetVisualChildren(current, children);
        return children;
    }

    private static void GetVisualChildren<T>(DependencyObject current, Collection<T> children) where T : DependencyObject
    {
        if (current == null)
            return;

        if (current.GetType() == typeof(T))
            children.Add((T)current);

        for (var i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(current); i++)
            GetVisualChildren(System.Windows.Media.VisualTreeHelper.GetChild(current, i), children);
    }

    // New method with level variable
    public static Collection<T> GetVisualChildren<T>(DependencyObject current, int level) where T : DependencyObject
    {
        Collection<T> children = [];
        GetVisualChildren(current, children, level);
        return children;
    }

    private static void GetVisualChildren<T>(DependencyObject current, Collection<T> children, int level) where T : DependencyObject
    {
        if (current == null || level < 0)
            return;

        if (current.GetType() == typeof(T))
            children.Add((T)current);

        if (level > 0)
        {
            var cnt = System.Windows.Media.VisualTreeHelper.GetChildrenCount(current);
            for (var i = 0; i < cnt; i++)
                GetVisualChildren(System.Windows.Media.VisualTreeHelper.GetChild(current, i), children, level - 1);
        }
    }
}
