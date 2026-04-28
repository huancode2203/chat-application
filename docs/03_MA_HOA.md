# PHẦN 3: HỆ THỐNG MÃ HÓA

## 3.1 Tổng quan 3 lớp mã hóa

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         HỆ THỐNG MÃ HÓA 3 LỚP                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  LAYER 1: AES-256-CBC (Symmetric)                                          │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ Mục đích: Mã hóa toàn bộ socket communication                       │   │
│  │ Key: Cố định, chia sẻ giữa Client và Server                         │   │
│  │ Tốc độ: Rất nhanh (phù hợp cho dữ liệu lớn)                         │   │
│  │ File: EncryptionHelper.cs                                           │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  LAYER 2: RSA-2048 (Asymmetric)                                            │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ Mục đích: Chữ ký số, xác thực người gửi                             │   │
│  │ Key: Public/Private key pair                                         │   │
│  │ Tốc độ: Chậm (chỉ cho dữ liệu nhỏ < 200 bytes)                      │   │
│  │ File: EncryptionHelper.cs                                           │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  LAYER 3: HYBRID (RSA + AES)                                               │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ Mục đích: Mã hóa file/attachment lớn                                │   │
│  │ Cách hoạt động: AES encrypt data, RSA encrypt AES key               │   │
│  │ Tốc độ: Nhanh + Bảo mật cao                                         │   │
│  │ File: EncryptionHelper.cs, EncryptionService.cs                     │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 3.2 AES-256-CBC (Layer 1)

### 3.2.1 Cấu hình

**File: ChatServer/Utils/EncryptionHelper.cs (Line 28-49)**

```csharp
#region ========== 1. MÃ HÓA ĐỐI XỨNG (SYMMETRIC - AES-256-CBC) ==========

// Key và IV cố định - PHẢI GIỐNG NHAU Ở CLIENT VÀ SERVER
private static readonly byte[] AesKey =
    Encoding.UTF8.GetBytes("ChatApp_AES_Key_32bytes_Long!@#$"); // 32 bytes = 256-bit
private static readonly byte[] AesIv =
    Encoding.UTF8.GetBytes("ChatApp_AES_IV!!");  // 16 bytes = 128-bit

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
```

### 3.2.2 Hàm AesEncrypt chi tiết

**File: ChatServer/Utils/EncryptionHelper.cs (Line 54-89)**

```csharp
/// <summary>
/// AES Encrypt với custom key/iv
/// </summary>
public static string AesEncrypt(string plainText, byte[] key, byte[] iv)
{
    // 1. Tạo AES instance
    using var aes = Aes.Create();
    aes.Key = key;           // 256-bit key
    aes.IV = iv;             // 128-bit IV
    aes.Mode = CipherMode.CBC;        // Cipher Block Chaining
    aes.Padding = PaddingMode.PKCS7;  // Padding scheme

    // 2. Tạo encryptor
    using var encryptor = aes.CreateEncryptor();

    // 3. Mã hóa qua CryptoStream
    using var ms = new MemoryStream();
    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
    using (var sw = new StreamWriter(cs, Encoding.UTF8))
    {
        sw.Write(plainText);
    }

    // 4. Convert sang Base64 để truyền qua socket
    return Convert.ToBase64String(ms.ToArray());
}

/// <summary>
/// AES Decrypt với custom key/iv
/// </summary>
public static string AesDecrypt(string cipherText, byte[] key, byte[] iv)
{
    // 1. Convert từ Base64 về bytes
    var buffer = Convert.FromBase64String(cipherText);

    // 2. Tạo AES instance
    using var aes = Aes.Create();
    aes.Key = key;
    aes.IV = iv;
    aes.Mode = CipherMode.CBC;
    aes.Padding = PaddingMode.PKCS7;

    // 3. Tạo decryptor và giải mã
    using var decryptor = aes.CreateDecryptor();
    using var ms = new MemoryStream(buffer);
    using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
    using var sr = new StreamReader(cs, Encoding.UTF8);

    return sr.ReadToEnd();
}
```

### 3.2.3 Luồng hoạt động

