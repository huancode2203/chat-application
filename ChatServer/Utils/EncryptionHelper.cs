using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ChatServer.Utils
{
    /// <summary>
    /// HỆ THỐNG MÃ HÓA 3 LỚP (Server Side):
    /// ============================================
    /// 1. MÃ HÓA ĐỐI XỨNG (Symmetric - AES-256-CBC)
    ///    - Socket communication encryption
    ///    - Database content encryption (DBMS_CRYPTO)
    ///    - Nhanh, hiệu quả cho dữ liệu lớn
    ///    
    /// 2. MÃ HÓA BẤT ĐỐI XỨNG (Asymmetric - RSA-2048)
    ///    - Chữ ký số (Digital signatures)
    ///    - Trao đổi khóa an toàn (Key exchange)
    ///    - Xác thực người gửi
    ///    
    /// 3. MÃ HÓA LAI (Hybrid - RSA + AES)
    ///    - File/attachment encryption
    ///    - End-to-end encryption cho tin nhắn nhạy cảm
    ///    - Kết hợp ưu điểm của cả hai phương pháp
    /// </summary>
    public static class EncryptionHelper
    {
        #region ========== 1. MÃ HÓA ĐỐI XỨNG (SYMMETRIC - AES-256-CBC) ==========
        // Sử dụng: Socket communication, database encryption
        // PHẢI GIỐNG CLIENT để giao tiếp được
        
        private static readonly byte[] AesKey = Encoding.UTF8.GetBytes("ChatApp_AES_Key_32bytes_Long!@#$"); // 32 bytes = 256-bit
        private static readonly byte[] AesIv = Encoding.UTF8.GetBytes("ChatApp_AES_IV!!");  // 16 bytes

        /// <summary>
        /// AES-256-CBC Encrypt - Mã hóa đối xứng cho socket messages
        /// </summary>
        public static string Encrypt(string plainText)
        {
            return AesEncrypt(plainText, AesKey, AesIv);
        }

        /// <summary>
        /// AES-256-CBC Decrypt - Giải mã đối xứng
        /// </summary>
        public static string Decrypt(string cipherText)
        {
            return AesDecrypt(cipherText, AesKey, AesIv);
        }

        /// <summary>
        /// AES Encrypt với custom key/iv
        /// </summary>
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

        /// <summary>
        /// AES Decrypt với custom key/iv
        /// </summary>
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
        /// AES-256 Encrypt với auto-generated IV (IV được prepend vào ciphertext)
        /// </summary>
        public static string EncryptAES(string plainText, byte[] key)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.GenerateIV();
            
            using var encryptor = aes.CreateEncryptor();
            using var ms = new MemoryStream();
            // Write IV first
            ms.Write(aes.IV, 0, aes.IV.Length);
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs, Encoding.UTF8))
            {
                sw.Write(plainText);
            }
            return Convert.ToBase64String(ms.ToArray());
        }

        /// <summary>
        /// AES-256 Decrypt với IV được prepend trong ciphertext
        /// </summary>
        public static string DecryptAES(string cipherText, byte[] key)
        {
            var fullCipher = Convert.FromBase64String(cipherText);
            
            // Extract IV (first 16 bytes)
            var iv = new byte[16];
            var cipher = new byte[fullCipher.Length - 16];
            Buffer.BlockCopy(fullCipher, 0, iv, 0, 16);
            Buffer.BlockCopy(fullCipher, 16, cipher, 0, cipher.Length);
            
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            
            using var decryptor = aes.CreateDecryptor();
            using var ms = new MemoryStream(cipher);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs, Encoding.UTF8);
            return sr.ReadToEnd();
        }

        /// <summary>
        /// AES Encrypt bytes (cho file/attachment)
        /// </summary>
        public static byte[] AesEncryptBytes(byte[] data, byte[] key, byte[] iv)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                cs.Write(data, 0, data.Length);
            }
            return ms.ToArray();
        }

        /// <summary>
        /// AES Decrypt bytes
        /// </summary>
        public static byte[] AesDecryptBytes(byte[] cipherData, byte[] key, byte[] iv)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            using var ms = new MemoryStream(cipherData);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var output = new MemoryStream();
            cs.CopyTo(output);
            return output.ToArray();
        }

        /// <summary>
        /// Tạo AES key ngẫu nhiên (256-bit)
        /// </summary>
        public static byte[] GenerateAesKey()
        {
            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.GenerateKey();
            return aes.Key;
        }

        /// <summary>
        /// Tạo IV ngẫu nhiên (128-bit)
        /// </summary>
        public static byte[] GenerateIv()
        {
            using var aes = Aes.Create();
            aes.GenerateIV();
            return aes.IV;
        }
        #endregion

        #region ========== 2. MÃ HÓA BẤT ĐỐI XỨNG (ASYMMETRIC - RSA-2048) ==========
        // Sử dụng: Chữ ký số, trao đổi khóa, xác thực người gửi
        
        private static RSA? _rsaInstance;
        
        private static RSA GetRsa()
        {
            _rsaInstance ??= RSA.Create(2048);
            return _rsaInstance;
        }

        /// <summary>
        /// RSA Sign - Tạo chữ ký số
        /// </summary>
        public static string RsaSign(string data)
        {
            var rsa = GetRsa();
            var dataBytes = Encoding.UTF8.GetBytes(data);
            var signature = rsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var result = Convert.ToBase64String(signature);
            Console.WriteLine($"[SERVER][RSA] SIGN: data={data.Substring(0, Math.Min(30, data.Length))}... => sig={result.Substring(0, Math.Min(40, result.Length))}...");
            return result;
        }

        /// <summary>
        /// RSA Verify - Xác thực chữ ký
        /// </summary>
        public static bool RsaVerify(string data, string signatureBase64)
        {
            try
            {
                var rsa = GetRsa();
                var dataBytes = Encoding.UTF8.GetBytes(data);
                var signature = Convert.FromBase64String(signatureBase64);
                var result = rsa.VerifyData(dataBytes, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                Console.WriteLine($"[SERVER][RSA] VERIFY: data={data.Substring(0, Math.Min(30, data.Length))}... => {(result ? "VALID" : "INVALID")}");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SERVER][RSA] VERIFY ERROR: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// RSA Encrypt - Mã hóa dữ liệu nhỏ (< 200 bytes)
        /// </summary>
        public static string RsaEncrypt(string plainText)
        {
            var rsa = GetRsa();
            var dataBytes = Encoding.UTF8.GetBytes(plainText);
            var encrypted = rsa.Encrypt(dataBytes, RSAEncryptionPadding.OaepSHA256);
            var result = Convert.ToBase64String(encrypted);
            Console.WriteLine($"[SERVER][RSA] ENCRYPT: plain={plainText.Substring(0, Math.Min(20, plainText.Length))}... => cipher={result.Substring(0, Math.Min(40, result.Length))}...");
            return result;
        }

        /// <summary>
        /// RSA Decrypt
        /// </summary>
        public static string RsaDecrypt(string cipherBase64)
        {
            var rsa = GetRsa();
            var cipherBytes = Convert.FromBase64String(cipherBase64);
            var decrypted = rsa.Decrypt(cipherBytes, RSAEncryptionPadding.OaepSHA256);
            var result = Encoding.UTF8.GetString(decrypted);
            Console.WriteLine($"[SERVER][RSA] DECRYPT: cipher={cipherBase64.Substring(0, Math.Min(40, cipherBase64.Length))}... => plain={result.Substring(0, Math.Min(20, result.Length))}...");
            return result;
        }

        /// <summary>
        /// Export RSA Public Key (Base64)
        /// </summary>
        public static string ExportPublicKey()
        {
            var rsa = GetRsa();
            return Convert.ToBase64String(rsa.ExportRSAPublicKey());
        }

        /// <summary>
        /// Export RSA Private Key (Base64) - CHỈ DÙNG CHO DEBUG
        /// </summary>
        public static string ExportPrivateKey()
        {
            var rsa = GetRsa();
            return Convert.ToBase64String(rsa.ExportRSAPrivateKey());
        }

        /// <summary>
        /// Tạo RSA key pair mới
        /// </summary>
        public static (string publicKey, string privateKey) GenerateRsaKeyPair()
        {
            using var rsa = RSA.Create(2048);
            return (
                Convert.ToBase64String(rsa.ExportRSAPublicKey()),
                Convert.ToBase64String(rsa.ExportRSAPrivateKey())
            );
        }

        /// <summary>
        /// RSA Verify với public key từ client - Xác thực chữ ký số
        /// </summary>
        public static bool RsaVerifyWithPublicKey(string data, string signatureBase64, string publicKeyBase64)
        {
            try
            {
                using var rsa = RSA.Create();
                var publicKeyBytes = Convert.FromBase64String(publicKeyBase64);
                rsa.ImportRSAPublicKey(publicKeyBytes, out _);
                
                var dataBytes = Encoding.UTF8.GetBytes(data);
                var signature = Convert.FromBase64String(signatureBase64);
                var result = rsa.VerifyData(dataBytes, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                Console.WriteLine($"[SERVER][RSA] VERIFY with client key: {(result ? "VALID" : "INVALID")}");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SERVER][RSA] VERIFY ERROR: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// RSA Sign với custom private key
        /// </summary>
        public static string RsaSignWithPrivateKey(string data, string privateKeyBase64)
        {
            using var rsa = RSA.Create();
            var privateKeyBytes = Convert.FromBase64String(privateKeyBase64);
            rsa.ImportRSAPrivateKey(privateKeyBytes, out _);
            
            var dataBytes = Encoding.UTF8.GetBytes(data);
            var signature = rsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return Convert.ToBase64String(signature);
        }
        #endregion

        #region ========== 3. MÃ HÓA LAI (HYBRID - RSA + AES) ==========
        // Sử dụng: File/attachment encryption, E2E encryption
        // Cách hoạt động:
        // - Tạo AES session key ngẫu nhiên
        // - Mã hóa data bằng AES (nhanh)
        // - Mã hóa AES key bằng RSA (bảo mật key exchange)

        /// <summary>
        /// Hybrid Encrypt - Mã hóa file/attachment lớn
        /// Returns: encryptedData|encryptedAesKey|iv (all base64)
        /// </summary>
        public static string HybridEncrypt(byte[] data)
        {
            Console.WriteLine($"[SERVER][HYBRID] ENCRYPT START: dataSize={data.Length} bytes");
            
            // Tạo session key ngẫu nhiên
            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.GenerateKey();
            aes.GenerateIV();
            Console.WriteLine($"[SERVER][HYBRID] Generated AES-256 session key");

            // Mã hóa data bằng AES
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
            Console.WriteLine($"[SERVER][HYBRID][AES] Data encrypted: {data.Length} => {encryptedData.Length} bytes");

            // Mã hóa AES key bằng RSA
            var rsa = GetRsa();
            var encryptedKey = rsa.Encrypt(aes.Key, RSAEncryptionPadding.OaepSHA256);
            Console.WriteLine($"[SERVER][HYBRID][RSA] AES key encrypted: 32 => {encryptedKey.Length} bytes");

            // Return format: data|key|iv
            var result = $"{Convert.ToBase64String(encryptedData)}|{Convert.ToBase64String(encryptedKey)}|{Convert.ToBase64String(aes.IV)}";
            Console.WriteLine($"[SERVER][HYBRID] ENCRYPT DONE: totalSize={result.Length} chars");
            return result;
        }

        /// <summary>
        /// Hybrid Encrypt string message
        /// </summary>
        public static string HybridEncryptString(string plainText)
        {
            return HybridEncrypt(Encoding.UTF8.GetBytes(plainText));
        }

        /// <summary>
        /// Hybrid Decrypt - Giải mã file/attachment
        /// </summary>
        public static byte[] HybridDecrypt(string encryptedPackage)
        {
            Console.WriteLine($"[SERVER][HYBRID] DECRYPT START: packageSize={encryptedPackage.Length} chars");
            
            var parts = encryptedPackage.Split('|');
            if (parts.Length != 3) throw new ArgumentException("Invalid encrypted package format");

            var encryptedData = Convert.FromBase64String(parts[0]);
            var encryptedKey = Convert.FromBase64String(parts[1]);
            var iv = Convert.FromBase64String(parts[2]);

            // Giải mã AES key bằng RSA
            var rsa = GetRsa();
            var aesKey = rsa.Decrypt(encryptedKey, RSAEncryptionPadding.OaepSHA256);
            Console.WriteLine($"[SERVER][HYBRID][RSA] AES key decrypted: {encryptedKey.Length} => 32 bytes");

            // Giải mã data bằng AES
            using var aes = Aes.Create();
            aes.Key = aesKey;
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            using var ms = new MemoryStream(encryptedData);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var output = new MemoryStream();
            cs.CopyTo(output);
            var result = output.ToArray();
            Console.WriteLine($"[SERVER][HYBRID][AES] Data decrypted: {encryptedData.Length} => {result.Length} bytes");
            Console.WriteLine($"[SERVER][HYBRID] DECRYPT DONE");
            return result;
        }

        /// <summary>
        /// Hybrid Decrypt to string
        /// </summary>
        public static string HybridDecryptString(string encryptedPackage)
        {
            var bytes = HybridDecrypt(encryptedPackage);
            return Encoding.UTF8.GetString(bytes);
        }

        /// <summary>
        /// Hybrid Encrypt với recipient public key (E2E)
        /// </summary>
        public static string HybridEncryptForRecipient(byte[] data, string recipientPublicKeyBase64)
        {
            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.GenerateKey();
            aes.GenerateIV();

            // Mã hóa data bằng AES
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

            // Mã hóa AES key bằng recipient's public key
            using var rsa = RSA.Create();
            rsa.ImportRSAPublicKey(Convert.FromBase64String(recipientPublicKeyBase64), out _);
            var encryptedKey = rsa.Encrypt(aes.Key, RSAEncryptionPadding.OaepSHA256);

            return $"{Convert.ToBase64String(encryptedData)}|{Convert.ToBase64String(encryptedKey)}|{Convert.ToBase64String(aes.IV)}";
        }
        #endregion

        #region ========== UTILITY FUNCTIONS ==========
        
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
        /// Compute SHA-256 hash as hex
        /// </summary>
        public static string ComputeHashHex(string data)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
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

        /// <summary>
        /// Verify HMAC
        /// </summary>
        public static bool VerifyHmac(string data, string hmacBase64, byte[] key)
        {
            var computed = ComputeHmac(data, key);
            return computed == hmacBase64;
        }
        #endregion
    }

    /// <summary>
    /// TÓM TẮT HỆ THỐNG MÃ HÓA:
    /// ============================================
    /// 
    /// MỨC ỨNG DỤNG (Application Level):
    /// ----------------------------------
    /// 1. AES-256 (EncryptionHelper.Encrypt/Decrypt)
    ///    - Mã hóa socket communication
    ///    - Mã hóa nhanh cho tất cả tin nhắn
    /// 
    /// 2. RSA-2048 (EncryptionHelper.RsaSign/RsaVerify/RsaEncrypt/RsaDecrypt)
    ///    - Chữ ký số xác thực người gửi
    ///    - Mã hóa dữ liệu nhỏ, key exchange
    /// 
    /// 3. Hybrid (EncryptionHelper.HybridEncrypt/HybridDecrypt)
    ///    - Mã hóa file/attachment lớn
    ///    - E2E encryption cho tin nhắn nhạy cảm
    /// 
    /// MỨC CƠ SỞ DỮ LIỆU (Database Level - DBMS_CRYPTO):
    /// --------------------------------------------------
    /// 1. AES-256 (DB_ENCRYPTION_PKG.AES_ENCRYPT/AES_DECRYPT)
    ///    - Mã hóa nội dung tin nhắn trong DB
    ///    - Procedure: SP_GUI_TINNHAN_MAHOA_AES
    /// 
    /// 2. RSA Key Management (DB_ENCRYPTION_PKG.SAVE_KEY_PAIR/GET_PUBLIC_KEY)
    ///    - Lưu trữ và quản lý RSA keys
    ///    - Bảng: ENCRYPTION_KEYS
    /// 
    /// 3. Hybrid (DB_ENCRYPTION_PKG.HYBRID_ENCRYPT/HYBRID_DECRYPT)
    ///    - E2E encryption trong database
    ///    - Procedure: SP_GUI_TINNHAN_MAHOA_HYBRID
    /// </summary>
    public class EncryptionDocumentation { }
}


