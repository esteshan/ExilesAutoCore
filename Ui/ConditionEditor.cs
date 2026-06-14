using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using ExileCore2.Shared.Helpers;
using ExilesAutoCore.Rules;
using ExilesAutoCore.State;
using ImGuiNET;

namespace ExilesAutoCore.Ui;

/// <summary>
/// Draws and edits a list of <see cref="Condition"/>s through dropdowns — no typing of code. Shared
/// by the rule builder and the combo-step builder. Each row shows only the inputs its kind needs,
/// with a live dot indicating whether that condition currently passes.
/// </summary>
public static class ConditionEditor
{
    /// <summary>Set per-frame by the plugin. When true, the expanded editor shows the raw kind dropdown.</summary>
    public static bool AdvancedMode;

    public static void Draw(List<Condition> conditions, GameState state)
    {
        var toDelete = -1;
        for (var i = 0; i < conditions.Count; i++)
        {
            ImGui.PushID(i);
            if (DrawCondition(conditions[i], state))
            {
                toDelete = i;
            }

            ImGui.PopID();
        }

        if (toDelete >= 0)
        {
            conditions.RemoveAt(toDelete);
        }

        // Grouped "Add condition" menu: pick by category instead of a flat list of technical names.
        if (ImGui.Button("Add condition"))
        {
            ImGui.OpenPopup("addCondition");
        }

        if (ImGui.BeginPopup("addCondition"))
        {
            if (ImGui.BeginMenu("Presets"))
            {
                foreach (var preset in ConditionPresets.All)
                {
                    if (ImGui.MenuItem(preset.Label))
                    {
                        conditions.AddRange(preset.Build());
                    }
                }

                ImGui.EndMenu();
            }

            ImGui.Separator();

            foreach (var category in Enum.GetValues<ConditionCategory>())
            {
                if (!ImGui.BeginMenu(category.ToString()))
                {
                    continue;
                }

                foreach (var entry in ConditionMeta.InCategory(category))
                {
                    if (ImGui.MenuItem(entry.Label))
                    {
                        conditions.Add(new Condition { Kind = entry.Kind });
                    }
                }

                ImGui.EndMenu();
            }

            ImGui.EndPopup();
        }
    }

