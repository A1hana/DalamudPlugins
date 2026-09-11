using System.Numerics;

namespace SamuraiPvp.Core;

// A scan keeps every eligible loaded enemy as a possible splash victim. Only
// CanBePrimary is restricted by the player's cast range, LoS, and native readiness.
internal readonly record struct Candidate(ulong Id, Vector3 Position, bool CanBePrimary);
internal readonly record struct Plan(ulong TargetId, int Kills);

internal static class TargetPlanner
{
    public static Plan? Choose(IReadOnlyList<Candidate> candidates, Vector3 local, ulong currentTarget)
    {
        Plan? best = null;
        var bestDistance = float.MaxValue;
        foreach (var center in candidates)
        {
            if (!center.CanBePrimary || !Rules.IsFinite(center.Position)) continue;
            var count = Count(candidates, center.Position);
            var distance = Vector3.DistanceSquared(local, center.Position);
            if (best is { } previous)
            {
                if (count < previous.Kills) continue;
                if (count == previous.Kills)
                {
                    // Never retain a lower-scoring target; keep the current target
                    // only for exact count ties, then nearest, then stable object ID.
                    if (previous.TargetId == currentTarget) continue;
                    if (center.Id != currentTarget && (distance > bestDistance ||
                        (distance == bestDistance && center.Id > previous.TargetId))) continue;
                }
            }
            best = new Plan(center.Id, count);
            bestDistance = distance;
        }
        return best;
    }

    public static int Count(IReadOnlyList<Candidate> candidates, Vector3 center)
    {
        var count = 0;
        foreach (var candidate in candidates)
            if (Rules.IsInSplash(center, candidate.Position)) count++;
        return count;
    }
}
