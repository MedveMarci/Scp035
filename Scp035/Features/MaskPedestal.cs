using System.Linq;
using LabApi.Features.Wrappers;
using MapGeneration.Distributors;
using MEC;
using Mirror;
using Scp035.ApiFeatures;
using UnityEngine;
using BaseLocker = MapGeneration.Distributors.Locker;
using Locker = LabApi.Features.Wrappers.Locker;
using Object = UnityEngine.Object;

namespace Scp035.Features;

internal static class MaskPedestal
{
    private static Locker _pedestal;

    internal static Pickup Pickup { get; private set; }

    internal static bool IsClaimable { get; set; }

    internal static bool TryGetFocusPoint(out Vector3 point)
    {
        if (Pickup is { IsDestroyed: false })
        {
            point = Pickup.Position;
            return true;
        }

        if (_pedestal is { IsDestroyed: false })
        {
            point = _pedestal.Position;
            return true;
        }

        return Scp035Location.TryGetPedestalPosition(out point);
    }

    internal static bool IsPedestalMask(ushort serial)
    {
        return Pickup is { IsDestroyed: false } && Pickup.Serial == serial;
    }

    internal static void Open()
    {
        _pedestal?.OpenAllChambers();
    }

    internal static void ClearPickup()
    {
        IsClaimable = false;

        if (Pickup is null)
            return;

        Particles.Stop(Pickup.GameObject);

        if (!Pickup.IsDestroyed)
            Pickup.Destroy();

        Pickup = null;
    }

    internal static Pickup Detach()
    {
        IsClaimable = false;

        Pickup mask = Pickup;
        Pickup = null;

        if (mask != null)
            Particles.Stop(mask.GameObject);

        return mask;
    }

    internal static void Reset()
    {
        ClearPickup();

        if (_pedestal is { IsDestroyed: false })
            _pedestal.Destroy();

        _pedestal = null;
    }

    internal static void Setup()
    {
        Reset();

        if (!Scp035Location.TryGetPedestalPlacement(out Vector3 position, out Quaternion rotation))
        {
            Scp035Location.WarnMissingRoom();
            return;
        }

        if (!NetworkClient.prefabs.TryGetValue(1763950070, out GameObject prefab) || !prefab.TryGetComponent(out BaseLocker lockerPrefab))
        {
            LogManager.Error("Failed to find the pedestal prefab, the SCP-035 mask was not spawned.");
            return;
        }

        BaseLocker pedestal = Object.Instantiate(lockerPrefab);
        pedestal.transform.SetPositionAndRotation(position, rotation);

        if (pedestal.TryGetComponent(out StructurePositionSync sync))
            sync.Start();

        NetworkServer.Spawn(pedestal.gameObject);

        Timing.CallDelayed(0.25f, () => Adopt(pedestal));
    }

    private static void Adopt(BaseLocker pedestal)
    {
        if (pedestal == null)
            return;

        Locker wrapper = Locker.Get(pedestal);
        if (wrapper == null || wrapper.Chambers.Count == 0)
        {
            LogManager.Error("The SCP-035 pedestal has no chambers.");
            return;
        }

        Pickup mask = wrapper.Chambers[0].GetAllItems().FirstOrDefault(item => item.Type == ItemType.SCP1344);
        if (mask == null)
        {
            LogManager.Error("Failed to find the SCP-1344 inside the SCP-035 pedestal.");
            return;
        }

        _pedestal = wrapper;
        Pickup = mask;

        if (Scp035.Singleton.Config.EnableParticles)
            Decorate(mask);
    }

    internal static void Decorate(Pickup mask)
    {
        if (mask == null || mask.IsDestroyed)
            return;

        Color32 color = new(255, 0, 0, 255);
        Particles.Highlight(mask.GameObject, color, LightShadows.None, 2f, 0.5f);
        Particles.Field(mask.GameObject, color, new Vector3(0.5f, 0.5f, 0.5f), 0.1f, 40);
    }
}