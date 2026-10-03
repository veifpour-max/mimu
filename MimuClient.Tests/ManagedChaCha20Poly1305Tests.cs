using System.Security.Cryptography;
using System.Text;
using LocalMimu.Models;
using Xunit;

namespace MimuClient.Tests;

public class ManagedChaCha20Poly1305Tests
{
    private const string Sunscreen =
        "Ladies and Gentlemen of the class of '99: If I could offer you only one tip for the future, sunscreen would be it.";

    private static byte[] Hex(string hex) =>
        Enumerable.Range(0, hex.Length / 2).Select(i => Convert.ToByte(hex.Substring(i * 2, 2), 16)).ToArray();

    private static string ToHex(byte[] bytes) => Convert.ToHexString(bytes).ToLowerInvariant();

    [Fact]
    public void ChaCha20Encrypt_Rfc8439_2_4_2_Vector()
    {
        var key = Hex("000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f");
        var nonce = Hex("000000000000004a00000000");
        var plaintext = Encoding.UTF8.GetBytes(Sunscreen);

        var cipherText = ManagedChaCha20Poly1305.ChaCha20Encrypt(key, 1, nonce, plaintext);

        Assert.Equal(Sunscreen.Length, cipherText.Length);
        Assert.Equal(
            "6e2e359a2568f98041ba0728dd0d6981e97e7aec1d4360c20a27afccfd9fae0b" +
            "f91b65c5524733ab8f593dabcd62b3571639d624e65152ab8f530c359f0861d8" +
            "07ca0dbf500d6a6156a38e088a22b65e52bc514d16ccf806818ce91ab7793736" +
            "5af90bbf74a35be6b40b8eedf2785e4287" +
            "4d",
            ToHex(cipherText));
    }

    [Fact]
    public void Poly1305_Rfc8439_2_5_2_Vector()
    {
        var key = Hex("85d6be7857556d337f4452fe42d506a80103808afb0db2fd4abff6af4149f51b");
        var message = Encoding.UTF8.GetBytes("Cryptographic Forum Research Group");

        var tag = ManagedChaCha20Poly1305.Poly1305(key, message);

        Assert.Equal("a8061dc1305136c6c22b8baf0c0127a9", ToHex(tag));
    }

    [Fact]
    public void AeadEncrypt_Rfc8439_2_8_2_Vector()
    {
        var key = Hex("808182838485868788898a8b8c8d8e8f909192939495969798999a9b9c9d9e9f");
        var nonce = Hex("070000004041424344454647");
        var aad = Hex("50515253c0c1c2c3c4c5c6c7");
        var plaintext = Encoding.UTF8.GetBytes(Sunscreen);

        var (cipherText, tag) = ManagedChaCha20Poly1305.Encrypt(key, nonce, plaintext, aad);

        Assert.Equal(
            "d31a8d34648e60db7b86afbc53ef7ec2a4aded51296e08fea9e2b5a736ee62d6" +
            "3dbea45e8ca9671282fafb69da92728b1a71de0a9e060b2905d6a5b67ecd3b36" +
            "92ddbd7f2d778b8c9803aee328091b58fab324e4fad675945585808b4831d7bc" +
            "3ff4def08e4b7a9de576d26586cec64b6116",
            ToHex(cipherText));
        Assert.Equal("1ae10b594f09e26a7e902ecbd0600691", ToHex(tag));
    }

    [Fact]
    public void AeadDecrypt_Rfc8439_A_5_Vector()
    {
        var key = Hex("1c9240a5eb55d38af333888604f6b5f0473917c1402b80099dca5cbc207075c0");
        var nonce = Hex("000000000102030405060708");
        var aad = Hex("f33388860000000000004e91");
        var cipherText = Hex(
            "64a0861575861af460f062c79be643bd5e805cfd345cf389f108670ac76c8cb2" +
            "4c6cfc18755d43eea09ee94e382d26b0bdb7b73c321b0100d4f03b7f355894cf" +
            "332f830e710b97ce98c8a84abd0b948114ad176e008d33bd60f982b1ff37c855" +
            "9797a06ef4f0ef61c186324e2b3506383606907b6a7c02b0f9f6157b53c867e4" +
            "b9166c767b804d46a59b5216cde7a4e99040c5a40433225ee282a1b0a06c523e" +
            "af4534d7f83fa1155b0047718cbc546a0d072b04b3564eea1b422273f548271a" +
            "0bb2316053fa76991955ebd63159434ecebb4e466dae5a1073a6727627097a10" +
            "49e617d91d361094fa68f0ff77987130305beaba2eda04df997b714d6c6f2c29" +
            "a6ad5cb4022b02709b");
        var tag = Hex("eead9d67890cbb22392336fea1851f38");

        var plaintext = ManagedChaCha20Poly1305.Decrypt(key, nonce, cipherText, tag, aad);

        Assert.Equal(
            "496e7465726e65742d4472616674732061726520647261667420646f63756d65" +
            "6e74732076616c696420666f722061206d6178696d756d206f6620736978206d" +
            "6f6e74687320616e64206d617920626520757064617465642c207265706c6163" +
            "65642c206f72206f62736f6c65746564206279206f7468657220646f63756d65" +
            "6e747320617420616e792074696d652e20497420697320696e617070726f7072" +
            "6961746520746f2075736520496e7465726e65742d4472616674732061732072" +
            "65666572656e6365206d6174657269616c206f7220746f206369746520746865" +
            "6d206f74686572207468616e206173202fe2809c776f726b20696e2070726f67" +
            "726573732e2fe2809d",
            ToHex(plaintext));
    }

    [Fact]
    public void Decrypt_WrongTag_Throws()
    {
        var key = Hex("808182838485868788898a8b8c8d8e8f909192939495969798999a9b9c9d9e9f");
        var nonce = Hex("070000004041424344454647");
        var plaintext = Encoding.UTF8.GetBytes(Sunscreen);

        var (cipherText, tag) = ManagedChaCha20Poly1305.Encrypt(key, nonce, plaintext);
        tag[0] ^= 0xFF;

        Assert.Throws<CryptographicException>(() =>
            ManagedChaCha20Poly1305.Decrypt(key, nonce, cipherText, tag));
    }

    [Fact]
    public void EncryptDecrypt_RoundTrips_EmptyAndLarge()
    {
        var key = Hex("808182838485868788898a8b8c8d8e8f909192939495969798999a9b9c9d9e9f");
        var nonce = new byte[12];

        var (emptyCt, emptyTag) = ManagedChaCha20Poly1305.Encrypt(key, nonce, Array.Empty<byte>());
        var emptyPt = ManagedChaCha20Poly1305.Decrypt(key, nonce, emptyCt, emptyTag);
        Assert.Empty(emptyPt);

        var big = new byte[100_000];
        new Random(42).NextBytes(big);
        var (bigCt, bigTag) = ManagedChaCha20Poly1305.Encrypt(key, nonce, big);
        Assert.Equal(big, ManagedChaCha20Poly1305.Decrypt(key, nonce, bigCt, bigTag));
    }

    [Fact]
    public void CryptoEngine_RoundTrips_OnPlatformWithoutCngChaCha()
    {
        using var alice = new CryptoEngine();
        using var bob = new CryptoEngine();

        var secret = alice.GetSharedSecret(bob.GetMyPublicKeyBase64());
        const string message = "Проверка fallback на этой платформе";

        var encrypted = alice.Encrypt(message, secret);
        var decrypted = bob.Decrypt(encrypted, secret);

        Assert.Equal(message, decrypted);
    }
}
