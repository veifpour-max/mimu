using System.Text.Json;

namespace LocalMimu.Models;

public class ServerConfig
{
    public int GlobalPort { get; set; } = 443;
    public string MinioEndpoint { get; set; } = "ip:port";
    public string MinioUser { get; set; } = "your_minio_user";
    public string MinioPass { get; set; } = "your_password";
    public string BucketName { get; set; } = "name_of_your_bucket";
}

public static class ServerConfigLoader
{
    private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "serversettings.json");

    public static ServerConfig Load()
    {
        if (!File.Exists(ConfigPath))
        {
            var defaultConfig = new ServerConfig();
            var json = JsonSerializer.Serialize(defaultConfig, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
            return defaultConfig;
        }

        var text = File.ReadAllText(ConfigPath);
        return JsonSerializer.Deserialize<ServerConfig>(text) ?? new ServerConfig();
    }
}