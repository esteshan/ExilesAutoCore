using System.Collections.Generic;
using System.Drawing;
using ExileCore2.Shared.Helpers;
using ExilesAutoCore.Rules;
using ExilesAutoCore.State;
using ImGuiNET;

namespace ExilesAutoCore.Ui;

/// <summary>
/// The no-code combo builder. Lists the <see cref="Combo"/>s and lets the user add ordered steps,
/// each with its own action and conditions. The step the combo is currently waiting on is
/// highlighted live, so you can watch the sequence advance in-game.
/// </summary>
public sealed class ComboBuilder
{
    public void Draw(List<Combo> combos, GameState state)
    {
        if (ImGui.Button("Add combo"))
        {
            combos.Add(new Combo());
        }

        var comboToDelete = -1;
        for (var i = 0; i < combos.Count; i++)
        {
            ImGui.PushID(i);
            if (DrawCombo(combos[i], state))
            {
                comboToDelete = i;
            }

            ImGui.PopID();
        }

        if (comboToDelete >= 0)
        {
            combos.RemoveAt(comboToDelete);
        }
    }

    /// <summary>Draws one combo. Returns true if the user pressed its Delete button.</summary>
    private static bool DrawCombo(Combo combo, GameState state)
    {
        Controls.StatusDot(combo.Enabled);
        ImGui.SameLine();
        ImGui.Checkbox("##enabled", ref combo.Enabled);
        ImGui.SameLine();

        var progress = combo.Steps.Count == 0 ? "no steps" : $"step {combo.CurrentStep + 1}/{combo.Steps.Count}";
        if (!ImGui.CollapsingHeader($"{combo.Name}  ({progress})###combo"))
        {
            return false;
        }

        var delete = false;
        ImGui.Indent();

        ImGui.SetNextItemWidth(220);
        ImGui.InputText("Name", ref combo.Name, 40);

        ImGui.SetNextItemWidth(120);
        ImGui.InputInt("Reset if idle (ms, 0 = never)", ref combo.ResetAfterIdleMs);

        ImGui.Separator();
        ImGui.TextColored(Color.Gray.ToImguiVec4(), "Steps (fired top to bottom, then loops):");
        DrawSteps(combo, state);

        ImGui.Separator();
        if (ImGui.Button("Delete combo"))
        {
            delete = true;
        }

        ImGui.Unindent();
        return delete;
    }

    private static void DrawSteps(Combo combo, GameState state)
    {
        var toDelete = -1;
        var moveUp = -1;
        var moveDown = -1;

        for (var i = 0; i < combo.Steps.Count; i++)
        {
            ImGui.PushID(i);
            var step = combo.Steps[i];

            // Highlight the step the combo is currently waiting on.
            var isCurrent = i == combo.CurrentStep && combo.Enabled;
            var label = isCurrent ? $"Step {i + 1} (current)" : $"Step {i + 1}";
            ImGui.TextColored((isCurrent ? Color.Yellow : Color.Gray).ToImguiVec4(), label);
            ImGui.SameLine();

            if (ImGui.SmallButton("up"))
            {
                moveUp = i;
            }

            ImGui.SameLine();
            if (ImGui.SmallButton("dn"))
            {
                moveDown = i;
            }

            ImGui.SameLine();
            if (ImGui.CollapsingHeader($"{step.Name}###step"))
            {
                ImGui.Indent();

                ImGui.SetNextItemWidth(200);
                ImGui.InputText("Step name", ref step.Name, 40);

                SkillActionEditor.Draw(step.Action);

                ImGui.SetNextItemWidth(120);
                ImGui.InputInt("Wait after firing (ms)", ref step.DelayAfterMs);

                ImGui.TextColored(Color.Gray.ToImguiVec4(), "Conditions for this step (all must be true):");
                ConditionEditor.Draw(step.Conditions, state);

                if (ImGui.Button("Delete step"))
                {
                    toDelete = i;
                }

                ImGui.Unindent();
            }

            ImGui.PopID();
        }

        if (ImGui.Button("Add step"))
        {
            combo.Steps.Add(new ComboStep());
        }

        ApplyStepEdits(combo, toDelete, moveUp, moveDown);
    }

    // Applies a single structural edit per frame, after the draw loop, so we never mutate mid-iteration.
    private static void ApplyStepEdits(Combo combo, int toDelete, int moveUp, int moveDown)
    {
        if (toDelete >= 0)
        {
            combo.Steps.RemoveAt(toDelete);
            combo.Reset();
        }
        else if (moveUp > 0)
        {
            (combo.Steps[moveUp - 1], combo.Steps[moveUp]) = (combo.Steps[moveUp], combo.Steps[moveUp - 1]);
            combo.Reset();
        }
        else if (moveDown >= 0 && moveDown < combo.Steps.Count - 1)
        {
            (combo.Steps[moveDown + 1], combo.Steps[moveDown]) = (combo.Steps[moveDown], combo.Steps[moveDown + 1]);
            combo.Reset();
        }
    }
}
