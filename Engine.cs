using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Windows.Forms;
using ExileCore2;
using ExileCore2.PoEMemory.Components;
using ExileCore2.Shared.Helpers;
using ExilesAutoCore.Rules;
using ExilesAutoCore.State;
using static ExileCore2.Shared.Nodes.HotkeyNodeV2;

namespace ExilesAutoCore;

/// <summary>
/// Runs the automation each frame. After the safety gate and the global cooldown, it tries the
/// reactive <see cref="SkillRule"/>s first (top-to-bottom priority — good for defensive/emergency
/// presses), then advances any active <see cref="Combo"/>s. Only one key is pressed per tick.
///
/// Skills on cooldown simply fail their <c>SkillReady</c> condition, so a lower-priority rule (or the
/// next combo step) fires instead — cooldown handling falls out of the conditions.
/// </summary>
public sealed class Engine
{
    private readonly Stopwatch _sinceLastPress = Stopwatch.StartNew();

    /// <summary>What the engine last did, for the status display.</summary>
    public string LastAction { get; private set; } = "Idle";

    /// <summary>"Ready" when running; otherwise why the engine is currently holding off.</summary>
    public string PauseReason { get; private set; } = "";

    public void Tick(GameController controller, GameState state, Profile profile, ExilesAutoCoreSettings settings)
    {
        if (!CanExecute(controller, out var reason))
        {
            PauseReason = reason;
            return;
        }

        PauseReason = "Ready";

        // Global throttle: never press more often than the configured cooldown.
        if (_sinceLastPress.ElapsedMilliseconds < settings.GlobalKeyPressCooldown)
        {
            return;
        }

        if (TryFireRules(controller, state, profile.Rules) || TryFireCombos(controller, state, profile.Combos))
        {
            _sinceLastPress.Restart();
        }
    }

    private bool TryFireRules(GameController controller, GameState state, List<SkillRule> rules)
    {
        foreach (var rule in rules)
        {
            if (!rule.Enabled || rule.OnCooldown || !rule.Matches(state))
            {
                continue;
            }

            Execute(controller, state, rule.Action);
            rule.MarkFired();
            LastAction = $"{rule.Name} -> pressed {rule.Action.Key}";
            return true;
        }

        return false;
    }

    private bool TryFireCombos(GameController controller, GameState state, List<Combo> combos)
    {
        foreach (var combo in combos)
        {
            if (!combo.Enabled)
            {
                continue;
            }

            var step = combo.StepReadyToFire(state);
            if (step == null)
            {
                continue;
            }

            var stepNumber = combo.CurrentStep + 1;
            Execute(controller, state, step.Action);
            combo.OnFired();
            LastAction = $"{combo.Name} step {stepNumber} -> pressed {step.Action.Key}";
            return true;
        }

        return false;
    }

    // Performs one action: aim at a target first if requested, then press the key.
    private static void Execute(GameController controller, GameState state, SkillAction action)
    {
        if (action.AutoFace)
        {
            var monster = state.NearestHostile(action.AutoFaceRange, action.AutoFaceRarity);
            if (monster != null)
            {
                var screenPos = controller.IngameState.Camera.WorldToScreen(monster.Position) +
                                controller.Window.GetWindowRectangle().TopLeft;
                Input.SetCursorPos(screenPos);
            }
        }

        InputHelper.SendInputPress(new HotkeyNodeValue(action.Key));
    }

    /// <summary>The safety gate: returns false (with a reason) whenever it would be unsafe to act.</summary>
    private static bool CanExecute(GameController controller, out string reason)
    {
        if (!controller.Window.IsForeground())
        {
            reason = "Game window not focused";
            return false;
        }

        if (controller.Game.IsEscapeState)
        {
            reason = "Escape menu open";
            return false;
        }

        var player = controller.Player;
        if (player == null || !player.TryGetComponent<Life>(out var life) || life.CurHP <= 0)
        {
            reason = "Player dead or not ready";
            return false;
        }

        if (player.TryGetComponent<Buffs>(out var buffs) && buffs.HasBuff("grace_period"))
        {
            reason = "Grace period active";
            return false;
        }

        if (controller.IngameState.IngameUi.ChatTitlePanel.IsVisible)
        {
            reason = "Chat is open";
            return false;
        }

        reason = "Ready";
        return true;
    }
}
