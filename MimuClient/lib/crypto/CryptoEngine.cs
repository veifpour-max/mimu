using System.ComponentModel;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace LocalMimu.Models;

public class CryptoEngine : IDisposable
{
    private ECDiffieHellman _ecdh;

    public CryptoEngine()
    {
        _ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.brainpoolP256r1);
    }

    public string GetMyPublicKeyBase64()
    {
        var getPublicKey = _ecdh.PublicKey.ExportSubjectPublicKeyInfo();
        var encodedPublicKey = Convert.ToBase64String(getPublicKey);
        return encodedPublicKey;
    }
    public byte[] ExportMyPrivateKey()
    {
        return _ecdh.ExportECPrivateKey();
    }
    public void LoadMyPrivateKey(byte[] privateKeyBytes)
    {
        _ecdh.ImportECPrivateKey(privateKeyBytes, out _);
    }
    public byte[] GetSharedSecret(string otherPublicKeyBase64)
    {
        var OtherPublicKeyBytes = Convert.FromBase64String(otherPublicKeyBase64);
        using var tempEcdh = ECDiffieHellman.Create();
        tempEcdh.ImportSubjectPublicKeyInfo(OtherPublicKeyBytes, out _);
        var sharedSecret = _ecdh.DeriveKeyMaterial(tempEcdh.PublicKey);
        return HKDF.DeriveKey(HashAlgorithmName.SHA256, sharedSecret, 32, null, null);
    }
    private static (byte[] CipherText, byte[] Tag) EncryptCore(byte[] key, byte[] nonce, byte[] plaintext)
    {
        if (!ChaCha20Poly1305.IsSupported)
        {
            return ManagedChaCha20Poly1305.Encrypt(key, nonce, plaintext);
        }
        byte[] cipherText = new byte[plaintext.Length];
        byte[] tag = new byte[16];
        using var chacha = new ChaCha20Poly1305(key);
        chacha.Encrypt(nonce, plaintext, cipherText, tag);
        return (cipherText, tag);
    }

    private static byte[] DecryptCore(byte[] key, byte[] nonce, byte[] cipherText, byte[] tag)
    {
        if (!ChaCha20Poly1305.IsSupported)
        {
            return ManagedChaCha20Poly1305.Decrypt(key, nonce, cipherText, tag);
        }
        byte[] text = new byte[cipherText.Length];
        using var chacha = new ChaCha20Poly1305(key);
        chacha.Decrypt(nonce, cipherText, tag, text);
        return text;
    }

    public EncryptedPayload Encrypt(string plainText, byte[] sharedSecret)
    {
        byte[] nonce = new byte[12];
        RandomNumberGenerator.Fill(nonce);
        var plainToUTF8 = System.Text.Encoding.UTF8.GetBytes(plainText);
        var (ChiperText, Tag) = EncryptCore(sharedSecret, nonce, plainToUTF8);

        return new EncryptedPayload
        {
            NonceBase64 = Convert.ToBase64String(nonce),
            ChiperTextBase64 = Convert.ToBase64String(ChiperText),
            TagBase64 = Convert.ToBase64String(Tag)
        };
    }
    public string Decrypt(EncryptedPayload e2e, byte[] sharedSecret)
    {
        var decodeNonce = Convert.FromBase64String(e2e.NonceBase64);
        var decodeTag = Convert.FromBase64String(e2e.TagBase64);
        var decodeChiperText = Convert.FromBase64String(e2e.ChiperTextBase64);

        byte[] text = DecryptCore(sharedSecret, decodeNonce, decodeChiperText, decodeTag);

        return System.Text.Encoding.UTF8.GetString(text);
    }
    public EncryptedPayload EncryptBytes(byte[] plainBytes, byte[] sharedSecret)
    {
        byte[] nonce = new byte[12];
        RandomNumberGenerator.Fill(nonce);
        var (ChiperText, Tag) = EncryptCore(sharedSecret, nonce, plainBytes);

        return new EncryptedPayload
        {
            NonceBase64 = Convert.ToBase64String(nonce),
            ChiperTextBase64 = Convert.ToBase64String(ChiperText),
            TagBase64 = Convert.ToBase64String(Tag)
        };
    }
    public string DecryptBytes(EncryptedPayload payload, byte[] sharedSecret)
    {
        var decodeNonce = Convert.FromBase64String(payload.NonceBase64);
        var decodeTag = Convert.FromBase64String(payload.TagBase64);
        var decodeChiperText = Convert.FromBase64String(payload.ChiperTextBase64);

        byte[] text = DecryptCore(sharedSecret, decodeNonce, decodeChiperText, decodeTag);

        return System.Text.Encoding.UTF8.GetString(text);
    }
    public byte[] DecryptBytesToBytes(EncryptedPayload payload, byte[] sharedSecret)
    {
        var decodeNonce = Convert.FromBase64String(payload.NonceBase64);
        var decodeTag = Convert.FromBase64String(payload.TagBase64);
        var decodeChiperText = Convert.FromBase64String(payload.ChiperTextBase64);

        return DecryptCore(sharedSecret, decodeNonce, decodeChiperText, decodeTag);
    }
    public byte[] GenerateSenderKeys()
    {
        byte[] senderArray = new byte[32];
        RandomNumberGenerator.Fill(senderArray);
        return senderArray;
    }

    public void Dispose()
    {
        _ecdh?.Dispose();
    }
}
