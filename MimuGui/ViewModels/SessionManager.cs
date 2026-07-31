using System.Security.Cryptography;
using System;
using System.IO;
using Microsoft.AspNetCore.DataProtection;
using System.Runtime.Intrinsics.Arm;
using Avalonia.Animation.Easings;
using System.Threading.Tasks;
using System.Linq;

namespace LocalMimu.Models;

public class SessionManager
{
    private readonly string _filePath;
    private readonly string _keyPath;

    private CryptoEngine _crypto;

    private readonly IDataProtector _protector;

    public SessionManager()
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
        _filePath = Path.Combine(folderPath, $"session{suffix}.bin");
        _keyPath = Path.Combine(folderPath, $"private_key{suffix}.bin");

    }
    public void SaveSession(string username, string pass, Guid myId, string address)
    {
        var newSession = new SessionModel(username, pass, myId, address);
        var json = Deser.SerJson(newSession);
        var rawBytes = System.Text.Encoding.UTF8.GetBytes(json);

        if (OperatingSystem.IsWindows())
        {
            var encrypted = ProtectedData.Protect(rawBytes, null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(_filePath, encrypted);
        }
        else if (OperatingSystem.IsLinux())
        {
            var safe = EncryptKeyLinux(rawBytes);
            File.WriteAllBytes(_filePath, safe);
        }
    }
    private byte[] GetMachineKeyLinux()
    {
        string? reading = null;
        try
        {
            reading = File.ReadAllText("/etc/machine-id").Trim();  // здесь особенно
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
    public SessionModel? LoadSession()
    {
        if (!File.Exists(_filePath))
        {
            return null;
        }
        else
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    var encrypted = File.ReadAllBytes(_filePath);
                    var decrypted = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
                    string json = System.Text.Encoding.UTF8.GetString(decrypted);
                    return Deser.DeserJson<SessionModel>(json);
                }
                else
                {
                    byte[] key = File.ReadAllBytes(_filePath);
                    var rawBytes = DecryptKeyLinux(key);
                    string bytes2text = System.Text.Encoding.UTF8.GetString(rawBytes);
                    return Deser.DeserJson<SessionModel>(bytes2text);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обработки сессии: {ex.Message}");
                DeleteSession();
                return null;
            }

        }
    }
    public void DeleteSession()
    {
        if (File.Exists(_filePath))
        {
            File.Delete(_filePath);
        }
    }

    public void SavePrivateKey(byte[] key)
    {
        if (OperatingSystem.IsWindows())
        {
            var encryptKey = ProtectedData.Protect(key, null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(_keyPath, encryptKey);
        }
        else
        {
            var safe = EncryptKeyLinux(key);
            File.WriteAllBytes(_keyPath, safe);
        }
    }
    public byte[]? LoadPrivateKey()
    {
        if (!File.Exists(_keyPath))
        {
            return null;
        }
        if (OperatingSystem.IsWindows())
        {
            var encrypted = File.ReadAllBytes(_keyPath);
            var decrypted = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            return decrypted;
        }
        else
        {
            var encrypted = File.ReadAllBytes(_keyPath);
            var rawBytes = DecryptKeyLinux(encrypted);
            return rawBytes;
        }

    }
}