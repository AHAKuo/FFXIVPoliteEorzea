# Polite Eorzea

A small Dalamud plugin for FFXIV that says hello and thank you to your party or alliance.

## Commands

| Command | What it does |
|---|---|
| `/hi` | Send a random greeting to party (or alliance when you are in one). |
| `/bye` | Send a random farewell the same way. |
| `/hi p` / `/hi a` / `/hi s` | Force party, alliance or say chat for that one message (same for `/bye`). |
| `/polite` | Open the settings window. |
| `/polite auto` | Toggle Auto mode. `/polite on` and `/polite off` also work. |

## Auto mode

When enabled, the plugin greets when you enter any Content Finder duty (dungeon, trial, raid, alliance raid, guildhest, deep dungeon, PvP if allowed) and sends a farewell when the duty completes. Both have a random delay range so they do not look instant. Solo instances are skipped by default.

Greeting timing can be either "when I load into the duty" or "when the duty starts" (barrier drops).

## Building

Requires the .NET 10 SDK and a Dalamud dev install (XIVLauncher). From the repo root:

```
dotnet build PoliteEorzea/PoliteEorzea.csproj -c Release
```

The plugin lands in `PoliteEorzea/bin/Release/PoliteEorzea.dll`. Add that path under Dalamud Settings > Experimental > Dev Plugin Locations.