    /// <summary>Draws one condition row with only the inputs its kind needs. Returns true to delete.</summary>
    private static bool DrawCondition(Condition c, GameState state)
    {
        Controls.StatusDot(c.Evaluate(state));
        ImGui.SameLine();

        // Collapsed = a category-coloured plain-English summary; expand to edit the details.
        ImGui.PushStyleColor(ImGuiCol.Text, CategoryColor(ConditionMeta.CategoryOf(c.Kind)).ToImguiVec4());
        var open = ImGui.TreeNodeEx($"{c.Describe()}###cond");
        ImGui.PopStyleColor();

        ImGui.SameLine();
        var delete = ImGui.SmallButton("x");

        if (!open)
        {
            return delete;
        }

        ImGui.Indent();
        if (AdvancedMode)
        {
            ImGui.SetNextItemWidth(170);
            Controls.EnumCombo("##kind", ref c.Kind);
        }
        else
        {
            ImGui.TextUnformatted(ConditionMeta.LabelOf(c.Kind));
        }

        switch (c.Kind)
        {
            case ConditionKind.LifePercent:
            case ConditionKind.EnergyShieldPercent:
            case ConditionKind.ManaPercent:
                SameLineComparison(c);
                SameLineValue(c);
                break;

            case ConditionKind.PlayerHasBuff:
            case ConditionKind.PlayerMissingBuff:
                SameLineText(c, "buff name");
                break;

            case ConditionKind.PlayerBuffCharges:
            case ConditionKind.PlayerBuffTimeLeft:
                SameLineText(c, "buff name");
                SameLineComparison(c);
                SameLineValue(c);
                break;

            case ConditionKind.PlayerHasAilment:
                SameLineAilmentPicker(c);
                break;

            case ConditionKind.MonsterCount:
                SameLineRange(c);
                SameLineRarity(c);
                SameLineComparison(c);
                SameLineValue(c);
                break;

            case ConditionKind.SkillReady:
            case ConditionKind.SkillUsing:
            case ConditionKind.SkillNotUsing:
            case ConditionKind.SkillOffCooldown:
            case ConditionKind.SkillManaAvailable:
                SameLineSkillPicker(c, state);
                break;

            case ConditionKind.SkillUseStage:
                SameLineSkillPicker(c, state);
                SameLineComparison(c);
                SameLineValue(c);
                break;

            case ConditionKind.WeaponSet:
                SameLineComparison(c);
                SameLineValue(c);
                break;

            case ConditionKind.IsMoving:
                SameLineMovingToggle(c);
                break;

            case ConditionKind.MouseButtonHeld:
                SameLineMouseButton(c);
                break;

            case ConditionKind.MonstersNearCursor:
                SameLineRange(c);
                SameLineRarity(c);
                SameLineComparison(c);
                SameLineValue(c);
                break;

            case ConditionKind.CursorDistance:
                SameLineComparison(c);
                SameLineValue(c);
                break;

            case ConditionKind.MonsterHasBuff:
            case ConditionKind.MonsterMissingBuff:
                SameLineRange(c);
                SameLineRarity(c);
                SameLineText(c, "buff/debuff name");
                break;

            case ConditionKind.MonsterHeavyStunned:
            case ConditionKind.MonsterLifePercent:
                SameLineRange(c);
                SameLineRarity(c);
                SameLineComparison(c);
                SameLineValue(c);
                break;

            case ConditionKind.MonsterCullable:
                SameLineRange(c);
                DrawCullThresholds(c);
                break;

            case ConditionKind.MonsterIsTargeted:
                MonsterStateRow(c, "is targeted", "not targeted");
                break;
            case ConditionKind.MonsterIsTargetable:
                MonsterStateRow(c, "is targetable", "not targetable");
                break;
            case ConditionKind.MonsterInvincible:
                MonsterStateRow(c, "is invincible", "is not invincible");
                break;
            case ConditionKind.MonsterOnLowLife:
                MonsterStateRow(c, "is on low life", "not on low life");
                break;
            case ConditionKind.MonsterOnFullLife:
                MonsterStateRow(c, "is on full life", "not on full life");
                break;
            case ConditionKind.MonsterCannotDie:
                MonsterStateRow(c, "cannot die", "can die");
                break;
            case ConditionKind.MonsterCannotBeStunned:
                MonsterStateRow(c, "cannot be stunned", "can be stunned");
                break;
            case ConditionKind.MonsterFrozen:
                MonsterStateRow(c, "is frozen", "not frozen");
                break;
            case ConditionKind.MonsterLightStunned:
                MonsterStateRow(c, "is light-stunned", "not light-stunned");
                break;
            case ConditionKind.MonsterChilled:
                MonsterStateRow(c, "is chilled", "not chilled");
                break;
            case ConditionKind.MonsterShocked:
                MonsterStateRow(c, "is shocked", "not shocked");
                break;
            case ConditionKind.MonsterElectrocuted:
                MonsterStateRow(c, "is electrocuted", "not electrocuted");
                break;
            case ConditionKind.MonsterBleeding:
                MonsterStateRow(c, "is bleeding", "not bleeding");
                break;
            case ConditionKind.MonsterPoisoned:
                MonsterStateRow(c, "is poisoned", "not poisoned");
                break;
            case ConditionKind.MonsterIgnited:
                MonsterStateRow(c, "is ignited", "not ignited");
                break;
            case ConditionKind.MonsterSapped:
                MonsterStateRow(c, "is sapped", "not sapped");
                break;
            case ConditionKind.MonsterScorched:
                MonsterStateRow(c, "is scorched", "not scorched");
                break;
            case ConditionKind.MonsterMaimed:
                MonsterStateRow(c, "is maimed", "not maimed");
                break;
            case ConditionKind.MonsterHindered:
                MonsterStateRow(c, "is hindered", "not hindered");
                break;
            case ConditionKind.MonsterPinned:
                MonsterStateRow(c, "is pinned", "not pinned");
                break;
            case ConditionKind.MonsterImmobilised:
                MonsterStateRow(c, "is immobilised", "not immobilised");
                break;
            case ConditionKind.MonsterDazed:
                MonsterStateRow(c, "is dazed", "not dazed");
                break;
            case ConditionKind.MonsterBlinded:
                MonsterStateRow(c, "is blinded", "not blinded");
                break;

            case ConditionKind.MonsterNearby:
            case ConditionKind.NoMonsterInvincible:
                SameLineRange(c);
                SameLineRarity(c);
                break;

            case ConditionKind.FlaskActive:
                SameLineFlaskSlot(c);
                SameLineBool(c, "active", "not active");
                break;

            case ConditionKind.FlaskReady:
            case ConditionKind.FlaskUsable:
                SameLineFlaskSlot(c);
                break;

            case ConditionKind.FlaskCharges:
                SameLineFlaskSlot(c);
                SameLineComparison(c);
                SameLineValue(c);
                break;

            case ConditionKind.InTown:
                SameLineBool(c, "in town", "not in town");
                break;

            case ConditionKind.InHideout:
                SameLineBool(c, "in hideout", "not in hideout");
                break;

            case ConditionKind.InPeacefulArea:
                SameLineBool(c, "in peaceful area", "not peaceful");
                break;

            case ConditionKind.ChatOpen:
                SameLineBool(c, "chat open", "chat closed");
                break;

            case ConditionKind.PanelOpen:
                SameLineBool(c, "panel open", "no panel open");
                break;

            // HoveringMonster needs no extra inputs.
        }

        ImGui.Unindent();
        ImGui.TreePop();
        return delete;
    }

