using System;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using UkrChatSupport.Core;

namespace UkrChatSupport;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IFramework? Framework { get; private set; }

    private volatile bool isHooked;
    private KeyboardHook? keyboardHook;

    public Configuration Configuration { get; init; }

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        StartHook();
    }

    private void StartHook()
    {
        Framework?.RunOnFrameworkThread(() =>
        {
            if (isHooked || keyboardHook != null) return;
            try
            {
                keyboardHook = new KeyboardHook();
                keyboardHook.OnKeyDown += OnKeyDown;
                keyboardHook.OnError += OnHookError;
                isHooked = true;
            }
            catch (Exception e)
            {
                Log.Error(e, e.Message);
            }
        });
    }

    private void StopHook()
    {
        Framework?.RunOnFrameworkThread(() =>
        {
            if (!isHooked || keyboardHook == null) return;
            try
            {
                keyboardHook.OnKeyDown -= OnKeyDown;
                keyboardHook.OnError -= OnHookError;
                keyboardHook.Dispose();
                keyboardHook = null;
                isHooked = false;
            }
            catch (Exception e)
            {
                Log.Error(e, e.Message);
            }
        });
    }

    private void OnKeyDown(KeyMap.Keys key, bool shift, ref bool skipNext)
    {
        try
        {
            if (!GameFocus.IsGameFocused || !GameFocus.IsChatInputActive)
                return;
            if (!IsUkrainianLayout())
                return;
            ReplaceInput(key, shift, ref skipNext);
        }
        catch (Exception e)
        {
            Log.Error(e, e.Message);
        }
    }

    private static bool IsUkrainianLayout()
    {
        var hwnd = Win32.GetForegroundWindow();
        var threadId = Win32.GetWindowThreadProcessId(hwnd, nint.Zero);
        var culture = Win32.GetCurrentLayout(threadId);
        return culture.TwoLetterISOLanguageName.Equals("uk", StringComparison.OrdinalIgnoreCase);
    }

    private void OnHookError(Exception e) => Log.Error(e, e.Message);

    private static void ReplaceInput(KeyMap.Keys key, bool isShift, ref bool skipNext)
    {
        foreach (var kr in KeyMap.Replacements)
        {
            if (kr.Key != key) continue;
            skipNext = true;
            // Replacement of letters: original key is suppressed (skipNext), replacement character is injected here.
            KeyboardHook.SendCharUnicode(isShift ? kr.RCapitalKey : kr.RKey);
            return;
        }
    }

    public void Dispose() => StopHook();
}
