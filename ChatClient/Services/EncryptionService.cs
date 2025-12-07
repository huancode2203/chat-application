using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ChatClient.Services
{
    /// <summary>
    /// Hybrid Encryption Service combining AES (symmetric) and RSA (asymmetric)
    /// - Use AES for fast encryption of data
    /// - Use RSA to encrypt the AES key for secure key exchange
    /// </summary>
    public class EncryptionService : IDisposable
    {
        private RSA? _rsa;
        private readonly int _aesKeySize = 256; // AES-256
        private readonly int _rsaKeySize = 2048; // RSA-2048

        public EncryptionService()
        {
            _rsa = RSA.Create(_rsaKeySize);
        }

        #region RSA Key Management

        /// <summary>
        /// Generate new RSA key pair
        /// </summary>
        public (string publicKey, string privateKey) GenerateRSAKeyPair()
        {
            if (_rsa == null)
                _rsa = RSA.Create(_rsaKeySize);

            var publicKey = Convert.ToBase64String(_rsa.ExportRSAPublicKey());
            var privateKey = Convert.ToBase64String(_rsa.ExportRSAPrivateKey());

            return (publicKey, privateKey);
        }

        /// <summary>
        /// Import RSA public key for encryption
        /// </summary>
        public void ImportPublicKey(string publicKeyBase64)
        {
            if (_rsa == null)
                _rsa = RSA.Create(_rsaKeySize);

            var publicKeyBytes = Convert.FromBase64String(publicKeyBase64);
            _rsa.ImportRSAPublicKey(publicKeyBytes, out _);
        }

        /// <summary>
        /// Import RSA private key for decryption
        /// </summary>
        public void ImportPrivateKey(string privateKeyBase64)
        {
            if (_rsa == null)
                _rsa = RSA.Create(_rsaKeySize);

            var privateKeyBytes = Convert.FromBase64String(privateKeyBase64);
            _rsa.ImportRSAPrivateKey(privateKeyBytes, out _);
        }

        #endregion

        #region Hybrid Encryption (AES + RSA)

        /// <summary>
        /// Encrypt data using hybrid approach:
        /// 1. Generate random AES key
        /// 2. Encrypt data with AES
        /// 3. Encrypt AES key with RSA
        /// Returns: (encryptedData, encryptedAesKey, iv)
        /// </summary>
        public (string encryptedData, string encryptedAesKey, string iv) EncryptHybrid(string plainText, string recipientPublicKey)
        {
            // Import recipient's public key
            using var recipientRsa = RSA.Create(_rsaKeySize);
            var publicKeyBytes = Convert.FromBase64String(recipientPublicKey);
            recipientRsa.ImportRSAPublicKey(publicKeyBytes, out _);

            // Generate random AES key and IV
            using var aes = Aes.Create();
            aes.KeySize = _aesKeySize;
            aes.GenerateKey();
            aes.GenerateIV();

            // Encrypt data with AES
            byte[] encryptedBytes;
            using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
            using (var msEncrypt = new MemoryStream())
            using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            using (var swEncrypt = new StreamWriter(csEncrypt))
            {
                swEncrypt.Write(plainText);
                swEncrypt.Flush();
                csEncrypt.FlushFinalBlock();
                encryptedBytes = msEncrypt.ToArray();
            }

            // Encrypt AES key with RSA
            var encryptedAesKeyBytes = recipientRsa.Encrypt(aes.Key, RSAEncryptionPadding.OaepSHA256);

            return (
                Convert.ToBase64String(encryptedBytes),
                Convert.ToBase64String(encryptedAesKeyBytes),
                Convert.ToBase64String(aes.IV)
            );
        }

        /// <summary>
        /// Decrypt data using hybrid approach:
        /// 1. Decrypt AES key using RSA private key
        /// 2. Decrypt data using AES key
        /// </summary>
        public string DecryptHybrid(string encryptedDataBase64, string encryptedAesKeyBase64, string ivBase64, string privateKey)
        {
            // Import private key
            using var rsa = RSA.Create(_rsaKeySize);
            var privateKeyBytes = Convert.FromBase64String(privateKey);
            rsa.ImportRSAPrivateKey(privateKeyBytes, out _);

            // Decrypt AES key using RSA
            var encryptedAesKey = Convert.FromBase64String(encryptedAesKeyBase64);
            var aesKey = rsa.Decrypt(encryptedAesKey, RSAEncryptionPadding.OaepSHA256);

            // Decrypt data using AES
            var encryptedData = Convert.FromBase64String(encryptedDataBase64);
            var iv = Convert.FromBase64String(ivBase64);

            using var aes = Aes.Create();
            aes.Key = aesKey;
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var msDecrypt = new MemoryStream(encryptedData);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);

            return srDecrypt.ReadToEnd();
        }

        #endregion

        #region AES Only (Symmetric)

        /// <summary>
        /// Encrypt using AES with provided key
        /// </summary>
        public (string encryptedData, string iv) EncryptAES(string plainText, string keyBase64)
        {
            var key = Convert.FromBase64String(keyBase64);

            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();

            byte[] encryptedBytes;
            using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
            using (var msEncrypt = new MemoryStream())
            using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            using (var swEncrypt = new StreamWriter(csEncrypt))
            {
                swEncrypt.Write(plainText);
                swEncrypt.Flush();
                csEncrypt.FlushFinalBlock();
                encryptedBytes = msEncrypt.ToArray();
            }

            return (Convert.ToBase64String(encryptedBytes), Convert.ToBase64String(aes.IV));
        }

        /// <summary>
        /// Decrypt using AES with provided key
        /// </summary>
        public string DecryptAES(string encryptedDataBase64, string keyBase64, string ivBase64)
        {
            var key = Convert.FromBase64String(keyBase64);
            var encryptedData = Convert.FromBase64String(encryptedDataBase64);
            var iv = Convert.FromBase64String(ivBase64);

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var msDecrypt = new MemoryStream(encryptedData);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);

            return srDecrypt.ReadToEnd();
        }

        /// <summary>
        /// Generate random AES key
        /// </summary>
        public string GenerateAESKey()
        {
            using var aes = Aes.Create();
            aes.KeySize = _aesKeySize;
            aes.GenerateKey();
            return Convert.ToBase64String(aes.Key);
        }

        #endregion

        #region RSA Only (Asymmetric)

        /// <summary>
        /// Encrypt small data with RSA (max ~200 bytes for RSA-2048)
        /// </summary>
        public string EncryptRSA(string plainText, string publicKeyBase64)
        {
            using var rsa = RSA.Create(_rsaKeySize);
            var publicKeyBytes = Convert.FromBase64String(publicKeyBase64);
            rsa.ImportRSAPublicKey(publicKeyBytes, out _);

            var dataBytes = Encoding.UTF8.GetBytes(plainText);
            var encryptedBytes = rsa.Encrypt(dataBytes, RSAEncryptionPadding.OaepSHA256);

            return Convert.ToBase64String(encryptedBytes);
        }

        /// <summary>
        /// Decrypt RSA encrypted data
        /// </summary>
        public string DecryptRSA(string encryptedDataBase64, string privateKeyBase64)
        {
            using var rsa = RSA.Create(_rsaKeySize);
            var privateKeyBytes = Convert.FromBase64String(privateKeyBase64);
            rsa.ImportRSAPrivateKey(privateKeyBytes, out _);

            var encryptedBytes = Convert.FromBase64String(encryptedDataBase64);
            var decryptedBytes = rsa.Decrypt(encryptedBytes, RSAEncryptionPadding.OaepSHA256);

            return Encoding.UTF8.GetString(decryptedBytes);
        }

        #endregion

        #region Digital Signature

        /// <summary>
        /// Sign data with RSA private key
        /// </summary>
        public string SignData(string data, string privateKeyBase64)
        {
            using var rsa = RSA.Create(_rsaKeySize);
            var privateKeyBytes = Convert.FromBase64String(privateKeyBase64);
            rsa.ImportRSAPrivateKey(privateKeyBytes, out _);

            var dataBytes = Encoding.UTF8.GetBytes(data);
            var signatureBytes = rsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            return Convert.ToBase64String(signatureBytes);
        }

        /// <summary>
        /// Verify signature with RSA public key
        /// </summary>
        public bool VerifySignature(string data, string signatureBase64, string publicKeyBase64)
        {
            using var rsa = RSA.Create(_rsaKeySize);
            var publicKeyBytes = Convert.FromBase64String(publicKeyBase64);
            rsa.ImportRSAPublicKey(publicKeyBytes, out _);

            var dataBytes = Encoding.UTF8.GetBytes(data);
            var signatureBytes = Convert.FromBase64String(signatureBase64);

            return rsa.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Compute SHA-256 hash of data
        /// </summary>
        public static string ComputeHash(string data)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hashBytes);
        }

        /// <summary>
        /// Generate random IV for AES
        /// </summary>
        public static string GenerateIV()
        {
            using var aes = Aes.Create();
            aes.GenerateIV();
            return Convert.ToBase64String(aes.IV);
        }

        #endregion

        public void Dispose()
        {
            _rsa?.Dispose();
            _rsa = null;
        }
    }

    /// <summary>
    /// Encrypted message wrapper
    /// </summary>
    public class EncryptedMessage
    {
        public string EncryptedContent { get; set; } = string.Empty;
        public string EncryptedAesKey { get; set; } = string.Empty;
        public string IV { get; set; } = string.Empty;
        public string? Signature { get; set; } // Optional digital signature
    }
}
