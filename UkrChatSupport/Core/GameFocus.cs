using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace UkrChatSupport.Core;

public unsafe static class GameFocus
{
    public static bool IsGameFocused => !Framework.Instance()->WindowInactive;
    public static bool IsChatInputActive => RaptureAtkModule.Instance()->AtkModule.IsTextInputActive();
}
