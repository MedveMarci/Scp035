using System.Collections.Generic;
using System.Linq;
using InventorySystem.Items.Autosync;
using InventorySystem.Items.Scp1509;
using InventorySystem.Items.Usables.Scp1344;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using MEC;
using Mirror;
using PlayerRoles;
using Scp035.ApiFeatures;
using UncomplicatedCustomRoles.API.Enums;
using UncomplicatedCustomRoles.API.Features;
using UncomplicatedCustomRoles.API.Features.Behaviour;
using UncomplicatedCustomRoles.API.Features.CustomModules;
using UncomplicatedCustomRoles.Manager;
using UnityEngine;
using YamlDotNet.Serialization;
using Scp1344Item = LabApi.Features.Wrappers.Scp1344Item;

namespace Scp035.Features;
#nullable enable
public class Scp035Role : EventCustomRole
{
    [YamlIgnore] private SummonedCustomRole? _lastAliveRole;
    [YamlIgnore] public override int Id { get; set; } = 35;

    [YamlIgnore] public override string Name { get; set; } = "<color=#C50000>SCP-035</color>";

    [YamlIgnore] public override bool OverrideRoleName { get; set; } = true;

    [YamlIgnore] public override string? Nickname { get; set; } = "";

    [YamlIgnore] public override string CustomInfo { get; set; } = "";

    [YamlIgnore] public override string BadgeName { get; set; } = "";

    [YamlIgnore] public override string BadgeColor { get; set; } = "";

    [YamlIgnore] public override RoleTypeId Role { get; set; } = RoleTypeId.ClassD;

    [YamlIgnore] public override Team? Team { get; set; } = PlayerRoles.Team.SCPs;

    [YamlIgnore] public override RoleTypeId RoleAppearance { get; set; } = RoleTypeId.ClassD;

    [YamlIgnore] public override List<Team> IsFriendOf { get; set; } = [PlayerRoles.Team.SCPs];

    public override HealthBehaviour Health { get; set; } = new()
    {
        Amount = 500,
        Maximum = 500
    };

    public override AhpBehaviour Ahp { get; set; } = new();

    public override HumeShieldBehaviour HumeShield { get; set; } = new()
    {
        Amount = 250,
        RegenerationDelay = 5f,
        RegenerationAmount = 10f
    };

    public override List<Effect>? Effects { get; set; } = [];

    public override StaminaBehaviour Stamina { get; set; } = new()
    {
        Infinite = false,
        RegenMultiplier = 2f,
        UsageMultiplier = 0.5f
    };

    public override int MaxScp330Candies { get; set; } = 2;

    [YamlIgnore] public override bool CanEscape { get; set; } = false;

    [YamlIgnore]
    public override Dictionary<string, string> RoleAfterEscape { get; set; } = new()
    {
        {
            "default",
            "InternalRole Spectator"
        },
        {
            "cuffed by InternalTeam ChaosInsurgency",
            "InternalRole ClassD"
        }
    };

    public override Vector3 Scale { get; set; } = Vector3.one;
    public override string SpawnBroadcast { get; set; } = "You have spawned as <color=#C50000>SCP-035</color>.";
    public override ushort SpawnBroadcastDuration { get; set; } = 5;
    public override string SpawnHint { get; set; } = "";
    public override float SpawnHintDuration { get; set; } = 0;
    public override Dictionary<ItemCategory, sbyte> CustomInventoryLimits { get; set; } = new();
    public override List<ItemType> Inventory { get; set; } = [];
    public override List<uint> CustomItemsInventory { get; set; } = [];
    public override Dictionary<ItemType, ushort> Ammo { get; set; } = new();
    public override float DamageMultiplier { get; set; } = 1;

    [YamlIgnore]
    public override SpawnBehaviour? SpawnSettings { get; set; } = new()
    {
        CanReplaceRoles = [],
        MaxPlayers = 0,
        MinPlayers = 0,
        Spawn = SpawnType.KeepCurrentPositionSpawn,
        SpawnChance = 0,
        SpawnPoints = [],
        SpawnRoles = [],
        SpawnRooms = [],
        SpawnZones = []
    };

    [YamlIgnore] public override List<object>? CustomFlags { get; set; } = [];

    [YamlIgnore] public override bool IgnoreSpawnSystem { get; set; } = true;
    [YamlIgnore] public Pickup? Pickup { get; set; }

