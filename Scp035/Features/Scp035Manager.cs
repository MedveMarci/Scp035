using System.Collections.Generic;
using System.Linq;
using CustomPlayerEffects;
using LabApi.Features.Extensions;
using LabApi.Features.Wrappers;
using MEC;
using PlayerRoles;
using Scp035.ApiFeatures;
using UncomplicatedCustomRoles.API.Enums;
using UncomplicatedCustomRoles.API.Features;
using UncomplicatedCustomRoles.Extensions;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Scp035.Features;

internal static class Scp035Manager
{
    private const int RoleIdStart = 3500;

    private const float IntroDuration = 3f;

    private static readonly Dictionary<int, Scp035Role> Registered = new();

    internal static IEnumerable<Player> Hosts
    {
        get
        {
            foreach (Player player in Player.ReadyList)
                if (IsScp035(player))
                    yield return player;
        }
    }

    internal static bool IsScp035(Player player)
    {
        return player != null && player.TryGetSummonedInstance(out SummonedCustomRole summoned) && summoned.Role is Scp035Role;
    }

    internal static void OnPlayersSpawned()
    {
        List<Player> humans = [];
        List<Player> scps = [];
        int alive = 0;

        foreach (Player player in Player.ReadyList)
        {
            if (!player.IsAlive)
                continue;

            alive++;

            if (player.IsSCP)
                scps.Add(player);
            else
                humans.Add(player);
        }

        bool conditionsMet = alive >= Scp035.Singleton.Config.MinimumPlayers && Random.Range(0, 100) < Scp035.Singleton.Config.SpawnChance;

        LogManager.Debug($"SCP-035 spawn check: {humans.Count} eligible players, {alive} total alive, minimum required: {Scp035.Singleton.Config.MinimumPlayers}, chance: {Scp035.Singleton.Config.SpawnChance}% -> {conditionsMet}.");

        if (Scp035.Singleton.Config.DisableSpawning)
        {
            if (Scp035.Singleton.Config.DisableLocker)
            {
                LogManager.Debug("SCP-035 spawning and the pedestal are both disabled, nothing to do.");
                return;
            }

            if (conditionsMet)
            {
                MaskPedestal.IsClaimable = true;
                LogManager.Debug("The mask on the pedestal is claimable this round.");
            }
            else
            {
                LogManager.Debug("Pedestal conditions were not met, removing the mask.");
                MaskPedestal.ClearPickup();
            }

            return;
        }

        if (!conditionsMet)
        {
            if (!Scp035.Singleton.Config.DisableLocker && MaskPedestal.Pickup != null)
            {
                MaskPedestal.IsClaimable = true;
                LogManager.Debug("Automatic spawn chance failed, the pedestal mask is claimable as a fallback.");
            }

            return;
        }

        Player chosen = Choose(humans, scps);
        if (chosen == null)
        {
            LogManager.Debug("No eligible player to become SCP-035, skipping the spawn.");
            return;
        }

        Spawn(chosen, true, true);
    }

    private static Player Choose(List<Player> humans, List<Player> scps)
    {
        if (Scp035.Singleton.Config.SelectFromScps && scps is { Count: > 0 })
        {
            LogManager.Debug($"Selecting SCP-035 from the {scps.Count} SCP player(s).");
            return scps[Random.Range(0, scps.Count)];
        }

        if (humans.Count <= 0)
            return null;

        LogManager.Debug("Selecting SCP-035 from the non-SCP players.");
        return humans[Random.Range(0, humans.Count)];
    }

    internal static bool Spawn(Player player, bool playIntro, bool teleport, Pickup sourcePickup = null)
    {
        if (!player.IsReady)
            return false;

        RoleTypeId appearance = player.Role;

        if (appearance.IsScp() || appearance.IsDead())
            appearance = Scp035.Singleton.Config.DefaultScp035Role;

        LogManager.Debug($"Spawning SCP-035 for {player.Nickname} ({player.UserId}); intro: {playIntro}, " + $"teleport: {teleport}, appearance: {appearance}, mask: {(sourcePickup != null ? "pickup" : "new")}.");

        player.DropEverything();

        if (appearance != player.Role)
            player.SetRole(appearance);

        if (teleport)
        {
            if (Scp035Location.TryGetSpawnPosition(out Vector3 spawnPosition))
            {
                player.Position = spawnPosition;
                FacePedestal(player, spawnPosition);
            }
            else
            {
                Scp035Location.WarnMissingRoom();
            }
        }

        if (!playIntro)
        {
            Assign(player, appearance, sourcePickup);
            return true;
        }

        player.EnableEffect<Ensnared>();
        player.EnableEffect<HeavyFooted>(255);
        MaskPedestal.Open();

        Timing.KillCoroutines($"Scp035-{player.UserId}");
        Timing.RunCoroutine(IntroCoroutine(player, appearance), $"Scp035-{player.UserId}");
        return true;
    }

    internal static void Release(Scp035Role role)
    {
        if (role == null)
            return;

        if (Registered.Remove(role.Id))
            CustomRole.Unregister(role.Id);
    }

    internal static void Reset()
    {
        foreach (int id in Registered.Keys.ToArray())
            CustomRole.Unregister(id);

        Registered.Clear();
    }

    private static void FacePedestal(Player player, Vector3 from)
    {
        if (!MaskPedestal.TryGetFocusPoint(out Vector3 target))
        {
            Room chamber = Scp035Location.Chamber;
            if (chamber != null)
                player.Rotation = chamber.Rotation;

            return;
        }

        Vector3 direction = target - from;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            return;

        player.Rotation = Quaternion.LookRotation(direction);
    }

    private static IEnumerator<float> IntroCoroutine(Player player, RoleTypeId appearance)
    {
        float elapsed = 0f;

        while (elapsed < IntroDuration)
        {
            if (!player.IsReady)
                yield break;

            FacePedestal(player, player.Position);

            yield return Timing.WaitForOneFrame;
            elapsed += Time.deltaTime;
        }

        player.DisableEffect<Ensnared>();
        player.DisableEffect<HeavyFooted>();

        MaskPedestal.ClearPickup();

        LogManager.Debug($"Possession animation finished, handing the SCP-035 role to {player.Nickname}.");
        Assign(player, appearance, null);
    }

    private static void Assign(Player player, RoleTypeId appearance, Pickup sourcePickup)
    {
        Scp035Role template = Scp035.Singleton.Config.Scp035Role ?? new Scp035Role();
        Scp035Role instance = template.Clone();
        if (instance == null)
        {
            LogManager.Error("Failed to build the SCP-035 role from the Scp035Plugin.Singleton.Config, the player keeps its current role.");
            return;
        }

        instance.Id = CustomRole.GetFirstFreeId(RoleIdStart);
        instance.Role = appearance;
        instance.RoleAppearance = appearance;
        instance.Pickup = sourcePickup;

        LoadStatusType status = CustomRole.Register(instance);
        if (status != LoadStatusType.Success)
        {
            LogManager.Error($"Failed to register the SCP-035 role for {player.Nickname}: {status}.");
            return;
        }

        Registered[instance.Id] = instance;
        player.SetCustomRole(instance);
    }
}