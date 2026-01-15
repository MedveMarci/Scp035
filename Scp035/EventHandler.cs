using System.Collections.Generic;
using System.Linq;
using CustomPlayerEffects;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.CustomHandlers;
using LabApi.Features.Extensions;
using LabApi.Features.Wrappers;
using MapGeneration;
using MapGeneration.Distributors;
using MEC;
using Mirror;
using PlayerRoles;
using Scp035.ApiFeatures;
using Scp035.Features;
using UncomplicatedCustomRoles.API.Features;
using UncomplicatedCustomRoles.Extensions;
using UnityEngine;
using Locker = LabApi.Features.Wrappers.Locker;

namespace Scp035;

public class EventHandler : CustomEventsHandler
{
    private static Locker _locker;
    private static Pickup _lockerPickup;
    internal static readonly Dictionary<uint, int> Scp035Serials = [];

    public override void OnServerRoundStarted()
    {
        if (Scp035.Singleton.Config == null)
            return;

        if (Scp035.Singleton.Config.DisableSpawning)
        {
            LogManager.Debug("SCP-035 spawning is disabled, only setting up the locker.");
            if (!Scp035.Singleton.Config.DisableLocker)
                SetupLocker();
            return;
        }

        SetupLocker();
        var chance = Scp035.Singleton.Config.SpawnChance;
        var minPlayers = Scp035.Singleton.Config.MinimumPlayers;
        var players = Player.ReadyList.Where(p => !p.IsSCP && p.IsAlive).ToList();
        LogManager.Debug($"SCP-035 spawn check: {players.Count} eligible players, minimum required: {minPlayers}, chance: {chance}%.");
        if (players.Count >= minPlayers && Random.Range(0, 100) < chance)
        {
            LogManager.Debug("SCP-035 will be spawned this round.");
            Player selectedPlayer;
            if (Scp035.Singleton.Config.SelectFromScps)
            {
                var scpPlayers = Player.ReadyList.Where(p => p.IsSCP && p.IsAlive).ToList();
                selectedPlayer = scpPlayers.Count == 0
                    ? players[Random.Range(0, players.Count)]
                    : scpPlayers[Random.Range(0, scpPlayers.Count)];
                LogManager.Debug($"Selecting SCP-035 from SCP players, found {scpPlayers.Count} SCP players.");
            }
            else
            {
                selectedPlayer = players[Random.Range(0, players.Count)];
                LogManager.Debug("Selecting SCP-035 from non-SCP players.");
            }

            SpawnScp035(selectedPlayer, true, true);
        }

        base.OnServerRoundStarted();
    }

    public override void OnServerRoundRestarted()
    {
        _locker = null;
        _lockerPickup = null;
        Scp035Serials.Clear();
        base.OnServerRoundRestarted();
    }

    public override void OnPlayerCuffing(PlayerCuffingEventArgs ev)
    {
        if (ev.Target.TryGetSummonedInstance(out var summonedCustomRole) &&
            summonedCustomRole.Role.Name.Contains("SCP-035"))
            ev.IsAllowed = false;

        base.OnPlayerCuffing(ev);
    }

    public override void OnPlayerPickingUpItem(PlayerPickingUpItemEventArgs ev)
    {
        if (Scp035Serials.TryGetValue(ev.Pickup.Serial, out var life))
        {
            if (ev.Pickup.Type != ItemType.SCP1344)
            {
                Scp035Serials.Remove(ev.Pickup.Serial);
                return;
            }
            ev.IsAllowed = false;
            Scp035Serials[ev.Pickup.Serial] = --life;
            SpawnScp035(ev.Player, false, false, ev.Pickup);
        }

        base.OnPlayerPickingUpItem(ev);
    }

    public override void OnServerPickupCreated(PickupCreatedEventArgs ev)
    {
        if (Scp035Serials.TryGetValue(ev.Pickup.Serial, out var life))
        {
            if (ev.Pickup.Type != ItemType.SCP1344)
            {
                Scp035Serials.Remove(ev.Pickup.Serial);
                return;
            }

            if (Scp035.Singleton.Config != null && life <= 0)
            {
                ev.Pickup.Destroy();
                Scp035Serials.Remove(ev.Pickup.Serial);
                return;
            }

            if (Scp035.Singleton.Config != null && Scp035.Singleton.Config.DisableParticles)
                return;
            Particles.HighlightObject(ev.Pickup.GameObject, new Color32(255, 0, 0, 255), LightShadows.None, 2f,
                0.5f);
            Particles.ProceduralParticles(ev.Pickup.GameObject, new Color32(255, 0, 0, 255), 0, 0.2f,
                new Vector3(0.5f, 0.5f, 0.5f),
                0.1f, 40);
        }

        base.OnServerPickupCreated(ev);
    }

