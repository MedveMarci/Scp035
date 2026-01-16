using System;
using CommandSystem;

namespace Scp035.ApiFeatures;

[CommandHandler(typeof(GameConsoleCommandHandler))]
public class BearmanLogs035 : ICommand
{
    public string Command => "bearmanlogs035";

    public string[] Aliases { get; } = ["bmlog035"];

    public string Description => "Sends collected plugin logs to the log server and returns the log id.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        var getLogHistory = LogManager.GetLogHistory();
        response = getLogHistory.logResult;
        return getLogHistory.success;
    }
}