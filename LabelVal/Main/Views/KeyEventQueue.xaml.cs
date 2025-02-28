using Org.BouncyCastle.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WinAPI.lib;

namespace LabelVal.Main.Views;
/// <summary>
/// Interaction logic for KeyEventQueue.xaml
/// </summary>
/// 

public struct KeyAndState
{
    public Key Key;
    public byte[] KeyboardState;

    public KeyAndState(Key key, byte[] state)
    {
        Key = key;
        KeyboardState = state;
    }
}

public partial class KeyEventQueue : UserControl
{
    private const int WM_KEYDOWN = 0x0100;

    [DllImport("user32.dll")]
    static extern bool PostMessage(IntPtr hWnd, UInt32 Msg, int wParam, int lParam);

    [DllImport("user32.dll")]
    static extern bool GetKeyboardState(byte[] lpKeyState);

    [DllImport("user32.dll")]
    static extern bool SetKeyboardState(byte[] lpKeyState);

    private IntPtr _handle;
    private bool _isMonitoring = true;

    private Queue<KeyAndState> _eventQ = new Queue<KeyAndState>();

    public KeyEventQueue()
    {
        InitializeComponent();

        this.Focusable = true;
        this.Loaded += KeyEventQueue_Loaded;
        this.PreviewKeyDown += KeyEventQueue_PreviewKeyDown;
        this.btn.Click += (s, e) => ReplayKeyEvents();
    }

    void KeyEventQueue_Loaded(object sender, RoutedEventArgs e)
    {

    }

    /// <summary>
    /// Get key and keyboard state (modifier keys), store them in a queue
    /// </summary>
    void KeyEventQueue_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (_isMonitoring)
        {
           // int key = KeyInterop.VirtualKeyFromKey(e.Key);
            byte[] state = new byte[256];
            GetKeyboardState(state);
            _eventQ.Enqueue(new KeyAndState(e.Key, state));
        }
    }

    /// <summary>
    /// Replay key events from queue
    /// </summary>
    private async void ReplayKeyEvents()
    {
        _handle = WinAPI.lib.WinAPI.FindWindow(null, "lvs-95xx"); // get handle to window

        if (_handle == IntPtr.Zero)
        {
            //MessageBox.Show("Window not found");
            return;
        }
        WinAPI.lib.WinAPI.SetForegroundWindow(_handle); // bring window to front

        _isMonitoring = false; // no longer add to queue
                               // thread the dequeueing, because the sequence of inputs is not preserved 
                               // unless a small delay between them is introduced. Normally the effect this
                               // produces should be very acceptable for an UI.
        byte[] state = new byte[256];
        GetKeyboardState(state);
        state[0x11] = 128;
        SetKeyboardState(state);
        GetKeyboardState(state);
        PostMessage(_handle, WM_KEYDOWN, (int) WinAPI.lib.WinAPI.CharToVirtualKey('Q'), 0);
        state[0x11] = 1;
        SetKeyboardState(state);
        _eventQ.Clear();
        _isMonitoring = true;
    }

}
