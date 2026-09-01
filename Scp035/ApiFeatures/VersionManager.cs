using System;
using System.Text.Json;
using Scp035.ApiFeatures.Net;

namespace Scp035.ApiFeatures;

internal static class VersionManager
{
    private const string ApiBase = "https://bearmanapi.hu";
    private const string SupportUrl = "https://discord.gg/KmpA8cfaSA";

    internal static void CheckForUpdates()
    {
        Scp035 plugin = Scp035.Singleton;

        if (plugin is null)
            return;

        string name = plugin.Name;
        Version current = plugin.Version;

        WebQuery.Get($"{ApiBase}/api/v1/plugin/{Uri.EscapeDataString(name)}/latest",
            response => OnLatestReceived(response, name, current));
    }

    private static void OnLatestReceived(HttpResponse response, string name, Version current)
    {
        if (!TryParse(response, "Version check failed", out JsonElement root))
            return;

        if (!root.TryGetProperty("version", out JsonElement versionProperty) ||
            versionProperty.ValueKind != JsonValueKind.String ||
            !Version.TryParse(versionProperty.GetString() ?? string.Empty, out Version latest))
        {
            LogManager.Error("Version check: the response format is invalid.");
            return;
        }

        string downloadUrl = GetDownloadUrl(root);

        WebQuery.Get(
            $"{ApiBase}/api/v1/plugin/{Uri.EscapeDataString(name)}/version/{Uri.EscapeDataString(current.ToString())}",
            recall => OnRecallReceived(recall, name, current, latest, downloadUrl));
    }

    private static void OnRecallReceived(HttpResponse response, string name, Version current, Version latest,
        string downloadUrl)
    {
        if (TryParse(response, "Recall check failed", out JsonElement root) &&
            root.TryGetProperty("is_recalled", out JsonElement recalled) && recalled.ValueKind == JsonValueKind.True)
        {
            string reason = root.TryGetProperty("recall_reason", out JsonElement reasonProperty) &&
                            reasonProperty.ValueKind == JsonValueKind.String
                ? reasonProperty.GetString()
                : "No reason provided.";

            LogManager.Error(
                $"This version of {name} has been recalled, update to {latest} as soon as possible.\nReason: {reason}",
                ConsoleColor.DarkRed);

            return;
        }

        Report(name, current, latest, downloadUrl);
    }

    private static void Report(string name, Version current, Version latest, string downloadUrl)
    {
        if (latest > current)
            LogManager.Info($"A new version of {name} is available: {latest} (you have {current}). {downloadUrl}".TrimEnd(),
                ConsoleColor.DarkRed);
        else if (current > latest)
            LogManager.Info(
                $"You are running a newer version of {name} ({current}) than {latest}. " +
                "This is a development build and it can contain errors or bugs.", ConsoleColor.DarkMagenta);
        else
            LogManager.Info($"Thank you for using {name} v{current}. Support: {SupportUrl}", ConsoleColor.Blue);
    }

    private static bool TryParse(HttpResponse response, string context, out JsonElement root)
    {
        root = default;

        if (!response.IsSuccessful)
        {
            LogManager.Error($"{context}: {response.Error ?? response.Code.ToString()}");
            return false;
        }

        if (string.IsNullOrWhiteSpace(response.Body))
        {
            LogManager.Error($"{context}: the response is empty.");
            return false;
        }

        try
        {
            root = JsonDocument.Parse(response.Body).RootElement;
            return true;
        }
        catch (Exception exception)
        {
            LogManager.Error($"{context}: the response could not be parsed.");
            LogManager.Debug(exception.ToString());

            return false;
        }
    }

    private static string GetDownloadUrl(JsonElement root)
    {
        return root.TryGetProperty("download_url", out JsonElement property) && property.ValueKind == JsonValueKind.String &&
               !string.IsNullOrEmpty(property.GetString())
            ? $"Download: {property.GetString()}"
            : string.Empty;
    }
}