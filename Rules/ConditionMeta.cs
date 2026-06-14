using System.Collections.Generic;
using System.Linq;

namespace ExilesAutoCore.Rules;

/// <summary>Top-level grouping for the "Add condition" menu.</summary>
public enum ConditionCategory
{
    Monster,
    Skill,
    Player,
    Cursor,
    Input,
    Flask,
    Area,
}

/// <summary>
/// Presentation metadata for conditions — maps each <see cref="ConditionKind"/> to a friendly
/// category + label for the grouped "Add condition" menu, and turns comparison operators into words.
/// This is pure UI dressing: the backend still stores the raw <see cref="ConditionKind"/> enum.
/// </summary>
public static class ConditionMeta
{
    public sealed record Entry(ConditionKind Kind, ConditionCategory Category, string Label);

    /// <summary>All user-addable kinds, grouped. AlwaysTrue is intentionally omitted (it's the default placeholder).</summary>
    public static readonly IReadOnlyList<Entry> All = new[]
    {
        new Entry(ConditionKind.MonsterNearby, ConditionCategory.Monster, "Rarity nearby"),
        new Entry(ConditionKind.MonsterCount, ConditionCategory.Monster, "Monsters in range"),
        new Entry(ConditionKind.MonsterLifePercent, ConditionCategory.Monster, "Life %"),
        new Entry(ConditionKind.MonsterOnLowLife, ConditionCategory.Monster, "On low life"),
        new Entry(ConditionKind.MonsterCullable, ConditionCategory.Monster, "Cullable (per-rarity life %)"),
        new Entry(ConditionKind.MonsterHasBuff, ConditionCategory.Monster, "Has buff / debuff"),
        new Entry(ConditionKind.MonsterMissingBuff, ConditionCategory.Monster, "Missing buff / debuff"),
        new Entry(ConditionKind.MonsterHeavyStunned, ConditionCategory.Monster, "Heavy-stun state"),
        new Entry(ConditionKind.MonsterLightStunned, ConditionCategory.Monster, "Light-stun state"),
        new Entry(ConditionKind.MonsterFrozen, ConditionCategory.Monster, "Is frozen"),
        new Entry(ConditionKind.MonsterChilled, ConditionCategory.Monster, "Is chilled"),
        new Entry(ConditionKind.MonsterShocked, ConditionCategory.Monster, "Is shocked"),
        new Entry(ConditionKind.MonsterElectrocuted, ConditionCategory.Monster, "Is electrocuted"),
        new Entry(ConditionKind.MonsterBleeding, ConditionCategory.Monster, "Is bleeding"),
        new Entry(ConditionKind.MonsterPoisoned, ConditionCategory.Monster, "Is poisoned"),
        new Entry(ConditionKind.MonsterIgnited, ConditionCategory.Monster, "Is ignited"),
        new Entry(ConditionKind.MonsterSapped, ConditionCategory.Monster, "Is sapped"),
        new Entry(ConditionKind.MonsterScorched, ConditionCategory.Monster, "Is scorched"),
        new Entry(ConditionKind.MonsterMaimed, ConditionCategory.Monster, "Is maimed"),
        new Entry(ConditionKind.MonsterHindered, ConditionCategory.Monster, "Is hindered"),
        new Entry(ConditionKind.MonsterPinned, ConditionCategory.Monster, "Is pinned"),
        new Entry(ConditionKind.MonsterImmobilised, ConditionCategory.Monster, "Is immobilised"),
        new Entry(ConditionKind.MonsterDazed, ConditionCategory.Monster, "Is dazed"),
        new Entry(ConditionKind.MonsterBlinded, ConditionCategory.Monster, "Is blinded"),
        new Entry(ConditionKind.MonsterOnFullLife, ConditionCategory.Monster, "On full life"),
        new Entry(ConditionKind.MonsterCannotDie, ConditionCategory.Monster, "Cannot die"),
        new Entry(ConditionKind.MonsterCannotBeStunned, ConditionCategory.Monster, "Cannot be stunned"),
        new Entry(ConditionKind.MonsterIsTargeted, ConditionCategory.Monster, "Is targeted"),
        new Entry(ConditionKind.MonsterIsTargetable, ConditionCategory.Monster, "Is targetable"),
        new Entry(ConditionKind.MonsterInvincible, ConditionCategory.Monster, "Is invincible"),
        new Entry(ConditionKind.NoMonsterInvincible, ConditionCategory.Monster, "None invincible (all clear)"),

        new Entry(ConditionKind.SkillReady, ConditionCategory.Skill, "Is ready"),
        new Entry(ConditionKind.SkillOffCooldown, ConditionCategory.Skill, "Off cooldown"),
        new Entry(ConditionKind.SkillManaAvailable, ConditionCategory.Skill, "Mana available"),
        new Entry(ConditionKind.SkillUsing, ConditionCategory.Skill, "Currently using"),
        new Entry(ConditionKind.SkillNotUsing, ConditionCategory.Skill, "Not currently using"),
        new Entry(ConditionKind.SkillUseStage, ConditionCategory.Skill, "Use stage"),

        new Entry(ConditionKind.LifePercent, ConditionCategory.Player, "Life %"),
        new Entry(ConditionKind.EnergyShieldPercent, ConditionCategory.Player, "Energy shield %"),
        new Entry(ConditionKind.ManaPercent, ConditionCategory.Player, "Mana %"),
        new Entry(ConditionKind.PlayerHasBuff, ConditionCategory.Player, "Has buff"),
        new Entry(ConditionKind.PlayerMissingBuff, ConditionCategory.Player, "Missing buff"),
        new Entry(ConditionKind.PlayerBuffCharges, ConditionCategory.Player, "Buff charges"),
        new Entry(ConditionKind.PlayerBuffTimeLeft, ConditionCategory.Player, "Buff time left %"),
        new Entry(ConditionKind.PlayerHasAilment, ConditionCategory.Player, "Ailment"),
        new Entry(ConditionKind.IsMoving, ConditionCategory.Player, "Movement"),
        new Entry(ConditionKind.WeaponSet, ConditionCategory.Player, "Weapon set"),

        new Entry(ConditionKind.HoveringMonster, ConditionCategory.Cursor, "Cursor over a monster"),
        new Entry(ConditionKind.MonstersNearCursor, ConditionCategory.Cursor, "Monsters near cursor"),
        new Entry(ConditionKind.CursorDistance, ConditionCategory.Cursor, "Cursor distance"),

        new Entry(ConditionKind.MouseButtonHeld, ConditionCategory.Input, "Mouse button held"),

        new Entry(ConditionKind.FlaskActive, ConditionCategory.Flask, "Active"),
        new Entry(ConditionKind.FlaskReady, ConditionCategory.Flask, "Ready (has charges)"),
        new Entry(ConditionKind.FlaskUsable, ConditionCategory.Flask, "Usable (smart)"),
        new Entry(ConditionKind.FlaskCharges, ConditionCategory.Flask, "Charges"),

        new Entry(ConditionKind.InTown, ConditionCategory.Area, "In town"),
        new Entry(ConditionKind.InHideout, ConditionCategory.Area, "In hideout"),
        new Entry(ConditionKind.InPeacefulArea, ConditionCategory.Area, "In peaceful area"),
        new Entry(ConditionKind.ChatOpen, ConditionCategory.Area, "Chat open"),
        new Entry(ConditionKind.PanelOpen, ConditionCategory.Area, "Any panel open"),
    };

    public static IEnumerable<Entry> InCategory(ConditionCategory category) => All.Where(e => e.Category == category);

    /// <summary>The category a kind belongs to (used for colour-coding). Defaults to Player for unlisted kinds.</summary>
    public static ConditionCategory CategoryOf(ConditionKind kind) =>
        All.FirstOrDefault(e => e.Kind == kind)?.Category ?? ConditionCategory.Player;

    /// <summary>The friendly label for a kind (shown in Simple mode in place of the raw dropdown).</summary>
    public static string LabelOf(ConditionKind kind) =>
        All.FirstOrDefault(e => e.Kind == kind)?.Label ?? kind.ToString();

    /// <summary>Turns a comparison operator into a natural-language word for condition descriptions.</summary>
    public static string Word(Comparison op) => op switch
    {
        Comparison.LessThan => "below",
        Comparison.LessOrEqual => "at most",
        Comparison.GreaterThan => "above",
        Comparison.GreaterOrEqual => "at least",
        Comparison.Equal => "is",
        Comparison.NotEqual => "is not",
        _ => "?",
    };
}
