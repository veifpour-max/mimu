using System.Security.Cryptography;
using System;
using System.IO;

namespace LocalMimu.Models;

public class SessionManager
{
    private readonly string _filePath;

    public SessionManager()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string folderPath = Path.Combine(appData, "LocalMimu");

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        _filePath = Path.Combine(folderPath, "session.bin");

    }
    public void SaveSession(string username, string hash, string address)
    {
        var newSession = new SessionModel(username, hash, address);
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
}