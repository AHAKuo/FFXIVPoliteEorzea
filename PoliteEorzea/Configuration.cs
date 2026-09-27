using System;
using System.Collections.Generic;
using Dalamud.Configuration;

namespace PoliteEorzea;

public enum GreetTrigger
{
    /// <summary>Send as soon as you have loaded into the duty (before the countdown).</summary>
    OnLoad = 0,

    /// <summary>Send when the duty actually starts (barrier drops / "Duty commenced").</summary>
    OnDutyStart = 1,
}

public enum ChatTarget
{
    /// <summary>Alliance chat when in an alliance, otherwise party chat.</summary>
    Auto = 0,
    Party = 1,
    Alliance = 2,
    Say = 3,
}

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    /// <summary>Master switch for automatic greetings and farewells.</summary>
    public bool AutoMode { get; set; } = false;

    public bool AutoGreet { get; set; } = true;
    public bool AutoFarewell { get; set; } = true;

    public GreetTrigger GreetTrigger { get; set; } = GreetTrigger.OnLoad;

    /// <summary>Random delay range (seconds) before an automatic greeting is sent.</summary>
    public float GreetDelayMin { get; set; } = 1.5f;
    public float GreetDelayMax { get; set; } = 4f;

    /// <summary>Random delay range (seconds) before an automatic farewell is sent.</summary>
    public float FarewellDelayMin { get; set; } = 2f;
    public float FarewellDelayMax { get; set; } = 6f;

    /// <summary>Where messages go when no explicit channel is given.</summary>
    public ChatTarget DefaultTarget { get; set; } = ChatTarget.Auto;

    /// <summary>Do not auto-send when you are not in a party (solo instances, unsynced solo runs).</summary>
    public bool SkipWhenSolo { get; set; } = true;

    /// <summary>Do not auto-send in PvP duties.</summary>
    public bool SkipPvP { get; set; } = false;

    /// <summary>Pick messages at random; when false, cycle through the list in order.</summary>
    public bool RandomOrder { get; set; } = true;

    /// <summary>Echo what the plugin did (or why it skipped) to your own chat log.</summary>
    public bool ShowFeedback { get; set; } = true;

    public List<string> Greetings { get; set; } =
    [
        "Hello! o/",
        "Hi everyone! :)",
        "Hey all, good luck and have fun!",
        "o/ Hello, hello!",
        "Hi! Thanks for having me.",
    ];

    public List<string> Farewells { get; set; } =
    [
        "Thanks for the run! o/",
        "GG, thank you all!",
        "Thanks everyone, have a great day!",
        "ty for the party! o/",
        "Well played, thanks all!",
    ];

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);
}
