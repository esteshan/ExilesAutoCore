using ExilesAutoCore.State;

namespace ExilesAutoCore.Rules;

/// <summary>
/// Shared logic for the per-rule/per-combo "only fire in these area types" toggles. Maps is the
/// catch-all: any area that isn't town, hideout, or another peaceful area counts as a map. Mirrors
/// ReAgent's RuleGroup area switch so the two plugins behave identically.
/// </summary>
public static class AreaFilter
{
    /// <summary>True if the player's current area type is enabled by the given toggles.</summary>
    public static bool Allows(GameState state, bool inMaps, bool inTown, bool inHideout, bool inPeaceful) =>
        (state.IsInHideout, state.IsInTown, state.IsInPeacefulArea) switch
        {
            (true, _, _) => inHideout,
            (_, true, _) => inTown,
            (_, _, true) => inPeaceful,
            _ => inMaps,
        };

    /// <summary>Compact badge of the enabled areas, e.g. "[MTH]" — matches ReAgent's tab label style.</summary>
    public static string Badge(bool inMaps, bool inTown, bool inHideout, bool inPeaceful) =>
        $"[{(inMaps ? "M" : "")}{(inTown ? "T" : "")}{(inHideout ? "H" : "")}{(inPeaceful ? "P" : "")}]";
}
