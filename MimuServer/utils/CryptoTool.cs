using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace LocalMimu.Models;

public static class Crypto
{
    public static string SHA256Encode(string passhash)
    {
        byte[] inByte = System.Text.Encoding.UTF8.GetBytes(passhash);
        var incomingHash = System.Security.Cryptography.SHA256.HashData(inByte);
        var finalHash = Convert.ToHexString(incomingHash);
        return finalHash;
    }
    public static string BcryptEncode(string passhash)
    {
        var hashing = BCrypt.Net.BCrypt.EnhancedHashPassword(passhash);
        return hashing;
    }
    public const int Argon2MemoryKb = 19456;
    public const int Argon2Iterations = 2;
    public const int Argon2Parallelism = 1;

    public static string Argon2Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Argon2Id(password, salt, Argon2MemoryKb, Argon2Iterations, Argon2Parallelism);
        return $"$argon2id$v=19$m={Argon2MemoryKb},t={Argon2Iterations},p={Argon2Parallelism}${B64(salt)}${B64(hash)}";
    }

    public static bool Argon2Verify(string password, string stored)
    {
        var parts = stored.Split('$', StringSplitOptions.RemoveEmptyEntries);
        try
        {
            if (parts.Length != 5 || parts[0] != "argon2id") return false;
            var mtp = parts[2].Split(',');
            var memoryKb = int.Parse(mtp[0].Substring(2));
            var iterations = int.Parse(mtp[1].Substring(2));
            var parallelism = int.Parse(mtp[2].Substring(2));
            var salt = UnB64(parts[3]);
            byte[] expected = UnB64(parts[4]);
            byte[] actual = Argon2Id(password, salt, memoryKb, iterations, parallelism);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public static bool VerifyPassword(string password, string storedHash, string legacySalt)
    {
        if (storedHash.StartsWith("$argon2id$"))
        {
            return Argon2Verify(password, storedHash);
        }
        var expected = SHA256Encode(password + legacySalt);
        return string.Equals(expected, storedHash, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsLegacyHash(string storedHash) => !storedHash.StartsWith("$argon2id$");

    private static byte[] Argon2Id(string password, byte[] salt, int memoryKb, int iterations, int parallelism)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            MemorySize = memoryKb,
            Iterations = iterations,
            DegreeOfParallelism = parallelism
        };
        return argon2.GetBytes(32);
    }
    private static string B64(byte[] data) => Convert.ToBase64String(data).TrimEnd('=');
    private static byte[] UnB64(string s)
    {
        var padded = s.PadRight(s.Length + (4 - s.Length % 4) % 4, '=');
        return Convert.FromBase64String(padded);
    }
}