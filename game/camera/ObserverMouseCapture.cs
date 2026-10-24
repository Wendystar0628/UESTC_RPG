using System.ComponentModel;
using System.Runtime.InteropServices;
using Godot;

namespace Campus.Camera;

/// <summary>
/// Owns only this observer's native capture. Godot's window focus does not identify the Windows
/// input desktop, so Win32 supplies that fact before Godot may clip or reposition the session cursor.
/// </summary>
internal sealed class ObserverMouseCapture
{
    private readonly bool _headless = DisplayServer.GetName() == "headless";
    private readonly bool _windows = OperatingSystem.IsWindows();
    private bool _captured;

    internal bool IsInputDesktop
    {
        get
        {
            if (_headless) return false;
            if (!_windows) return true;
            nint window = (nint)DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle);
            if (window == 0) return false;
            uint thread = GetWindowThreadProcessId(window, out _);
            if (thread == 0) throw new Win32Exception(Marshal.GetLastWin32Error());
            nint desktop = GetThreadDesktop(thread);
            if (desktop == 0) throw new Win32Exception(Marshal.GetLastWin32Error());
            // GetThreadDesktop returns a borrowed handle; it must not be closed here.
            const int UoiIo = 6;
            if (!GetUserObjectInformation(desktop, UoiIo, out int receivingInput, sizeof(int), out _))
                throw new Win32Exception(Marshal.GetLastWin32Error());
            return receivingInput != 0;
        }
    }

    private bool CanCapture()
    {
        if (!IsInputDesktop) return false;
        if (!_windows) return DisplayServer.WindowIsFocused();
        nint window = (nint)DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle);
        if (GetForegroundWindow() != window) return false;
        uint thread = GetWindowThreadProcessId(window, out _);
        if (thread == 0) throw new Win32Exception(Marshal.GetLastWin32Error());
        var info = new GuiThreadInfo { Size = (uint)Marshal.SizeOf<GuiThreadInfo>() };
        if (!GetGUIThreadInfo(thread, ref info)) throw new Win32Exception(Marshal.GetLastWin32Error());
        return info.Focus == window;
    }

    public void Refresh(bool requested)
    {
        bool capture = requested && CanCapture();
        if (capture == _captured) return;
        // Visible is only the release of capture acquired by this owner. A background owner
        // that never captured must not even write Visible: Godot implements it with ClipCursor(NULL).
        Input.MouseMode = capture ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;
        _captured = capture;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct GuiThreadInfo
    {
        public uint Size, Flags;
        public nint Active, Focus, Capture, MenuOwner, MoveSize, Caret;
        public int Left, Top, Right, Bottom;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(nint window, out uint process);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint GetThreadDesktop(uint thread);
    [DllImport("user32.dll", EntryPoint = "GetUserObjectInformationW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetUserObjectInformation(nint handle, int index, out int value,
        uint length, out uint needed);
    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetGUIThreadInfo(uint thread, ref GuiThreadInfo info);
}
