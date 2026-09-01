using System.ComponentModel;
using PlayerRoles;
using Scp035.Features;
using UnityEngine;

namespace Scp035.Configs;

public sealed class Config
{
    [Description("Whether or not is the plugin is in debug mode?")]
    public bool Debug { get; set; } = false;

    [Description("Whether to disable SCP-035 spawning at the start of the round.\n# Players still can become SCP-035 by going to his chamber (SCP-173) and picking up the SCP-1344.")]
    public bool DisableSpawning { get; set; } = false;

    [Description("Whether to disable SCP-035's Locker spawning at the start of the round.\n# This takes effect only if 'DisableSpawning' is set to true.")]
    public bool DisableLocker { get; set; } = false;

    [Description("Chance (in percentage) of SCP-035 spawning at the start of the round.")]
    public int SpawnChance { get; set; } = 25;

    [Description("Minimum number of players required for SCP-035 to spawn.")]
    public int MinimumPlayers { get; set; } = 10;

    [Description("Whether SCP-035 can only be selected from SCP players.")]
    public bool SelectFromScps { get; set; } = false;

    [Description("Whether to enable particle effects for SCP-035 Item.")]
    public bool EnableParticles { get; set; } = true;

    [Description("Whether to enable particle effects for SCP-035 Player.")]
    public bool EnablePlayerParticles { get; set; } = false;

    [Description("How many particles a single particle effect keeps alive at once.\n# Lower values are cheaper for the server and the clients. Allowed range: 1 - 32.")]
    public int ParticleDensity { get; set; } = 4;

    [Description("Maximum lifetime (uses) of SCP-035 per mask.\n# Minimum is 1; any value below 1 is treated as 1.")]
    public int MaxLifetimePerMask { get; set; } = 3;

    [Description("Amount of health SCP-035 loses over each drain interval.\n# Set to 0 to disable health draining.")]
    public float HealthDrainAmount { get; set; } = 3f;

    [Description("How often (in seconds) SCP-035 loses health.")]
    public float HealthDrainInterval { get; set; } = 1f;

    [Description("Whether to play a CASSIE announcement when SCP-035 dies for good\n# (its mask ran out of lives, or it was the last SCP alive).\n# The announcement itself is played by the 'CustomScpAnnouncer' custom flag of the SCP-035 role,\n# so the announced name can be changed under 'scp035_role' -> 'custom_flags'.")]
    public bool EnableTerminationAnnouncement { get; set; } = true;

    [Description("Default role SCP-035 players will turn into.")]
    public RoleTypeId DefaultScp035Role { get; set; } = RoleTypeId.ClassD;

    [Description("Colour of SCP-035's nickname on the in-game name tag.\n# Only the colours the game accepts are allowed: pink, red, brown, silver, lightgreen, crimson, cyan,\n# aqua, deeppink, tomato, yellow, magenta, bluegreen, orange, lime, green, emerald, carmine, nickel,\n# mint, armygreen, pumpkin, white, black. Leave empty to keep the default colour.")]
    public string NameTagColor { get; set; } = "red";

    [Description("Hex colour of SCP-035's name in the Remote Admin player list. Leave empty to keep the default.")]
    public string RemoteAdminColor { get; set; } = "#C50000";

    [Description("Where SCP-035 and its pedestal are placed.")]
    public LocationConfig Location { get; set; } = new();

    [Description("Settings of the UncomplicatedCustomItems (UCI) integration.")]
    public UciIntegrationConfig UciIntegration { get; set; } = new();

    [Description("Configs for the SCP-035 role players turn into")]
    public Scp035Role Scp035Role { get; set; } = new();

    internal void Validate()
    {
        SpawnChance = Mathf.Clamp(SpawnChance, 0, 100);
        MinimumPlayers = Mathf.Max(MinimumPlayers, 0);
        ParticleDensity = Mathf.Clamp(ParticleDensity, 1, 32);
        MaxLifetimePerMask = Mathf.Max(MaxLifetimePerMask, 1);
        HealthDrainAmount = Mathf.Max(HealthDrainAmount, 0f);

        if (HealthDrainInterval <= 0f)
            HealthDrainInterval = 1f;

        Location ??= new LocationConfig();

        UciIntegration ??= new UciIntegrationConfig();

        if (UciIntegration.BlockedHintDuration <= 0f)
            UciIntegration.BlockedHintDuration = 2f;

        Scp035Role ??= new Scp035Role();
    }
}