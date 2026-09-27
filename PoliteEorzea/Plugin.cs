using System;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using PoliteEorzea.Windows;

namespace PoliteEorzea;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static ICondition Condition { get; private set; } = null!;
    [PluginService] internal static IDutyState DutyState { get; private set; } = null!;
    [PluginService] internal static IPartyList PartyList { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    private const string MainCommand = "/polite";
    private const string MainCommandAlias = "/politeeorzea";
    private const string HiCommand = "/hi";
    private const string ByeCommand = "/bye";

    public Configuration Configuration { get; }
    public MessagePicker Picker { get; } = new();
    public DutyWatcher Watcher { get; }

    public readonly WindowSystem WindowSystem = new("PoliteEorzea");
    private ConfigWindow ConfigWindow { get; }

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        ConfigWindow = new ConfigWindow(this);
        WindowSystem.AddWindow(ConfigWindow);

        Watcher = new DutyWatcher(this);

        CommandManager.AddHandler(MainCommand, new CommandInfo(OnMainCommand)
        {
            HelpMessage = "Open Polite Eorzea settings. \"/polite auto\" toggles Auto mode, \"/polite on|off\" sets it.",
        });
        CommandManager.AddHandler(MainCommandAlias, new CommandInfo(OnMainCommand) { ShowInHelp = false });
        CommandManager.AddHandler(HiCommand, new CommandInfo(OnHiCommand)
        {
            HelpMessage = "Send a random greeting to party/alliance. Optional: \"/hi p\", \"/hi a\", \"/hi s\" to force party, alliance or say.",
        });
        CommandManager.AddHandler(ByeCommand, new CommandInfo(OnByeCommand)
        {
            HelpMessage = "Send a random farewell to party/alliance. Optional: \"/bye p\", \"/bye a\", \"/bye s\".",
        });

        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi += ToggleConfigUi;

        Log.Information("Polite Eorzea loaded. Auto mode: {Auto}", Configuration.AutoMode ? "on" : "off");
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleConfigUi;

        Watcher.Dispose();
        WindowSystem.RemoveAllWindows();
        ConfigWindow.Dispose();

        CommandManager.RemoveHandler(MainCommand);
        CommandManager.RemoveHandler(MainCommandAlias);
        CommandManager.RemoveHandler(HiCommand);
        CommandManager.RemoveHandler(ByeCommand);
    }

    public void ToggleConfigUi() => ConfigWindow.Toggle();

    public void SetAutoMode(bool on)
    {
        Configuration.AutoMode = on;
        Configuration.Save();
        if (!on) Watcher.Cancel();
        Feedback($"Auto mode {(on ? "enabled" : "disabled")}.", always: true);
    }

    private void OnMainCommand(string command, string args)
    {
        switch (args.Trim().ToLowerInvariant())
        {
            case "auto":
            case "toggle":
                SetAutoMode(!Configuration.AutoMode);
                break;
            case "on":
                SetAutoMode(true);
                break;
            case "off":
                SetAutoMode(false);
                break;
            default:
                ToggleConfigUi();
                break;
        }
    }

    private void OnHiCommand(string command, string args) => SendGreeting(ParseTarget(args), auto: false);
    private void OnByeCommand(string command, string args) => SendFarewell(ParseTarget(args), auto: false);

    private ChatTarget ParseTarget(string args) => args.Trim().ToLowerInvariant() switch
    {
        "p" or "party" => ChatTarget.Party,
        "a" or "alliance" => ChatTarget.Alliance,
        "s" or "say" => ChatTarget.Say,
        _ => Configuration.DefaultTarget,
    };

    public void SendGreeting(ChatTarget target, bool auto)
    {
        var text = Picker.NextGreeting(Configuration);
        if (text == null)
        {
            Feedback("No greetings configured. Add some in /polite.", always: true);
            return;
        }
        SendLine(text, target, auto ? "auto greeting" : "greeting");
    }

    public void SendFarewell(ChatTarget target, bool auto)
    {
        var text = Picker.NextFarewell(Configuration);
        if (text == null)
        {
            Feedback("No farewells configured. Add some in /polite.", always: true);
            return;
        }
        SendLine(text, target, auto ? "auto farewell" : "farewell");
    }

    private void SendLine(string text, ChatTarget target, string what)
    {
        if (!ClientState.IsLoggedIn)
        {
            Feedback("Not logged in.", always: true);
            return;
        }

        var inAlliance = PartyList.IsAlliance;
        var prefix = ChatSender.Prefix(target, inAlliance);

        // Messages never get to smuggle their own command in front of the channel prefix.
        var body = text.TrimStart('/').Trim();
        var line = $"{prefix} {body}";

        var error = ChatSender.Send(line);
        if (error != null)
        {
            Feedback($"Could not send {what}: {error}", always: true);
            Log.Warning("Send failed ({What}): {Error}", what, error);
            return;
        }

        Log.Information("Sent {What} to {Target}: {Text}", what, ChatSender.TargetName(target, inAlliance), body);
    }

    /// <summary>Print a note to the local chat log (visible only to this player).</summary>
    public void Feedback(string message, bool always = false)
    {
        if (!always && !Configuration.ShowFeedback)
            return;
        ChatGui.Print(message, "Polite Eorzea");
    }
}
