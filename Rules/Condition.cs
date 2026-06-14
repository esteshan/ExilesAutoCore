using System;
using System.Linq;
using System.Windows.Forms;
using ExilesAutoCore.State;

namespace ExilesAutoCore.Rules;

/// <summary>Which mouse button a MouseButtonHeld condition watches.</summary>
public enum MouseButton
{
    Left,
    Right,
    Middle,
}

/// <summary>How a numeric condition compares the live game value against its threshold.</summary>
public enum Comparison
{
    LessThan,
    LessOrEqual,
    GreaterThan,
    GreaterOrEqual,
    Equal,
    NotEqual,
}

/// <summary>
/// The kind of check a condition performs. Each kind uses only the subset of <see cref="Condition"/>
/// fields it needs (see Evaluate/Describe), and the builder UI shows just those fields.
/// </summary>
public enum ConditionKind
{
    AlwaysTrue,
    LifePercent,
    EnergyShieldPercent,
    ManaPercent,
    PlayerHasBuff,
    PlayerMissingBuff,
    PlayerBuffCharges,
    MonsterCount,
    SkillReady,
    IsMoving,
    HoveringMonster,
    MouseButtonHeld,
    MonstersNearCursor,
    CursorDistance,
    MonsterHasBuff,
    MonsterMissingBuff,
    MonsterHeavyStunned,
    // Appended at the end so existing saved configs keep their enum values.
    SkillUsing,
    SkillNotUsing,
    MonsterLifePercent,
    MonsterIsTargeted,
    MonsterIsTargetable,
    MonsterInvincible,
    SkillOffCooldown,
    SkillManaAvailable,
    FlaskActive,
    FlaskReady,
    FlaskCharges,
    InTown,
    InHideout,
    InPeacefulArea,
    ChatOpen,
    PanelOpen,
    PlayerBuffTimeLeft,
    PlayerHasAilment,
    WeaponSet,
    SkillUseStage,
    MonsterOnLowLife,
    MonsterNearby,
    MonsterCullable,
    FlaskUsable,
    NoMonsterInvincible,
    MonsterCannotDie,
    MonsterFrozen,
    MonsterOnFullLife,
    MonsterLightStunned,
    MonsterCannotBeStunned,
    MonsterChilled,
    MonsterShocked,
    MonsterElectrocuted,
    MonsterBleeding,
    MonsterPoisoned,
    MonsterIgnited,
    MonsterSapped,
    MonsterScorched,
    MonsterMaimed,
    MonsterHindered,
    MonsterPinned,
    MonsterImmobilised,
    MonsterDazed,
    MonsterBlinded,
}

/// <summary>
/// One dropdown-built check, evaluated against the live <see cref="GameState"/>. This is the
/// no-code replacement for a hand-written expression: structured data the UI edits and the engine
/// evaluates directly — nothing is compiled, and the user never types code.
///
/// Members are public fields (not properties) so the ImGui builder can edit them by ref.
/// </summary>
public sealed class Condition
{
    public ConditionKind Kind = ConditionKind.AlwaysTrue;

    /// <summary>Comparison used by the numeric kinds (life %, monster count, buff charges).</summary>
    public Comparison Operator = Comparison.GreaterOrEqual;

    /// <summary>Numeric threshold for the numeric kinds. Defaults to 1 so a fresh ">= " count
    /// condition means "at least one" rather than the always-true ">= 0".</summary>
    public float Value = 1;

    /// <summary>Buff or skill name, for the kinds that target one (PlayerHasBuff, SkillReady, ...).</summary>
    public string Text = "";

    /// <summary>Search radius in world units, for MonsterCount.</summary>
    public int Range = 50;

    /// <summary>Rarity filter, for MonsterCount.</summary>
    public MonsterRarity Rarity = MonsterRarity.Any;

    /// <summary>Desired value for the boolean kinds (e.g. IsMoving == true / false).</summary>
    public bool BoolValue = true;

    /// <summary>Which mouse button MouseButtonHeld watches.</summary>
    public MouseButton MouseButton = MouseButton.Left;

    /// <summary>Which flask slot (1 or 2) the flask conditions check.</summary>
    public int FlaskSlot = 1;

