using System.Windows.Forms;
using ExilesAutoCore.State;

namespace ExilesAutoCore.Rules;

/// <summary>How a <see cref="SkillAction"/> delivers its press: a keyboard key or a mouse button.</summary>
public enum SkillInputKind
{
    Key,
    LeftClick,
    RightClick,
}

/// <summary>
/// What to do when a rule or combo step fires: which key/button to press, and optionally aim the
/// cursor at a monster first. Shared by <see cref="SkillRule"/> and <see cref="ComboStep"/> so the
/// press/aim behaviour is defined in exactly one place.
/// </summary>
public sealed class SkillAction
{
    /// <summary>Whether this action presses a keyboard <see cref="Key"/> or clicks a mouse button.</summary>
    public SkillInputKind InputKind = SkillInputKind.Key;

    /// <summary>The key to press (used only when <see cref="InputKind"/> is <see cref="SkillInputKind.Key"/>).</summary>
    public Keys Key = Keys.Q;

    /// <summary>When true, move the cursor onto a target monster before pressing (aims the skill).</summary>
    public bool AutoFace = false;

    /// <summary>Auto-face only considers monsters of this rarity (e.g. AtLeastRare to ignore trash).</summary>
    public MonsterRarity AutoFaceRarity = MonsterRarity.Any;

    /// <summary>Auto-face only considers monsters within this distance (world units).</summary>
    public int AutoFaceRange = 100;

    /// <summary>Human-readable description of the input this action sends, for status text and labels.</summary>
    public string InputLabel => InputKind switch
    {
        SkillInputKind.LeftClick => "Left Click",
        SkillInputKind.RightClick => "Right Click",
        _ => Key.ToString(),
    };
}
