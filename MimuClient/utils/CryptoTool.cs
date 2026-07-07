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
}