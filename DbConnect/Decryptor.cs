using System;
using System.Security.Cryptography;
using System.Text;


namespace DbConnect
{
    // ReSharper disable once IdentifierTypo
    internal static class Decryptor
    {
        private const string CipherKey = "Night@Run@Rtc@Analyzer";

        public static string Encrypt(string data)
        {
            var cipher = new RijndaelManaged
            {
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7,
                KeySize = 0x80,
                BlockSize = 0x80
            };

            var pwdBytes = Encoding.UTF8.GetBytes(CipherKey);
            var keyBytes = new byte[0x10];
            var len = pwdBytes.Length;

            if (len > keyBytes.Length)
            {
                len = keyBytes.Length;
            }

            Array.Copy(pwdBytes, keyBytes, len);
            cipher.Key = keyBytes;
            cipher.IV = keyBytes;
            var transform = cipher.CreateEncryptor();
            var plainText = Encoding.UTF8.GetBytes(data);

            return Convert.ToBase64String(transform.TransformFinalBlock(plainText, 0, plainText.Length));
        }

        public static string Decrypt(string data)
        {
            var cipher = new RijndaelManaged
            {
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7,
                KeySize = 0x80,
                BlockSize = 0x80
            };

            var encryptedData = Convert.FromBase64String(data);
            var pwdBytes = Encoding.UTF8.GetBytes(CipherKey);
            var keyBytes = new byte[0x10];
            var len = pwdBytes.Length;

            if (len > keyBytes.Length)
            {
                len = keyBytes.Length;
            }

            Array.Copy(pwdBytes, keyBytes, len);
            cipher.Key = keyBytes;
            cipher.IV = keyBytes;
            var plainText = cipher.CreateDecryptor().TransformFinalBlock(encryptedData, 0, encryptedData.Length);

            return Encoding.UTF8.GetString(plainText);
        }
    }
}
