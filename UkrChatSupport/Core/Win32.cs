using System.Globalization;
using System.Runtime.InteropServices;
using System.Security;

namespace UkrChatSupport.Core;

[SuppressUnmanagedCodeSecurity]
public static class Win32
{
    [DllImport("user32.dll")]
    public static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(nint hwnd, nint process);

    [DllImport("user32.dll")]
    private static extern nint GetKeyboardLayout(uint thread);

    public static CultureInfo GetCurrentLayout(uint windowThreadProcessId)
    {
        try
        {
            return new CultureInfo(GetKeyboardLayout(windowThreadProcessId).ToInt32() & 0xFFFF);
        }
        catch
        {
            return new CultureInfo(1033);
        }
    }
}
