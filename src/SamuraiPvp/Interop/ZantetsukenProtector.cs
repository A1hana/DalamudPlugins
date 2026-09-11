using System.Numerics;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game;
using SamuraiPvp.Core;

namespace SamuraiPvp.Interop;

// Adapted from the local PvpAutoLb NinjaTenchuProtector. All original requests keep
// their action, target, mode and arguments. There is no action-generation path.
internal sealed unsafe class ZantetsukenProtector : IDisposable
{
    private readonly Services services;
    private readonly Configuration configuration;
    private readonly GameState game;
    private Hook<ActionManager.Delegates.GetActionStatus>? statusHook;
    private Hook<ActionManager.Delegates.UseAction>? useHook;
    private Hook<ActionManager.Delegates.UseActionLocation>? locationHook;
    private long lastError;

    public bool Available => statusHook is { IsEnabled: true } && useHook is { IsEnabled: true } &&
        locationHook is { IsEnabled: true };

    public ZantetsukenProtector(Services services, Configuration configuration, GameState game)
    {
        this.services = services;
        this.configuration = configuration;
        this.game = game;
        try
        {
            statusHook = services.Interop.HookFromAddress<ActionManager.Delegates.GetActionStatus>(
                ActionManager.Addresses.GetActionStatus.Value, StatusDetour);
            useHook = services.Interop.HookFromAddress<ActionManager.Delegates.UseAction>(
                ActionManager.Addresses.UseAction.Value, UseDetour);
            locationHook = services.Interop.HookFromAddress<ActionManager.Delegates.UseActionLocation>(
                ActionManager.Addresses.UseActionLocation.Value, LocationDetour);
            statusHook.Enable();
            useHook.Enable();
            locationHook.Enable();
        }
        catch (Exception ex)
        {
            services.Log.Error(ex, "[SamuraiPvp] 无法初始化斩铁剑保护，自动选人已暂停。");
            Dispose();
        }
    }

    private bool Protect(ActionType type, uint id) => type == ActionType.Action && id == Rules.Zantetsuken &&
        configuration.Enabled && configuration.ProtectLb && game.IsSamuraiPvP;

    // Bypasses OUR status detour only. The scan must not depend on the currently
    // selected target's safety, or it could never acquire the first valid target.
    public bool IsReady(ulong targetId)
    {
        var manager = ActionManager.Instance();
        return manager != null && statusHook != null &&
            statusHook.Original(manager, ActionType.Action, Rules.Zantetsuken, targetId, true, true, null) == 0;
    }

    private uint StatusDetour(ActionManager* manager, ActionType type, uint id, ulong targetId,
        bool checkRecast, bool checkCasting, uint* extraInfo)
    {
        var protecting = false;
        try
        {
            protecting = Protect(type, id);
            if (protecting && !game.SafeTarget(targetId))
            {
                if (extraInfo != null) *extraInfo = 0;
                return 1;
            }
        }
        catch (Exception ex)
        {
            LogError(ex);
            if (protecting)
            {
                if (extraInfo != null) *extraInfo = 0;
                return 1;
            }
        }
        return statusHook!.Original(manager, type, id, targetId, checkRecast, checkCasting, extraInfo);
    }

    private bool UseDetour(ActionManager* manager, ActionType type, uint id, ulong targetId,
        uint extraParam, ActionManager.UseActionMode mode, uint comboRouteId, bool* areaTargeted)
    {
        var protecting = false;
        try
        {
            protecting = Protect(type, id);
            if (protecting && (!game.SafeTarget(targetId) || !IsReady(targetId)))
            {
                if (areaTargeted != null) *areaTargeted = false;
                return false;
            }
        }
        catch (Exception ex)
        {
            LogError(ex);
            if (protecting)
            {
                if (areaTargeted != null) *areaTargeted = false;
                return false;
            }
        }
        return useHook!.Original(manager, type, id, targetId, extraParam, mode, comboRouteId, areaTargeted);
    }

    // Also recheck the final dispatch: Kuzushi can expire, a shield can be applied,
    // or a queued target can die after the keypress. Native execution still checks
    // resources, bind, other incapacitation, and animation state itself.
    private bool LocationDetour(ActionManager* manager, ActionType type, uint id, ulong targetId,
        Vector3* location, uint extraParam, byte a7)
    {
        var protecting = false;
        try
        {
            protecting = Protect(type, id);
            if (protecting && !game.SafeTarget(targetId)) return false;
        }
        catch (Exception ex)
        {
            LogError(ex);
            if (protecting) return false;
        }
        return locationHook!.Original(manager, type, id, targetId, location, extraParam, a7);
    }

    private void LogError(Exception ex)
    {
        var now = Environment.TickCount64;
        if (now - lastError < 5000) return;
        lastError = now;
        services.Log.Warning(ex, "[SamuraiPvp] 斩铁剑检查失败；当前请求受到保护。 ");
    }

    public void Dispose()
    {
        locationHook?.Dispose();
        useHook?.Dispose();
        statusHook?.Dispose();
        locationHook = null;
        useHook = null;
        statusHook = null;
    }
}
