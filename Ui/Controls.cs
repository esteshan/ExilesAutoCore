using System;
using System.Drawing;
using ExileCore2.Shared.Helpers;
using ImGuiNET;

namespace ExilesAutoCore.Ui;

/// <summary>Tiny shared ImGui helpers used across the builders.</summary>
public static class Controls
{
    /// <summary>A combo box over all values of an enum, edited by ref.</summary>
    public static void EnumCombo<T>(string label, ref T value) where T : struct, Enum
    {
        var names = Enum.GetNames<T>();
        var values = Enum.GetValues<T>();
        var current = Math.Max(0, Array.IndexOf(values, value));
        if (ImGui.Combo(label, ref current, names, names.Length))
        {
            value = values[current];
        }
    }

    /// <summary>A small lit/unlit indicator for "is this currently true".</summary>
    public static void StatusDot(bool on)
    {
        ImGui.TextColored((on ? Color.Lime : Color.Gray).ToImguiVec4(), on ? "(*)" : "( )");
    }

    /// <summary>
    /// The four "active in which area types" checkboxes on one line, mirroring ReAgent's group
    /// toggles. Shared by the rule and combo builders.
    /// </summary>
    public static void AreaToggles(ref bool inMaps, ref bool inTown, ref bool inHideout, ref bool inPeaceful)
    {
        ImGui.TextColored(Color.Gray.ToImguiVec4(), "Active in:");
        ImGui.SameLine();
        ImGui.Checkbox("Maps", ref inMaps);
        ImGui.SameLine();
        ImGui.Checkbox("Town", ref inTown);
        ImGui.SameLine();
        ImGui.Checkbox("Hideout", ref inHideout);
        ImGui.SameLine();
        ImGui.Checkbox("Other peaceful", ref inPeaceful);
    }
}
