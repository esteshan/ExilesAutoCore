using System.Collections.Generic;
using ExileCore2.Shared.Interfaces;
using ExileCore2.Shared.Nodes;
using ExilesAutoCore.Rules;

namespace ExilesAutoCore;

public sealed class ExilesAutoCoreSettings : ISettings
{
    // Master on/off toggle the Loader shows next to the plugin name.
    public ToggleNode Enable { get; set; } = new(true);

    // Safety switch: when OFF, rules are evaluated/shown but NO keys are ever pressed.
    // Defaults to OFF so building rules never starts firing skills unexpectedly.
    public ToggleNode EnableAutomation { get; set; } = new(false);

    // When ON, the condition editor exposes the raw kind dropdown for inline re-typing.
    // Simple mode (default) hides it — you change a condition's type via the Add menu.
    public ToggleNode AdvancedMode { get; set; } = new(false);

    // Minimum milliseconds between key presses, to avoid spamming input.
    public RangeNode<int> GlobalKeyPressCooldown { get; set; } = new(150, 0, 1000);

    // How far out (world units) we look for monsters when evaluating "monsters nearby" conditions.
    public RangeNode<int> MaxMonsterRange { get; set; } = new(200, 0, 500);

    // Named profiles (each with its own rules + combos) and which one is active.
    public List<Profile> Profiles { get; set; } = new();
    public int ActiveProfileIndex { get; set; } = 0;

    // Legacy single lists from before profiles existed. Kept only so an older saved config is
    // migrated into a "Default" profile on first load; not used afterwards.
    public List<SkillRule> Rules { get; set; } = new();
    public List<Combo> Combos { get; set; } = new();
}
