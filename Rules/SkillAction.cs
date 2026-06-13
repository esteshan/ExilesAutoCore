using System.Windows.Forms;
using ExilesAutoCore.State;

namespace ExilesAutoCore.Rules;

/// <summary>
/// What to do when a rule or combo step fires: which key to press, and optionally aim the cursor at
/// a monster first. Shared by <see cref="SkillRule"/> and <see cref="ComboStep"/> so the press/aim
/// behaviour is defined in exactly one place.
/// </summary>
public sealed class SkillAction
{
    /// <summary>The key to press.</summary>
    public Keys Key = Keys.Q;

    /// <summary>When true, move the cursor onto a target monster before pressing (aims the skill).</summary>
    public bool AutoFace = false;

    /// <summary>Auto-face only considers monsters of this rarity (e.g. AtLeastRare to ignore trash).</summary>
    public MonsterRarity AutoFaceRarity = MonsterRarity.Any;

    /// <summary>Auto-face only considers monsters within this distance (world units).</summary>
    public int AutoFaceRange = 100;
}
