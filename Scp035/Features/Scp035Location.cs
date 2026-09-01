using System.Linq;
using LabApi.Features.Wrappers;
using Scp035.ApiFeatures;
using Scp035.Configs;
using UnityEngine;

namespace Scp035.Features;

internal static class Scp035Location
{
    internal static Room Chamber => Room.Get(Scp035.Singleton.Config.Location.Room).FirstOrDefault();

    internal static bool TryGetSpawnPosition(out Vector3 position)
    {
        return TryTransform(Scp035.Singleton.Config.Location.SpawnPosition, out position);
    }

    internal static bool TryGetPedestalPosition(out Vector3 position)
    {
        return TryTransform(Scp035.Singleton.Config.Location.PedestalPosition, out position);
    }

    internal static bool TryGetPedestalPlacement(out Vector3 position, out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;

        Room chamber = Chamber;
        if (chamber == null)
            return false;

        LocationConfig location = Scp035.Singleton.Config.Location;
        position = chamber.Transform.TransformPoint(location.PedestalPosition);
        rotation = chamber.Transform.rotation * Quaternion.Euler(location.PedestalRotation);
        return true;
    }

    internal static void WarnMissingRoom()
    {
        LogManager.Warn($"The room '{Scp035.Singleton.Config.Location.Room}' is missing from this map, so SCP-035 cannot be placed. Change 'location' -> 'room' in the config.");
    }

    private static bool TryTransform(Vector3 localPosition, out Vector3 position)
    {
        Room chamber = Chamber;
        if (chamber == null)
        {
            position = Vector3.zero;
            return false;
        }

        position = chamber.Transform.TransformPoint(localPosition);
        return true;
    }
}