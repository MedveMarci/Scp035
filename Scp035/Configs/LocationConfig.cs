using System.ComponentModel;
using MapGeneration;
using UnityEngine;

namespace Scp035.Configs;

public sealed class LocationConfig
{
    [Description("Room the SCP-035 pedestal and the possession scene belong to.\n# Every position below is relative to this room, so they follow it wherever the map generator puts it.")]
    public RoomName Room { get; set; } = RoomName.Hcz049;

    [Description("Position SCP-035 is teleported to, relative to the room above.")]
    public Vector3 SpawnPosition { get; set; } = new(33f, 96.8f, 11.86f);

    [Description("Position of the pedestal holding the spare mask, relative to the room above.")]
    public Vector3 PedestalPosition { get; set; } = new(33f, 95.841f, 13.246f);

    [Description("Rotation (in degrees) of the pedestal, relative to the room above.")]
    public Vector3 PedestalRotation { get; set; } = new(0f, 180f, 0f);
}