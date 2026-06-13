using ExileCore2.Shared.Nodes;
using ExilesAutoCore.Rules;
using ImGuiNET;
using static ExileCore2.Shared.Nodes.HotkeyNodeV2;

namespace ExilesAutoCore.Ui;

/// <summary>
/// Edits a <see cref="SkillAction"/>: the key to press (click-to-bind picker) and the optional
/// auto-face target filter. Shared by the rule builder and the combo-step builder.
/// </summary>
public static class SkillActionEditor
{
    public static void Draw(SkillAction action)
    {
        var picker = new HotkeyNodeV2(new HotkeyNodeValue(action.Key)) { AllowControllerKeys = true };
        if (picker.DrawPickerButton($"Key: {action.Key}") && picker.Value?.Key is { } pressed)
        {
            action.Key = pressed;
        }

        ImGui.Checkbox("Auto-face: aim cursor at a monster before pressing", ref action.AutoFace);
        if (!action.AutoFace)
        {
            return;
        }

        ImGui.SameLine();
        ImGui.Text("target");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(120);
        Controls.EnumCombo("##facerarity", ref action.AutoFaceRarity);
        ImGui.SameLine();
        ImGui.Text("within");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(80);
        ImGui.InputInt("##facerange", ref action.AutoFaceRange);
    }
}