    // Tints a condition's summary by its category so a step's conditions are scannable at a glance.
    private static Color CategoryColor(ConditionCategory category) => category switch
    {
        ConditionCategory.Monster => Color.IndianRed,
        ConditionCategory.Skill => Color.YellowGreen,
        ConditionCategory.Player => Color.Gainsboro,
        ConditionCategory.Cursor => Color.MediumPurple,
        ConditionCategory.Input => Color.Goldenrod,
        ConditionCategory.Flask => Color.SkyBlue,
        ConditionCategory.Area => Color.MediumAquamarine,
        _ => Color.Gainsboro,
    };

    // --- Per-field inputs (each continues the condition's row) ---------------------------------

    private static void SameLineComparison(Condition c)
    {
        ImGui.SameLine();
        ImGui.SetNextItemWidth(110);
        var ops = Enum.GetValues<Comparison>();
        var labels = ops.Select(ConditionMeta.Word).ToArray();
        var index = Array.IndexOf(ops, c.Operator);
        if (index < 0)
        {
            index = 0;
        }

        if (ImGui.Combo("##op", ref index, labels, labels.Length))
        {
            c.Operator = ops[index];
        }
    }

    private static void SameLineValue(Condition c)
    {
        ImGui.SameLine();
        ImGui.SetNextItemWidth(80);
        ImGui.InputFloat("##value", ref c.Value);
    }

    private static void SameLineText(Condition c, string hint)
    {
        ImGui.SameLine();
        ImGui.SetNextItemWidth(190);
        ImGui.InputTextWithHint("##text", hint, ref c.Text, 64);
    }

    private static void SameLineRange(Condition c)
    {
        ImGui.SameLine();
        ImGui.Text("within");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(80);
        ImGui.InputInt("##range", ref c.Range);
    }

