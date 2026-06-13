using System;
using System.Collections.Generic;
using ExilesAutoCore.State;

namespace ExilesAutoCore.Rules;

/// <summary>One named, ready-to-use condition (or small group) the user can add in one click.</summary>
public sealed record ConditionPreset(string Label, Func<IReadOnlyList<Condition>> Build);

/// <summary>
/// Built-in presets shown at the top of the "Add condition" menu. Each fills in the fiddly
/// defaults (operator, range, rarity, value) so a beginner only has to pick a skill/buff name where
/// one is needed. They produce ordinary <see cref="Condition"/>s — fully editable afterward.
/// </summary>
public static class ConditionPresets
{
    public static readonly IReadOnlyList<ConditionPreset> All = new[]
    {
        new ConditionPreset("Only when a monster is nearby", () => One(new Condition
        {
            Kind = ConditionKind.MonsterCount, Rarity = MonsterRarity.Any,
            Range = 60, Operator = Comparison.GreaterOrEqual, Value = 1,
        })),
        new ConditionPreset("Only on a rare or unique nearby", () => One(new Condition
        {
            Kind = ConditionKind.MonsterCount, Rarity = MonsterRarity.AtLeastRare,
            Range = 60, Operator = Comparison.GreaterOrEqual, Value = 1,
        })),
        new ConditionPreset("Only if 3+ monsters near cursor", () => One(new Condition
        {
            Kind = ConditionKind.MonstersNearCursor, Rarity = MonsterRarity.Any,
            Range = 100, Operator = Comparison.GreaterOrEqual, Value = 3,
        })),
        new ConditionPreset("Only when a monster is heavy-stunned", () => One(new Condition
        {
            Kind = ConditionKind.MonsterHeavyStunned, Rarity = MonsterRarity.AtLeastRare,
            Range = 60, Operator = Comparison.GreaterOrEqual, Value = 1,
        })),
        new ConditionPreset("Only on a low-life monster", () => One(new Condition
        {
            Kind = ConditionKind.MonsterLifePercent, Rarity = MonsterRarity.AtLeastRare,
            Range = 60, Operator = Comparison.LessOrEqual, Value = 35,
        })),
        new ConditionPreset("Only if missing this debuff (pick one)", () => One(new Condition
        {
            Kind = ConditionKind.MonsterMissingBuff, Rarity = MonsterRarity.Any, Range = 60,
        })),
        new ConditionPreset("Only if skill is ready (pick one)", () => One(new Condition
        {
            Kind = ConditionKind.SkillReady,
        })),
        new ConditionPreset("Don't cast while using a skill (pick one)", () => One(new Condition
        {
            Kind = ConditionKind.SkillNotUsing,
        })),
        new ConditionPreset("Panic at low life", () => One(new Condition
        {
            Kind = ConditionKind.LifePercent, Operator = Comparison.LessOrEqual, Value = 35,
        })),
        new ConditionPreset("Keep a buff up — refresh when low (pick one)", () => One(new Condition
        {
            Kind = ConditionKind.PlayerBuffTimeLeft, Operator = Comparison.LessThan, Value = 30,
        })),
    };

    private static IReadOnlyList<Condition> One(Condition condition) => new[] { condition };
}
