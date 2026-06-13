using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Windows.Forms;
using ExileCore2;
using ExileCore2.PoEMemory.Components;
using ExileCore2.Shared.Helpers;

namespace ExilesAutoCore.State;

/// <summary>
/// A read-only snapshot of everything the rules can inspect, for a single frame.
/// Build a fresh one each tick from the live <see cref="GameController"/>; the engine and the
/// settings UI both read from it. Expensive lookups (nearby monsters) are <see cref="Lazy{T}"/>,
/// so constructing a snapshot is cheap until a rule actually asks for that data.
///
/// This is the de-coupled replacement for ReAgent's RuleState: same useful read surface, but with
/// no rule-engine plumbing, no scripting sandbox, and no dependency on the plugin object.
/// </summary>
public sealed class GameState
{
    private readonly Lazy<NearbyMonsterInfo> _nearbyMonsters;

    /// <summary>True only when we're in-game with a valid, living player. Rules should no-op otherwise.</summary>
    public bool IsValid { get; }

    public VitalsInfo Vitals { get; }
    public BuffDictionary Buffs { get; }
    public SkillDictionary Skills { get; }
    public FlasksInfo Flasks { get; }

    public bool IsMoving { get; }
    public bool IsInTown { get; }
    public bool IsInHideout { get; }
    public bool IsInPeacefulArea { get; }
    public string AreaName { get; } = "";

    /// <summary>The equipped weapon set (0 or 1) for weapon-swap builds.</summary>
    public int ActiveWeaponSetIndex { get; }

    /// <summary>True while the chat input panel is open.</summary>
    public bool IsChatOpen { get; }

    /// <summary>True while any inventory/stash/large/fullscreen panel is open.</summary>
    public bool IsAnyPanelOpen { get; }

    /// <summary>The mouse cursor's position in world-grid coordinates (same space as entity GridPos).</summary>
    public Vector2 MousePosition { get; }

    /// <summary>Distance from the player to the cursor, in world units.</summary>
    public float CursorDistanceFromPlayer { get; }

    public GameState(GameController controller, int maxMonsterRange)
    {
        // Default everything to safe/empty so callers never have to null-check the snapshot.
        Skills = new SkillDictionary(null, null, isActiveSkillSet: true);
        Buffs = new BuffDictionary([], null);
        _nearbyMonsters = new Lazy<NearbyMonsterInfo>(
            () => new NearbyMonsterInfo(controller, maxMonsterRange), LazyThreadSafetyMode.None);

        var player = controller?.Player;
        if (player == null || !player.IsValid)
        {
            return;
        }

        var area = controller.Area.CurrentArea;
        IsInTown = area.IsTown;
        IsInHideout = area.IsHideout;
        IsInPeacefulArea = area.IsPeaceful;
        AreaName = area.Name;

        if (player.TryGetComponent<Actor>(out var actor))
        {
            IsMoving = actor.isMoving;
            Skills = new SkillDictionary(controller, player, isActiveSkillSet: true);
        }

        if (player.TryGetComponent<Life>(out var life))
        {
            Vitals = new VitalsInfo(life);
        }

        if (player.TryGetComponent<Stats>(out var stats))
        {
            ActiveWeaponSetIndex = stats.ActiveWeaponSetIndex;
        }

        player.TryGetComponent<Buffs>(out var buffs);
        Buffs = new BuffDictionary(buffs?.BuffsList ?? [], Skills);

        Flasks = new FlasksInfo(controller);

        var ui = controller.IngameState.IngameUi;
        IsChatOpen = ui.ChatTitlePanel.IsVisible;
        IsAnyPanelOpen = ui.OpenLeftPanel.IsVisible || ui.OpenRightPanel.IsVisible ||
                         ui.LargePanels.Any(p => p.IsVisible) || ui.FullscreenPanels.Any(p => p.IsVisible);

        MousePosition = controller.IngameState.ServerData.WorldMousePosition.WorldToGrid();
        CursorDistanceFromPlayer = Vector2.Distance(player.GridPos, MousePosition);

        // We have the data rules actually depend on; mark the snapshot usable.
        IsValid = Vitals != null;
    }

    /// <summary>Number of hostile monsters within <paramref name="range"/> matching <paramref name="rarity"/>.</summary>
    public int MonsterCount(int range, MonsterRarity rarity) => _nearbyMonsters.Value.GetMonsterCount(range, rarity);

    public int MonsterCount(int range) => MonsterCount(range, MonsterRarity.Any);

    /// <summary>Hostile monsters within <paramref name="range"/> matching <paramref name="rarity"/>.</summary>
    public IEnumerable<MonsterInfo> Monsters(int range, MonsterRarity rarity) => _nearbyMonsters.Value.GetMonsters(range, rarity);

    public IEnumerable<MonsterInfo> Monsters(int range) => Monsters(range, MonsterRarity.Any);

    /// <summary>
    /// The closest hostile monster matching <paramref name="rarity"/> within <paramref name="range"/>
    /// (used by auto-face), or null if none qualify. Results are ordered nearest-first.
    /// </summary>
    public MonsterInfo NearestHostile(int range, MonsterRarity rarity) =>
        _nearbyMonsters.Value.GetMonsters(range, rarity).FirstOrDefault();

    /// <summary>True when the cursor is over a monster the game considers targeted/highlighted.</summary>
    public bool IsHoveringMonster =>
        _nearbyMonsters.Value.GetMonsters(int.MaxValue, MonsterRarity.Any).Any(m => m.IsTargeted);

    /// <summary>Hostile monsters within <paramref name="radius"/> of the cursor (not the player).</summary>
    public int MonstersNearCursor(int radius, MonsterRarity rarity) =>
        _nearbyMonsters.Value.GetMonsters(int.MaxValue, rarity)
            .Count(m => Vector2.Distance(m.GridPosition, MousePosition) <= radius);

    /// <summary>True if any hostile monster within <paramref name="range"/> has the named buff/debuff.</summary>
    public bool AnyMonsterHasBuff(int range, MonsterRarity rarity, string buff) =>
        _nearbyMonsters.Value.GetMonsters(range, rarity).Any(m => m.Buffs.Has(buff));

    /// <summary>True if any hostile monster within <paramref name="range"/> is missing the named buff/debuff.</summary>
    public bool AnyMonsterMissingBuff(int range, MonsterRarity rarity, string buff) =>
        _nearbyMonsters.Value.GetMonsters(range, rarity).Any(m => !m.Buffs.Has(buff));

    /// <summary>Live key/mouse-button state (e.g. Keys.LButton). Read at evaluation time, not snapshotted.</summary>
    public bool IsKeyDown(Keys key) => Input.IsKeyDown(key);

    /// <summary>True if the given flask slot's effect is currently active. Slot is 1-based.</summary>
    public bool FlaskActive(int slot) => GetFlask(slot)?.Active ?? false;

    /// <summary>True if the given flask slot has enough charges to use right now. Slot is 1-based.</summary>
    public bool FlaskReady(int slot) => GetFlask(slot)?.CanBeUsed ?? false;

    /// <summary>Current charges on the given flask slot (0 if empty). Slot is 1-based.</summary>
    public int FlaskCharges(int slot) => GetFlask(slot)?.Charges ?? 0;

    private FlaskInfo GetFlask(int slot) => Flasks != null && slot is >= 1 and <= 2 ? Flasks[slot - 1] : null;
}
