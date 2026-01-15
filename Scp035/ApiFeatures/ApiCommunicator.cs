using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using LabApi.Features;

namespace Scp035.ApiFeatures;

public static class ApiCommunicator
{
    private const string ApiBase = "http://localhost:5000";

    internal static async Task CheckForUpdatesAsync()
    {
        try
        {
            var currentRemoteVersion = await FetchVersionInfoAsync();
            if (currentRemoteVersion != null)
            {
                if (currentRemoteVersion.Value.TryGetProperty("is_recalled", out var recalledProp) &&
                    recalledProp.ValueKind == JsonValueKind.True)
                {
                    var reason = "No reason provided.";
                    if (currentRemoteVersion.Value.TryGetProperty("recall_reason", out var reasonProp) &&
                        reasonProp.ValueKind == JsonValueKind.String)
                    {
                        reason = reasonProp.GetString();
                    }

                    LogManager.Error(
                        $"The version {Scp035.Singleton.Version} of {Scp035.Singleton.Name} has been recalled by the author. Reason: {reason}");
                    return;
                }
            }
            
            var name = Scp035.Singleton.Name;
            var currentVersion = Scp035.Singleton.Version;

            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd($"{name}/{currentVersion}");

            var latestUrl = $"{ApiBase}/api/v1/plugin/{Uri.EscapeDataString(name)}/latest";
            using var resp = await client.GetAsync(latestUrl).ConfigureAwait(false);
            if (resp.StatusCode == HttpStatusCode.NotFound)
            {
                LogManager.Warn($"No releases found for {name} (no latest version).");
                return;
            }

            if (!resp.IsSuccessStatusCode)
            {
                LogManager.Error($"Version check failed with status {(int)resp.StatusCode} when contacting {latestUrl}.");
                return;
            }

            var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            string latestTag = null;
            if (root.TryGetProperty("version", out var verProp) && verProp.ValueKind == JsonValueKind.String)
                latestTag = verProp.GetString();

            var latestVer = ParseVersion(latestTag);

            var isPrerelease = false;
            if (root.TryGetProperty("is_prerelease", out var preProp) && preProp.ValueKind == JsonValueKind.True)
                isPrerelease = true;

            if (latestVer != null && latestVer.CompareTo(currentVersion) > 0)
            {
                LogManager.Info(
                    $"A new {name} version is available: {latestTag} (current {currentVersion}). Download: {GetDownloadUrl(root)}",
                    ConsoleColor.DarkRed);
            }
            else if (latestVer != null && latestVer.CompareTo(currentVersion) < 0)
            {
                LogManager.Info(
                    $"You are running a newer version ({currentVersion}) than the latest release ({latestTag}) on the plugin server. This is probably a development build.",
                    ConsoleColor.DarkMagenta);
            }
            else
            {
                LogManager.Info(
                    $"Thanks for using {name} v{currentVersion}. To get support and latest news, join to my Discord Server: https://discord.gg/KmpA8cfaSA",
                    ConsoleColor.Blue);
            }

            if (isPrerelease)
            {
                LogManager.Info(
                    "This is a pre-release version. There might be bugs, if you find one, please report it on the plugin server or Discord.",
                    ConsoleColor.DarkYellow);
            }
        }
        catch (Exception e)
        {
            LogManager.Error("Version check failed. This is not critical, you can ignore it.");
            LogManager.Debug($"Version check failed.\n{e}");
        }
    }

    private static async Task<JsonElement?> FetchVersionInfoAsync()
    {
        try
        {
            var pluginName = Scp035.Singleton.Name;
            var version = Scp035.Singleton.Version;
            
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd($"{pluginName}/{version}");

            var url = $"{ApiBase}/api/v1/plugin/{Uri.EscapeDataString(pluginName)}/version/{version}";
            using var resp = await client.GetAsync(url).ConfigureAwait(false);

            if (resp.StatusCode == HttpStatusCode.NotFound)
            {
                LogManager.Warn($"Plugin {pluginName} or version {version} not found on API.");
                return null;
            }

            if (!resp.IsSuccessStatusCode)
            {
                LogManager.Error($"Failed to fetch version info: {(int)resp.StatusCode} for {url}");
                return null;
            }

            var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.Clone();
        }
        catch (Exception e)
        {
            LogManager.Error("Fetching version info failed. This is not critical, you can ignore it.");
            LogManager.Debug($"FetchVersionInfoAsync failed.\n{e}");
            return null;
        }
    }

    private static string GetDownloadUrl(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object) return "(no download url)";
        if (root.TryGetProperty("download_url", out var d) && d.ValueKind == JsonValueKind.String)
            return d.GetString();
        return "(no download url)";
    }

    private static Version ParseVersion(string tag)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(tag)) return null;
            var t = tag.Trim();
            if (t.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                t = t.Substring(1);

            var cut = t.IndexOfAny(['-', '+']);
            if (cut >= 0)
                t = t.Substring(0, cut);

            return Version.TryParse(t, out var v) ? v : null;
        }
        catch
        {
            return null;
        }
    }
    
    internal static async Task<string> SendLogsAsync(string content)
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd($"{Scp035.Singleton.Name}/{Scp035.Singleton.Version}");
    
            var url = $"{ApiBase}/api/v1/plugin/{Uri.EscapeDataString(Scp035.Singleton.Name)}/log";

            LogManager.Info("Sending logs to BearmanAPI...", ConsoleColor.Green);

            var payload = new
            {
                content,
                plugin_version = Scp035.Singleton.Version.ToString(),
                labapi_version = LabApiProperties.CurrentVersion
            };
            var json = JsonSerializer.Serialize(payload);
            var logContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            using var resp = await client.PostAsync(url, logContent).ConfigureAwait(false);
    
            if (!resp.IsSuccessStatusCode)
            {
                LogManager.Error($"Failed to send logs: {(int)resp.StatusCode}");
                return null;
            }
    
            var responseBody = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            using var doc = JsonDocument.Parse(responseBody);
            if (doc.RootElement.TryGetProperty("log_id", out var logIdProp) && logIdProp.ValueKind == JsonValueKind.String)
                return logIdProp.GetString();
            

            LogManager.Warn("Logs sent but no log_id returned.");
            return null;
        }
        catch (Exception e)
        {
            LogManager.Error("Sending logs failed. This is not critical, you can ignore it.");
            LogManager.Debug($"SendLogsAsync failed.\n{e}");
            return null;
        }
    }
}