    // Per-rarity life% thresholds for the "cullable" check. Defaults match Killing Palm.
    public float CullNormal = 35;
    public float CullMagic = 20;
    public float CullRare = 10;
    public float CullUnique = 5;

    /// <summary>Evaluates this single check against the current frame's state.</summary>
    public bool Evaluate(GameState state)
    {
        if (state == null)
        {
            return false;
        }

        return Kind switch
        {
            ConditionKind.AlwaysTrue => true,
            ConditionKind.LifePercent => Compare(state.Vitals?.HP.Percent ?? 0),
            ConditionKind.EnergyShieldPercent => Compare(state.Vitals?.ES.Percent ?? 0),
            ConditionKind.ManaPercent => Compare(state.Vitals?.Mana.Percent ?? 0),
            ConditionKind.PlayerHasBuff => state.Buffs.Has(Text),
            ConditionKind.PlayerMissingBuff => !state.Buffs.Has(Text),
            ConditionKind.PlayerBuffCharges => Compare(state.Buffs[Text].Charges),
            ConditionKind.MonsterCount => Compare(state.MonsterCount(Range, Rarity)),
            ConditionKind.SkillReady => state.Skills[Text].CanBeUsed,
            ConditionKind.SkillUsing => state.Skills[Text].IsUsing,
            ConditionKind.SkillNotUsing => !state.Skills[Text].IsUsing,
            ConditionKind.IsMoving => state.IsMoving == BoolValue,
            ConditionKind.HoveringMonster => state.IsHoveringMonster,
            ConditionKind.MouseButtonHeld => state.IsKeyDown(MouseButtonKey),
            ConditionKind.MonstersNearCursor => Compare(state.MonstersNearCursor(Range, Rarity)),
            ConditionKind.CursorDistance => Compare(state.CursorDistanceFromPlayer),
            ConditionKind.MonsterHasBuff => state.AnyMonsterHasBuff(Range, Rarity, Text),
            ConditionKind.MonsterMissingBuff => state.AnyMonsterMissingBuff(Range, Rarity, Text),
            ConditionKind.MonsterHeavyStunned => state.Monsters(Range, Rarity).Any(m => Compare(m.HeavyStun)),
            ConditionKind.MonsterLifePercent => state.Monsters(Range, Rarity).Any(m => Compare(m.Vitals.HP.Percent)),
            // BoolValue lets each of these be required either way. We negate the per-monster
            // predicate (Any(m => !x)) rather than the whole Any (!Any(m => x)) so "is not
            // invincible" means "there's a monster in range that is not invincible" — the useful
            // targeting reading — not "no monster in range is invincible".
            ConditionKind.MonsterIsTargeted => state.Monsters(Range, Rarity).Any(m => m.IsTargeted == BoolValue),
            ConditionKind.MonsterIsTargetable => state.Monsters(Range, Rarity).Any(m => m.IsTargetable == BoolValue),
            ConditionKind.MonsterInvincible => state.Monsters(Range, Rarity).Any(m => m.IsInvincible == BoolValue),
            // Distinct from MonsterInvincible(BoolValue=false): this is "NO monster in range is
            // invincible" (!Any), a clear-to-commit guard — not "there exists a non-invincible one".
            ConditionKind.NoMonsterInvincible => !state.Monsters(Range, Rarity).Any(m => m.IsInvincible),
            ConditionKind.MonsterCannotDie => state.Monsters(Range, Rarity).Any(m => m.CannotDie == BoolValue),
            ConditionKind.MonsterFrozen => state.Monsters(Range, Rarity).Any(m => m.IsFrozen == BoolValue),
            ConditionKind.MonsterOnFullLife => state.Monsters(Range, Rarity).Any(m => m.OnFullLife == BoolValue),
            ConditionKind.MonsterLightStunned => state.Monsters(Range, Rarity).Any(m => m.IsLightStunned == BoolValue),
            ConditionKind.MonsterCannotBeStunned => state.Monsters(Range, Rarity).Any(m => m.CannotBeStunned == BoolValue),
            ConditionKind.MonsterChilled => state.Monsters(Range, Rarity).Any(m => m.IsChilled == BoolValue),
            ConditionKind.MonsterShocked => state.Monsters(Range, Rarity).Any(m => m.IsShocked == BoolValue),
            ConditionKind.MonsterElectrocuted => state.Monsters(Range, Rarity).Any(m => m.IsElectrocuted == BoolValue),
            ConditionKind.MonsterBleeding => state.Monsters(Range, Rarity).Any(m => m.IsBleeding == BoolValue),
            ConditionKind.MonsterPoisoned => state.Monsters(Range, Rarity).Any(m => m.IsPoisoned == BoolValue),
            ConditionKind.MonsterIgnited => state.Monsters(Range, Rarity).Any(m => m.IsIgnited == BoolValue),
            ConditionKind.MonsterSapped => state.Monsters(Range, Rarity).Any(m => m.IsSapped == BoolValue),
            ConditionKind.MonsterScorched => state.Monsters(Range, Rarity).Any(m => m.IsScorched == BoolValue),
            ConditionKind.MonsterMaimed => state.Monsters(Range, Rarity).Any(m => m.IsMaimed == BoolValue),
            ConditionKind.MonsterHindered => state.Monsters(Range, Rarity).Any(m => m.IsHindered == BoolValue),
            ConditionKind.MonsterPinned => state.Monsters(Range, Rarity).Any(m => m.IsPinned == BoolValue),
            ConditionKind.MonsterImmobilised => state.Monsters(Range, Rarity).Any(m => m.IsImmobilised == BoolValue),
            ConditionKind.MonsterDazed => state.Monsters(Range, Rarity).Any(m => m.IsDazed == BoolValue),
            ConditionKind.MonsterBlinded => state.Monsters(Range, Rarity).Any(m => m.IsBlinded == BoolValue),
            ConditionKind.SkillOffCooldown => !state.Skills[Text].OnCooldown,
            ConditionKind.SkillManaAvailable => state.Skills[Text].ManaCost <= (state.Vitals?.Mana.Current ?? 0),
            ConditionKind.FlaskActive => state.FlaskActive(FlaskSlot) == BoolValue,
            ConditionKind.FlaskReady => state.FlaskReady(FlaskSlot),
            ConditionKind.FlaskUsable => state.FlaskUsable(FlaskSlot),
            ConditionKind.FlaskCharges => Compare(state.FlaskCharges(FlaskSlot)),
            ConditionKind.InTown => state.IsInTown == BoolValue,
            ConditionKind.InHideout => state.IsInHideout == BoolValue,
            ConditionKind.InPeacefulArea => state.IsInPeacefulArea == BoolValue,
            ConditionKind.ChatOpen => state.IsChatOpen == BoolValue,
            ConditionKind.PanelOpen => state.IsAnyPanelOpen == BoolValue,
            ConditionKind.PlayerBuffTimeLeft => Compare(state.Buffs[Text].PercentTimeLeft),
            ConditionKind.PlayerHasAilment => Ailments.IsActive(state, Text) == BoolValue,
            ConditionKind.WeaponSet => Compare(state.ActiveWeaponSetIndex),
            ConditionKind.SkillUseStage => Compare(state.Skills[Text].UseStage),
            ConditionKind.MonsterOnLowLife => state.Monsters(Range, Rarity).Any(m => m.OnLowLife == BoolValue),
            ConditionKind.MonsterNearby => state.MonsterCount(Range, Rarity) >= 1,
            ConditionKind.MonsterCullable => state.Monsters(Range, MonsterRarity.Any).Any(IsCullable),
            _ => false,
        };
    }

