using System;
using System.Reflection;
using HarmonyLib;
using LabApi.Features.Wrappers;
using Scp035.ApiFeatures;
using Scp035.Features;

namespace Scp035.Integrations;

internal static class CedModIntegration
{
    private static bool _patched;

    internal static void Initialise()
    {
        if (_patched)
            return;

        Type autoban = FindAutobanType();
        if (autoban == null)
        {
            LogManager.Debug("CedMod was not found, the integration stays inactive.");
            return;
        }

        try
        {
            _patched = ApplyPatch(autoban);
        }
        catch (Exception e)
        {
            LogManager.Warn($"Could not patch CedMod, kills involving SCP-035 may still count as teamkills.\n{e.Message}");
        }
    }

    internal static void Disable()
    {
        if (!_patched)
            return;

        try
        {
            RemovePatch();
        }
        catch (Exception e)
        {
            LogManager.Debug($"Could not unpatch CedMod: {e.Message}");
        }

        _patched = false;
    }

    private static Type FindAutobanType()
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type type;
            try
            {
                type = assembly.GetType("CedMod.FriendlyFireAutoban", false);
            }
            catch
            {
                type = null;
            }

            if (type != null)
                return type;
        }

        return null;
    }
    
    private static Harmony _harmony;

    private static bool ApplyPatch(Type autoban)
    {
        MethodInfo isTeamKill = AccessTools.Method(autoban, "IsTeamKill");
        if (isTeamKill == null)
        {
            LogManager.Warn("CedMod was found, but its FriendlyFireAutoban.IsTeamKill method was not, kills involving SCP-035 may still count as teamkills.");
            return false;
        }

        _harmony = new Harmony("medvemarci.scp035.cedmod");
        _harmony.Patch(isTeamKill, new HarmonyMethod(typeof(CedModIntegration), nameof(IsTeamKillPrefix)));

        LogManager.Debug("CedMod has been detected, kills involving SCP-035 are no longer counted as teamkills.");
        return true;
    }

    private static void RemovePatch()
    {
        _harmony?.UnpatchAll("medvemarci.scp035.cedmod");
        _harmony = null;
    }

    private static bool IsTeamKillPrefix(Player player, Player attacker, ref bool __result)
    {
        if (!Scp035Manager.IsScp035(player) && !Scp035Manager.IsScp035(attacker))
            return true;

        __result = false;
        return false;
    }
}
