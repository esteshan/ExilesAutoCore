using System.Collections.Generic;
using System.Diagnostics;
using ExilesAutoCore.State;

namespace ExilesAutoCore.Rules;

/// <summary>
/// An ordered, looping sequence of <see cref="ComboStep"/>s — the Dragon-Age-tactics "do X, then Y,
/// then Z" state machine. The engine advances it one step at a time: it fires the current step only
/// when that step's conditions pass AND the previous step's delay has elapsed, then moves to the next
/// step (wrapping back to the first). Per-step conditions let the sequence wait for the right moment
/// (e.g. "detonate only once the target has my debuff").
///
/// Configuration is public fields (serialized + UI-edited). Sequence progress is private runtime
/// state, which is intentionally NOT serialized.
/// </summary>
public sealed class Combo
{
    public bool Enabled = true;
    public string Name = "New combo";
    public List<ComboStep> Steps = new();

    // Which area types the combo may run in. Defaults to Maps-only, like SkillRule.
    public bool EnabledInMaps = true;
    public bool EnabledInTown = false;
    public bool EnabledInHideout = false;
    public bool EnabledInPeacefulAreas = false;

    /// <summary>If the sequence makes no progress for this long, it resets to the first step (0 = never).</summary>
    public int ResetAfterIdleMs = 2000;

    // --- runtime state (private, so it is not serialized) ---
    private int _currentStep;
    private readonly Stopwatch _sinceLastFire = new();
    private int _delayBeforeNextStepMs;

    /// <summary>Which step the combo is currently waiting to fire (for the UI).</summary>
    public int CurrentStep => _currentStep;

    /// <summary>True if this combo is allowed to run in the player's current area type.</summary>
    public bool ActiveInCurrentArea(GameState state) => AreaFilter.Allows(
        state, EnabledInMaps, EnabledInTown, EnabledInHideout, EnabledInPeacefulAreas);

    /// <summary>
    /// Returns the step to fire right now, or null if the combo should keep waiting. Also resets a
    /// stalled combo back to the start after <see cref="ResetAfterIdleMs"/>. Call <see cref="OnFired"/>
    /// after actually pressing the returned step's key.
    /// </summary>
    public ComboStep StepReadyToFire(GameState state)
    {
        if (Steps.Count == 0)
        {
            return null;
        }

        // Recover a stuck sequence: if it's been waiting partway through for too long, start over.
        if (ResetAfterIdleMs > 0 && _currentStep != 0 &&
            _sinceLastFire.IsRunning && _sinceLastFire.ElapsedMilliseconds > ResetAfterIdleMs)
        {
            Reset();
        }

        if (_currentStep >= Steps.Count)
        {
            _currentStep = 0;
        }

        // Still honoring the previous step's "let the cast land" delay.
        if (_sinceLastFire.IsRunning && _sinceLastFire.ElapsedMilliseconds < _delayBeforeNextStepMs)
        {
            return null;
        }

        var step = Steps[_currentStep];
        return step.Matches(state) ? step : null;
    }

    /// <summary>Records that the current step fired and advances to the next (wrapping to the start).</summary>
    public void OnFired()
    {
        _delayBeforeNextStepMs = Steps[_currentStep].DelayAfterMs;
        _currentStep = (_currentStep + 1) % Steps.Count;
        _sinceLastFire.Restart();
    }

    /// <summary>Clears sequence progress back to the first step.</summary>
    public void Reset()
    {
        _currentStep = 0;
        _sinceLastFire.Reset();
        _delayBeforeNextStepMs = 0;
    }
}
