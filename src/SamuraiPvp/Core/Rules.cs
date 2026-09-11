using System.Numerics;

namespace SamuraiPvp.Core;

internal static class Rules
{
    public const uint SamuraiJob = 34;
    public const uint Zantetsuken = 29537;
    public const uint Kuzushi = 3202;
    public const uint HallowedGround = 1302;
    public const uint UndeadRedemption = 3039;
    public const uint Covered = 1301;
    public const uint MountedWarmachina = 1420;
    public const ulong NoTarget = 0xE0000000;
    public const float CastRange = 20;
    public const float Radius = 5;

    public static bool IsImmuneStatus(uint id) => id is HallowedGround or UndeadRedemption or Covered;

    // Only integer shield percentages are exposed. Compare rational values without
    // truncating the estimated shield HP, and widen before multiplying to avoid overflow.
    public static bool IsHpExecutable(uint hp, uint maxHp, byte shieldPercent)
        => hp > 0 && maxHp > 0 && (ulong)hp * 100 + (ulong)maxHp * shieldPercent <= (ulong)maxHp * 100;

    public static bool IsOwnKuzushi(uint statusId, uint sourceId, uint localId, float remaining, float minimum)
        => statusId == Kuzushi && localId is not 0 and not (uint)NoTarget && sourceId == localId
            && float.IsFinite(remaining) && remaining > minimum;

    public static bool IsFinite(Vector3 position)
        => float.IsFinite(position.X) && float.IsFinite(position.Y) && float.IsFinite(position.Z);

    public static bool IsInSplash(Vector3 center, Vector3 victim)
        => IsFinite(center) && IsFinite(victim) && Vector3.DistanceSquared(center, victim) <= Radius * Radius;
}

internal enum Exclusion
{
    None,
    InvalidPlayer,
    Friendly,
    NoOwnKuzushi,
    Immune,
    Warmachina,
    ExcessHpAndShield,
}

internal readonly record struct TargetFacts(
    bool ValidPlayer, bool Hostile, bool HasOwnKuzushi, bool Immune, bool Warmachina,
    uint Hp, uint MaxHp, byte ShieldPercent)
{
    public Exclusion Evaluate()
    {
        if (!ValidPlayer || Hp == 0 || MaxHp == 0) return Exclusion.InvalidPlayer;
        if (!Hostile) return Exclusion.Friendly;
        if (Immune) return Exclusion.Immune;
        if (Warmachina) return Exclusion.Warmachina;
        if (!HasOwnKuzushi) return Exclusion.NoOwnKuzushi;
        return Rules.IsHpExecutable(Hp, MaxHp, ShieldPercent) ? Exclusion.None : Exclusion.ExcessHpAndShield;
    }
}
