using System.Collections.Generic;
using UnityEngine;

namespace Scp035.Features;

internal static class MaskRegistry
{
    private const float ClaimGuardSeconds = 1f;

    private const float DeathDropGraceSeconds = 2f;

    private static readonly Dictionary<ushort, int> Lives = new();
    private static readonly Dictionary<ushort, float> ClaimGuards = new();
    private static readonly Dictionary<ushort, float> DeathDrops = new();

    internal static bool IsMask(ushort serial)
    {
        return Lives.ContainsKey(serial);
    }

    internal static bool TryGetLives(ushort serial, out int lives)
    {
        return Lives.TryGetValue(serial, out lives);
    }

    internal static void SetLives(ushort serial, int lives)
    {
        Lives[serial] = lives;
    }

    internal static void Forget(ushort serial)
    {
        Lives.Remove(serial);
    }

    internal static bool TryBeginClaim(ushort serial)
    {
        float now = Time.time;
        if (ClaimGuards.TryGetValue(serial, out float busyUntil) && now < busyUntil)
            return false;

        ClaimGuards[serial] = now + ClaimGuardSeconds;
        return true;
    }

    internal static void AllowDeathDrop(ushort serial)
    {
        DeathDrops[serial] = Time.time + DeathDropGraceSeconds;
    }

    internal static bool ConsumeDeathDrop(ushort serial)
    {
        if (!DeathDrops.TryGetValue(serial, out float validUntil))
            return false;

        DeathDrops.Remove(serial);
        return Time.time <= validUntil;
    }

    internal static void Clear()
    {
        Lives.Clear();
        ClaimGuards.Clear();
        DeathDrops.Clear();
    }
}