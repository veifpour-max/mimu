using LocalMimu.Models;
using Xunit;

namespace MimuClient.Tests;

public class CryptoEngineTests
{
    [Fact]
    public void EncryptDecrypt_RoundTrips_PlainText()
    {
        using var alice = new CryptoEngine();
        using var bob = new CryptoEngine();

        var aliceSecret = alice.GetSharedSecret(bob.GetMyPublicKeyBase64());
        var bobSecret = bob.GetSharedSecret(alice.GetMyPublicKeyBase64());

        const string message = "Секретное сообщение которое сервер не должен видеть";

        var encrypted = alice.Encrypt(message, aliceSecret);
        var decrypted = bob.Decrypt(encrypted, bobSecret);

        Assert.Equal(message, decrypted);
        Assert.DoesNotContain("Секретное", encrypted.ChiperTextBase64);
    }

    [Fact]
    public void SharedSecrets_Match_ForBoth()
    {
        using var alice = new CryptoEngine();
        using var bob = new CryptoEngine();

        var aliceSecret = alice.GetSharedSecret(bob.GetMyPublicKeyBase64());
        var bobSecret = bob.GetSharedSecret(alice.GetMyPublicKeyBase64());

        var aliceToBase64 = Convert.ToBase64String(aliceSecret);
        var bobToBase64 = Convert.ToBase64String(bobSecret);

        Assert.Equal(aliceToBase64, bobToBase64);
    }

    [Fact]
    public void EncryptBytes_DecryptBytesToBytes_RoundTrips()
    {
        using var alice = new CryptoEngine();
        using var bob = new CryptoEngine();

        var secret = alice.GetSharedSecret(bob.GetMyPublicKeyBase64());
        var shared = bob.GetSharedSecret(alice.GetMyPublicKeyBase64());

        var plainBytes = new byte[] { 1, 2, 3, 4, 5, 250 };
        var encrypted = alice.EncryptBytes(plainBytes, secret);
        var decrypted = bob.DecryptBytesToBytes(encrypted, shared);

        Assert.Equal(plainBytes, decrypted);
    }

    [Fact]
    public void PrivateKey_ExportImport_RoundTrips()
    {
        using var original = new CryptoEngine();
        var exportedPrivate = original.ExportMyPrivateKey();
        var originalPublic = original.GetMyPublicKeyBase64();

        using var restored = new CryptoEngine();
        restored.LoadMyPrivateKey(exportedPrivate);
        var restoredPublic = restored.GetMyPublicKeyBase64();

        Assert.Equal(originalPublic, restoredPublic);
    }
}