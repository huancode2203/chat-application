using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ChatServer.Utils
{
    /// <summary>
    /// Hệ thống bảo mật 3 lớp (Server Side):
    /// 1. AES-256 - Socket communication
    /// 2. RSA-2048 - Digital signatures
    /// 3. Hybrid - Large file encryption
    /// </summary>
    public static class EncryptionHelper
    {
        #region AES-256 Socket Encryption
        // PHẢI GIỐNG CLIENT để giao tiếp được
        private static readonly byte[] AesKey = Encoding.UTF8.GetBytes("ChatApp_AES_Key_32bytes_Long!@#$"); // 32 bytes
        private static readonly byte[] AesIv = Encoding.UTF8.GetBytes("ChatApp_AES_IV!!");  // 16 bytes

        public static string Encrypt(string plainText)
        {
            using var aes = Aes.Create();
            aes.Key = AesKey;
            aes.IV = AesIv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            
            using var encryptor = aes.CreateEncryptor();
            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs, Encoding.UTF8))
            {
                sw.Write(plainText);
            }
            return Convert.ToBase64String(ms.ToArray());
        }

        public static string Decrypt(string cipherText)
        {
            var buffer = Convert.FromBase64String(cipherText);
            using var aes = Aes.Create();
            aes.Key = AesKey;
            aes.IV = AesIv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            
            using var decryptor = aes.CreateDecryptor();
            using var ms = new MemoryStream(buffer);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs, Encoding.UTF8);
            return sr.ReadToEnd();
        }
        #endregion

        #region RSA Digital Signature
        private static RSA? _rsaInstance;
        
        private static RSA GetRsa()
        {
            _rsaInstance ??= RSA.Create(2048);
            return _rsaInstance;
        }

        public static string RsaSign(string data)
        {
            var rsa = GetRsa();
            var dataBytes = Encoding.UTF8.GetBytes(data);
            var signature = rsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return Convert.ToBase64String(signature);
        }

        public static bool RsaVerify(string data, string signatureBase64)
        {
            try
            {
                var rsa = GetRsa();
                var dataBytes = Encoding.UTF8.GetBytes(data);
                var signature = Convert.FromBase64String(signatureBase64);
                return rsa.VerifyData(dataBytes, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
            catch { return false; }
        }
        #endregion

        #region Hybrid Encryption (RSA + AES) for Attachments
        public static string HybridEncrypt(byte[] data)
        {
            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.GenerateKey();
            aes.GenerateIV();

            byte[] encryptedData;
            using (var encryptor = aes.CreateEncryptor())
            using (var ms = new MemoryStream())
            {
                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                {
                    cs.Write(data, 0, data.Length);
                }
                encryptedData = ms.ToArray();
            }

            var rsa = GetRsa();
            var encryptedKey = rsa.Encrypt(aes.Key, RSAEncryptionPadding.OaepSHA256);

            return $"{Convert.ToBase64String(encryptedData)}|{Convert.ToBase64String(encryptedKey)}|{Convert.ToBase64String(aes.IV)}";
        }

        public static byte[] HybridDecrypt(string encryptedPackage)
        {
            var parts = encryptedPackage.Split('|');
            if (parts.Length != 3) throw new ArgumentException("Invalid format");

            var encryptedData = Convert.FromBase64String(parts[0]);
            var encryptedKey = Convert.FromBase64String(parts[1]);
            var iv = Convert.FromBase64String(parts[2]);

            var rsa = GetRsa();
            var aesKey = rsa.Decrypt(encryptedKey, RSAEncryptionPadding.OaepSHA256);

            using var aes = Aes.Create();
            aes.Key = aesKey;
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            using var ms = new MemoryStream(encryptedData);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var output = new MemoryStream();
            cs.CopyTo(output);
            return output.ToArray();
        }
        #endregion

        #region Utility
        public static string ComputeHash(string data)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hashBytes);
        }
        #endregion
    }
}


