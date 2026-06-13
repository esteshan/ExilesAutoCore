using System.Collections.Generic;

namespace ExilesAutoCore.Rules;

/// <summary>
/// A named set of rules and combos. Profiles let the user keep separate automation setups per
/// character or build (e.g. a Warrior profile and a Witch profile) and switch the active one,
/// instead of sharing a single global setup across every character.
/// </summary>
public sealed class Profile
{
    public string Name = "Default";
    public List<SkillRule> Rules = new();
    public List<Combo> Combos = new();
}
