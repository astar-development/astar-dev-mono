using System.Security.Cryptography;
using System.Text;

namespace AStar.Dev.Utilities;

/// <summary>
///     The <see cref="EncryptionExtensions" /> class contains extension methods to encrypt / decrypt the specified string
/// </summary>
public static class EncryptionExtensions
{
    private const string Key = "oe3QnEe&@NnJ$$^L$1N@4WVKFayaAAOb";
    private const string Iv  = "sBA&3z*4cQf%$!ww";

    /// <summary>
    ///     The Encrypt extension method will encrypt the specified string (using AES encryption)
    /// </summary>
    /// <param name="plainText">The string to encrypt</param>
    /// <param name="key">
    ///     The optional key to use for the encryption. If supplied, it must be 16, 24, or 32 bytes long. If not
    ///     specified, an internal default (32 bytes) will be used
    /// </param>
    /// <param name="iv">
    ///     The optional iv (initialisation vector) to use for the decryption. If supplied, it must be 16 bytes
    ///     long. If not specified, an internal default will be used
    /// </param>
    /// <returns>The original string encrypted appropriately</returns>
    public static string Encrypt(this string plainText, string? key = null, string? iv = null)
    {
        using var aesAlg = Aes.Create();

        aesAlg.Key = Encoding.UTF8.GetBytes(key ?? Key);
        aesAlg.IV  = Encoding.UTF8.GetBytes(iv  ?? Iv);

        var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

        using var msEncrypt = new MemoryStream();
        using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);

        using(var swEncrypt = new StreamWriter(csEncrypt))
        {
            swEncrypt.Write(plainText);
        }

        return Convert.ToBase64String(msEncrypt.ToArray());
    }

    /// <summary>
    ///     The Decrypt extension method will encrypt the specified string (using AES encryption)
    /// </summary>
    /// <param name="encryptedText">The string to decrypt</param>
    /// <param name="key">
    ///     The optional key to use for the encryption. If supplied, it must be 16, 24, or 32 bytes long. If not
    ///     specified, an internal default (32 bytes) will be used
    /// </param>
    /// <param name="iv">
    ///     The optional iv (initialisation vector) to use for the decryption. If supplied, it must be 16 bytes
    ///     long. If not specified, an internal default will be used
    /// </param>
    /// <returns>The decrypted string</returns>
    public static string Decrypt(this string encryptedText, string? key = null, string? iv = null)
    {
        using var aesAlg = Aes.Create();

        aesAlg.Key = Encoding.UTF8.GetBytes(key ?? Key);
        aesAlg.IV  = Encoding.UTF8.GetBytes(iv  ?? Iv);

        var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

        using var msDecrypt = new MemoryStream(Convert.FromBase64String(encryptedText));

        using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);

        using var srDecrypt = new StreamReader(csDecrypt);

        return srDecrypt.ReadToEnd();
    }
}