```
┌────────────────────────────────────────────────────────────────────────────┐
│                          AES SOCKET FLOW                                   │
├────────────────────────────────────────────────────────────────────────────┤
│                                                                            │
│   CLIENT                                          SERVER                   │
│   ──────                                          ──────                   │
│                                                                            │
│   1. Tạo JSON Request                                                      │
│      {"Action":"Login",                                                    │
│       "SenderUsername":"giamdoc",                                          │
│       "Password":"123"}                                                    │
│            │                                                               │
│            ▼                                                               │
│   2. EncryptionHelper.Encrypt(json)                                        │
│      Input:  {"Action":"Login",...}                                        │
│      Output: "UCs1VxKDcvgWVV2I5klQ6mDz..."                                │
│            │                                                               │
│            │  TCP Socket                                                   │
│            └──────────────────────────────────►                           │
│                                                   │                        │
│                                                   ▼                        │
│                                          3. EncryptionHelper.Decrypt()     │
│                                             Input:  "UCs1VxKDcvgWVV2I..."  │
│                                             Output: {"Action":"Login",...} │
│                                                   │                        │
│                                                   ▼                        │
│                                          4. Process Request                │
│                                                   │                        │
│                                                   ▼                        │
│                                          5. EncryptionHelper.Encrypt()     │
│                                             Response JSON → Encrypted      │
│            ◄──────────────────────────────────────┘                        │
│            │                                                               │
│            ▼                                                               │
│   6. EncryptionHelper.Decrypt()                                            │
│      Display response to user                                              │
│                                                                            │
└────────────────────────────────────────────────────────────────────────────┘
```

### 3.2.4 Console Log

**File: ChatServer/Services/SocketServerService.cs (Line 66-75)**

```csharp
// === ENCRYPTION LOG ===
Console.WriteLine($"[SERVER][AES] <<< FROM CLIENT (encrypted): {encryptedLine.Substring(0, 60)}...");

var json = EncryptionHelper.Decrypt(encryptedLine);
Console.WriteLine($"[SERVER][AES] --- DECRYPTED: {json.Substring(0, 100)}...");

var responseJson = await _chatProcessingService.HandleRequestAsync(json);
var responseEncrypted = EncryptionHelper.Encrypt(responseJson);

Console.WriteLine($"[SERVER][AES] >>> TO CLIENT (encrypted): {responseEncrypted.Substring(0, 60)}...");
```

**Output mẫu:**

```
[SERVER][AES] <<< FROM CLIENT (encrypted): UCs1VxKDcvgWVV2I5klQ6mDzhWtGsodeNcgBPMb9dZzC...
[SERVER][AES] --- DECRYPTED: {"Action":"Login","SenderUsername":"giamdoc","Password":"123"...
[SERVER][AES] >>> TO CLIENT (encrypted): 4VLy2C6jxEC7tsf4wzmf0rSZDlypl72YaPtPv1fv+3+5aKpL...
```

---

## 3.3 RSA-2048 (Layer 2)

### 3.3.1 Tạo RSA Key Pair

**File: ChatServer/Utils/EncryptionHelper.cs (Line 291-300)**

```csharp
/// <summary>
/// Tạo RSA key pair mới (2048-bit)
/// </summary>
public static (string publicKey, string privateKey) GenerateRsaKeyPair()
{
    using var rsa = RSA.Create(2048);  // 2048-bit key
    return (
        Convert.ToBase64String(rsa.ExportRSAPublicKey()),   // Public key
        Convert.ToBase64String(rsa.ExportRSAPrivateKey())   // Private key (BẢO MẬT!)
    );
}
```

### 3.3.2 Chữ ký số (Digital Signature)

**File: ChatServer/Utils/EncryptionHelper.cs (Line 215-244)**

```csharp
/// <summary>
/// RSA Sign - Tạo chữ ký số
/// Dùng để xác thực người gửi message
/// </summary>
public static string RsaSign(string data)
{
    var rsa = GetRsa();  // Singleton RSA instance
    var dataBytes = Encoding.UTF8.GetBytes(data);

    // Sign với SHA256 hash
    var signature = rsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    var result = Convert.ToBase64String(signature);

    Console.WriteLine($"[SERVER][RSA] SIGN: data={data.Substring(0, 30)}... => sig={result.Substring(0, 40)}...");
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
        Console.WriteLine($"[SERVER][RSA] VERIFY: data={data.Substring(0, 30)}... => {(result ? "VALID" : "INVALID")}");
        return result;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[SERVER][RSA] VERIFY ERROR: {ex.Message}");
        return false;
    }
}
```