    public override void OnSpawned(SummonedCustomRole role)
    {
        LogManager.Debug("Spawning SCP-035 role for player " + role.Player.Nickname);
        role.AddModule(typeof(SilentAnnouncer));
        role.AddModule(typeof(ColorfulNickname), new Dictionary<string, object> { { "color", "#C50000" } });
        role.AddModule(typeof(ColorfulRaName), new Dictionary<string, object> { { "color", "#C50000" } });
        var player = role.Player;
        player.InfoArea &= ~PlayerInfoArea.UnitName;
        player.InfoArea &= ~PlayerInfoArea.PowerStatus;
        var scp1344 = Pickup != null ? player.AddItem(Pickup) : player.AddItem(ItemType.SCP1344);
        LogManager.Debug(
            $"Pickup: {(Pickup != null ? "Pickup" : "null")}, SCP-1344 Item: {(scp1344 != null ? scp1344.ToString() : "null")}");
        if (scp1344 != null)
        {
            if (scp1344 is Scp1344Item scp1344Item)
                scp1344Item.Status = Scp1344Status.Active;
            if (!EventHandler.Scp035Serials.ContainsKey(scp1344.Serial))
                EventHandler.Scp035Serials.Add(scp1344.Serial, (Scp035.Singleton.Config?.MaxLifetimePerMask ?? 3) - 1);
        }

        var savedItem = ItemType.None;
        if (player.IsInventoryFull)
        {
            savedItem = player.Items.First(saveItem => saveItem.Type != ItemType.SCP1344).Type;
            LogManager.Debug("Removing item " + savedItem + " to give SCP-1509 for spawn effect.");
            player.RemoveItem(savedItem);
        }

        var scp1509Item = player.AddItem(ItemType.SCP1509);

        if (scp1509Item == null)
        {
            if (savedItem != ItemType.None)
                player.AddItem(savedItem);
            LogManager.Error("Failed to give SCP-1509 item to SCP-035 to play the spawn effect.");
            return;
        }

        using (new AutosyncRpc(scp1509Item.Base.ItemId, out var writer))
        {
            writer.WriteByte((byte)Scp1509MessageType.SpawnResurrectParticles);
            writer.WriteVector3(player.Position);
        }

        player.RemoveItem(scp1509Item.Type);

        if (savedItem != ItemType.None)
            player.AddItem(savedItem);

        if (_lastAliveRole != null)
        {
            LogManager.Debug("Removing SCP-035 announcer from last alive SCP-035.");
            _lastAliveRole.RemoveModule<CustomScpAnnouncer>();
            _lastAliveRole.AddModule(typeof(SilentAnnouncer));
            _lastAliveRole = null;
        }

        LogManager.Debug(
            $"Scp1344 Serial: {(scp1344 != null ? scp1344.ToString() : "null")} | EventHandler.Scp035Serials Count: {EventHandler.Scp035Serials.Count} | Lives Left: {(scp1344 != null && EventHandler.Scp035Serials.TryGetValue(scp1344.Serial, out var lives2) ? lives2.ToString() : "N/A")}");
        if (scp1344 != null && EventHandler.Scp035Serials.Count == 1 &&
            EventHandler.Scp035Serials.TryGetValue(scp1344.Serial, out var lives) && lives == 0)
        {
            LogManager.Debug("Adding SCP-035 announcer to the spawned SCP-035.");
            role.AddModule(typeof(CustomScpAnnouncer), new Dictionary<string, object>
            {
                { "name", "SCP-035" }
            });
            role.RemoveModule<SilentAnnouncer>();
            _lastAliveRole = role;
        }
        
        if (Scp035.Singleton.Config != null && Scp035.Singleton.Config.EnablePlayerParticles)
            Particles.ProceduralParticles(player.GameObject, new Color32(255, 0, 0, 255), 0, 0.2f,
                new Vector3(0.5f, 0.5f, 0.5f),
                0.1f, 40);

        base.OnSpawned(role);
    }

    public override void OnSearchPickupRequest(PlayerSearchingPickupEventArgs ev)
    {
        if (ev.Pickup.Type == ItemType.SCP1344)
            ev.IsAllowed = false;

        base.OnSearchPickupRequest(ev);
    }

    public override void OnRemoved(SummonedCustomRole role)
    {
        LogManager.Debug("Removing SCP-035 role for player " + role.Player.Nickname);
        CustomRole.Unregister(role.Role);
        Timing.KillCoroutines(role.Player.GameObject);
        base.OnRemoved(role);
    }
}