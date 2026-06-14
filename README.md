# ExilesAutoCore

A no-code skill-automation plugin for [ExileCore2](https://github.com/exApiTools).
It lets you build skill-rotation rules, combos, and conditions from an in-game UI instead of
writing code — a state layer (vitals, buffs, skills, nearby monsters) drives a rule engine that
presses your skills based on the conditions you configure.


## Project layout

| Folder | Purpose |
|--------|---------|
| `State/` | Game-state snapshot — vitals, buffs, skills, flasks, nearby monsters. |
| `Rules/` | Rule engine — profiles, skill rules, conditions, combos, presets. |
| `Ui/`    | ImGui editors — rule builder, condition editor, combo builder, theme. |
| `Engine.cs` | Per-frame evaluation loop that runs rules against the state. |
| `ExilesAutoCore.cs` / `ExilesAutoCoreSettings.cs` | Plugin entry point and settings. |
