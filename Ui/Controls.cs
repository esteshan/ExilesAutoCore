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
}