    private static void SameLineRarity(Condition c)
    {
        ImGui.SameLine();
        ImGui.SetNextItemWidth(120);
        Controls.EnumCombo("##rarity", ref c.Rarity);
    }

    private static void SameLineAilmentPicker(Condition c)
    {
        ImGui.SameLine();
        var names = Ailments.Names.ToArray();
        var current = Array.IndexOf(names, c.Text);
        ImGui.SetNextItemWidth(150);
        if (ImGui.Combo("##ailment", ref current, names, names.Length) && current >= 0)
        {
            c.Text = names[current];
        }

        SameLineBool(c, "is affected", "not affected");
    }

    // Per-rarity life% thresholds for the cullable condition, each on its own line under the row.
    private static void DrawCullThresholds(Condition c)
    {
        ImGui.SetNextItemWidth(70);
        ImGui.InputFloat("Normal life% <=", ref c.CullNormal);
        ImGui.SetNextItemWidth(70);
        ImGui.InputFloat("Magic life% <=", ref c.CullMagic);
        ImGui.SetNextItemWidth(70);
        ImGui.InputFloat("Rare life% <=", ref c.CullRare);
        ImGui.SetNextItemWidth(70);
        ImGui.InputFloat("Unique life% <=", ref c.CullUnique);
    }

    private static void SameLineFlaskSlot(Condition c)
    {
        ImGui.SameLine();
        ImGui.SetNextItemWidth(110);
        var index = c.FlaskSlot <= 1 ? 0 : 1;
        string[] options = { "Flask 1", "Flask 2" };
        if (ImGui.Combo("##flask", ref index, options, options.Length))
        {
            c.FlaskSlot = index + 1;
        }
    }

    // A monster-state row: range + rarity filter plus an is/is-not toggle. Shared by every
    // boolean monster-state condition (invincible, frozen, the ailments, ...).
    private static void MonsterStateRow(Condition c, string trueLabel, string falseLabel)
    {
        SameLineRange(c);
        SameLineRarity(c);
        SameLineBool(c, trueLabel, falseLabel);
    }

    // A two-option toggle for boolean conditions (e.g. "in town" / "not in town").
    private static void SameLineBool(Condition c, string trueLabel, string falseLabel)
    {
        ImGui.SameLine();
        ImGui.SetNextItemWidth(150);
        var index = c.BoolValue ? 0 : 1;
        string[] options = { trueLabel, falseLabel };
        if (ImGui.Combo("##bool", ref index, options, options.Length))
        {
            c.BoolValue = index == 0;
        }
    }

    private static void SameLineMouseButton(Condition c)
    {
        ImGui.SameLine();
        ImGui.SetNextItemWidth(110);
        Controls.EnumCombo("##mousebtn", ref c.MouseButton);
    }

    private static void SameLineMovingToggle(Condition c)
    {
        ImGui.SameLine();
        ImGui.SetNextItemWidth(140);
        var index = c.BoolValue ? 0 : 1;
        string[] options = { "is moving", "is stationary" };
        if (ImGui.Combo("##moving", ref index, options, options.Length))
        {
            c.BoolValue = index == 0;
        }
    }

    // Offers a dropdown of the player's slotted skills; falls back to free text when not in game.
    private static void SameLineSkillPicker(Condition c, GameState state)
    {
        ImGui.SameLine();
        var skills = state.Skills.AllSkills;
        if (skills.Count == 0)
        {
            SameLineText(c, "skill name");
            return;
        }

        var names = skills.Select(s => s.Name).ToArray();
        var current = Array.IndexOf(names, c.Text);
        ImGui.SetNextItemWidth(190);
        if (ImGui.Combo("##skill", ref current, names, names.Length) && current >= 0)
        {
            c.Text = names[current];
        }

        if (current < 0)
        {
            ImGui.SameLine();
            ImGui.TextColored(Color.Gray.ToImguiVec4(), string.IsNullOrEmpty(c.Text) ? "(pick a skill)" : $"({c.Text})");
        }
    }
}
