using System;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;

namespace PoliteEorzea;

/// <summary>
/// Auto mode: watches duty enter / start / complete and schedules the
/// greeting and farewell with a small randomized delay.
/// </summary>
public sealed class DutyWatcher : IDisposable
{
    private enum Kind { Greeting, Farewell }

    private sealed class Pending
    {
        public Kind Kind;
        public bool WaitForLoad;
        public DateTime? DueAt;
    }

    private readonly Plugin plugin;
    private readonly Random random = new();
    private Pending? pending;

    /// <summary>Territory we already greeted in, so a single duty only gets one hello.</summary>
    private uint greetedTerritory;

    private readonly IDutyState.DutyStartedDelegate onDutyStarted;
    private readonly IDutyState.DutyCompletedDelegate onDutyCompleted;

    public DutyWatcher(Plugin plugin)
    {
        this.plugin = plugin;

        onDutyStarted = _ => OnDutyStarted();
        onDutyCompleted = _ => OnDutyCompleted();

        Plugin.ClientState.TerritoryChanged += OnTerritoryChanged;
        Plugin.DutyState.DutyStarted += onDutyStarted;
        Plugin.DutyState.DutyCompleted += onDutyCompleted;
        Plugin.Framework.Update += OnUpdate;
    }

    public void Dispose()
    {
        Plugin.ClientState.TerritoryChanged -= OnTerritoryChanged;
        Plugin.DutyState.DutyStarted -= onDutyStarted;
        Plugin.DutyState.DutyCompleted -= onDutyCompleted;
        Plugin.Framework.Update -= OnUpdate;
    }

    /// <summary>Human readable description of what is queued, for the config window.</summary>
    public string Status
    {
        get
        {
            if (pending == null) return "Idle";
            var what = pending.Kind == Kind.Greeting ? "greeting" : "farewell";
            if (pending.WaitForLoad) return $"Waiting to finish loading, then {what}";
            if (pending.DueAt is { } due)
            {
                var secs = Math.Max(0, (due - DateTime.UtcNow).TotalSeconds);
                return $"Sending {what} in {secs:0.0}s";
            }
            return $"Queued {what}";
        }
    }

    public void Cancel() => pending = null;

    private Configuration Config => plugin.Configuration;

    private void OnTerritoryChanged(uint territory)
    {
        // Any zone change invalidates whatever was queued.
        pending = null;
        greetedTerritory = 0;

        if (!Config.AutoMode || !Config.AutoGreet || Config.GreetTrigger != GreetTrigger.OnLoad)
            return;

        if (!IsInstancedDuty(territory, out _))
            return;

        pending = new Pending { Kind = Kind.Greeting, WaitForLoad = true };
    }

    private void OnDutyStarted()
    {
        if (!Config.AutoMode || !Config.AutoGreet || Config.GreetTrigger != GreetTrigger.OnDutyStart)
            return;

        var territory = Plugin.ClientState.TerritoryType;
        if (territory == greetedTerritory)
            return; // wipe + restart, or a second "start" in the same instance

        Schedule(Kind.Greeting);
    }

    private void OnDutyCompleted()
    {
        if (!Config.AutoMode || !Config.AutoFarewell)
            return;

        Schedule(Kind.Farewell);
    }

    private void Schedule(Kind kind)
    {
        var (min, max) = kind == Kind.Greeting
            ? (Config.GreetDelayMin, Config.GreetDelayMax)
            : (Config.FarewellDelayMin, Config.FarewellDelayMax);

        if (max < min) (min, max) = (max, min);
        var delay = min + random.NextDouble() * (max - min);
        pending = new Pending { Kind = kind, DueAt = DateTime.UtcNow.AddSeconds(delay) };
    }

    private void OnUpdate(IFramework framework)
    {
        if (pending == null)
            return;

        if (!Config.AutoMode)
        {
            pending = null;
            return;
        }

        if (pending.WaitForLoad)
        {
            if (Plugin.Condition[ConditionFlag.BetweenAreas] || Plugin.Condition[ConditionFlag.BetweenAreas51])
                return;
            if (Plugin.ObjectTable.LocalPlayer == null)
                return;

            pending.WaitForLoad = false;
            var kind = pending.Kind;
            Schedule(kind);
            return;
        }

        if (pending.DueAt is not { } due || DateTime.UtcNow < due)
            return;

        var toSend = pending;
        pending = null;
        Fire(toSend.Kind);
    }

    private void Fire(Kind kind)
    {
        var territory = Plugin.ClientState.TerritoryType;

        if (!IsInstancedDuty(territory, out var isPvP))
        {
            plugin.Feedback("Skipped: not in an instanced duty.");
            return;
        }

        if (Config.SkipPvP && isPvP)
        {
            plugin.Feedback("Skipped: PvP duty.");
            return;
        }

        if (Config.SkipWhenSolo && Plugin.PartyList.Length <= 1)
        {
            plugin.Feedback("Skipped: you are not in a party.");
            return;
        }

        if (kind == Kind.Greeting)
        {
            greetedTerritory = territory;
            plugin.SendGreeting(Config.DefaultTarget, auto: true);
        }
        else
        {
            plugin.SendFarewell(Config.DefaultTarget, auto: true);
        }
    }

    /// <summary>True when the territory is content-finder content (dungeon, trial, raid, alliance raid, PvP, etc.).</summary>
    public static bool IsInstancedDuty(uint territory, out bool isPvP)
    {
        isPvP = false;
        if (territory == 0)
            return false;

        var sheet = Plugin.DataManager.GetExcelSheet<TerritoryType>();
        if (sheet == null || !sheet.TryGetRow(territory, out var row))
            return false;

        var cfc = row.ContentFinderCondition;
        if (cfc.RowId == 0 || !cfc.IsValid)
            return false;

        isPvP = cfc.Value.PvP || Plugin.ClientState.IsPvP;
        return true;
    }

    public static string DutyName(uint territory)
    {
        var sheet = Plugin.DataManager.GetExcelSheet<TerritoryType>();
        if (sheet == null || !sheet.TryGetRow(territory, out var row))
            return string.Empty;
        var cfc = row.ContentFinderCondition;
        return cfc.IsValid ? cfc.Value.Name.ExtractText() : string.Empty;
    }
}
