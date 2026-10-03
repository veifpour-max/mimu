using System.Numerics;
using System.Security.Cryptography;

namespace LocalMimu.Models;

// ПРЕДУПРЕЖДЕНИЕ ДЛЯ РАЗРАБОТЧИКОВ

// Это - кросс-платформленная реализация ChaCha20Poly1305 была написана из-за проблемы что сам модуль в C#(ChaCha20Poly1305) поддерживается с билда 20134 - Windows 11.
// Написана она с использованием официальной инструкции rfc8439
// написан ИИ но есть и мои изменения(на такое мне не хватает сил и уровня) - не мной, перепроверен и работоспособен.
public static class ManagedChaCha20Poly1305
{
    public const int KeySize = 32;   // 256 бит
    public const int NonceSize = 12; // 96 бит
    public const int TagSize = 16;   // 128 бит

    public static (byte[] CipherText, byte[] Tag) Encrypt(byte[] key, byte[] nonce, byte[] plaintext, byte[]? aad = null)
    {
        ValidateKeyNonce(key, nonce);
        byte[] cipherText = new byte[plaintext.Length];
        ChaCha20Xor(key, 1, nonce, plaintext, cipherText);
        byte[] tag = Poly1305Tag(key, nonce, aad, cipherText);
        return (cipherText, tag);
    }

    public static byte[] Decrypt(byte[] key, byte[] nonce, byte[] cipherText, byte[] tag, byte[]? aad = null)
    {
        ValidateKeyNonce(key, nonce);
        if (tag == null || tag.Length != TagSize)
        {
            throw new CryptographicException("Некорректный размер тега Poly1305");
        }
        byte[] expectedTag = Poly1305Tag(key, nonce, aad, cipherText);
        if (!CryptographicOperations.FixedTimeEquals(expectedTag, tag))
        {
            throw new CryptographicException("Ошибка проверки подлинности: тег Poly1305 не совпал");
        }
        byte[] plaintext = new byte[cipherText.Length];
        ChaCha20Xor(key, 1, nonce, cipherText, plaintext);
        return plaintext;
    }
    public static byte[] ChaCha20Encrypt(byte[] key, uint counter, byte[] nonce, byte[] plaintext)
    {
        ValidateKeyNonce(key, nonce);
        byte[] output = new byte[plaintext.Length];
        ChaCha20Xor(key, counter, nonce, plaintext, output);
        return output;
    }
    public static byte[] Poly1305(byte[] key, byte[] message)
    {
        if (key == null || key.Length != KeySize)
        {
            throw new ArgumentException($"Ключ Poly1305 должен быть {KeySize} байт", nameof(key));
        }
        byte[] clampedR = new byte[16];
        Array.Copy(key, clampedR, 16);
        clampedR[3] &= 15; clampedR[7] &= 15; clampedR[11] &= 15; clampedR[15] &= 15;
        clampedR[4] &= 252; clampedR[8] &= 252; clampedR[12] &= 252;
        BigInteger r = ToPositiveBigInteger(clampedR);
        BigInteger s = ToPositiveBigInteger(key[16..32]);
        BigInteger prime = (BigInteger.One << 130) - 5;

        BigInteger acc = BigInteger.Zero;
        for (int offset = 0; offset < message.Length; offset += 16)
        {
            int len = Math.Min(16, message.Length - offset);
            // блок дополняется единичным байтом 0x01 после данных (RFC 8439 §2.5.2)
            byte[] block = new byte[len + 2];
            Array.Copy(message, offset, block, 0, len);
            block[len] = 1;
            BigInteger n = new BigInteger(block); // little-endian, старший ноль гарантирует положительность
            acc = (acc + n) * r % prime;
        }
        acc += s;
        BigInteger low128 = acc & ((BigInteger.One << 128) - 1);
        byte[] tag = new byte[TagSize];
        byte[] accBytes = low128.ToByteArray(); // little-endian, возможен лишний знаковый ноль
        Array.Copy(accBytes, tag, Math.Min(accBytes.Length, TagSize));
        return tag;
    }

    private static void ChaCha20Xor(byte[] key, uint counter, byte[] nonce, ReadOnlySpan<byte> input, Span<byte> output)
    {
        Span<byte> keystream = stackalloc byte[64];
        uint blockCounter = counter;
        for (int offset = 0; offset < input.Length; offset += 64)
        {
            ChaCha20Block(key, blockCounter, nonce, keystream);
            int len = Math.Min(64, input.Length - offset);
            for (int i = 0; i < len; i++)
            {
                output[offset + i] = (byte)(input[offset + i] ^ keystream[i]);
            }
            blockCounter++;
        }
    }

