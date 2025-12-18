using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;

namespace clib.Extensions;

public static class ChatExtensions {
    public static void PrintMessage(this IChatGui chat, string message)
        => chat.Print(new XivChatEntry {
            Type = XivChatType.Echo,
            Message = $"[{CLibMain.Name}] {message}"
        });

    public static void PrintError(this IChatGui chat, string message)
        => chat.Print(new XivChatEntry {
            Type = XivChatType.SystemError,
            Message = $"[{CLibMain.Name}] {message}"
        });

    public static void PrintColor(this IChatGui chat, string message, UIColor color)
        => chat.Print(new XivChatEntry {
            Type = XivChatType.Echo,
            Message = new SeString(
                new UIForegroundPayload((ushort)color.RowId),
                new TextPayload($"[{CLibMain.Name}] {message}"),
                UIForegroundPayload.UIForegroundOff)
        });
}
