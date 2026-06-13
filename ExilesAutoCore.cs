using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using ExileCore2;
using ExileCore2.Shared.Helpers;
using ExilesAutoCore.Rules;
using ExilesAutoCore.State;
using ExilesAutoCore.Ui;
using ImGuiNET;

namespace ExilesAutoCore;

public sealed class ExilesAutoCore : BaseSettingsPlugin<ExilesAutoCoreSettings>
{
    private readonly RuleBuilder _ruleBuilder = new();
    private readonly ComboBuilder _comboBuilder = new();
    private readonly Engine _engine = new();

    public override void DrawSettings()
    {
        // Only show the configuration menu while the plugin itself is enabled (its checkbox is ticked).
        if (!Settings.Enable)
        {
            ImGui.TextColored(Color.Gray.ToImguiVec4(), "Enable ExilesAutoCore (tick it in the plugin list) to configure it.");
            return;
        }

        // Single-threaded ImGui draw, so a static flag is enough to thread the mode into the editor.
        ConditionEditor.AdvancedMode = Settings.AdvancedMode;

        Theme.Push();
        try
        {
            base.DrawSettings();

            DrawProfileBar();
            var profile = ActiveProfile();

            // A fresh snapshot each frame the settings window is open, used to light the live status dots.
            var state = new GameState(GameController, Settings.MaxMonsterRange);

            if (ImGui.CollapsingHeader("Automation rules", ImGuiTreeNodeFlags.DefaultOpen))
            {
                DrawAutomationStatus();
                _ruleBuilder.Draw(profile.Rules, state);
            }

            if (ImGui.CollapsingHeader("Combos", ImGuiTreeNodeFlags.DefaultOpen))
            {
                _comboBuilder.Draw(profile.Combos, state);
            }
        }
        finally
        {
            Theme.Pop();
        }
    }

    public override void Render()
    {
        // The engine runs every frame (not just when settings are open), but only when the plugin and
        // automation are both armed and we're in a valid in-game state.
        if (!Settings.Enable || !Settings.EnableAutomation)
        {
            return;
        }

        var state = new GameState(GameController, Settings.MaxMonsterRange);
        if (!state.IsValid)
        {
            return;
        }

        _engine.Tick(GameController, state, ActiveProfile(), Settings);
    }

    // Ensures at least one profile exists — migrating any pre-profiles rules/combos into a "Default"
    // one — clamps the active index, and returns the active profile.
    private Profile ActiveProfile()
    {
        if (Settings.Profiles.Count == 0)
        {
            Settings.Profiles.Add(new Profile
            {
                Name = "Default",
                Rules = new List<SkillRule>(Settings.Rules),
                Combos = new List<Combo>(Settings.Combos),
            });
            Settings.Rules.Clear();
            Settings.Combos.Clear();
        }

        if (Settings.ActiveProfileIndex < 0 || Settings.ActiveProfileIndex >= Settings.Profiles.Count)
        {
            Settings.ActiveProfileIndex = 0;
        }

        return Settings.Profiles[Settings.ActiveProfileIndex];
    }

    private void DrawProfileBar()
    {
        ActiveProfile(); // make sure a profile exists before building the dropdown

        var names = Settings.Profiles.Select(p => p.Name).ToArray();
        var index = Settings.ActiveProfileIndex;
        ImGui.SetNextItemWidth(200);
        if (ImGui.Combo("Profile", ref index, names, names.Length))
        {
            Settings.ActiveProfileIndex = index;
        }

        ImGui.SameLine();
        if (ImGui.Button("New profile"))
        {
            Settings.Profiles.Add(new Profile { Name = $"Profile {Settings.Profiles.Count + 1}" });
            Settings.ActiveProfileIndex = Settings.Profiles.Count - 1;
        }

        ImGui.SameLine();
        if (ImGui.Button("Delete profile") && Settings.Profiles.Count > 1)
        {
            Settings.Profiles.RemoveAt(Settings.ActiveProfileIndex);
            Settings.ActiveProfileIndex = 0;
        }

        var active = Settings.Profiles[Settings.ActiveProfileIndex];
        ImGui.SetNextItemWidth(200);
        ImGui.InputText("Profile name", ref active.Name, 40);
        ImGui.Separator();
    }

    private void DrawAutomationStatus()
    {
        if (!Settings.EnableAutomation)
        {
            ImGui.TextColored(Color.Gray.ToImguiVec4(), "Automation: OFF — turn on \"Enable automation\" above to press keys.");
        }
        else if (_engine.PauseReason is "" or "Ready")
        {
            ImGui.TextColored(Color.Lime.ToImguiVec4(), "Automation: ON (running)");
        }
        else
        {
            ImGui.TextColored(Color.Orange.ToImguiVec4(), $"Automation: ON but paused — {_engine.PauseReason}");
        }

        ImGui.Text($"Last action: {_engine.LastAction}");
        ImGui.Separator();
    }
}
