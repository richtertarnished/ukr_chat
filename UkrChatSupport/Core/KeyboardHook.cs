#pragma warning disable CS0649
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using static UkrChatSupport.Core.KeyMap;

namespace UkrChatSupport.Core;

public class KeyboardHook : IDisposable
{
    public delegate void ErrorEventHandler(Exception e);
    public delegate void LocalKeyEventHandler(Keys key, bool shift, ref bool skipNext);

    public enum KeyEvents
    {
        KeyDown = 0x100,
        KeyUp = 257,
        SKeyDown = 260,
        SKeyUp = 261
    }

    private readonly nint hookID;
    private readonly CallbackDelegate theHookCb;
    private bool isFinalized;

    public KeyboardHook()
    {
        theHookCb = KeybHookProc;
        var hInstance = LoadLibrary("User32");
        hookID = SetWindowsHookEx(HookType.WH_KEYBOARD_LL, theHookCb, hInstance, 0);

        if (hookID == nint.Zero)
        {
            var errorCode = Marshal.GetLastWin32Error();
            throw new Win32Exception(
                errorCode,
                $"Failed to set keyboard hook. Error {errorCode}: {new Win32Exception(errorCode).Message}");
        }
    }

    public void Dispose()
    {
        if (isFinalized) return;
        UnhookWindowsHookEx(hookID);
        isFinalized = true;
    }

    public event LocalKeyEventHandler? OnKeyDown;
    public event ErrorEventHandler? OnError;

    [DllImport("user32", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
    private static extern nint SetWindowsHookEx(HookType idHook, CallbackDelegate lpfn, nint hInstance, int threadId);

    [DllImport("user32", CallingConvention = CallingConvention.StdCall)]
    private static extern bool UnhookWindowsHookEx(nint idHook);

    [DllImport("user32", CallingConvention = CallingConvention.StdCall)]
    private static extern int CallNextHookEx(nint idHook, int nCode, nint wParam, nint lParam);

    [DllImport("kernel32.dll")]
    private static extern nint LoadLibrary(string lpFileName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint numberOfInputs, INPUT[] inputs, int sizeOfInputStructure);

    /// <summary>Inject a single Unicode character as keyboard input (used for letter replacement).</summary>
    public static void SendCharUnicode(int utf32)
    {
        const uint KEYEVENTF_UNICODE = 4u;
        const uint KEYEVENTF_KEYUP = 2u;
        var text = char.ConvertFromUtf32(utf32);
        var list = new List<INPUT>();
        foreach (var c in text)
        {
            var down = default(INPUT);
            down.Type = 1u;
            down.Data.Keyboard.Vk = 0;
            down.Data.Keyboard.Scan = c;
            down.Data.Keyboard.Flags = KEYEVENTF_UNICODE;
            list.Add(down);
            var up = default(INPUT);
            up.Type = 1u;
            up.Data.Keyboard.Vk = 0;
            up.Data.Keyboard.Scan = c;
            up.Data.Keyboard.Flags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP;
            list.Add(up);
        }
        var array = list.ToArray();
        SendInput((uint)array.Length, array, Marshal.SizeOf(typeof(INPUT)));
    }

    ~KeyboardHook()
    {
        if (!isFinalized)
        {
            UnhookWindowsHookEx(hookID);
            isFinalized = true;
        }
    }

    [STAThread]
    private int KeybHookProc(int code, nint w, nint l)
    {
        if (code < 0) return CallNextHookEx(hookID, code, w, l);
        var skipNext = false;
        try
        {
            var keyEvents = (KeyEvents)(int)w;
            var key = Marshal.ReadInt32(l);
            switch (keyEvents)
            {
                case KeyEvents.KeyDown or KeyEvents.SKeyDown when OnKeyDown != null:
                    OnKeyDown((Keys)key, GetShiftPressed(), ref skipNext);
                    break;
            }
        }
        catch (Exception e)
        {
            OnError?.Invoke(e);
        }
        return skipNext ? -1 : CallNextHookEx(hookID, code, w, l);
    }

    [DllImport("user32.dll")]
    public static extern short GetKeyState(Keys nVirtKey);

    public static bool GetShiftPressed() => GetKeyState(Keys.ShiftKey) is > 1 or < -1;

    private delegate int CallbackDelegate(int code, nint w, nint l);

    private enum HookType
    {
        WH_KEYBOARD_LL = 13
    }

    internal struct INPUT
    {
        public uint Type;
        public InputUnion Data;
    }

    /// <summary>Union size (28) = max of KEYBDINPUT, MOUSEINPUT, HARDWAREINPUT so SendInput gets correct layout.</summary>
    [StructLayout(LayoutKind.Explicit, Size = 28)]
    internal struct InputUnion
    {
        [FieldOffset(0)] public KEYBDINPUT Keyboard;
    }

    internal struct KEYBDINPUT
    {
        public ushort Vk;
        public ushort Scan;
        public uint Flags;
        public uint Time;
        public nint ExtraInfo;
    }
}
#pragma warning restore CS0649
