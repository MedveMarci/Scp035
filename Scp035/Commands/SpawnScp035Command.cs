using System;
using System.Collections.Generic;
using CommandSystem;
using LabApi.Features.Permissions;
using LabApi.Features.Wrappers;
using Scp035.Features;
using Utils;

namespace Scp035.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public sealed class SpawnScp035Command : ICommand, IUsageProvider
{
    public string Command => "spawnscp035";

    public string[] Aliases { get; } = [];

    public string Description => "SCP-035 Spawn Command";

    public string[] Usage { get; } = ["%player%", "TeleportPlayerToSpawn: true/false"];

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.HasPermissions("scp035.spawn"))
        {
            response = "You do not have permission to execute this command!";
            return false;
        }

        if (Scp035.Singleton == null)
        {
            response = "The SCP-035 plugin is not active, check the server console for the reason.";
            return false;
        }

        if (arguments.Count < 1)
        {
            response = "Provide at least one player!\nUsage: %player% [TeleportPlayerToSpawn: true/false]";
            return false;
        }

        List<ReferenceHub> targets = RAUtils.ProcessPlayerIdOrNamesList(arguments, 0, out string[] remaining);
        if (targets == null || targets.Count == 0)
        {
            response = "No player matched the given name or id!";
            return false;
        }

        bool teleport = false;
        if (remaining is { Length: > 0 } && !bool.TryParse(remaining[0], out teleport))
        {
            response = $"'{remaining[0]}' is not a boolean value (true/false)!";
            return false;
        }

        int affected = 0;
        foreach (ReferenceHub hub in targets)
        {
            Player player = Player.Get(hub);
            if (player != null && Scp035Manager.Spawn(player, false, teleport))
                affected++;
        }

        response = affected == 0 ? "None of the given players could become SCP-035." : $"Done! The request affected {affected} player{(affected == 1 ? string.Empty : "s")}.";

        return affected > 0;
    }
}