    internal static void SpawnScp035(Player player, bool isRoundStart, bool spawnPosition, Pickup pickup = null)
    {
        LogManager.Debug($"Spawning SCP-035 for player {player.Nickname} (UserID: {player.UserId}), isRoundStart: {isRoundStart}, spawnPosition: {spawnPosition}, pickup: {(pickup != null ? pickup.Type.ToString() : "null")}.");
        var room = Room.Get(RoomName.Hcz049).First();
        if (spawnPosition)
            player.Position = room.Transform.TransformPoint(new Vector3(33, 96.8f, 11.86f));
        if (isRoundStart)
        {
            player.EnableEffect<Ensnared>();
            player.EnableEffect<HeavyFooted>(255);
            _locker.OpenAllChambers();
            Timing.KillCoroutines($"Scp035-{player.UserId}");
            LogManager.Debug("Starting SCP-035 spawning coroutine.");
            Timing.RunCoroutine(SpawningCoroutine(player, room), $"Scp035-{player.UserId}");
        }
        else
        {
            var role = player.Role;
            if (role.IsScp() || role.IsDead())
                role = Scp035.Singleton.Config?.DefaultScp035Role ?? RoleTypeId.ClassD;
            LogManager.Debug("Directly assigning SCP-035 role to player.");
            var scp035Role = new Scp035Role
            {
                Id = 5000 + (int)player.Role + player.PlayerId + 35,
                Role = role,
                RoleAppearance = player.Role
            };
            if (pickup != null)
            {
                scp035Role.Pickup = pickup;
                pickup.Destroy();
            }
            CustomRole.Register(scp035Role);
            player.SetCustomRole(scp035Role);
        }
    }

    public override void OnPlayerDroppingItem(PlayerDroppingItemEventArgs ev)
    {
        if (Scp035Serials.ContainsKey(ev.Item.Serial))
        {
            if (ev.Item.Type != ItemType.SCP1344)
            {
                Scp035Serials.Remove(ev.Item.Serial);
                return;
            }

            ev.IsAllowed = false;
        }

        base.OnPlayerDroppingItem(ev);
    }

    private static IEnumerator<float> SpawningCoroutine(Player player, Room room)
    {
        var elapsed = 0f;
        LogManager.Debug("Starting SCP-035 spawn effect coroutine for player " + player.Nickname);
        while (elapsed < 3f)
        {
            player.Rotation = room.Rotation;
            yield return Timing.WaitForSeconds(Timing.WaitForOneFrame);
            elapsed += Time.deltaTime;
        }

        _lockerPickup.Destroy();
        _lockerPickup = null;
        var role = player.Role;
        if (role.IsScp() || role.IsDead())
            role = Scp035.Singleton.Config?.DefaultScp035Role ?? RoleTypeId.ClassD;
        LogManager.Debug("Assigning SCP-035 role to player after spawn effect.");
        var scp035Role = new Scp035Role
        {
            Id = 5000 + (int)player.Role + player.PlayerId + 35,
            Role = role,
            RoleAppearance = player.Role
        };
        CustomRole.Register(scp035Role);
        player.SetCustomRole(scp035Role);
    }

    public override void OnPlayerInteractingLocker(PlayerInteractingLockerEventArgs ev)
    {
        var spawning = Scp035.Singleton.Config?.DisableSpawning ?? false;
        if (ev.Locker.GameObject.name == "Scp035Locker" && !spawning)
            ev.IsAllowed = false;

        base.OnPlayerInteractingLocker(ev);
    }

    private static void SetupLocker()
    {
        LogManager.Debug("Setting up SCP-035 locker.");
        var room = Room.Get(RoomName.Hcz049).First();
        var prefab =
            NetworkClient.prefabs.Values.FirstOrDefault(p => p != null && p.name == "Scp1344PedestalStructure Variant");

        if (prefab == null || !prefab.TryGetComponent(out MapGeneration.Distributors.Locker locker))
        {
            LogManager.Error("Failed to find SCP-035 locker prefab.");
            return;
        }

        var instantiate = Object.Instantiate(locker);
        var absolutePosition = room.Transform.TransformPoint(new Vector3(33f, 95.841f, 13.246f));
        var absoluteRotation = room.Transform.rotation * Quaternion.Euler(new Vector3(180f, 0f, 0f));
        instantiate.transform.SetPositionAndRotation(absolutePosition, absoluteRotation);

        if (instantiate.TryGetComponent<StructurePositionSync>(out var component1))
        {
            component1.Network_position = instantiate.transform.position;
            component1.Network_rotationY =
                (sbyte)Mathf.RoundToInt(instantiate.transform.rotation.eulerAngles.y / 5.625f);
        }

        instantiate.gameObject.name = "Scp035Locker";
        NetworkServer.Spawn(instantiate.gameObject);
        var labLocker = Locker.Get(instantiate);
        labLocker.ClearAllChambers();
        labLocker.ClearLockerLoot();
        labLocker.AddLockerLoot(ItemType.SCP1344, 1, 100, 1, 1);
        var lockerChamber = labLocker.Chambers.First();
        lockerChamber.AcceptableItems = [ItemType.SCP1344];
        lockerChamber.Fill();

        var scp1344Pickup = Pickup.Get(lockerChamber.Base.Content.First());
        if (scp1344Pickup is not { Type: ItemType.SCP1344 })
        {
            LogManager.Error("Failed to find SCP-1344 in SCP-035 locker.");
            return;
        }

        scp1344Pickup.IsLocked = true;
        _locker = labLocker;
        _lockerPickup = scp1344Pickup;

        if (Scp035.Singleton.Config == null || Scp035.Singleton.Config.DisableParticles) return;
        Particles.HighlightObject(scp1344Pickup.GameObject, new Color32(255, 0, 0, 255), LightShadows.None, 2f,
            0.5f);
        Particles.ProceduralParticles(scp1344Pickup.GameObject, new Color32(255, 0, 0, 255), 0, 0.2f,
            new Vector3(0.5f, 0.5f, 0.5f),
            0.1f, 40);

    }

    public override void OnServerWaitingForPlayers()
    {
        _ = ApiCommunicator.CheckForUpdatesAsync();
        base.OnServerWaitingForPlayers();
    }
}