using System.Security.Cryptography;
using System;
using System.IO;

namespace LocalMimu.Models;

public class SessionManager
{
    private readonly string _filePath;
    private readonly string _keyPath;

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
        else
        {
            File.WriteAllText(_filePath, json);
        }
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
                    string json = File.ReadAllText(_filePath);
                    return Deser.DeserJson<SessionModel>(json);
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
        string path = Path.Combine(_filePath, _keyPath);
        if (OperatingSystem.IsWindows())
        {
            var encryptKey = ProtectedData.Protect(key, null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(path, encryptKey);
        }
        else
        {
            File.WriteAllBytes(path, key);
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
            return encrypted;
        }

    }
}