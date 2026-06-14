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

    // Optional link to a build guide this profile follows (e.g. a mobalytics/PoE2 page), shown in the
    // profile bar with an "Open build guide" button. Purely informational; empty by default.
    public string ShowcaseUrl = "";

    public List<SkillRule> Rules = new();
    public List<Combo> Combos = new();
}
