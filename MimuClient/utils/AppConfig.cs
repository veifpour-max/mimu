using System.Text.Json;

namespace LocalMimu.Models;
public class AppConfig
{
    public string ServerIp {get; set;} = "146.158.101.114";
    public int ServerPort {get; set;} = 8000;

}
public static class ConfigLoader
{
    private static readonly string ConfigPath = "appsettings.json";

    public static AppConfig Load()
    {
        if (!File.Exists(ConfigPath))
        {
            var defaultConfig = new AppConfig();
            Save(defaultConfig);
            return defaultConfig;
        }

        var text = File.ReadAllText(ConfigPath);
        return JsonSerializer.Deserialize<AppConfig>(text) ?? new AppConfig();
    }

    public static void Save(AppConfig config)
    {
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigPath, json);
    }
}