using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

namespace PoliteEorzea.Windows;

public class ConfigWindow : Window, IDisposable
{
    private static readonly string[] TriggerNames = ["When I load into the duty", "When the duty starts (barrier drops)"];
    private static readonly string[] TargetNames = ["Auto (alliance if in one, else party)", "Party", "Alliance", "Say"];

    private readonly Plugin plugin;
    private string newGreeting = string.Empty;
    private string newFarewell = string.Empty;

    public ConfigWindow(Plugin plugin) : base("Polite Eorzea###PoliteEorzeaConfig")
    {
        this.plugin = plugin;
        Size = new Vector2(520, 620);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints { MinimumSize = new Vector2(420, 300), MaximumSize = new Vector2(2000, 2000) };
    }

    public void Dispose() { }

    private Configuration Config => plugin.Configuration;

    public override void Draw()
    {
        DrawAutoSection();
        ImGui.Separator();
        DrawSendingSection();
        ImGui.Separator();
        DrawQuickButtons();
        ImGui.Separator();

        if (ImGui.CollapsingHeader("Greetings", ImGuiTreeNodeFlags.DefaultOpen))
            DrawList(Config.Greetings, ref newGreeting, "greeting");

        if (ImGui.CollapsingHeader("Farewells", ImGuiTreeNodeFlags.DefaultOpen))
            DrawList(Config.Farewells, ref newFarewell, "farewell");
    }

    private void DrawAutoSection()
    {
        var auto = Config.AutoMode;
        using (ImRaii.PushColor(ImGuiCol.Text, auto ? new Vector4(0.45f, 1f, 0.55f, 1f) : new Vector4(1f, 1f, 1f, 1f)))
        {
            if (ImGui.Checkbox("Auto mode", ref auto))
                plugin.SetAutoMode(auto);
        }
        Tooltip("Automatically greet when you enter an instanced duty (dungeon, trial, raid, alliance raid...)\nand say thanks when it completes. Toggle from chat with /polite auto.");

        ImGui.SameLine();
        ImGui.TextDisabled(plugin.Watcher.Status);

        using var disabled = ImRaii.Disabled(!auto);
        ImGui.Indent();

        var greet = Config.AutoGreet;
        if (ImGui.Checkbox("Greet on enter", ref greet)) { Config.AutoGreet = greet; Config.Save(); }

        ImGui.SameLine(200);
        var bye = Config.AutoFarewell;
        if (ImGui.Checkbox("Farewell on completion", ref bye)) { Config.AutoFarewell = bye; Config.Save(); }

        var trigger = (int)Config.GreetTrigger;
        ImGui.SetNextItemWidth(300);
        if (ImGui.Combo("Greet timing", ref trigger, TriggerNames, TriggerNames.Length))
        {
            Config.GreetTrigger = (GreetTrigger)trigger;
            Config.Save();
        }
        Tooltip("\"Load\" says hi while everyone is gathering, before the countdown.\n\"Duty starts\" waits for the barrier to drop.");

        var gMin = Config.GreetDelayMin; var gMax = Config.GreetDelayMax;
        ImGui.SetNextItemWidth(300);
        if (ImGui.DragFloatRange2("Greeting delay (s)", ref gMin, ref gMax, 0.1f, 0f, 30f, "%.1f", "%.1f"))
        {
            Config.GreetDelayMin = gMin; Config.GreetDelayMax = gMax; Config.Save();
        }
        Tooltip("A random delay in this range, so it does not look robotic.");

        var fMin = Config.FarewellDelayMin; var fMax = Config.FarewellDelayMax;
        ImGui.SetNextItemWidth(300);
        if (ImGui.DragFloatRange2("Farewell delay (s)", ref fMin, ref fMax, 0.1f, 0f, 60f, "%.1f", "%.1f"))
        {
            Config.FarewellDelayMin = fMin; Config.FarewellDelayMax = fMax; Config.Save();
        }

        var skipSolo = Config.SkipWhenSolo;
        if (ImGui.Checkbox("Skip when not in a party", ref skipSolo)) { Config.SkipWhenSolo = skipSolo; Config.Save(); }
        Tooltip("Solo story instances and unsynced solo runs stay quiet.");

        ImGui.SameLine(200);
        var skipPvp = Config.SkipPvP;
        if (ImGui.Checkbox("Skip PvP duties", ref skipPvp)) { Config.SkipPvP = skipPvp; Config.Save(); }

        ImGui.Unindent();
    }

    private void DrawSendingSection()
    {
        var target = (int)Config.DefaultTarget;
        ImGui.SetNextItemWidth(300);
        if (ImGui.Combo("Send to", ref target, TargetNames, TargetNames.Length))
        {
            Config.DefaultTarget = (ChatTarget)target;
            Config.Save();
        }
        Tooltip("Used by Auto mode and by /hi and /bye without an argument.\n/hi p, /hi a, /hi s force party, alliance or say for one message.");

        var rnd = Config.RandomOrder;
        if (ImGui.Checkbox("Pick messages at random", ref rnd)) { Config.RandomOrder = rnd; Config.Save(); }
        Tooltip("Off: cycle through the list from top to bottom.");

        ImGui.SameLine(260);
        var fb = Config.ShowFeedback;
        if (ImGui.Checkbox("Show skip notes in my chat", ref fb)) { Config.ShowFeedback = fb; Config.Save(); }
        Tooltip("Prints a private note when Auto mode decides not to send (solo, PvP...).\nOnly you see these.");
    }

    private void DrawQuickButtons()
    {
        var inAlliance = Plugin.PartyList.IsAlliance;
        var partyText = Plugin.PartyList.Length <= 1 ? "not in a party" : $"party of {Plugin.PartyList.Length}";
        var allianceText = inAlliance ? ", in an alliance" : string.Empty;
        ImGui.TextUnformatted($"Right now: {partyText}{allianceText}. Messages go to {ChatSender.TargetName(Config.DefaultTarget, inAlliance)} chat.");

        if (ImGui.Button("Send greeting now (/hi)"))
            plugin.SendGreeting(Config.DefaultTarget, auto: false);
        ImGui.SameLine();
        if (ImGui.Button("Send farewell now (/bye)"))
            plugin.SendFarewell(Config.DefaultTarget, auto: false);
    }

    private void DrawList(List<string> list, ref string newEntry, string noun)
    {
        using var id = ImRaii.PushId(noun);

        var removeAt = -1;
        for (var i = 0; i < list.Count; i++)
        {
            using var rowId = ImRaii.PushId(i);

            var text = list[i];
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 40 * ImGuiHelpers.GlobalScale);
            if (ImGui.InputText("##text", ref text, 400))
            {
                list[i] = text;
                Config.Save();
            }

            ImGui.SameLine();
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Trash))
                removeAt = i;
            Tooltip($"Remove this {noun}");
        }

        if (removeAt >= 0)
        {
            list.RemoveAt(removeAt);
            Config.Save();
        }

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 40 * ImGuiHelpers.GlobalScale);
        var submitted = ImGui.InputText("##new", ref newEntry, 400, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGui.SameLine();
        if (ImGuiComponents.IconButton(FontAwesomeIcon.Plus) || submitted)
        {
            if (!string.IsNullOrWhiteSpace(newEntry))
            {
                list.Add(newEntry.Trim());
                newEntry = string.Empty;
                Config.Save();
            }
        }
        Tooltip($"Add a new {noun} (Enter also adds)");

        if (list.Count == 0)
            ImGui.TextDisabled($"No {noun}s yet. Add one above.");
    }

    private static void Tooltip(string text)
    {
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(text);
    }
}
