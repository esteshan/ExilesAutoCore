using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ExilesAutoCore.State;

namespace ExilesAutoCore.Rules;

/// <summary>
/// A single automation rule: when every condition is true, the engine performs <see cref="Action"/>
/// (press a key, optionally aiming first). Conditions are ANDed — all must pass for the rule to fire.
///
/// Members are public fields (not properties) so the ImGui builder can edit them by ref.
/// </summary>
public sealed class SkillRule
{
    public bool Enabled = true;
    public string Name = "New rule";

    /// <summary>What happens when the rule fires.</summary>
    public SkillAction Action = new();

    /// <summary>All of these must be true for the rule to fire.</summary>
    public List<Condition> Conditions = new();

    /// <summary>Minimum seconds between firings of this rule (0 = no per-rule cooldown).</summary>
    public float Cooldown = 0;

    // Runtime only (private, not serialized): when this rule last fired, for the cooldown.
    private readonly Stopwatch _sinceFire = new();

    /// <summary>True when every condition currently passes (an empty list counts as true).</summary>
    public bool Matches(GameState state) => Conditions.All(c => c.Evaluate(state));

    /// <summary>True while the rule is still inside its own cooldown window and shouldn't fire yet.</summary>
    public bool OnCooldown => Cooldown > 0 && _sinceFire.IsRunning && _sinceFire.Elapsed.TotalSeconds < Cooldown;

    /// <summary>Records that the rule just fired, (re)starting its cooldown timer.</summary>
    public void MarkFired() => _sinceFire.Restart();
}
