using System.Collections.Generic;
using System.Linq;
using ExilesAutoCore.State;

namespace ExilesAutoCore.Rules;

/// <summary>
/// Maps friendly ailment names to the underlying buff ids that represent them. An ailment is "active"
/// when the player has any of its buff ids. Ids mirror ReAgent's CustomAilments data so they match
/// what PoE2 actually applies (ailments are not a single buff — e.g. "Burning" covers several ids).
/// </summary>
public static class Ailments
{
    public static readonly IReadOnlyDictionary<string, string[]> Map = new Dictionary<string, string[]>
    {
        ["Bleeding"] = new[] { "bleeding", "physical_damage_and_bleed", "puncture", "puncture_moving" },
        ["Burning"] = new[] { "demon_righteous_fire_aura", "fire_damage_and_ignite", "ground_fire_burn", "ignited", "righteous_fire_aura", "searing_bond_in_beam" },
        ["Chilled"] = new[] { "chilled", "chilling_bond_in_beam", "ground_ice_chill", "yugul_pool_chilled" },
        ["Corruption"] = new[] { "corrupted_blood", "corrupted_blood_rain" },
        ["Cursed"] = new[] { "curse_assassins_mark", "curse_chaos_weakness", "curse_cold_weakness", "curse_elemental_weakness", "curse_enfeeble", "curse_fire_weakness", "curse_lightning_weakness", "curse_newpunishment", "curse_temporal_chains", "curse_vulnerability", "curse_warlords_mark" },
        ["Exposed"] = new[] { "reduced_cold_resistance_from_skill", "reduced_fire_resistance_from_skill", "reduced_lightning_resistance_from_skill" },
        ["Frozen"] = new[] { "cold_damage_and_freeze", "frozen" },
        ["Poisoned"] = new[] { "caustic_cloud", "chaos_bond_in_beam", "ground_desecration", "poison", "viper_strike_orb" },
        ["Shocked"] = new[] { "ground_lightning_shock", "lightning_damage_and_shock", "seawitch_lightning_beam", "shocked" },
    };

    public static IEnumerable<string> Names => Map.Keys;

    /// <summary>True if the player currently has any buff id that represents the named ailment.</summary>
    public static bool IsActive(GameState state, string name) =>
        Map.TryGetValue(name, out var buffIds) && buffIds.Any(state.Buffs.Has);
}
