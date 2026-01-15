using System;
using System.Collections.Generic;
using LabApi.Features.Console;
using LabApi.Loader.Features.Yaml;
using NorthwoodLib.Pools;

namespace Scp035.ApiFeatures;

internal static class LogManager
{
    private static bool DebugEnabled => Scp035.Singleton.Config?.Debug ?? false;
    private static readonly List<LogEntry> History = [];

    private class LogEntry(long timestamp, string level, string message)
    {
        public long Timestamp { get; } = timestamp;
        public string Level { get; } = level;
        public string Message { get; } = message;
    }
    
    public static void Debug(string message)
    {
        History.Add(new LogEntry(DateTimeOffset.Now.ToUnixTimeMilliseconds(), "Debug", message));
        if (!DebugEnabled)
            return;

        Logger.Raw($"[DEBUG] [{Scp035.Singleton.Name}] {message}", ConsoleColor.Green);
    }

    public static void Info(string message, ConsoleColor color = ConsoleColor.Cyan)
    {
        History.Add(new LogEntry(DateTimeOffset.Now.ToUnixTimeMilliseconds(), "Info", message));
        Logger.Raw($"[INFO] [{Scp035.Singleton.Name}] {message}", color);
    }

    public static void Warn(string message)
    {
        History.Add(new LogEntry(DateTimeOffset.Now.ToUnixTimeMilliseconds(), "Warn", message));
        Logger.Warn(message);
    }

    public static void Error(string message)
    {
        History.Add(new LogEntry(DateTimeOffset.Now.ToUnixTimeMilliseconds(), "Error", message));
        Logger.Raw($"[ERROR] [{Scp035.Singleton.Name}] {message}", ConsoleColor.Red);
    }
    
    public static (string logResult, bool success) GetLogHistory()
    {
        var stringBuilder = StringBuilderPool.Shared.Rent();
        foreach (var log in History)
                stringBuilder.AppendLine($"[{DateTimeOffset.FromUnixTimeMilliseconds(log.Timestamp):yyyy-MM-dd HH:mm:ss}] [{log.Level}] {log.Message}");

        if (Scp035.Singleton.Config?.Scp035Role != null)
        {
            stringBuilder.AppendLine("\n--- SCP-035 CustomRole ---\n");
            stringBuilder.Append($"{YamlConfigParser.Serializer.Serialize(Scp035.Singleton.Config.Scp035Role)}");
        }
        
        var httpTask = ApiCommunicator.SendLogsAsync(StringBuilderPool.Shared.ToStringReturn(stringBuilder));
    
        string result;
        try
        {
            result = httpTask.GetAwaiter().GetResult();
        }
        catch (Exception e)
        {
            Debug($"GetLogHistory SendLogsAsync failed: {e}");
            return ("Log history request failed to complete.", false);
        }
        return (result != null ? $"Log history sent, received id: {result}" : "Log history request completed without an id.", true);
    }
}