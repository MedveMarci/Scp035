using System;
using LabApi.Events.CustomHandlers;
using LabApi.Features;
using LabApi.Loader.Features.Plugins;
using PlayerRoles.RoleAssign;

namespace Scp035;

public class Scp035 : Plugin<Config>
{
    private readonly EventHandler _eventHandler = new();
    public string githubRepo = "MedveMarci/Scp035";
    public override string Name => "Scp035";
    public override string Description => "Adds SCP-035 to the game.";
    public override string Author => "MedveMarci";
    public override Version Version => new(1, 0, 2);
    public override Version RequiredApiVersion => LabApiProperties.CurrentVersion;
    public static Scp035 Singleton { get; private set; }

    public override void Enable()
    {
        Singleton = this;
        RoleAssigner.OnPlayersSpawned += _eventHandler.OnPlayersSpawned;
        CustomHandlersManager.RegisterEventsHandler(_eventHandler);
    }

    public override void Disable()
    {
        Singleton = null;
        RoleAssigner.OnPlayersSpawned -= _eventHandler.OnPlayersSpawned;
        CustomHandlersManager.UnregisterEventsHandler(_eventHandler);
    }
}