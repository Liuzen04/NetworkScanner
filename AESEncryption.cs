using System;
using System.Security.Cryptography;
using System.Text;

namespace NetworkScanner
{
    /// <summary>
    /// Lớp mã hóa AES
    /// </summary>
    public static class AESEncryption
    {
        private static readonly byte[] Salt = Encoding.UTF8.GetBytes("NetworkScannerSalt2024");

        public static string Encrypt(string plainText, string password)
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

            using (var aes = new AesCryptoServiceProvider())
            {
                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                var key = new Rfc2898DeriveBytes(password, Salt, 1000);
                aes.Key = key.GetBytes(aes.KeySize / 8);
                aes.IV = key.GetBytes(aes.BlockSize / 8);

                using (var encryptor = aes.CreateEncryptor())
                {
                    byte[] encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                    return Convert.ToBase64String(encryptedBytes);
                }
            }
        }

        public static string Decrypt(string cipherText, string password)
        {
            byte[] cipherBytes = Convert.FromBase64String(cipherText);

            using (var aes = new AesCryptoServiceProvider())
            {
                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                var key = new Rfc2898DeriveBytes(password, Salt, 1000);
                aes.Key = key.GetBytes(aes.KeySize / 8);
                aes.IV = key.GetBytes(aes.BlockSize / 8);

                using (var decryptor = aes.CreateDecryptor())
                {
                    byte[] decryptedBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                    return Encoding.UTF8.GetString(decryptedBytes);
                }
            }
        }
    }
}