### 3.3.3 Xác thực Login với RSA Signature

**File: ChatServer/Services/ChatProcessingService.cs (Line 162-186)**

```csharp
private async Task<string> HandleLoginAsync(ChatRequest request)
{
    // ... validation ...

    // ========== RSA SIGNATURE VERIFICATION (Optional) ==========
    // Nếu client gửi signature, verify để xác thực người gửi
    if (!string.IsNullOrEmpty(request.Signature) && !string.IsNullOrEmpty(request.PublicKey))
    {
        try
        {
            // Data được sign: "username:password"
            var dataToVerify = $"{request.SenderUsername}:{request.Password}";

            // Verify với public key từ client
            var isValid = EncryptionHelper.RsaVerifyWithPublicKey(
                dataToVerify,
                request.Signature,
                request.PublicKey
            );

            if (!isValid)
            {
                Console.WriteLine($"[SERVER][RSA] INVALID signature for login: {request.SenderUsername}");
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "RSA signature verification failed. Request may be tampered."
                });
            }
            Console.WriteLine($"[SERVER][RSA] VALID signature for login: {request.SenderUsername}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SERVER][RSA] Signature verification error: {ex.Message}");
            // Continue without signature verification if error
        }
    }

    // ... continue login process ...
}
```

---

## 3.4 Hybrid Encryption (Layer 3)

### 3.4.1 Nguyên lý hoạt động

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                       HYBRID ENCRYPTION                                     │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   Vấn đề:                                                                   │
│   - RSA chậm, chỉ mã hóa được < 200 bytes                                  │
│   - AES nhanh nhưng cần key exchange an toàn                               │
│                                                                             │
│   Giải pháp: Kết hợp cả hai!                                               │
│                                                                             │
│   ┌─────────────────────────────────────────────────────────────────────┐  │
│   │  ENCRYPT:                                                            │  │
│   │                                                                      │  │
│   │  1. Generate random AES-256 session key (32 bytes)                  │  │
│   │                     │                                                │  │
│   │                     ▼                                                │  │
│   │  2. Encrypt FILE with AES (fast)                                    │  │
│   │     [FILE 10MB] ─── AES ───► [ENCRYPTED_DATA 10MB]                  │  │
│   │                     │                                                │  │
│   │                     ▼                                                │  │
│   │  3. Encrypt AES KEY with RSA (secure)                               │  │
│   │     [AES_KEY 32B] ─── RSA ───► [ENCRYPTED_KEY 256B]                 │  │
│   │                     │                                                │  │
│   │                     ▼                                                │  │
│   │  4. Package: ENCRYPTED_DATA | ENCRYPTED_KEY | IV                    │  │
│   │                                                                      │  │
│   └─────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│   ┌─────────────────────────────────────────────────────────────────────┐  │
│   │  DECRYPT:                                                            │  │
│   │                                                                      │  │
│   │  1. Split package: DATA | KEY | IV                                  │  │
│   │  2. Decrypt AES KEY with RSA private key                            │  │
│   │  3. Decrypt DATA with AES key                                       │  │
│   │                                                                      │  │
│   └─────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 3.4.2 Code HybridEncrypt

**File: ChatServer/Utils/EncryptionHelper.cs (Line 352-384)**

