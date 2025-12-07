using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ChatClient.Utils
{
    /// <summary>
    /// Hệ thống bảo mật 3 lớp:
    /// 1. AES-256 (Symmetric) - Mã hóa nội dung tin nhắn socket
    /// 2. RSA-2048 (Asymmetric) - Chữ ký số xác thực người gửi
    /// 3. Hybrid (RSA+AES) - Mã hóa file đính kèm lớn
    /// </summary>
    public static class EncryptionHelper
    {
        #region ========== 1. AES-256 SYMMETRIC ENCRYPTION ==========
        // Sử dụng: Mã hóa/giải mã socket communication (nhanh, hiệu quả)
        
        private static readonly byte[] AesKey = Encoding.UTF8.GetBytes("ChatApp_AES_Key_32bytes_Long!@#$"); // 32 bytes = 256-bit
        private static readonly byte[] AesIv = Encoding.UTF8.GetBytes("ChatApp_AES_IV!!");  // 16 bytes

        /// <summary>
        /// AES Encrypt - Dùng cho socket messages
        /// </summary>
        public static string Encrypt(string plainText)
        {
            return AesEncrypt(plainText, AesKey, AesIv);
        }

        /// <summary>
        /// AES Decrypt - Dùng cho socket messages
        /// </summary>
        public static string Decrypt(string cipherText)
        {
            return AesDecrypt(cipherText, AesKey, AesIv);
        }

        public static string AesEncrypt(string plainText, byte[] key, byte[] iv)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
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

        public static string AesDecrypt(string cipherText, byte[] key, byte[] iv)
        {
            var buffer = Convert.FromBase64String(cipherText);
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            
            using var decryptor = aes.CreateDecryptor();
            using var ms = new MemoryStream(buffer);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs, Encoding.UTF8);
            return sr.ReadToEnd();
        }

        /// <summary>
        /// Generate random AES-256 key
        /// </summary>
        public static byte[] GenerateAesKey()
        {
            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.GenerateKey();
            return aes.Key;
        }
        #endregion

        #region ========== 2. RSA-2048 ASYMMETRIC ENCRYPTION ==========
        // Sử dụng: Chữ ký số (Digital Signature) để xác thực người gửi
        
        // Demo RSA Keys (Production: mỗi user có key pair riêng)
        private static RSA? _rsaInstance;
        
        private static RSA GetRsa()
        {
            if (_rsaInstance == null)
            {
                _rsaInstance = RSA.Create(2048);
            }
            return _rsaInstance;
        }

        /// <summary>
        /// RSA Sign - Tạo chữ ký số cho message
        /// </summary>
        public static string RsaSign(string data)
        {
            var rsa = GetRsa();
            var dataBytes = Encoding.UTF8.GetBytes(data);
            var signature = rsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return Convert.ToBase64String(signature);
        }

        /// <summary>
        /// RSA Verify - Xác thực chữ ký số
        /// </summary>
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

        /// <summary>
        /// RSA Encrypt - Mã hóa dữ liệu nhỏ (< 200 bytes)
        /// </summary>
        public static string RsaEncrypt(string plainText, RSA publicKey)
        {
            var dataBytes = Encoding.UTF8.GetBytes(plainText);
            var encrypted = publicKey.Encrypt(dataBytes, RSAEncryptionPadding.OaepSHA256);
            return Convert.ToBase64String(encrypted);
        }

        /// <summary>
        /// RSA Decrypt
        /// </summary>
        public static string RsaDecrypt(string cipherBase64, RSA privateKey)
        {
            var cipherBytes = Convert.FromBase64String(cipherBase64);
            var decrypted = privateKey.Decrypt(cipherBytes, RSAEncryptionPadding.OaepSHA256);
            return Encoding.UTF8.GetString(decrypted);
        }

        /// <summary>
        /// Export RSA Public Key
        /// </summary>
        public static string ExportPublicKey()
        {
            var rsa = GetRsa();
            return Convert.ToBase64String(rsa.ExportRSAPublicKey());
        }
        #endregion

        #region ========== 3. HYBRID ENCRYPTION (RSA + AES) ==========
        // Sử dụng: Mã hóa file đính kèm lớn
        // Cách hoạt động:
        // - Tạo AES key ngẫu nhiên cho mỗi file
        // - Mã hóa file bằng AES (nhanh)
        // - Mã hóa AES key bằng RSA (bảo mật key exchange)

        /// <summary>
        /// Hybrid Encrypt - Mã hóa attachment/file lớn
        /// Returns: encryptedData|encryptedAesKey|iv (all base64, separated by |)
        /// </summary>
        public static string HybridEncrypt(byte[] data)
        {
            // Generate random AES key for this file
            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.GenerateKey();
            aes.GenerateIV();

            // Encrypt data with AES
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

            // Encrypt AES key with RSA
            var rsa = GetRsa();
            var encryptedKey = rsa.Encrypt(aes.Key, RSAEncryptionPadding.OaepSHA256);

            // Return combined: data|key|iv
            return $"{Convert.ToBase64String(encryptedData)}|{Convert.ToBase64String(encryptedKey)}|{Convert.ToBase64String(aes.IV)}";
        }

        /// <summary>
        /// Hybrid Decrypt - Giải mã attachment/file lớn
        /// </summary>
        public static byte[] HybridDecrypt(string encryptedPackage)
        {
            var parts = encryptedPackage.Split('|');
            if (parts.Length != 3)
                throw new ArgumentException("Invalid encrypted package format");

            var encryptedData = Convert.FromBase64String(parts[0]);
            var encryptedKey = Convert.FromBase64String(parts[1]);
            var iv = Convert.FromBase64String(parts[2]);

            // Decrypt AES key with RSA
            var rsa = GetRsa();
            var aesKey = rsa.Decrypt(encryptedKey, RSAEncryptionPadding.OaepSHA256);

            // Decrypt data with AES
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

        #region ========== UTILITY METHODS ==========
        
        /// <summary>
        /// Generate random bytes
        /// </summary>
        public static byte[] GenerateRandomBytes(int length)
        {
            var buffer = new byte[length];
            RandomNumberGenerator.Fill(buffer);
            return buffer;
        }

        /// <summary>
        /// Compute SHA-256 hash
        /// </summary>
        public static string ComputeHash(string data)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hashBytes);
        }

        /// <summary>
        /// Compute HMAC-SHA256 for message authentication
        /// </summary>
        public static string ComputeHmac(string data, byte[] key)
        {
            using var hmac = new HMACSHA256(key);
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hashBytes);
        }
        #endregion
    }

    /// <summary>
    /// Security usage summary:
    /// 
    /// 1. AES-256 (EncryptionHelper.Encrypt/Decrypt)
    ///    - Socket communication encryption
    ///    - Fast symmetric encryption for all messages
    ///    - Used in: SocketClientService.SendRequestAsync
    /// 
    /// 2. RSA-2048 (EncryptionHelper.RsaSign/RsaVerify)
    ///    - Digital signatures for sender authentication
    ///    - Verify message integrity
    ///    - Used in: Critical operations (login, admin actions)
    /// 
    /// 3. Hybrid RSA+AES (EncryptionHelper.HybridEncrypt/HybridDecrypt)
    ///    - Large file/attachment encryption
    ///    - Combines RSA key exchange with AES data encryption
    ///    - Used in: Attachment upload/download
    /// </summary>
    public class SecurityDocumentation { }
}


