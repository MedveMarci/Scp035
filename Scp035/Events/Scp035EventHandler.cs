using LabApi.Events.Arguments.Interfaces;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.CustomHandlers;
using LabApi.Features.Wrappers;
using PlayerRoles;
using Scp035.ApiFeatures;
using Scp035.Configs;
using Scp035.Features;
using Scp035.Integrations;
using UncomplicatedCustomRoles.API.Features;
using UncomplicatedCustomRoles.Extensions;

namespace Scp035.Events;

internal sealed class Scp035EventHandler : CustomEventsHandler
{
    public override void OnServerWaitingForPlayers()
    {
        VersionManager.CheckForUpdates();

        Scp035Manager.Reset();
        MaskRegistry.Clear();
        Particles.StopAll();
        MaskPedestal.Reset();

        Scp035.Singleton.Config.Validate();

        UciIntegration.Initialise();

        if (Scp035.Singleton.Config.DisableSpawning && Scp035.Singleton.Config.DisableLocker)
            return;

        MaskPedestal.Setup();
    }

    public override void OnServerPickupCreated(PickupCreatedEventArgs ev)
    {
        Pickup pickup = ev.Pickup;
        if (pickup == null || !MaskRegistry.TryGetLives(pickup.Serial, out int lives))
            return;

        if (pickup.Type != ItemType.SCP1344)
        {
            MaskRegistry.Forget(pickup.Serial);
            return;
        }

        if (lives <= 0)
        {
            MaskRegistry.Forget(pickup.Serial);
            Particles.Stop(pickup.GameObject);
            pickup.Destroy();
            return;
        }

        if (Scp035.Singleton.Config.EnableParticles)
            MaskPedestal.Decorate(pickup);
    }

    public override void OnPlayerDying(PlayerDyingEventArgs ev)
    {
        Player player = ev.Player;
        if (player == null)
            return;

        Item mask = FindCarriedMask(player);
        if (mask == null)
            return;

        ushort serial = mask.Serial;

        if (IsLastScpAlive(player))
            MaskRegistry.SetLives(serial, 0);

        MaskRegistry.TryGetLives(serial, out int lives);

        MaskRegistry.AllowDeathDrop(serial);

        if (lives > 0)
        {
            LogManager.Debug($"SCP-035 died; mask {serial} was dropped with {lives} host change(s) left.");
            return;
        }

        LogManager.Debug($"SCP-035 died for good (mask {serial}), playing the termination announcement.");
        Scp035Role.AllowTerminationAnnouncement(player);
    }

    public override void OnPlayerDroppingItem(PlayerDroppingItemEventArgs ev)
    {
        Item item = ev.Item;
        if (item == null || !MaskRegistry.IsMask(item.Serial))
            return;

        if (item.Type != ItemType.SCP1344)
        {
            MaskRegistry.Forget(item.Serial);
            return;
        }

        if (!MaskRegistry.ConsumeDeathDrop(item.Serial))
            ev.IsAllowed = false;
    }

    public override void OnPlayerPickingUpItem(PlayerPickingUpItemEventArgs ev)
    {
        if (!ev.IsAllowed || TryDeny(ev.Player, ev.Pickup, ev))
            return;

        Player player = ev.Player;
        Pickup pickup = ev.Pickup;
        if (player == null || pickup == null)
            return;

        ushort serial = pickup.Serial;

        if (MaskPedestal.IsClaimable && MaskPedestal.IsPedestalMask(serial))
        {
            ev.IsAllowed = false;

            if (!MaskRegistry.TryBeginClaim(serial))
                return;

            Scp035Manager.Spawn(player, false, false, MaskPedestal.Detach());
            return;
        }

        if (!MaskRegistry.TryGetLives(serial, out int lives))
            return;

        if (pickup.Type != ItemType.SCP1344)
        {
            MaskRegistry.Forget(serial);
            return;
        }

        ev.IsAllowed = false;

        if (!MaskRegistry.TryBeginClaim(serial))
            return;

        MaskRegistry.SetLives(serial, lives - 1);
        Particles.Stop(pickup.GameObject);
        Scp035Manager.Spawn(player, false, false, pickup);
    }

    public override void OnPlayerSearchingPickup(PlayerSearchingPickupEventArgs ev)
    {
        TryDeny(ev.Player, ev.Pickup, ev);
    }

    public override void OnPlayerSearchingArmor(PlayerSearchingArmorEventArgs ev)
    {
        if (ev.IsAllowed)
            TryDeny(ev.Player, ev.BodyArmorPickup, ev);
    }

    public override void OnPlayerPickingUpArmor(PlayerPickingUpArmorEventArgs ev)
    {
        if (ev.IsAllowed)
            TryDeny(ev.Player, ev.BodyArmorPickup, ev);
    }

    public override void OnPlayerCuffing(PlayerCuffingEventArgs ev)
    {
        if (ev.IsAllowed && Scp035Manager.IsScp035(ev.Target))
            ev.IsAllowed = false;
    }

    private static bool TryDeny(Player player, Pickup pickup, ICancellableEvent ev)
    {
        if (pickup == null || !Scp035Manager.IsScp035(player))
            return false;

        if (pickup.Type == ItemType.SCP1344 || pickup.Category == ItemCategory.SpecialWeapon)
        {
            ev.IsAllowed = false;
            return true;
        }

        if (!UciIntegration.IsPickupBlocked(pickup, out string itemName))
            return false;

        ev.IsAllowed = false;
        SendBlockedHint(player, itemName);
        return true;
    }

    private static void SendBlockedHint(Player player, string itemName)
    {
        UciIntegrationConfig settings = Scp035.Singleton.Config.UciIntegration;
        if (settings == null || string.IsNullOrWhiteSpace(settings.BlockedHint))
            return;

        string text = settings.BlockedHint.Replace("{item}", string.IsNullOrWhiteSpace(itemName) ? "this item" : itemName);

        player.SendHint(text, settings.BlockedHintDuration);
    }

    private static Item FindCarriedMask(Player player)
    {
        foreach (Item item in player.Items)
            if (item.Type == ItemType.SCP1344 && MaskRegistry.IsMask(item.Serial))
                return item;

        return null;
    }

    private static bool IsLastScpAlive(Player dying)
    {
        foreach (Player player in Player.ReadyList)
        {
            if (player == dying || !player.IsAlive)
                continue;

            if (player.IsSCP)
                return false;

            if (player.TryGetSummonedInstance(out SummonedCustomRole role) && role.Role.Team == Team.SCPs)
                return false;
        }

        return true;
    }
}