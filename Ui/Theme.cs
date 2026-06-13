using System.Numerics;
using ImGuiNET;

namespace ExilesAutoCore.Ui;

/// <summary>
/// A scoped ImGui style for our settings panel — rounded frames, roomier spacing, and a refined
/// dark/orange palette. Push() at the start of DrawSettings, Pop() at the end (always, via finally),
/// so the styling applies only to our content and never leaks into other plugins' UI.
/// </summary>
public static class Theme
{
    private static int _colors;
    private static int _vars;

    public static void Push()
    {
        _colors = 0;
        _vars = 0;

        // Collapsing-header bars (sections, rule/combo headers).
        Color(ImGuiCol.Header, 0.66f, 0.33f, 0.09f, 0.85f);
        Color(ImGuiCol.HeaderHovered, 0.80f, 0.42f, 0.12f, 1f);
        Color(ImGuiCol.HeaderActive, 0.80f, 0.42f, 0.12f, 1f);

        // Buttons.
        Color(ImGuiCol.Button, 0.34f, 0.20f, 0.09f, 1f);
        Color(ImGuiCol.ButtonHovered, 0.66f, 0.33f, 0.09f, 1f);
        Color(ImGuiCol.ButtonActive, 0.80f, 0.42f, 0.12f, 1f);

        // Inputs / combos / sliders.
        Color(ImGuiCol.FrameBg, 0.15f, 0.13f, 0.11f, 1f);
        Color(ImGuiCol.FrameBgHovered, 0.24f, 0.20f, 0.15f, 1f);
        Color(ImGuiCol.FrameBgActive, 0.30f, 0.24f, 0.17f, 1f);
        Color(ImGuiCol.SliderGrab, 0.80f, 0.42f, 0.12f, 1f);
        Color(ImGuiCol.SliderGrabActive, 0.92f, 0.52f, 0.16f, 1f);
        Color(ImGuiCol.CheckMark, 0.92f, 0.52f, 0.16f, 1f);

        Var(ImGuiStyleVar.FrameRounding, 4f);
        Var(ImGuiStyleVar.GrabRounding, 4f);
        Var(ImGuiStyleVar.FrameBorderSize, 1f);
        Var(ImGuiStyleVar.FramePadding, new Vector2(6, 4));
        Var(ImGuiStyleVar.ItemSpacing, new Vector2(8, 6));
        Var(ImGuiStyleVar.IndentSpacing, 14f);
    }

    public static void Pop()
    {
        ImGui.PopStyleVar(_vars);
        ImGui.PopStyleColor(_colors);
        _vars = 0;
        _colors = 0;
    }

    private static void Color(ImGuiCol target, float r, float g, float b, float a)
    {
        ImGui.PushStyleColor(target, new Vector4(r, g, b, a));
        _colors++;
    }

    private static void Var(ImGuiStyleVar target, float value)
    {
        ImGui.PushStyleVar(target, value);
        _vars++;
    }

    private static void Var(ImGuiStyleVar target, Vector2 value)
    {
        ImGui.PushStyleVar(target, value);
        _vars++;
    }
}
