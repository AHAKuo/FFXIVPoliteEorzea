using System;
using System.Text;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace PoliteEorzea;

/// <summary>
/// Sends text through the game's own chat box entry point, exactly as if the
/// player had typed it and pressed Enter.
/// </summary>
public static unsafe class ChatSender
{
    private const int MaxBytes = 500;

    /// <summary>Send a raw chat line (including any leading command such as "/p").</summary>
    /// <returns>Null on success, otherwise a human-readable reason it was refused.</returns>
    public static string? Send(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "Message is empty.";

        foreach (var c in text)
        {
            if (c == '\n' || c == '\r' || (char.IsControl(c) && c != '\t'))
                return "Message contains a line break or control character.";
        }

        var bytes = Encoding.UTF8.GetBytes(text);
        if (bytes.Length > MaxBytes)
            return $"Message is too long ({bytes.Length} bytes, max {MaxBytes}).";

        var uiModule = UIModule.Instance();
        if (uiModule == null)
            return "Game UI is not ready.";

        var str = Utf8String.FromString(text);
        try
        {
            uiModule->ProcessChatBoxEntry(str, 0, false);
        }
        finally
        {
            str->Dtor(true);
        }

        return null;
    }

    /// <summary>Command prefix for a chat target ("/p", "/a", "/s").</summary>
    public static string Prefix(ChatTarget target, bool inAlliance) => target switch
    {
        ChatTarget.Party => "/p",
        ChatTarget.Alliance => "/a",
        ChatTarget.Say => "/s",
        _ => inAlliance ? "/a" : "/p",
    };

    public static string TargetName(ChatTarget target, bool inAlliance) => target switch
    {
        ChatTarget.Party => "party",
        ChatTarget.Alliance => "alliance",
        ChatTarget.Say => "say",
        _ => inAlliance ? "alliance" : "party",
    };
}
