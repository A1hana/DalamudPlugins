using Dalamud.Game.ClientState.Objects.SubKinds;
using SamuraiPvp.Interop;

namespace SamuraiPvp.Core;

internal sealed record DisplayState(string Message, int CurrentKills = 0, int BestKills = 0,
    string CurrentName = "—", string BestName = "—", bool Ready = false, int Candidates = 0);

internal sealed class TargetController(Services services, Configuration configuration,
    GameState game, ZantetsukenProtector protector)
{
    private readonly List<Candidate> candidates = new(64);
    private readonly Dictionary<ulong, IPlayerCharacter> players = new(64);
    private ulong ownedTarget;
    private long lastScan;
    private long lastError;
    public DisplayState Display { get; private set; } = new("等待武士 PvP");

    public void Update(bool force = false)
    {
        try
        {
            if (!configuration.Enabled) { Reset("插件已关闭"); return; }
            if (!game.IsSamuraiPvP) { Reset("等待武士 PvP"); return; }
            if (!game.LocalAlive) { Reset("自身无法行动"); return; }
            if (!protector.Available) { Reset("保护接口不可用，自动选人暂停"); return; }
            if (!game.GaugeFull) { Reset("LB 未就绪"); return; }
            if (game.LbPending)
            {
                Display = new("斩铁剑已提交");
                return;
            }

            var now = Environment.TickCount64;
            if (!force && now - lastScan < configuration.ScanIntervalMs) return;
            lastScan = now;
            Scan();
        }
        catch (Exception ex)
        {
            Reset("扫描暂不可用");
            if (Environment.TickCount64 - lastError < 5000) return;
            lastError = Environment.TickCount64;
            services.Log.Warning(ex, "[SamuraiPvp] 扫描失败，预测已清空。");
        }
    }

    private void Scan()
    {
        candidates.Clear();
        players.Clear();
        var local = services.Objects.LocalPlayer!;
        foreach (var obj in services.Objects.PlayerObjects)
        {
            if (obj is not IPlayerCharacter player || game.Evaluate(player) != Exclusion.None) continue;
            if (!players.TryAdd(player.GameObjectId, player)) continue;
            candidates.Add(new Candidate(player.GameObjectId, player.Position,
                game.InCastRangeAndSight(player) && protector.IsReady(player.GameObjectId)));
        }

        var current = services.Targets.Target?.GameObjectId ?? 0;
        if (current != ownedTarget) ownedTarget = 0;
        var best = TargetPlanner.Choose(candidates, local.Position, current);

        if (configuration.AutoSelect && best is { } chosen)
        {
            var target = players[chosen.TargetId];
            // Do not write every frame when the best target has not changed.
            if (current != chosen.TargetId)
            {
                services.Targets.Target = target;
                ownedTarget = chosen.TargetId;
            }
        }
        else if (best == null || !configuration.AutoSelect)
        {
            ReleaseOwnedTarget();
        }

        // Soft targets can be used by controller input. Report the actual current
        // action target separately from the best hard target, avoiding a false HUD.
        var actual = game.ResolveTarget(Rules.NoTarget);
        var actualCandidate = candidates.FindIndex(c => c.Id == actual?.GameObjectId && c.CanBePrimary);
        var currentKills = actualCandidate >= 0 ? TargetPlanner.Count(candidates, candidates[actualCandidate].Position) : 0;
        var bestName = best is { } plan ? players[plan.TargetId].Name.ToString() : "—";
        Display = new DisplayState(best == null ? "无可用斩杀目标 / 当前无法施放" :
            configuration.AutoSelect ? "自动选人中 · 手动释放 LB" : "仅预测 · 手动选人", currentKills,
            best?.Kills ?? 0, actual?.Name.ToString() ?? "—", bestName, true, candidates.Count);
    }

    public void Reset(string message)
    {
        ReleaseOwnedTarget();
        candidates.Clear();
        players.Clear();
        lastScan = 0;
        Display = new(message);
    }

    private void ReleaseOwnedTarget()
    {
        // Never clear a different player-selected target, and never set camera lock-on.
        if (ownedTarget != 0 && services.Targets.Target?.GameObjectId == ownedTarget)
            services.Targets.Target = null;
        ownedTarget = 0;
    }
}
