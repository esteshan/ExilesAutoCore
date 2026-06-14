using ExileCore2;
using ExileCore2.PoEMemory.Components;
using ExileCore2.PoEMemory.MemoryObjects;
using ExileCore2.Shared.Enums;

namespace ExilesAutoCore.State;

/// <summary>
/// A monster (or other combat entity) with the extra combat data rules care about:
/// its vitals, animation state, skills, buffs, rarity, and whether it can be damaged.
/// </summary>
public sealed class MonsterInfo : EntityInfo
{
    private bool? _isInvincible;

    public MonsterInfo(GameController controller, Entity entity) : base(controller, entity)
    {
        Vitals = new VitalsInfo(entity.GetComponent<Life>());
        Actor = new ActorInfo(entity);
        Skills = new SkillDictionary(controller, entity, isActiveSkillSet: true);
    }

    public VitalsInfo Vitals { get; }
    public ActorInfo Actor { get; }
    public SkillDictionary Skills { get; }

    public BuffDictionary Buffs => new(Entity.GetComponent<Buffs>()?.BuffsList ?? [], null);

    /// <summary>True while the monster is immune to damage (e.g. during certain boss phases).</summary>
    public bool IsInvincible => _isInvincible ??= Stats[GameStat.CannotBeDamaged].Value != 0;

    /// <summary>Raw IsHeavyStunned game stat: 0 = not heavy-stunned, 1 = heavy-stunned.</summary>
    public int HeavyStun => Stats[GameStat.IsHeavyStunned].Value;

    /// <summary>True when the game flags this monster as on low life (its own low-life threshold).</summary>
    public bool OnLowLife => Stats[GameStat.OnLowLife].Value != 0;

    /// <summary>True when the game flags this monster as on full life.</summary>
    public bool OnFullLife => Stats[GameStat.OnFullLife].Value != 0;

    /// <summary>True while the monster cannot die (e.g. scripted boss phases). Distinct from IsInvincible.</summary>
    public bool CannotDie => Stats[GameStat.CannotDie].Value != 0;

    /// <summary>True while the monster is frozen.</summary>
    public bool IsFrozen => Stats[GameStat.IsFrozen].Value != 0;

    /// <summary>True while the monster is light-stunned (the lesser stun, distinct from heavy stun).</summary>
    public bool IsLightStunned => Stats[GameStat.IsLightStunned].Value != 0;

    /// <summary>True while the monster cannot be stunned.</summary>
    public bool CannotBeStunned => Stats[GameStat.CannotBeStunned].Value != 0;

    // Ailment / debuff state flags, read straight from the monster's GameStats.
    public bool IsChilled => Stats[GameStat.IsChilled].Value != 0;
    public bool IsShocked => Stats[GameStat.IsShocked].Value != 0;
    public bool IsElectrocuted => Stats[GameStat.IsElectrocuted].Value != 0;
    public bool IsBleeding => Stats[GameStat.IsBleeding].Value != 0;
    public bool IsPoisoned => Stats[GameStat.IsPoisoned].Value != 0;
    public bool IsIgnited => Stats[GameStat.IsIgnited].Value != 0;
    public bool IsSapped => Stats[GameStat.IsSapped].Value != 0;
    public bool IsScorched => Stats[GameStat.IsScorched].Value != 0;
    public bool IsMaimed => Stats[GameStat.IsMaimed].Value != 0;
    public bool IsHindered => Stats[GameStat.IsHindered].Value != 0;
    public bool IsPinned => Stats[GameStat.IsPinned].Value != 0;
    public bool IsImmobilised => Stats[GameStat.IsImmobilised].Value != 0;
    public bool IsDazed => Stats[GameStat.IsDazed].Value != 0;
    public bool IsBlinded => Stats[GameStat.Blinded].Value != 0;

    /// <summary>Convenience flag: true when <see cref="HeavyStun"/> is non-zero.</summary>
    public bool IsHeavyStunned => HeavyStun != 0;

    public MonsterRarity Rarity => Entity.Rarity switch
    {
        ExileCore2.Shared.Enums.MonsterRarity.White => MonsterRarity.Normal,
        ExileCore2.Shared.Enums.MonsterRarity.Magic => MonsterRarity.Magic,
        ExileCore2.Shared.Enums.MonsterRarity.Rare => MonsterRarity.Rare,
        ExileCore2.Shared.Enums.MonsterRarity.Unique => MonsterRarity.Unique,
        _ => MonsterRarity.Normal,
    };
}
