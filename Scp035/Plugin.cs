using System;
using System.Linq;
using LabApi.Events.CustomHandlers;
using LabApi.Features;
using LabApi.Loader;
using LabApi.Loader.Features.Plugins;
using PlayerRoles.RoleAssign;
using Scp035.ApiFeatures;

namespace Scp035;

public class Scp035 : Plugin<Config>
{
    private readonly EventHandler _eventHandler = new();
    public override string Name => "Scp035";
    public override string Description => "Adds SCP-035 to the game.";
    public override string Author => "MedveMarci";
    public override Version Version => new(1, 1, 1);
    public override Version RequiredApiVersion => LabApiProperties.CurrentVersion;
    public static Scp035 Singleton { get; private set; }

    public override void Enable()
    {
        Singleton = this;

        var ucrPlugin = PluginLoader.EnabledPlugins.FirstOrDefault(plugin => plugin.Name.Contains("UncomplicatedCustomRoles"));
        if (ucrPlugin == null)
        {
            LogManager.Error("UncomplicatedCustomRoles plugin is required for Scp035 to work! Disabling plugin...");
            Disable();
            return;
        }

        if (ucrPlugin.Version < new Version(9, 5, 1))
        {
            LogManager.Error(
                "UncomplicatedCustomRoles version 9.5.1 or higher is required for Scp035 to work! Disabling plugin...");
            Disable();
            return;
        }

        RoleAssigner.OnPlayersSpawned += EventHandler.OnPlayersSpawned;
        CustomHandlersManager.RegisterEventsHandler(_eventHandler);
    }

    public override void Disable()
    {
        Singleton = null;
        RoleAssigner.OnPlayersSpawned -= EventHandler.OnPlayersSpawned;
        CustomHandlersManager.UnregisterEventsHandler(_eventHandler);
    }
}