```csharp
/// <summary>
/// Hybrid Encrypt - Mã hóa file/attachment lớn
/// Returns: encryptedData|encryptedAesKey|iv (all base64, separated by |)
/// </summary>
public static string HybridEncrypt(byte[] data)
{
    Console.WriteLine($"[SERVER][HYBRID] ENCRYPT START: dataSize={data.Length} bytes");

    // 1. Tạo session key ngẫu nhiên (mỗi file 1 key riêng)
    using var aes = Aes.Create();
    aes.KeySize = 256;
    aes.GenerateKey();  // Random 256-bit key
    aes.GenerateIV();   // Random 128-bit IV
    Console.WriteLine($"[SERVER][HYBRID] Generated AES-256 session key");

    // 2. Mã hóa data bằng AES (nhanh)
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

    // 3. Mã hóa AES key bằng RSA (bảo mật key exchange)
    var rsa = GetRsa();
    var encryptedKey = rsa.Encrypt(aes.Key, RSAEncryptionPadding.OaepSHA256);
    Console.WriteLine($"[SERVER][HYBRID][RSA] AES key encrypted: 32 => {encryptedKey.Length} bytes");

    // 4. Đóng gói: data|key|iv
    var result = $"{Convert.ToBase64String(encryptedData)}|{Convert.ToBase64String(encryptedKey)}|{Convert.ToBase64String(aes.IV)}";
    Console.WriteLine($"[SERVER][HYBRID] ENCRYPT DONE: totalSize={result.Length} chars");
    return result;
}
```

### 3.4.3 Code HybridDecrypt

**File: ChatServer/Utils/EncryptionHelper.cs (Line 398-428)**

```csharp
/// <summary>
/// Hybrid Decrypt - Giải mã file/attachment
/// </summary>
public static byte[] HybridDecrypt(string encryptedPackage)
{
    Console.WriteLine($"[SERVER][HYBRID] DECRYPT START: packageSize={encryptedPackage.Length} chars");

    // 1. Tách package
    var parts = encryptedPackage.Split('|');
    if (parts.Length != 3)
        throw new ArgumentException("Invalid encrypted package format");

    var encryptedData = Convert.FromBase64String(parts[0]);  // Encrypted file data
    var encryptedKey = Convert.FromBase64String(parts[1]);   // Encrypted AES key
    var iv = Convert.FromBase64String(parts[2]);             // IV

    // 2. Giải mã AES key bằng RSA private key
    var rsa = GetRsa();
    var aesKey = rsa.Decrypt(encryptedKey, RSAEncryptionPadding.OaepSHA256);
    Console.WriteLine($"[SERVER][HYBRID][RSA] AES key decrypted: {encryptedKey.Length} => 32 bytes");

    // 3. Giải mã data bằng AES
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
```

---

## 3.5 Cách chỉnh sửa/mở rộng

### 3.5.1 Thay đổi AES Key

```csharp
// File: ChatServer/Utils/EncryptionHelper.cs
// VÀ File: ChatClient/Utils/EncryptionHelper.cs (phải giống nhau!)

// Key phải đúng 32 bytes cho AES-256
private static readonly byte[] AesKey =
    Encoding.UTF8.GetBytes("YOUR_NEW_32_BYTE_KEY_HERE!!!!!!!"); // 32 chars

// IV phải đúng 16 bytes
private static readonly byte[] AesIv =
    Encoding.UTF8.GetBytes("YOUR_16BYTE_IV!!");  // 16 chars
```

### 3.5.2 Thêm RSA Signature cho action khác

```csharp
// File: ChatServer/Services/ChatProcessingService.cs

private async Task<string> HandleSendMessageAsync(ChatRequest request)
{
    // Thêm RSA verification
    if (!string.IsNullOrEmpty(request.Signature))
    {
        var dataToVerify = $"{request.SenderUsername}:{request.Content}";
        if (!EncryptionHelper.RsaVerifyWithPublicKey(dataToVerify, request.Signature, request.PublicKey))
        {
            return JsonSerializer.Serialize(new ServerResponse
            {
                Success = false,
                Message = "Message signature invalid"
            });
        }
    }

    // ... process message ...
}
```

### 3.5.3 Thêm End-to-End Encryption

```csharp
// Mã hóa tin nhắn cho người nhận cụ thể
public static string EncryptForRecipient(string message, string recipientPublicKey)
{
    return HybridEncryptForRecipient(
        Encoding.UTF8.GetBytes(message),
        recipientPublicKey
    );
}

// Người nhận giải mã với private key của họ
public static string DecryptWithPrivateKey(string encryptedPackage, string privateKey)
{
    var bytes = HybridDecryptWithPrivateKey(encryptedPackage, privateKey);
    return Encoding.UTF8.GetString(bytes);
}
```

---

_Tiếp theo: 04_DATABASE.md - Oracle Database Security_