    private static void ChaCha20Block(byte[] key, uint counter, byte[] nonce, Span<byte> block64)
    {
        Span<uint> state = stackalloc uint[16];
        state[0] = 0x61707865;  
        state[1] = 0x3320646e; 
        state[2] = 0x79622d32; 
        state[3] = 0x6b206574;  
        for (int i = 0; i < 8; i++)
        {
            state[4 + i] = BitConverter.ToUInt32(key, i * 4);
        }
        state[12] = counter;
        state[13] = BitConverter.ToUInt32(nonce, 0);
        state[14] = BitConverter.ToUInt32(nonce, 4);
        state[15] = BitConverter.ToUInt32(nonce, 8);

        Span<uint> work = stackalloc uint[16];
        state.CopyTo(work);
        for (int round = 0; round < 10; round++)
        {
            QuarterRound(work, 0, 4, 8, 12);
            QuarterRound(work, 1, 5, 9, 13);
            QuarterRound(work, 2, 6, 10, 14);
            QuarterRound(work, 3, 7, 11, 15);

            QuarterRound(work, 0, 5, 10, 15);
            QuarterRound(work, 1, 6, 11, 12);
            QuarterRound(work, 2, 7, 8, 13);
            QuarterRound(work, 3, 4, 9, 14);
        }
        for (int i = 0; i < 16; i++)
        {
            uint word = work[i] + state[i];
            int j = i * 4;
            block64[j] = (byte)word;
            block64[j + 1] = (byte)(word >> 8);
            block64[j + 2] = (byte)(word >> 16);
            block64[j + 3] = (byte)(word >> 24);
        }
    }

    private static void QuarterRound(Span<uint> s, int a, int b, int c, int d)
    {
        s[a] += s[b]; s[d] ^= s[a]; s[d] = (s[d] << 16) | (s[d] >> 16);
        s[c] += s[d]; s[b] ^= s[c]; s[b] = (s[b] << 12) | (s[b] >> 20);
        s[a] += s[b]; s[d] ^= s[a]; s[d] = (s[d] << 8) | (s[d] >> 24);
        s[c] += s[d]; s[b] ^= s[c]; s[b] = (s[b] << 7) | (s[b] >> 25);
    }

    private static byte[] Poly1305Tag(byte[] key, byte[] nonce, byte[]? aad, byte[] cipherText)
    {
        Span<byte> block0 = stackalloc byte[64];
        ChaCha20Block(key, 0, nonce, block0);
        byte[] polyKey = block0.Slice(0, 32).ToArray();
        byte[] macData = BuildMacData(aad, cipherText);
        return Poly1305(polyKey, macData);
    }
    private static byte[] BuildMacData(byte[]? aad, byte[] cipherText)
    {
        int aadLen = aad?.Length ?? 0;
        int aadPad = (16 - aadLen % 16) % 16;
        int ctPad = (16 - cipherText.Length % 16) % 16;
        byte[] macData = new byte[aadLen + aadPad + cipherText.Length + ctPad + 16];
        if (aadLen > 0)
        {
            Array.Copy(aad, 0, macData, 0, aadLen);
        }
        Array.Copy(cipherText, 0, macData, aadLen + aadPad, cipherText.Length);
        WriteLe64(macData, aadLen + aadPad + cipherText.Length + ctPad, (ulong)aadLen);
        WriteLe64(macData, aadLen + aadPad + cipherText.Length + ctPad + 8, (ulong)cipherText.Length);
        return macData;
    }

    private static void WriteLe64(byte[] buffer, int offset, ulong value)
    {
        for (int i = 0; i < 8; i++)
        {
            buffer[offset + i] = (byte)(value >> (8 * i));
        }
    }

    private static BigInteger ToPositiveBigInteger(byte[] littleEndianBytes)
    {
        byte[] positive = new byte[littleEndianBytes.Length + 1];
        Array.Copy(littleEndianBytes, positive, littleEndianBytes.Length);
        return new BigInteger(positive);
    }

    private static void ValidateKeyNonce(byte[] key, byte[] nonce)
    {
        if (key == null || key.Length != KeySize)
        {
            throw new ArgumentException($"Ключ должен быть {KeySize} байт", nameof(key));
        }
        if (nonce == null || nonce.Length != NonceSize)
        {
            throw new ArgumentException($"Nonce должен быть {NonceSize} байт", nameof(nonce));
        }
    }
}
