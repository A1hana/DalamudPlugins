using Dalamud.Game.ClientState.Objects.SubKinds;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using SamuraiPvp.Core;
using NativeObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

namespace SamuraiPvp.Interop;

internal sealed unsafe class GameState(Services services, Configuration configuration)
{
    public bool IsSamuraiPvP => services.ClientState.IsPvP &&
        services.Objects.LocalPlayer?.ClassJob.RowId == Rules.SamuraiJob;

    public bool LocalAlive => services.Objects.LocalPlayer is { CurrentHp: > 0, IsDead: false };

    public bool GaugeFull
    {
        get
        {
            var gauge = LimitBreakController.Instance();
            return gauge != null && gauge->IsPvP && gauge->BarUnits > 0 && gauge->CurrentUnits >= gauge->BarUnits;
        }
    }

    public bool LbPending
    {
        get
        {
            var manager = ActionManager.Instance();
            return manager != null && (manager->CastActionId == Rules.Zantetsuken && manager->CastTimeTotal > 0 ||
                manager->ActionQueued && manager->QueuedActionType == ActionType.Action &&
                manager->QueuedActionId == Rules.Zantetsuken);
        }
    }

    public IPlayerCharacter? ResolveTarget(ulong targetId)
    {
        // An explicit (macro / queued) target must never silently fall back to the
        // currently selected enemy when it disappears or resolves to a non-player.
        if (targetId is not 0 and not Rules.NoTarget)
            return services.Objects.SearchById(targetId) as IPlayerCharacter;
        return (services.Targets.SoftTarget ?? services.Targets.Target) as IPlayerCharacter;
    }

    public Exclusion Evaluate(IPlayerCharacter? target)
    {
        var local = services.Objects.LocalPlayer;
        if (target == null || local == null || target.Address == 0 ||
            target.GameObjectId == local.GameObjectId || !target.IsTargetable || target.IsDead ||
            !Rules.IsFinite(target.Position)) return Exclusion.InvalidPlayer;

        var ownKuzushi = false;
        var immune = false;
        var warmachina = false;
        foreach (var status in target.StatusList)
        {
            if (status == null || status.StatusId == 0) continue;
            ownKuzushi |= Rules.IsOwnKuzushi(status.StatusId, status.SourceId, local.EntityId,
                status.RemainingTime, configuration.MinimumKuzushiSeconds);
            immune |= Rules.IsImmuneStatus(status.StatusId);
            warmachina |= status.StatusId == Rules.MountedWarmachina;
        }

        // This is the client's own target-category probe, not nameplate color or job.
        var hostile = ActionManager.CanUseActionOnTarget(Rules.Zantetsuken, (NativeObject*)target.Address);
        return new TargetFacts(true, hostile, ownKuzushi, immune, warmachina,
            target.CurrentHp, target.MaxHp, target.ShieldPercentage).Evaluate();
    }

    public bool InCastRangeAndSight(IPlayerCharacter target)
    {
        var local = services.Objects.LocalPlayer;
        if (local == null || local.Address == 0 || target.Address == 0) return false;
        // Uses actual action range, hitboxes, elevation and line of sight. No legacy
        // fixed distance subtraction and no arbitrary extension of the 20-yalm cast.
        return ActionManager.GetActionInRangeOrLoS(Rules.Zantetsuken,
            (NativeObject*)local.Address, (NativeObject*)target.Address) == 0;
    }

    public bool SafeTarget(ulong targetId)
    {
        if (!LocalAlive || !GaugeFull) return false;
        var target = ResolveTarget(targetId);
        return Evaluate(target) == Exclusion.None && InCastRangeAndSight(target!);
    }
}
