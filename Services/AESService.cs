using System.Security.Cryptography;

namespace NewVivaApi.Services;

public static class AESService
{
    public sealed class AESKey
    {
        public AESKey(byte[] key, byte[] iv)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            IV = iv ?? throw new ArgumentNullException(nameof(iv));
            
            if (key.Length != 32)
                throw new ArgumentException("Key must be 32 bytes (256 bits)", nameof(key));
            if (iv.Length != 16)
                throw new ArgumentException("IV must be 16 bytes (128 bits)", nameof(iv));
        }

        public byte[] Key { get; }
        public byte[] IV { get; }
    }

    public static byte[] CreateAESKey(string password, out byte[] iv)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        using var derived = new Rfc2898DeriveBytes(
            password, 
            saltSize: 16, 
            iterations: 120000, // Increased from 60000 for better security
            hashAlgorithm: HashAlgorithmName.SHA256); // SHA256 is sufficient and faster than SHA512

        iv = derived.Salt;
        return derived.GetBytes(32); // 256-bit key
    }


    public static byte[] CalculateAESKey(string password, byte[] iv)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);
        ArgumentNullException.ThrowIfNull(iv);

        using var derived = new Rfc2898DeriveBytes(
            password, 
            iv, 
            iterations: 120000, 
            hashAlgorithm: HashAlgorithmName.SHA256);

        return derived.GetBytes(32);
    }


    public static byte[] Encrypt(byte[] data, AESKey key)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(key);

        if (data.Length == 0)
            return [];

        using var aes = Aes.Create();
        aes.Key = key.Key;
        aes.IV = key.IV;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        using var memoryStream = new MemoryStream();
        using var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
        
        cryptoStream.Write(data);
        cryptoStream.FlushFinalBlock();
        
        return memoryStream.ToArray();
    }


    public static byte[] Decrypt(byte[] data, AESKey key)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(key);

        if (data.Length == 0)
            return [];

        using var aes = Aes.Create();
        aes.Key = key.Key;
        aes.IV = key.IV;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        using var memoryStream = new MemoryStream(data);
        using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
        using var resultStream = new MemoryStream();
        
        cryptoStream.CopyTo(resultStream);
        return resultStream.ToArray();
    }


    public static string EncryptString(string plainText, AESKey key)
    {
        ArgumentException.ThrowIfNullOrEmpty(plainText);
        ArgumentNullException.ThrowIfNull(key);

        var data = System.Text.Encoding.UTF8.GetBytes(plainText);
        var encrypted = Encrypt(data, key);
        return Convert.ToBase64String(encrypted);
    }


    public static string DecryptString(string encryptedText, AESKey key)
    {
        ArgumentException.ThrowIfNullOrEmpty(encryptedText);
        ArgumentNullException.ThrowIfNull(key);

        var data = Convert.FromBase64String(encryptedText);
        var decrypted = Decrypt(data, key);
        return System.Text.Encoding.UTF8.GetString(decrypted);
    }
}