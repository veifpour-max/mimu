using System.Security.Cryptography;
using System.Text;

namespace LocalMimu.Models;
public static class TurnCredentialIssuer
{
    public static TurnCredentials Issue(Guid userId, byte[] sharedSecret, string turnUrl, long? nowUnix = null, long ttlSeconds = 3600)
    {
        long expiresAt = (nowUnix ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds()) + ttlSeconds;
        string username = $"{expiresAt}:{userId}";
        byte[] hash = HMACSHA1.HashData(sharedSecret, Encoding.UTF8.GetBytes(username));
        return new TurnCredentials
        {
            TurnUrl = turnUrl,
            Username = username,
            Password = Convert.ToBase64String(hash),
            TTLSeconds = ttlSeconds
        };
    }
}
