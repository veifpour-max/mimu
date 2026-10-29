using System.Security.Cryptography;

namespace LocalMimu.Models;


public static class ServerPinStore
{
    private static readonly object _lock = new();
    private static Dictionary<string, string>? _pins;

    private static string FilePath()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string dir = Path.Combine(appData, "LocalMimu");
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        return Path.Combine(dir, "server_pins.json");
    }

    public static string? Get(string host, int port)
    {
        Load();
        lock (_lock)
        {
            return _pins!.TryGetValue($"{host}:{port}", out var pin) ? pin : null;
        }
    }

    public static void Set(string host, int port, string pinHex)
    {
        Load();
        _pins![$"{host}:{port}"] = pinHex;
        Save();

    }
    public static void RemoveAll()
    {
        Load();
        _pins!.Clear();
        Save();
    }

    public static string ComputePin(System.Security.Cryptography.X509Certificates.X509Certificate2 cert)
    {
        return Convert.ToHexString(SHA256.HashData(cert.Export(System.Security.Cryptography.X509Certificates.X509ContentType.Cert)));
    }

    private static void Load()
    {
        lock (_lock)
        {
            if (_pins != null) return;
            try
            {
                var json = File.ReadAllText(FilePath());
                _pins = Deser.DeserJson<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
            }
            catch
            {
                _pins = new Dictionary<string, string>();
            }
        }
    }

    private static void Save()
    {
        lock (_lock)
        {
            try
            {
                File.WriteAllText(FilePath(), Deser.SerJson(_pins));
            }
            catch
            {
            }
        }
    }
}
