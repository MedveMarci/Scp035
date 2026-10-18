using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace Scp035.ApiFeatures;

internal static class VersionManager
{
    internal static void CheckForUpdates()
    {
        string name = Scp035.Singleton.Name;
        string current = Scp035.Singleton.Version.ToString();

        Task.Run(async () =>
        {
            try
            {
                string url = $"https://bearmanapi.hu/api/v1/plugin/{Uri.EscapeDataString(name)}/check-update" + $"?current={Uri.EscapeDataString(current)}";
                Task<string> request = HttpQuery.GetAsync(url);
                if (await Task.WhenAny(request, Task.Delay(TimeSpan.FromSeconds(8))) != request)
                {
                    LogManager.Error("Version check timed out.");
                    return;
                }

                using JsonDocument doc = JsonDocument.Parse(await request);
                if (!doc.RootElement.TryGetProperty("log", out JsonElement log) || !log.TryGetProperty("message", out JsonElement message) || message.ValueKind != JsonValueKind.String)
                {
                    LogManager.Error("Version check failed: invalid response.");
                    return;
                }

                ConsoleColor color = log.TryGetProperty("color", out JsonElement c) && Enum.TryParse(c.GetString(), out ConsoleColor parsed) ? parsed : ConsoleColor.White;

                if (log.TryGetProperty("level", out JsonElement level) && level.GetString() == "error")
                    LogManager.Error(message.GetString(), color);
                else
                    LogManager.Info(message.GetString(), color);
            }
            catch (Exception ex)
            {
                LogManager.Error($"Version check exception:\n{ex}");
            }
        });
    }
}