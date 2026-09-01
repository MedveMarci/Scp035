using System;
using LabApi.Events.CustomHandlers;
using LabApi.Features;
using LabApi.Loader;
using LabApi.Loader.Features.Plugins;
using PlayerRoles.RoleAssign;
using Scp035.ApiFeatures;
using Scp035.Configs;
using Scp035.Events;
using Scp035.Features;
using Scp035.Integrations;

namespace Scp035;

public sealed class Scp035 : Plugin<Config>
{
    private static readonly Version MinimumUcrVersion = new(9, 6, 0);
    private readonly Scp035EventHandler _eventHandler = new();
    private bool _hooked;

    public static Scp035 Singleton { get; private set; }

    public override string Name => "Scp035";

    public override string Description => "Adds SCP-035, the Possessive Mask, to the game.";

    public override string Author => "MedveMarci";

    public override Version Version => new(2, 0, 0);

    public override Version RequiredApiVersion => LabApiProperties.CurrentVersion;

    public override void Enable()
    {
        Singleton = this;

        if (!TryGetUcrVersion(out Version ucrVersion))
        {
            LogManager.Error("You need to install UncomplicatedCustomRoles for Scp035 to work!");
            Singleton = null;
            return;
        }

        if (ucrVersion < MinimumUcrVersion)
        {
            LogManager.Error($"UncomplicatedCustomRoles {MinimumUcrVersion} or newer is required by Scp035 " + $"(found {ucrVersion}); the plugin stays inactive.");
            Singleton = null;
            return;
        }

        Config.Validate();
        UciIntegration.Initialise();

        RoleAssigner.OnPlayersSpawned += Scp035Manager.OnPlayersSpawned;
        CustomHandlersManager.RegisterEventsHandler(_eventHandler);
        _hooked = true;
    }

    public override void Disable()
    {
        if (_hooked)
        {
            RoleAssigner.OnPlayersSpawned -= Scp035Manager.OnPlayersSpawned;
            CustomHandlersManager.UnregisterEventsHandler(_eventHandler);
            _hooked = false;
        }

        Scp035Manager.Reset();
        MaskRegistry.Clear();
        Particles.StopAll();
        MaskPedestal.Reset();

        Singleton = null;
    }

    private static bool TryGetUcrVersion(out Version version)
    {
        foreach (Plugin plugin in PluginLoader.Plugins.Keys)
        {
            if (!plugin.Name.Equals("UncomplicatedCustomRoles", StringComparison.OrdinalIgnoreCase))
                continue;

            version = plugin.Version;
            return true;
        }

        version = null;
        return false;
    }
}