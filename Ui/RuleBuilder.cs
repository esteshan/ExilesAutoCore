using System;
using System.Collections.Generic;
using System.Drawing;
using ExileCore2.Shared.Helpers;
using ExilesAutoCore.Rules;
using ExilesAutoCore.State;
using ImGuiNET;

namespace ExilesAutoCore.Ui;

/// <summary>
/// The no-code rule builder. Lists the <see cref="SkillRule"/>s and lets the user add, reorder,
/// edit, and delete them and their conditions entirely through dropdowns — no code. Rules fire
/// top-to-bottom (priority #1 first), and the up/dn buttons reorder them. A live dot shows whether
/// each rule currently passes against the real game.
/// </summary>
public sealed class RuleBuilder
{
    private enum RuleEdit
    {
        None,
        Delete,
        MoveUp,
        MoveDown,
    }

    /// <param name="onImport">Opens an import dialog and appends the imported rule to <paramref name="rules"/>.</param>
    /// <param name="onExport">Opens an export dialog for the given rule.</param>
    public void Draw(List<SkillRule> rules, GameState state, Action onImport, Action<SkillRule> onExport)
    {
        if (ImGui.Button("Add rule"))
        {
            rules.Add(new SkillRule());
        }

        ImGui.SameLine();
        if (ImGui.Button("Import rule"))
        {
            onImport();
        }

        ImGui.TextColored(Color.Gray.ToImguiVec4(), "Priority #1 fires first. Use up/dn to reorder.");

        var edit = RuleEdit.None;
        var editIndex = -1;
        for (var i = 0; i < rules.Count; i++)
        {
            ImGui.PushID(i);
            var result = DrawRule(rules[i], i, state, onExport);
            if (result != RuleEdit.None)
            {
                edit = result;
                editIndex = i;
            }

            ImGui.PopID();
        }

        ApplyEdit(rules, edit, editIndex);
    }

    /// <summary>Draws one rule. Returns the structural edit (if any) the user requested this frame.</summary>
    private static RuleEdit DrawRule(SkillRule rule, int index, GameState state, Action<SkillRule> onExport)
    {
        var edit = RuleEdit.None;

        Controls.StatusDot(rule.Enabled && rule.ActiveInCurrentArea(state) && rule.Matches(state));
        ImGui.SameLine();

        // Priority number + reorder buttons.
        ImGui.Text($"#{index + 1}");
        ImGui.SameLine();
        if (ImGui.SmallButton("up"))
        {
            edit = RuleEdit.MoveUp;
        }

        ImGui.SameLine();
        if (ImGui.SmallButton("dn"))
        {
            edit = RuleEdit.MoveDown;
        }

        ImGui.SameLine();
        if (ImGui.SmallButton("exp"))
        {
            onExport(rule);
        }

        ImGui.SameLine();
        ImGui.Checkbox("##enabled", ref rule.Enabled);
        ImGui.SameLine();

        var areaBadge = AreaFilter.Badge(rule.EnabledInMaps, rule.EnabledInTown, rule.EnabledInHideout, rule.EnabledInPeacefulAreas);
        if (!ImGui.CollapsingHeader($"{areaBadge} {rule.Name}  (key {rule.Action.InputLabel})###rule"))
        {
            return edit;
        }

        ImGui.Indent();

        ImGui.SetNextItemWidth(220);
        ImGui.InputText("Name", ref rule.Name, 40);

        Controls.AreaToggles(ref rule.EnabledInMaps, ref rule.EnabledInTown, ref rule.EnabledInHideout, ref rule.EnabledInPeacefulAreas);

        SkillActionEditor.Draw(rule.Action);

        ImGui.SetNextItemWidth(120);
        ImGui.InputFloat("Min seconds between fires (0 = none)", ref rule.Cooldown);

        ImGui.Separator();
        ImGui.TextColored(Color.Gray.ToImguiVec4(), "Conditions (all must be true to fire):");
        ConditionEditor.Draw(rule.Conditions, state);

        ImGui.Separator();
        if (ImGui.Button("Export rule"))
        {
            onExport(rule);
        }

        ImGui.SameLine();
        if (ImGui.Button("Delete rule"))
        {
            edit = RuleEdit.Delete;
        }
        // (Export is also on the header row as "exp" so it's reachable without expanding the rule.)

        ImGui.Unindent();
        return edit;
    }

    // Applies a single structural edit per frame, after the draw loop, so we never mutate mid-iteration.
    private static void ApplyEdit(List<SkillRule> rules, RuleEdit edit, int index)
    {
        switch (edit)
        {
            case RuleEdit.Delete:
                rules.RemoveAt(index);
                break;

            case RuleEdit.MoveUp when index > 0:
                (rules[index - 1], rules[index]) = (rules[index], rules[index - 1]);
                break;

            case RuleEdit.MoveDown when index < rules.Count - 1:
                (rules[index + 1], rules[index]) = (rules[index], rules[index + 1]);
                break;
        }
    }
}
