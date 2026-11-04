using System.Text;
using LocalMimu.Models;
using Xunit;

namespace MimuServer.Tests;

public class TurnCredentialIssuerTests
{
    private static readonly byte[] Secret = Encoding.UTF8.GetBytes("test-shared-secret-0123456789abcdef");
    private const long FixedNow = 1_700_000_000;

    private static long ExpectedExpiry => FixedNow + 3600;

    [Fact]
    public void Issue_UsernameFormat_TimestampColonUserId()
    {
        var userId = Guid.NewGuid();
        var creds = TurnCredentialIssuer.Issue(userId, Secret, "turn:x", FixedNow);

        var parts = creds.Username.Split(':');
        Assert.Equal(2, parts.Length);
        Assert.Equal(ExpectedExpiry, long.Parse(parts[0]));
        Assert.Equal(userId.ToString(), parts[1]);
    }

    [Fact]
    public void Issue_TtlSeconds_IsDuration()
    {
        var creds = TurnCredentialIssuer.Issue(Guid.NewGuid(), Secret, "turn:x", FixedNow);
        Assert.Equal(3600, creds.TTLSeconds);
    }

    [Fact]
    public void Issue_Password_IsValidBase64OfHmacSha1()
    {
        var userId = Guid.NewGuid();
        var creds = TurnCredentialIssuer.Issue(userId, Secret, "turn:x", FixedNow);

        var decoded = Convert.FromBase64String(creds.Password);
        Assert.Equal(20, decoded.Length); // 160 бит должно быть

        var expected = System.Security.Cryptography.HMACSHA1.HashData(
            Secret, Encoding.UTF8.GetBytes($"{ExpectedExpiry}:{userId}"));
        Assert.Equal(expected, decoded);
    }

    [Fact]
    public void Issue_IsDeterministic_ForSameInputs()
    {
        var userId = Guid.NewGuid();
        var first = TurnCredentialIssuer.Issue(userId, Secret, "turn:x", FixedNow);
        var second = TurnCredentialIssuer.Issue(userId, Secret, "turn:x", FixedNow);

        Assert.Equal(first.Username, second.Username);
        Assert.Equal(first.Password, second.Password);
    }

    [Fact]
    public void Issue_DifferentUsers_GetDifferentPasswords()
    {
        var first = TurnCredentialIssuer.Issue(Guid.NewGuid(), Secret, "turn:x", FixedNow);
        var second = TurnCredentialIssuer.Issue(Guid.NewGuid(), Secret, "turn:x", FixedNow);

        Assert.NotEqual(first.Password, second.Password);
    }
}
