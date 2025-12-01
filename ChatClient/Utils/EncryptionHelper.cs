using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ChatClient.Utils
{
    /// <summary>
    /// Mã hóa end-to-end đơn giản dùng AES symetric.
    /// Thực tế nên dùng key riêng cho từng người dùng / phiên, ở đây demo dùng key chung giữa client và server.
    /// </summary>
    public static class EncryptionHelper
    {
        // Key/IV demo. THỰC TẾ phải sinh ngẫu nhiên, lưu an toàn, có cơ chế trao đổi key (Diffie-Hellman, v.v.).
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