    /// <summary>A short, plain-English summary for the rule list (e.g. "Life below 35%").</summary>
    public string Describe() => Kind switch
    {
        ConditionKind.AlwaysTrue => "Always",
        ConditionKind.LifePercent => $"Life {Word} {Value:0}%",
        ConditionKind.EnergyShieldPercent => $"Energy shield {Word} {Value:0}%",
        ConditionKind.ManaPercent => $"Mana {Word} {Value:0}%",
        ConditionKind.PlayerHasBuff => $"Has buff '{Text}'",
        ConditionKind.PlayerMissingBuff => $"Missing buff '{Text}'",
        ConditionKind.PlayerBuffCharges => $"Buff '{Text}' charges {Word} {Value:0}",
        ConditionKind.MonsterCount => $"Monsters ({Rarity}, <={Range}) {Word} {Value:0}",
        ConditionKind.SkillReady => $"Skill '{Text}' is ready",
        ConditionKind.SkillUsing => $"Using skill '{Text}'",
        ConditionKind.SkillNotUsing => $"Not using skill '{Text}'",
        ConditionKind.IsMoving => BoolValue ? "Moving" : "Standing still",
        ConditionKind.HoveringMonster => "Cursor is over a monster",
        ConditionKind.MouseButtonHeld => $"{MouseButton} mouse button held",
        ConditionKind.MonstersNearCursor => $"Monsters near cursor ({Rarity}, <={Range}) {Word} {Value:0}",
        ConditionKind.CursorDistance => $"Cursor distance {Word} {Value:0}",
        ConditionKind.MonsterHasBuff => $"A monster ({Rarity}, <={Range}) has '{Text}'",
        ConditionKind.MonsterMissingBuff => $"A monster ({Rarity}, <={Range}) is missing '{Text}'",
        ConditionKind.MonsterHeavyStunned => $"A monster ({Rarity}, <={Range}) heavy-stun {Word} {Value:0}",
        ConditionKind.MonsterLifePercent => $"A monster ({Rarity}, <={Range}) life {Word} {Value:0}%",
        ConditionKind.MonsterIsTargeted => $"A monster ({Rarity}, <={Range}) is {(BoolValue ? "" : "not ")}targeted",
        ConditionKind.MonsterIsTargetable => $"A monster ({Rarity}, <={Range}) is {(BoolValue ? "" : "not ")}targetable",
        ConditionKind.MonsterInvincible => $"A monster ({Rarity}, <={Range}) is {(BoolValue ? "" : "not ")}invincible",
        ConditionKind.NoMonsterInvincible => $"No monster ({Rarity}, <={Range}) is invincible",
        ConditionKind.MonsterCannotDie => $"A monster ({Rarity}, <={Range}) {(BoolValue ? "cannot" : "can")} die",
        ConditionKind.MonsterFrozen => $"A monster ({Rarity}, <={Range}) is {(BoolValue ? "" : "not ")}frozen",
        ConditionKind.MonsterOnFullLife => $"A monster ({Rarity}, <={Range}) is {(BoolValue ? "" : "not ")}on full life",
        ConditionKind.MonsterLightStunned => $"A monster ({Rarity}, <={Range}) is {(BoolValue ? "" : "not ")}light-stunned",
        ConditionKind.MonsterCannotBeStunned => $"A monster ({Rarity}, <={Range}) {(BoolValue ? "cannot" : "can")} be stunned",
        ConditionKind.MonsterChilled => $"A monster ({Rarity}, <={Range}) is {(BoolValue ? "" : "not ")}chilled",
        ConditionKind.MonsterShocked => $"A monster ({Rarity}, <={Range}) is {(BoolValue ? "" : "not ")}shocked",
        ConditionKind.MonsterElectrocuted => $"A monster ({Rarity}, <={Range}) is {(BoolValue ? "" : "not ")}electrocuted",
        ConditionKind.MonsterBleeding => $"A monster ({Rarity}, <={Range}) is {(BoolValue ? "" : "not ")}bleeding",
        ConditionKind.MonsterPoisoned => $"A monster ({Rarity}, <={Range}) is {(BoolValue ? "" : "not ")}poisoned",
        ConditionKind.MonsterIgnited => $"A monster ({Rarity}, <={Range}) is {(BoolValue ? "" : "not ")}ignited",
        ConditionKind.MonsterSapped => $"A monster ({Rarity}, <={Range}) is {(BoolValue ? "" : "not ")}sapped",
        ConditionKind.MonsterScorched => $"A monster ({Rarity}, <={Range}) is {(BoolValue ? "" : "not ")}scorched",
        ConditionKind.MonsterMaimed => $"A monster ({Rarity}, <={Range}) is {(BoolValue ? "" : "not ")}maimed",
        ConditionKind.MonsterHindered => $"A monster ({Rarity}, <={Range}) is {(BoolValue ? "" : "not ")}hindered",
        ConditionKind.MonsterPinned => $"A monster ({Rarity}, <={Range}) is {(BoolValue ? "" : "not ")}pinned",
        ConditionKind.MonsterImmobilised => $"A monster ({Rarity}, <={Range}) is {(BoolValue ? "" : "not ")}immobilised",
        ConditionKind.MonsterDazed => $"A monster ({Rarity}, <={Range}) is {(BoolValue ? "" : "not ")}dazed",
        ConditionKind.MonsterBlinded => $"A monster ({Rarity}, <={Range}) is {(BoolValue ? "" : "not ")}blinded",
        ConditionKind.SkillOffCooldown => $"Skill '{Text}' is off cooldown",
        ConditionKind.SkillManaAvailable => $"Skill '{Text}' has mana to cast",
        ConditionKind.FlaskActive => BoolValue ? $"Flask {FlaskSlot} is active" : $"Flask {FlaskSlot} is not active",
        ConditionKind.FlaskReady => $"Flask {FlaskSlot} is ready",
        ConditionKind.FlaskUsable => $"Flask {FlaskSlot} can actually be used",
        ConditionKind.FlaskCharges => $"Flask {FlaskSlot} charges {Word} {Value:0}",
        ConditionKind.InTown => BoolValue ? "In town" : "Not in town",
        ConditionKind.InHideout => BoolValue ? "In hideout" : "Not in hideout",
        ConditionKind.InPeacefulArea => BoolValue ? "In a peaceful area" : "Not in a peaceful area",
        ConditionKind.ChatOpen => BoolValue ? "Chat is open" : "Chat is closed",
        ConditionKind.PanelOpen => BoolValue ? "A panel is open" : "No panel is open",
        ConditionKind.PlayerBuffTimeLeft => $"Buff '{Text}' time left {Word} {Value:0}%",
        ConditionKind.PlayerHasAilment => BoolValue ? $"Affected by {Text}" : $"Not affected by {Text}",
        ConditionKind.WeaponSet => $"Weapon set {Word} {Value:0}",
        ConditionKind.SkillUseStage => $"Skill '{Text}' use-stage {Word} {Value:0}",
        ConditionKind.MonsterOnLowLife => $"A monster ({Rarity}, <={Range}) is {(BoolValue ? "" : "not ")}on low life",
        ConditionKind.MonsterNearby => $"A {Rarity} monster is nearby (<={Range})",
        ConditionKind.MonsterCullable => $"A cullable monster nearby (<={Range}; N{CullNormal:0}/M{CullMagic:0}/R{CullRare:0}/U{CullUnique:0})",
        _ => Kind.ToString(),
    };

    private Keys MouseButtonKey => MouseButton switch
    {
        MouseButton.Left => Keys.LButton,
        MouseButton.Right => Keys.RButton,
        MouseButton.Middle => Keys.MButton,
        _ => Keys.LButton,
    };

    // A monster is cullable when its life% is at/below the threshold for its own rarity.
    private bool IsCullable(MonsterInfo monster)
    {
        var hp = monster.Vitals.HP.Percent;
        return monster.Rarity switch
        {
            MonsterRarity.Normal => hp <= CullNormal,
            MonsterRarity.Magic => hp <= CullMagic,
            MonsterRarity.Rare => hp <= CullRare,
            MonsterRarity.Unique => hp <= CullUnique,
            _ => false,
        };
    }

    private bool Compare(double actual) => Operator switch
    {
        Comparison.LessThan => actual < Value,
        Comparison.LessOrEqual => actual <= Value,
        Comparison.GreaterThan => actual > Value,
        Comparison.GreaterOrEqual => actual >= Value,
        Comparison.Equal => Math.Abs(actual - Value) < 0.0001,
        Comparison.NotEqual => Math.Abs(actual - Value) >= 0.0001,
        _ => false,
    };

    private string Word => ConditionMeta.Word(Operator);
}
