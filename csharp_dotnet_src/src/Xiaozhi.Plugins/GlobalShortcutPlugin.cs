using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Interop;

namespace Xiaozhi.Plugins;

public class GlobalShortcutPlugin : IPlugin
{
    public string Name => "GlobalShortcutPlugin";

    public event Action? OnManualTalkTriggered;
    public event Action? OnAutoTalkToggled;
    public event Action? OnAbortTriggered;

    private const int WM_HOTKEY = 0x0312;
    private const uint MOD_CONTROL = 0x0002;
    private const uint VK_J = 0x4A;
    private const uint VK_K = 0x4B;
    private const uint VK_Q = 0x51;

    private const int HOTKEY_ID_J = 9001;
    private const int HOTKEY_ID_K = 9002;
    private const int HOTKEY_ID_Q = 9003;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private IntPtr _hWnd;
    private HwndSource? _hwndSource;

    public void RegisterWindow(IntPtr hWnd)
    {
        _hWnd = hWnd;
        _hwndSource = HwndSource.FromHwnd(hWnd);
        _hwndSource?.AddHook(HwndHook);

        RegisterHotKey(_hWnd, HOTKEY_ID_J, MOD_CONTROL, VK_J); // Ctrl + J
        RegisterHotKey(_hWnd, HOTKEY_ID_K, MOD_CONTROL, VK_K); // Ctrl + K
        RegisterHotKey(_hWnd, HOTKEY_ID_Q, MOD_CONTROL, VK_Q); // Ctrl + Q
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            int id = wParam.ToInt32();
            if (id == HOTKEY_ID_J)
            {
                OnManualTalkTriggered?.Invoke();
                handled = true;
            }
            else if (id == HOTKEY_ID_K)
            {
                OnAutoTalkToggled?.Invoke();
                handled = true;
            }
            else if (id == HOTKEY_ID_Q)
            {
                OnAbortTriggered?.Invoke();
                handled = true;
            }
        }
        return IntPtr.Zero;
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public Task ShutdownAsync()
    {
        if (_hWnd != IntPtr.Zero)
        {
            UnregisterHotKey(_hWnd, HOTKEY_ID_J);
            UnregisterHotKey(_hWnd, HOTKEY_ID_K);
            UnregisterHotKey(_hWnd, HOTKEY_ID_Q);
        }
        _hwndSource?.RemoveHook(HwndHook);
        return Task.CompletedTask;
    }
}
