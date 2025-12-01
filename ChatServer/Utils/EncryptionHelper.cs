using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ChatServer.Utils
{
    /// <summary>
    /// EncryptionHelper phía server phải dùng cùng key/IV với phía client
    /// để thực hiện mã hóa end-to-end trên kênh TCP.
    /// Ở đây dùng AES đơn giản cho mục đích demo.
    /// </summary>
    public static class EncryptionHelper
    {
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("DemoChatAppKey16"); // 16 bytes
        private static readonly byte[] Iv = Encoding.UTF8.GetBytes("DemoChatAppIv_16");  // 16 bytes

        public static string Encrypt(string plainText)
        {
            using var aes = Aes.Create();
            aes.Key = Key;
            aes.IV = Iv;
            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs))
            {
                sw.Write(plainText);
            }
            return Convert.ToBase64String(ms.ToArray());
        }

        public static string Decrypt(string cipherText)
        {
            var buffer = Convert.FromBase64String(cipherText);
            using var aes = Aes.Create();
            aes.Key = Key;
            aes.IV = Iv;
            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(buffer);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);
            return sr.ReadToEnd();
        }
    }
}


