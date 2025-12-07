# 🔐 HƯỚNG DẪN MÃ HÓA - CHAT APPLICATION

## Tổng quan kiến trúc mã hóa 3 lớp

Hệ thống sử dụng **3 phương pháp mã hóa** khác nhau cho các mục đích khác nhau:

---

## 1. 🔵 AES-256 (Symmetric) - Socket Communication

### Mục đích

Mã hóa **tất cả dữ liệu** truyền qua socket giữa Client và Server.

### Vị trí

- **Server**: `ChatServer/Utils/EncryptionHelper.cs` - `Encrypt()` / `Decrypt()`
- **Client**: `ChatClient/Utils/EncryptionHelper.cs` - `Encrypt()` / `Decrypt()`

### Sử dụng

```csharp
// Client gửi request
var json = JsonSerializer.Serialize(request);
var encrypted = EncryptionHelper.Encrypt(json);  // AES-256
await _writer.WriteLineAsync(encrypted);

// Server nhận và giải mã
var json = EncryptionHelper.Decrypt(encryptedLine);  // AES-256
```

### Đặc điểm

- **Key cố định**: `ChatApp_AES_Key_32bytes_Long!@#$` (32 bytes)
- **IV cố định**: `ChatApp_AES_IV!!` (16 bytes)
- **Lưu ý**: Key và IV phải **GIỐNG NHAU** giữa Client và Server
- **Ưu điểm**: Nhanh, hiệu quả cho real-time communication
- **Nhược điểm**: Key tĩnh (nên rotate key định kỳ trong production)

---

## 2. 🔴 RSA-2048 (Asymmetric) - Digital Signatures

### Mục đích

Ký số và xác thực **tính toàn vẹn** của dữ liệu quan trọng.

### Vị trí

- `EncryptionHelper.cs` - `RsaSign()` / `RsaVerify()`

### Sử dụng

```csharp
// Ký dữ liệu
var signature = EncryptionHelper.RsaSign(data);

// Xác minh chữ ký
bool isValid = EncryptionHelper.RsaVerify(data, signature);
```

### Đặc điểm

- **Key size**: 2048 bits
- **Hash**: SHA-256
- **Padding**: PKCS1
- **Sử dụng cho**: Login, admin actions, critical operations

---

## 3. 🟢 HYBRID (RSA + AES) - File Attachments

### Mục đích

Mã hóa **file đính kèm** (có thể lớn).

### Vị trí

- `EncryptionHelper.cs` - `HybridEncrypt()` / `HybridDecrypt()`

### Cách hoạt động

1. **Upload**:

   ```csharp
   // Tạo random AES key
   // Mã hóa file bằng AES-256
   // Mã hóa AES key bằng RSA-2048
   var encryptedPackage = EncryptionHelper.HybridEncrypt(fileBytes);
   // Package format: "encryptedData|encryptedKey|iv"
   ```

2. **Download**:
   ```csharp
   // Giải mã RSA key
   // Giải mã file bằng AES
   var fileBytes = EncryptionHelper.HybridDecrypt(encryptedPackage);
   ```

### Đặc điểm

- **AES**: 256-bit key, random cho mỗi file
- **RSA**: 2048-bit, mã hóa AES key
- **Format**: `base64(encryptedData)|base64(encryptedKey)|base64(iv)`
- **Ưu điểm**: An toàn, không giới hạn kích thước file
- **Lưu trữ**: Package được lưu vào `ATTACHMENT.FILEDATA` (BLOB)

---

## 📊 So sánh

| Loại         | Mục đích             | Tốc độ       | Bảo mật      | Key     |
| ------------ | -------------------- | ------------ | ------------ | ------- |
| **AES-256**  | Socket communication | ⚡ Rất nhanh | 🔒 Cao       | Static  |
| **RSA-2048** | Digital signatures   | 🐌 Chậm      | 🔒🔒 Rất cao | Dynamic |
| **Hybrid**   | File attachments     | ⚡ Nhanh     | 🔒🔒 Rất cao | Random  |

