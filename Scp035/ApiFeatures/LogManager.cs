using System;
using LabApi.Features.Console;

namespace Scp035.ApiFeatures;

internal static class LogManager
{
    private static bool DebugEnabled => Scp035.Singleton?.Config.Debug ?? false;

    private static string PluginName => Scp035.Singleton?.Name ?? "Scp035";

    internal static void Debug(string message)
    {
        if (!DebugEnabled)
            return;

        Logger.Raw($"[DEBUG] [{PluginName}] {message}", ConsoleColor.Green);
    }

    internal static void Info(string message, ConsoleColor color = ConsoleColor.Cyan)
    {
        Logger.Raw($"[INFO] [{PluginName}] {message}", color);
    }

    internal static void Warn(string message)
    {
        Logger.Raw($"[WARN] [{PluginName}] {message}", ConsoleColor.Yellow);
    }

    internal static void Error(string message, ConsoleColor color = ConsoleColor.Red)
    {
        Logger.Raw($"[ERROR] [{PluginName}] {message}", color);
    }
}