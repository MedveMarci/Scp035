using System.ComponentModel;
using PlayerRoles;
using Scp035.Features;

namespace Scp035;

public class Config
{
    [Description("Whether or not is the plugin is in debug mode?")]
    public bool Debug { get; set; } = false;

    [Description(
        "Whether to disable SCP-035 spawning at the start of the round.\n# Players still can become SCP-035 by going to his chamber (SCP-173) and picking up the SCP-1344.")]
    public bool DisableSpawning { get; set; } = false;

    [Description(
        "Whether to disable SCP-035's Locker spawning at the start of the round.\n# This takes effect only if 'DisableSpawning' is set to true.")]
    public bool DisableLocker { get; set; } = false;

    [Description("Chance (in percentage) of SCP-035 spawning at the start of the round.")]
    public int SpawnChance { get; set; } = 25;

    [Description("Minimum number of players required for SCP-035 to spawn.")]
    public int MinimumPlayers { get; set; } = 10;

    [Description("Whether SCP-035 can be selected from existing SCP players.")]
    public bool SelectFromScps { get; set; } = false;

    [Description("Whether to disable particle effects for SCP-035 Item.")]
    public bool DisableParticles { get; set; } = false;

    [Description("Whether to enable particle effects for SCP-035 Player.")]
    public bool EnablePlayerParticles { get; set; } = false;

    [Description("Maximum lifetime (uses) of SCP-035 per item.\n# Set to 0 for infinite uses.")]
    public int MaxLifetimePerMask { get; set; } = 3;

    [Description("Default role SCP-035 players will turn into.")]
    public RoleTypeId DefaultScp035Role { get; set; } = RoleTypeId.ClassD;

    [Description("Configs for the SCP-035 role players turn into")]
    public Scp035Role Scp035Role { get; set; } = new();
}