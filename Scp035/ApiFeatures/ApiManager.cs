using System;
using System.Net;
using System.Text.Json;
using LabApi.Features;

namespace Scp035.ApiFeatures;

public static class ApiManager
{
    private const string ApiBase = "https://bearmanapi.hu";

    internal static void CheckForUpdates()
    {
            var name = Scp035.Singleton.Name;
            var currentVersion = Scp035.Singleton.Version;

            var resp = HttpQuery.Get($"{ApiBase}/api/v1/plugin/{Uri.EscapeDataString(name)}/latest");
            var (statusCode, message) = ParseApiResponse(resp);

            if (statusCode != HttpStatusCode.OK)
            {
                LogManager.Error($"Version check failed: {statusCode} - {message}");
                return;
            }

            var root = JsonDocument.Parse(resp).RootElement;

            if (!root.TryGetProperty("version", out var versionProp) || versionProp.ValueKind != JsonValueKind.String)
            {
                LogManager.Error("Version check failed: 'version' field missing or invalid.");
                return;
            }

            var version = versionProp.GetString();

            if (version == null || !Version.TryParse(version, out var latestRemoteVersion))
            {
                LogManager.Error("Version check failed: Invalid version format.");
                return;
            }

            var outdated = latestRemoteVersion > currentVersion;
            var currentIsNewerThanRemote = currentVersion > latestRemoteVersion;

            var currentVersionResp =
                HttpQuery.Get(
                    $"{ApiBase}/api/v1/plugin/{Uri.EscapeDataString(name)}/version/{Uri.EscapeDataString(currentVersion.ToString())}");
            var (currentStatusCode, currentMessage) = ParseApiResponse(currentVersionResp);
            if (currentStatusCode != HttpStatusCode.OK)
                LogManager.Debug($"Recall check failed: {currentStatusCode} - {currentMessage}");
            

            var recallRoot = JsonDocument.Parse(currentVersionResp).RootElement;

            if (recallRoot.TryGetProperty("is_recalled", out var isRecalledProp) &&
                isRecalledProp.ValueKind == JsonValueKind.True)
            {
                var recallReason = recallRoot.TryGetProperty("recall_reason", out var reasonProp) &&
                                   reasonProp.ValueKind == JsonValueKind.String
                    ? reasonProp.GetString()
                    : "No reason provided.";
                LogManager.Error(
                    $"This version of {name} has been recalled.\nPlease update to {latestRemoteVersion} version as soon as possible.\nReason: {recallReason}",
                    ConsoleColor.DarkRed);
                return;
            }

            if (outdated)
                LogManager.Info(
                    $"A new of {name} version is available: {version} (current {currentVersion}). {GetDownloadUrl(root)}",
                    ConsoleColor.DarkRed);
            else
                LogManager.Info(
                    $"Thanks for using {name} v{currentVersion}. To get support and latest news, join to my Discord Server: https://discord.gg/KmpA8cfaSA",
                    ConsoleColor.Blue);


            if (!currentIsNewerThanRemote) return;
            LogManager.Info(
                $"You are running a newer version of {name} ({currentVersion}) than {latestRemoteVersion}. This is a development/pre-release build and it can contain errors or bugs.",
                ConsoleColor.DarkMagenta);
    }

    private static string GetDownloadUrl(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object) return "";
        if (root.TryGetProperty("download_url", out var d) && d.ValueKind == JsonValueKind.String)
            return string.IsNullOrEmpty(d.GetString()) ? "" : $"Download: {d.GetString()}";

        return "";
    }

    internal static string SendLogsAsync(string content)
    {
        try
        {
            var url = $"{ApiBase}/api/v1/plugin/{Uri.EscapeDataString(Scp035.Singleton.Name)}/log";

            LogManager.Info("Sending logs to BearmanAPI...", ConsoleColor.Green);

            var payload = new
            {
                content,
                plugin_version = Scp035.Singleton.Version.ToString(),
                labapi_version = LabApiProperties.CurrentVersion
            };
            var json = JsonSerializer.Serialize(payload);
            var resp = HttpQuery.Post(url, json, "application/json");
            var data = ParseApiResponse(resp);
            if (data.StatusCode != HttpStatusCode.Created)
            {
                LogManager.Error($"Failed to send logs: {data.StatusCode}");
                return null;
            }

            if (JsonDocument.Parse(resp).RootElement.TryGetProperty("log_id", out var logIdProp) &&
                logIdProp.ValueKind == JsonValueKind.String)
                return logIdProp.GetString();

            LogManager.Warn("Logs sent but no log_id returned.");
            return null;
        }
        catch (Exception e)
        {
            LogManager.Error($"Sending logs failed.\n{e}");
            return null;
        }
    }

    private static (HttpStatusCode StatusCode, string Message) ParseApiResponse(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var statusCode = HttpStatusCode.InternalServerError;
            string message = null;

            if (root.TryGetProperty("status", out var statusProp) && statusProp.ValueKind == JsonValueKind.Number)
                statusCode = (HttpStatusCode)statusProp.GetInt32();

            if (root.TryGetProperty("message", out var messageProp) && messageProp.ValueKind == JsonValueKind.String)
                message = messageProp.GetString();

            return (statusCode, message);
        }
        catch (Exception e)
        {
            LogManager.Error("Failed to parse API response.");
            LogManager.Debug($"ParseApiResponse failed.\n{e}");
            return (HttpStatusCode.InternalServerError, null);
        }
    }
}