---

## 🗄️ Database Schema

### Bảng ATTACHMENT

```sql
CREATE TABLE ATTACHMENT (
  ATTACH_ID   NUMBER PRIMARY KEY,
  MATK        VARCHAR2(20),
  FILENAME    VARCHAR2(255),
  MIMETYPE    VARCHAR2(200),
  FILESIZE    NUMBER,
  FILEDATA    BLOB,              -- Chứa encrypted package
  IS_ENCRYPTED NUMBER(1) DEFAULT 0,  -- 1 = encrypted
  ENCRYPTION_KEY VARCHAR2(500),  -- (Không dùng cho hybrid)
  ENCRYPTION_IV VARCHAR2(100),   -- (Không dùng cho hybrid)
  UPLOADED_AT TIMESTAMP
);
```

### Stored Procedures

- `SP_UPLOAD_ATTACHMENT` - Upload file (có flag IS_ENCRYPTED)
- `SP_UPLOAD_ATTACHMENT_ENCRYPTED` - Upload với key/IV riêng (legacy)

---

## 🔍 Kiểm tra mã hóa hoạt động

### 1. Kiểm tra file đã mã hóa

```sql
SELECT ATTACH_ID, FILENAME, IS_ENCRYPTED, LENGTH(FILEDATA) AS ENCRYPTED_SIZE
FROM ATTACHMENT
WHERE IS_ENCRYPTED = 1;
```

### 2. Xem raw data (sẽ thấy gibberish)

```sql
SELECT DBMS_LOB.SUBSTR(FILEDATA, 100, 1) AS ENCRYPTED_PREVIEW
FROM ATTACHMENT
WHERE ATTACH_ID = 1;
```

### 3. Test upload/download

- Upload file qua ChatClient
- Kiểm tra `IS_ENCRYPTED = 1` trong database
- Download file và verify nội dung giống file gốc

---

## ⚠️ Lưu ý bảo mật

1. **AES Static Key**: Trong production, nên:

   - Rotate key định kỳ
   - Sử dụng key exchange protocol (Diffie-Hellman)
   - Lưu key trong secure vault (Azure Key Vault, AWS KMS)

2. **RSA Key Management**:

   - Private key KHÔNG BAO GIỜ gửi qua mạng
   - Public key lưu trong `TAIKHOAN.PUBLIC_KEY`
   - Backup private key an toàn

3. **Hybrid Encryption**:
   - Mỗi file có AES key riêng (random)
   - Không thể decrypt nếu mất RSA private key
   - Nên backup encryption keys

---

## 📝 Code Examples

### Upload Encrypted File

```csharp
// Client
var fileBytes = await File.ReadAllBytesAsync(filePath);
var response = await _socketClient.UploadAttachmentAsync(user, fileName, fileBytes);

// Server (ChatProcessingService.cs)
var encryptedPackage = EncryptionHelper.HybridEncrypt(bytes);
var encryptedBytes = Encoding.UTF8.GetBytes(encryptedPackage);
await _dbContext.UploadAttachmentAsync(matk, fileName, mimeType, fileSize, encryptedBytes, 1);
```

### Download Encrypted File

```csharp
// Server (DbContext.cs)
var encryptedPackage = Encoding.UTF8.GetString(encryptedData);
var decryptedData = EncryptionHelper.HybridDecrypt(encryptedPackage);
return (attachmentId, fileName, mimeType, fileSize, decryptedData);

// Client nhận data đã decrypt sẵn
```

---

## 🎯 Best Practices

1. ✅ **Luôn mã hóa** file nhạy cảm
2. ✅ **Verify signature** cho critical operations
3. ✅ **Log audit** mọi encryption/decryption operations
4. ✅ **Kiểm tra clearance level** trước khi decrypt
5. ✅ **Handle errors** gracefully (fallback to unencrypted nếu cần)

---

**Tác giả**: ChatApplication Team  
**Cập nhật**: December 2025  
**Version**: 1.0
