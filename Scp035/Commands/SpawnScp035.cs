using System;
using CommandSystem;
using LabApi.Features.Permissions;
using LabApi.Features.Wrappers;
using Utils;

namespace Scp035.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class SpawnScp035 : ICommand, IUsageProvider
{
    public string Command => "spawnscp035";

    public string[] Aliases { get; } = ["035"];

    public string Description => "SCP-035 Spawn Command";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.HasPermissions("scp035.spawn"))
        {
            response = "You do not have permission to execute this command!";
            return false;
        }

        if (arguments.Count < 2)
        {
            response =
                $"To execute this command provide at least 2 arguments!\nUsage: {arguments.Array[0]} {this.DisplayCommandUsage()}";
            return false;
        }

        if (!bool.TryParse(arguments.At(1), out var result))
        {
            response = "The second argument must be a boolean value (true/false)!";
            return false;
        }

        var referenceHubList = RAUtils.ProcessPlayerIdOrNamesList(arguments, 0, out _);
        var num = 0;
        foreach (var referenceHub in referenceHubList)
        {
            EventHandler.SpawnScp035(Player.Get(referenceHub), false, result);
            num++;
        }

        response = $"Done! The request affected {num} player{(num == 1 ? "!" : "s!")}";
        return true;
    }

    public string[] Usage { get; } = ["%player%", "TeleportPlayerToSpawn: true/false"];
}