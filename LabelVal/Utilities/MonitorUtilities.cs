using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;

namespace LabelVal.Utilities;

/// <summary>
/// Represents the different types of scaling.
/// </summary>
/// <seealso cref="https://msdn.microsoft.com/en-us/library/windows/desktop/dn280511.aspx"/>
[SQLite.StoreAsText]
public enum DPITypes
{
    [Description("Effective DPI")]
    EFFECTIVE = 0,
    [Description("Angular DPI")]
    ANGULAR = 1,
    [Description("Raw DPI")]
    RAW = 2,
}

public static class MonitorUtilities
{
    public static uint DPI { get; private set; }

    static MonitorUtilities() => UpdateDPI(DPITypes.RAW);

    public static uint UpdateDPI(DPITypes dpiType)
    {
        System.Windows.Window win = App.Current.MainWindow;
        GetDpi(dpiType, out uint dpiX, out _, new System.Drawing.Point((int)win.Left, (int)win.Top));
        return DPI = dpiX;
    }

    /// <summary>
    /// Returns the scaling of the given screen.
    /// </summary>
    /// <param name="dpiType">The type of dpi that should be given back..</param>
    /// <param name="dpiX">Gives the horizontal scaling back (in dpi).</param>
    /// <param name="dpiY">Gives the vertical scaling back (in dpi).</param>
    public static void GetDpi(DPITypes dpiType, out uint dpiX, out uint dpiY, System.Drawing.Point point)
    {
        //var point = new System.Drawing.Point(1, 1);
        nint hmonitor = MonitorFromPoint(point, _MONITOR_DEFAULTTONEAREST);

        switch (GetDpiForMonitor(hmonitor, dpiType, out dpiX, out dpiY).ToInt32())
        {
            case _S_OK: return;
            case _E_INVALIDARG:
                throw new ArgumentException("Unknown error. See https://msdn.microsoft.com/en-us/library/windows/desktop/dn280510.aspx for more information.");
            default:
                throw new COMException("Unknown error. See https://msdn.microsoft.com/en-us/library/windows/desktop/dn280510.aspx for more information.");
        }
    }

    public static double GetPixelsPerDip(DPITypes dpiType)
    {
        // Assuming the DPI is obtained for the primary monitor where the main window is located.
        // You might want to adjust the logic to target a specific monitor if necessary.
        System.Windows.Window win = App.Current.MainWindow;
        System.Drawing.Point windowPosition = new System.Drawing.Point((int)win.Left, (int)win.Top);

        GetDpi(dpiType, out uint dpiX, out _, windowPosition);

        // Standard DPI value for Windows is 96.
        const double standardDpi = 96.0;

        // Calculate PixelsPerDip by dividing the horizontal DPI by the standard DPI.
        double pixelsPerDip = dpiX / standardDpi;

        return pixelsPerDip;
    }
    public static double GetPixelsPerDip(Window window, DPITypes dpiType)
    {
        if (window == null)
        {
            throw new ArgumentNullException(nameof(window), "Window cannot be null.");
        }

        // Get the window's position on the screen.
        // Note: This considers the window's top-left corner for determining its monitor.
        System.Drawing.Point windowPosition = new System.Drawing.Point((int)window.Left, (int)window.Top);

        // Call GetDpi using the window's position to get the DPI for the monitor it's displayed on.
        GetDpi(dpiType, out uint dpiX, out _, windowPosition);

        // Standard DPI value for Windows is 96.
        const double standardDpi = 96.0;

        // Calculate PixelsPerDip by dividing the horizontal DPI by the standard DPI.
        double pixelsPerDip = dpiX / standardDpi;

        return pixelsPerDip;
    }

    //https://msdn.microsoft.com/en-us/library/windows/desktop/dd145062.aspx
    [DllImport("User32.dll")]
    private static extern IntPtr MonitorFromPoint([In] System.Drawing.Point pt, [In] uint dwFlags);

    //https://msdn.microsoft.com/en-us/library/windows/desktop/dn280510.aspx
    [DllImport("Shcore.dll")]
    private static extern IntPtr GetDpiForMonitor([In] IntPtr hmonitor, [In] DPITypes dpiType, [Out] out uint dpiX, [Out] out uint dpiY);

    private const int _S_OK = 0;
    private const int _MONITOR_DEFAULTTONEAREST = 2;
    private const int _E_INVALIDARG = -2147024809;

    // Example function to get the DPI X value
    public static double GetDpiX(Window window)
    {
        if (window == null)
        {
            throw new ArgumentNullException(nameof(window), "Window cannot be null.");
        }

        var dpi = VisualTreeHelper.GetDpi(window);
        return dpi.PixelsPerInchX;
    }

    // Example function to get the DPI Y value
    public static double GetDpiY(Window window)
    {
        if (window == null)
        {
            throw new ArgumentNullException(nameof(window), "Window cannot be null.");
        }

        var dpi = VisualTreeHelper.GetDpi(window);
        return dpi.PixelsPerInchY;
    }

    // Example function to get PixelsPerDip
    public static double GetPixelsPerDip(Window window)
    {
        if (window == null)
        {
            throw new ArgumentNullException(nameof(window), "Window cannot be null.");
        }

        var dpi = VisualTreeHelper.GetDpi(window);
        return dpi.PixelsPerInchX / 96.0; // Assuming 96 DPI as the standard DPI
    }

    public static DpiScale GetDpi(Window window) => 
        window == null ? throw new ArgumentNullException(nameof(window), "Window cannot be null.") : VisualTreeHelper.GetDpi(window);
    public static DpiScale GetDpi() =>
    App.Current.MainWindow == null ? throw new ArgumentNullException(nameof(App.Current.MainWindow), "Window cannot be null.") : VisualTreeHelper.GetDpi(App.Current.MainWindow);
}
