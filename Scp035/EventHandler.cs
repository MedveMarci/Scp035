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
using Locker = MapGeneration.Distributors.Locker;
using LabApiLocker = LabApi.Features.Wrappers.Locker;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Scp035;

public class EventHandler : CustomEventsHandler
{
    private static LabApiLocker _locker;
    private static Pickup _lockerPickup;
    internal static readonly Dictionary<uint, int> Scp035Serials = [];

    private static Config Cfg => Scp035.Singleton.Config;

    public static void OnPlayersSpawned()
    {
        var cfg = Cfg;
        if (cfg == null || cfg.DisableSpawning)
        {
            LogManager.Debug("SCP-035 spawning is disabled in config, skipping spawn check.");
            return;
        }

        var players = Player.ReadyList.Where(p => !p.IsSCP && p.IsAlive).ToList();
        LogManager.Debug(
            $"SCP-035 spawn check: {players.Count} eligible players, minimum required: {cfg.MinimumPlayers}, chance: {cfg.SpawnChance}%.");

        if (players.Count < cfg.MinimumPlayers || Random.Range(0, 100) >= cfg.SpawnChance)
            return;

        LogManager.Debug("SCP-035 will be spawned this round.");
        Player selectedPlayer;
        if (cfg.SelectFromScps)
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

    public override void OnPlayerCuffing(PlayerCuffingEventArgs ev)
    {
        if (ev.Target.TryGetSummonedInstance(out var summonedCustomRole) &&
            summonedCustomRole.Role.Name.Contains("SCP-035"))
            ev.IsAllowed = false;

        base.OnPlayerCuffing(ev);
    }

    public override void OnPlayerPickingUpItem(PlayerPickingUpItemEventArgs ev)
    {
        if (Cfg.DisableSpawning && !Cfg.DisableLocker &&
            _lockerPickup != null &&
            ev.Pickup.Serial == _lockerPickup.Serial)
        {
            SpawnScp035(ev.Player, false, false, ev.Pickup);
            base.OnPlayerPickingUpItem(ev);
            return;
        }

        if (Scp035Serials.TryGetValue(ev.Pickup.Serial, out var life))
        {
            if (ev.Pickup.Type != ItemType.SCP1344)
            {
                Scp035Serials.Remove(ev.Pickup.Serial);
                base.OnPlayerPickingUpItem(ev);
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
        if (!Scp035Serials.TryGetValue(ev.Pickup.Serial, out var life))
        {
            base.OnServerPickupCreated(ev);
            return;
        }

        if (ev.Pickup.Type != ItemType.SCP1344)
        {
            Scp035Serials.Remove(ev.Pickup.Serial);
            base.OnServerPickupCreated(ev);
            return;
        }

        var cfg = Cfg;
        if (cfg != null && life <= 0)
        {
            ev.Pickup.Destroy();
            Scp035Serials.Remove(ev.Pickup.Serial);
            base.OnServerPickupCreated(ev);
            return;
        }

        if (cfg == null || cfg.DisableParticles)
        {
            base.OnServerPickupCreated(ev);
            return;
        }

        Particles.HighlightObject(ev.Pickup.GameObject, new Color32(255, 0, 0, 255), LightShadows.None, 2f, 0.5f);
        Particles.ProceduralParticles(ev.Pickup.GameObject, new Color32(255, 0, 0, 255), 0, 0.2f,
            new Vector3(0.5f, 0.5f, 0.5f), 0.1f, 40);

        base.OnServerPickupCreated(ev);
    }

    public static void SpawnScp035(Player player, bool isRoundStart, bool spawnPosition, Pickup pickup = null)
    {
        LogManager.Debug(
            $"Spawning SCP-035 for player {player.Nickname} (UserID: {player.UserId}), isRoundStart: {isRoundStart}, spawnPosition: {spawnPosition}, pickup: {(pickup != null ? pickup.Type.ToString() : "null")}.");
        var room = Room.Get(RoomName.Hcz049).First();
        var role = player.Role;
        LogManager.Debug($"The player isSCP: {player.IsSCP}, roleIsScp: {role.IsScp()}, roleIsDead: {role.IsDead()}.");
        if (role.IsScp() || role.IsDead())
            role = Cfg?.DefaultScp035Role ?? RoleTypeId.ClassD;
        player.DropEverything();
        if (role != player.Role)
            player.SetRole(role);
        if (spawnPosition)
            player.Position = room.Transform.TransformPoint(new Vector3(33, 96.8f, 11.86f));
        if (isRoundStart)
        {
            player.EnableEffect<Ensnared>();
            player.EnableEffect<HeavyFooted>(255);
            _locker?.OpenAllChambers();
            Timing.KillCoroutines($"Scp035-{player.UserId}");
            LogManager.Debug("Starting SCP-035 spawning coroutine.");
            Timing.RunCoroutine(SpawningCoroutine(player, room, role), $"Scp035-{player.UserId}");
        }
        else
        {
            LogManager.Debug($"Directly assigning SCP-035 role to player with role: {role}");
            var scp035Role = Cfg?.Scp035Role ?? new Scp035Role();
            scp035Role.Id = 5000 + (int)role + player.PlayerId + 35;
            scp035Role.Role = role;
            scp035Role.RoleAppearance = role;
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

    private static IEnumerator<float> SpawningCoroutine(Player player, Room room, RoleTypeId role)
    {
        var elapsed = 0f;
        LogManager.Debug($"Starting SCP-035 spawn effect coroutine for player {player.Nickname}");
        while (elapsed < 3f)
        {
            player.Rotation = room.Rotation;
            yield return Timing.WaitForOneFrame;
            elapsed += Time.deltaTime;
        }

        LogManager.Debug($"SCP-035 spawn effect finished, elapsed: {elapsed:F2}s");

        if (_lockerPickup != null)
        {
            _lockerPickup.Destroy();
            _lockerPickup = null;
        }

        LogManager.Debug($"Assigning SCP-035 role to player after spawn effect with role: {role}");
        var scp035Role = Cfg?.Scp035Role ?? new Scp035Role();
        scp035Role.Id = 5000 + (int)role + player.PlayerId + 35;
        scp035Role.Role = role;
        scp035Role.RoleAppearance = role;
        CustomRole.Register(scp035Role);
        player.SetCustomRole(scp035Role);
    }

    private static void SetupLocker()
    {
        LogManager.Debug("Setting up SCP-035 locker.");
        if (_locker != null)
        {
            LogManager.Debug("SCP-035 locker already exists, destroying it.");
            _locker.Destroy();
            _locker = null;
        }

        if (_lockerPickup != null)
        {
            LogManager.Debug("SCP-035 locker pickup already exists, destroying it.");
            _lockerPickup.Destroy();
            _lockerPickup = null;
        }

        var room = Room.Get(RoomName.Hcz049).First();
        if (!NetworkClient.prefabs.TryGetValue(1763950070, out var prefab) ||
            !prefab.TryGetComponent(out Locker lockerPrefab))
        {
            LogManager.Error("Failed to find locker prefab in NetworkClient prefabs.");
            return;
        }

        var locker = Object.Instantiate(lockerPrefab);
        var absolutePosition = room.Transform.TransformPoint(new Vector3(33f, 95.841f, 13.246f));
        var absoluteRotation = room.Transform.rotation * Quaternion.Euler(new Vector3(0f, 180f, 0f));
        locker.transform.SetPositionAndRotation(absolutePosition, absoluteRotation);

        if (locker.TryGetComponent(out StructurePositionSync sync))
            sync.Start();

        NetworkServer.Spawn(locker.gameObject);

        Timing.CallDelayed(0.25f, () =>
        {
            var labLocker = LabApiLocker.Get(locker);
            if (labLocker.Chambers.Count == 0)
            {
                LogManager.Error("SCP-035 locker has no chambers.");
                return;
            }

            var lockerChamber = labLocker.Chambers.First();
            var scp1344Pickup = lockerChamber.GetAllItems().FirstOrDefault(item => item.Type == ItemType.SCP1344);
            if (scp1344Pickup == null)
            {
                LogManager.Error("Failed to find SCP-1344 in SCP-035 locker.");
                return;
            }

            _locker = labLocker;
            _lockerPickup = scp1344Pickup;

            var cfg = Cfg;
            if (cfg == null || cfg.DisableParticles) return;
            Particles.HighlightObject(scp1344Pickup.GameObject, new Color32(255, 0, 0, 255), LightShadows.None, 2f,
                0.5f);
            Particles.ProceduralParticles(scp1344Pickup.GameObject, new Color32(255, 0, 0, 255), 0, 0.2f,
                new Vector3(0.5f, 0.5f, 0.5f), 0.1f, 40);
        });
    }

    public override void OnServerWaitingForPlayers()
    {
        ApiManager.CheckForUpdates();
        LogManager.ClearHistory();

        _locker = null;
        _lockerPickup = null;
        Scp035Serials.Clear();

        var cfg = Cfg;
        if (cfg == null || (cfg.DisableSpawning && cfg.DisableLocker))
        {
            base.OnServerWaitingForPlayers();
            return;
        }

        SetupLocker();
        base.OnServerWaitingForPlayers();
    }
}
