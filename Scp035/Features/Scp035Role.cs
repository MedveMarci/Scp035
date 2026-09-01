using System;
using System.Collections.Generic;
using System.Linq;
using CustomPlayerEffects;
using InventorySystem.Items.Autosync;
using InventorySystem.Items.Scp1509;
using InventorySystem.Items.Usables.Scp1344;
using LabApi.Features.Wrappers;
using LabApi.Loader.Features.Yaml;
using MEC;
using Mirror;
using PlayerRoles;
using Scp035.ApiFeatures;
using UncomplicatedCustomRoles.API.Enums;
using UncomplicatedCustomRoles.API.Features;
using UncomplicatedCustomRoles.API.Features.Behaviour;
using UncomplicatedCustomRoles.API.Features.CustomModules;
using UncomplicatedCustomRoles.Extensions;
using UncomplicatedCustomRoles.Manager;
using UnityEngine;
using YamlDotNet.Serialization;
using Scp1344Item = LabApi.Features.Wrappers.Scp1344Item;

namespace Scp035.Features;
#nullable enable

public class Scp035Role : EventCustomRole
{
    private CoroutineHandle _healthDrain;

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
        Maximum = 250,
        RegenerationDelay = 5f,
        RegenerationAmount = 10f,
        RegenerationSpeed = 1f
    };

    public override List<Effect>? Effects { get; set; } = [];

    public override StaminaBehaviour Stamina { get; set; } = new()
    {
        Infinite = false,
        RegenMultiplier = 2f,
        UsageMultiplier = 1f
    };

    public override int MaxScp330Candies { get; set; } = 2;

    [YamlIgnore] public override bool CanEscape { get; set; } = false;

    [YamlIgnore]
    public override Dictionary<string, string> RoleAfterEscape { get; set; } = new()
    {
        { "default", "InternalRole Spectator" },
        { "cuffed by InternalTeam ChaosInsurgency", "InternalRole ClassD" }
    };

    public override Vector3 Scale { get; set; } = Vector3.one;

    public override string SpawnBroadcast { get; set; } = "You have spawned as <color=#C50000>SCP-035</color>.";

    public override ushort SpawnBroadcastDuration { get; set; } = 5;

    public override string SpawnHint { get; set; } = "";

    public override float SpawnHintDuration { get; set; } = 0;

    public override Dictionary<ItemCategory, sbyte> CustomInventoryLimits { get; set; } = new()
    {
        { ItemCategory.SCPItem, 4 }
    };

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

    public override List<object>? CustomFlags { get; set; } =
    [
        new Dictionary<object, object>
        {
            {
                "CustomScpAnnouncer", new Dictionary<object, object>
                {
                    { "name", "SCP-035" }
                }
            }
        }
    ];

    [YamlIgnore] public override bool IgnoreSpawnSystem { get; set; } = true;

    [YamlIgnore] public Pickup? Pickup { get; set; }

    public Scp035Role? Clone()
    {
        try
        {
            string yaml = YamlConfigParser.Serializer.Serialize(this);
            return YamlConfigParser.Deserializer.Deserialize<Scp035Role>(yaml);
        }
        catch (Exception exception)
        {
            LogManager.Error($"Failed to clone the SCP-035 role.\n{exception}");
            return null;
        }
    }

    public override void OnSpawned(SummonedCustomRole role)
    {
        Player? player = role.Player;
        if (player == null)
        {
            base.OnSpawned(role);
            return;
        }

        LogManager.Debug($"Applying the SCP-035 role to {player.Nickname}.");

        player.DisableEffect<SpawnProtected>();

        ApplyModules(role);
        GiveMask(player);
        PlayPossessionEffect(player);

        if (Scp035.Singleton.Config.EnablePlayerParticles)
            Particles.Field(player.GameObject, new Color32(255, 0, 0, 255), new Vector3(0.5f, 0.5f, 0.5f), 0.1f, 30);

        StartHealthDrain(role);

        base.OnSpawned(role);
    }

    public override void OnRemoved(SummonedCustomRole role)
    {
        LogManager.Debug($"Removing the SCP-035 role from {role.Player?.Nickname ?? "an unknown player"}.");

        Timing.KillCoroutines(_healthDrain);

        if (role.Player != null)
            Particles.Stop(role.Player.GameObject);

        Scp035Manager.Release(this);

        base.OnRemoved(role);
    }

    internal static void AllowTerminationAnnouncement(Player player)
    {
        if (!Scp035.Singleton.Config.EnableTerminationAnnouncement)
            return;

        if (!player.TryGetSummonedInstance(out SummonedCustomRole role))
            return;

        if (!role.HasModule<CustomScpAnnouncer>())
        {
            LogManager.Warn("The SCP-035 role has no 'CustomScpAnnouncer' custom flag, so no termination announcement can be played. Add it back to 'custom_flags' or turn 'enable_termination_announcement' off.");
            return;
        }

        role.RemoveModules<SilentAnnouncer>();
    }

    private static void ApplyModules(SummonedCustomRole role)
    {
        role.AddModule(typeof(SilentAnnouncer));

        Dictionary<string, object> infoTagArguments = new()
        {
            { "show_unitname", false },
            { "show_powerstatus", false }
        };

        string nameTagColor = Scp035.Singleton.Config.NameTagColor;
        if (!string.IsNullOrWhiteSpace(nameTagColor))
            infoTagArguments["nickname_color"] = nameTagColor!;

        role.AddModule(typeof(InfoTag), infoTagArguments);

        string remoteAdminColor = Scp035.Singleton.Config.RemoteAdminColor;
        if (!string.IsNullOrWhiteSpace(remoteAdminColor))
            role.AddModule(typeof(ColorfulRaName), new Dictionary<string, object> { { "color", remoteAdminColor! } });
    }

    private void GiveMask(Player player)
    {
        Pickup? source = Pickup;
        Pickup = null;

        Item? mask = null;

        if (source is { IsDestroyed: false })
        {
            mask = player.AddItem(source);

            source.Destroy();
        }

        mask ??= player.AddItem(ItemType.SCP1344);

        if (mask == null)
        {
            LogManager.Error($"Failed to give the SCP-1344 to {player.Nickname}.");
            return;
        }

        if (mask is Scp1344Item scp1344)
            scp1344.Status = Scp1344Status.Active;

        if (!MaskRegistry.IsMask(mask.Serial))
            MaskRegistry.SetLives(mask.Serial, Math.Max(1, Scp035.Singleton.Config.MaxLifetimePerMask) - 1);

        LogManager.Debug($"{player.Nickname} now carries mask {mask.Serial} with " + $"{(MaskRegistry.TryGetLives(mask.Serial, out int lives) ? lives.ToString() : "0")} host change(s) left.");
    }

    private static void PlayPossessionEffect(Player player)
    {
        ItemType displaced = ItemType.None;

        if (player.IsInventoryFull)
        {
            Item? spare = player.Items.FirstOrDefault(item => item.Type != ItemType.SCP1344);
            if (spare == null)
            {
                LogManager.Debug("The inventory is full of masks, skipping the possession effect.");
                return;
            }

            displaced = spare.Type;
            player.RemoveItem(spare);
        }

        try
        {
            Item? scp1509 = player.AddItem(ItemType.SCP1509);
            if (scp1509 == null)
            {
                LogManager.Error("Failed to borrow an SCP-1509 to play the SCP-035 possession effect.");
                return;
            }

            using (new AutosyncRpc(scp1509.Base.ItemId, out NetworkWriter writer))
            {
                writer.WriteByte((byte)Scp1509MessageType.SpawnResurrectParticles);
                writer.WriteVector3(player.Position);
            }

            player.RemoveItem(scp1509);
        }
        catch (Exception exception)
        {
            LogManager.Error($"Failed to play the SCP-035 possession effect.\n{exception}");
        }
        finally
        {
            if (displaced != ItemType.None)
                player.AddItem(displaced);
        }
    }

    private void StartHealthDrain(SummonedCustomRole role)
    {
        if (Scp035.Singleton.Config.HealthDrainAmount <= 0f || Scp035.Singleton.Config.HealthDrainInterval <= 0f)
            return;

        Timing.KillCoroutines(_healthDrain);
        _healthDrain = Timing.RunCoroutine(HealthDrain(role, Scp035.Singleton.Config.HealthDrainAmount, Scp035.Singleton.Config.HealthDrainInterval));
    }

    private static IEnumerator<float> HealthDrain(SummonedCustomRole role, float amount, float interval)
    {
        Player player = role.Player;
        int roleId = role.Role.Id;

        while (true)
        {
            yield return Timing.WaitForSeconds(interval);

            if (!player.IsAlive || !player.TryGetSummonedInstance(out SummonedCustomRole current) || current.Role.Id != roleId)
                yield break;

            player.Health -= amount;

            if (player.Health > 0f)
                continue;

            player.Kill("SCP-035 mask corruption");
            yield break;
        }
    }
}