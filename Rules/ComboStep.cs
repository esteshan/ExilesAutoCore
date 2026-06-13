using System.Collections.Generic;
using System.Linq;
using ExilesAutoCore.State;

namespace ExilesAutoCore.Rules;

/// <summary>
/// One step of a <see cref="Combo"/>: when the combo reaches this step and these conditions pass, it
/// performs <see cref="Action"/>, then waits <see cref="DelayAfterMs"/> before the next step may fire
/// (giving the cast/animation time to land).
/// </summary>
public sealed class ComboStep
{
    public string Name = "Step";

    /// <summary>What this step does when it fires.</summary>
    public SkillAction Action = new();

    /// <summary>All of these must pass before this step fires (empty = no extra gating).</summary>
    public List<Condition> Conditions = new();

    /// <summary>Milliseconds to wait after firing this step before the next step may fire.</summary>
    public int DelayAfterMs = 150;

    public bool Matches(GameState state) => Conditions.All(c => c.Evaluate(state));
}
