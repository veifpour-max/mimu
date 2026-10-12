using System.Security.Cryptography;

namespace LocalMimu.Models;

public class ClientGroupManager
{
    public Guid? Id { get; set; }
    public required string GroupName { get; set; }
    public required Guid OwnerId { get; set; }
    public required List<Guid> _members = new();
    public bool IsGroup { get; set; }

    public bool IsAllowedToChat(Guid myId)
    {
        if (IsGroup || myId == OwnerId)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

}

public class CryptoVaultForKeys
{
    public string _keyPath;
    private readonly object _lock = new();
    private readonly Dictionary<Guid, Dictionary<Guid, byte[]>> _vault = new();

    public CryptoVaultForKeys()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string folderPath = Path.Combine(appData, "LocalMimu");

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
        var suffix = "";
        var args = Environment.GetCommandLineArgs();

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--profile" && i + 1 < args.Length)
            {
                suffix = "_" + args[i + 1];
                break;
            }

        }

        _keyPath = Path.Combine(folderPath, $"group_{suffix}_key.bin");

        Load();
    }

    public void KeyWrite(Guid groupId, Guid senderId, byte[] key)
    {
        lock (_lock)
        {
            if (!_vault.TryGetValue(groupId, out var bySender))
            {
                bySender = new Dictionary<Guid, byte[]>();
                _vault[groupId] = bySender;
            }
            bySender[senderId] = key;
            Save();
        }
    }

    public byte[]? KeyGet(Guid groupId, Guid senderId)
    {
        lock (_lock)
        {
            if (_vault.TryGetValue(groupId, out var bySender) && bySender.TryGetValue(senderId, out var key))
            {
                return key;
            }
            return null;
        }
    }

    private void Load()
    {
        if (!File.Exists(_keyPath))
        {
            return;
        }
        try
        {
            byte[] rawBytes;
            if (OperatingSystem.IsWindows())
            {
                rawBytes = ProtectedData.Unprotect(File.ReadAllBytes(_keyPath), null, DataProtectionScope.CurrentUser);
            }
            else
            {
                rawBytes = DecryptKeyLinux(File.ReadAllBytes(_keyPath));
            }

            string json = System.Text.Encoding.UTF8.GetString(rawBytes);
            var keys = Deser.DeserJson<List<CryptoGroupKey>>(json);

            if (keys == null)
            {
                return;
            }

            foreach (var entry in keys)
            {
                if (!_vault.TryGetValue(entry.GroupId, out var bySender))
                {
                    bySender = new Dictionary<Guid, byte[]>();
                    _vault[entry.GroupId] = bySender;
                }
                bySender[entry.SenderId] = entry.SenderKey;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка загрузки ключей групп: {ex.Message}");
            DeleteVault();
        }
    }

    private void Save()
    {
        var keys = new List<CryptoGroupKey>();
        foreach (var group in _vault)
        {
            foreach (var sender in group.Value)
            {
                keys.Add(new CryptoGroupKey
                {
                    GroupId = group.Key,
                    SenderId = sender.Key,
                    SenderKey = sender.Value
                });
            }
        }

        var rawBytes = System.Text.Encoding.UTF8.GetBytes(Deser.SerJson(keys));

        if (OperatingSystem.IsWindows())
        {
            var encrypted = ProtectedData.Protect(rawBytes, null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(_keyPath, encrypted);
        }
        else
        {
            File.WriteAllBytes(_keyPath, EncryptKeyLinux(rawBytes));
        }
    }

    private void DeleteVault()
    {
        if (File.Exists(_keyPath))
        {
            File.Delete(_keyPath);
        }
    }

    private byte[] GetMachineKeyLinux()
    {
        string? reading = null;
        try
        {
            reading = File.ReadAllText("/etc/machine-id").Trim();
        }
        catch
        {
            reading = Environment.MachineName;
        }
        var exec = reading + Environment.UserName;
        return SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(exec));
    }

    private byte[] EncryptKeyLinux(byte[] rawData)
    {
        var key = GetMachineKeyLinux();
        byte[] nonce = new byte[12];
        RandomNumberGenerator.Fill(nonce);
        byte[] chiperText = new byte[rawData.Length];
        byte[] tag = new byte[16];
        using var chacha = new ChaCha20Poly1305(key);
        chacha.Encrypt(nonce, rawData, chiperText, tag);
        var final = nonce.Concat(chiperText).Concat(tag).ToArray();
        return final;
    }

    private byte[] DecryptKeyLinux(byte[] encryptedData)
    {
        var key = GetMachineKeyLinux();
        var nonce = encryptedData[..12];
        byte[] tag = encryptedData[^16..];
        var chiper = encryptedData[12..^16];
        var plainText = new byte[chiper.Length];
        using var chacha = new ChaCha20Poly1305(key);
        chacha.Decrypt(nonce, chiper, tag, plainText);
        return plainText;
    }
}
