# Polite Eorzea

A small Dalamud plugin for FFXIV that says hello and thank you to your party or alliance.

## Install

Polite Eorzea is distributed through a custom plugin repository.

1. In game, type `/xlsettings` and open the **Experimental** tab.
2. Paste this URL into **Custom Plugin Repositories**, press the **+** button, then **Save**:

   ```
   https://raw.githubusercontent.com/AHAKuo/ahadevtools/main/pluginmaster.json
   ```

3. Open `/xlplugins`, search for **Polite Eorzea**, and install it.

The same repository URL also carries any other plugins published under [AHAKuo/ahadevtools](https://github.com/AHAKuo/ahadevtools).

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

Messages go to alliance chat automatically when you are in an alliance, otherwise to party chat. You can pin a channel in the settings.

## Building

Requires the .NET 10 SDK and a Dalamud dev install (XIVLauncher). From the repo root:

```
dotnet build PoliteEorzea/PoliteEorzea.csproj -c Release
```

The plugin lands in `PoliteEorzea/bin/Release/PoliteEorzea.dll`. To load a local build, add that path under Dalamud Settings > Experimental > Dev Plugin Locations.

## Releasing

Each GitHub release must carry two assets from `PoliteEorzea/bin/Release/PoliteEorzea/`: `latest.zip` and `PoliteEorzea.json`. The custom repository picks up the latest release automatically.

## License

AGPL-3.0. See [LICENSE](LICENSE).
