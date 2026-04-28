# PHẦN 7: HƯỚNG DẪN SỬ DỤNG VÀ MỞ RỘNG

## 7.1 Hướng dẫn sử dụng

### 7.1.1 Đăng ký tài khoản

1. Mở ChatClient
2. Click **"Đăng ký"**
3. Điền thông tin:
   - Tên đăng nhập (unique)
   - Mật khẩu
   - Email (để nhận OTP)
   - Họ và tên
   - Số điện thoại
4. Click **"Đăng ký"**
5. Kiểm tra email, nhập mã OTP 6 số
6. Đăng nhập với tài khoản vừa tạo

**Lưu ý:**

- MATK được tự động sinh (TK001, TK002, ...)
- Chức vụ mặc định: Thực tập sinh (CV005)
- Clearance Level mặc định: 1

### 7.1.2 Đăng nhập

1. Nhập username và password
2. Click **"Đăng nhập"**
3. Nếu chưa xác thực OTP → Nhập OTP từ email
4. Sau 5 lần sai → Tài khoản bị khóa 30 phút

### 7.1.3 Chat

**Chat riêng:**

1. Click **"Tin nhắn mới"** hoặc icon compose
2. Tìm kiếm user theo username
3. Chọn user → Tạo cuộc trò chuyện riêng
4. Nhập tin nhắn, click **"Gửi"**

**Chat nhóm:**

1. Click **"Tạo nhóm"**
2. Nhập tên nhóm
3. Thêm thành viên
4. Click **"Tạo"**

**Gửi file:**

1. Click icon đính kèm (📎)
2. Chọn file
3. File được mã hóa Hybrid (RSA+AES) trước khi gửi

### 7.1.4 Admin Panel (Server)

1. Đăng nhập với tài khoản Admin (giamdoc/123)
2. Mở **Admin Panel** từ menu

**Tab Users:**

- Xem danh sách tất cả users
- Tạo user mới
- Ban/Unban user
- Unlock tài khoản bị khóa
- Sửa thông tin user (chức vụ, phòng ban, clearance)

**Tab Conversations:**

- Xem tất cả cuộc trò chuyện
- Xóa cuộc trò chuyện

**Tab Audit Logs:**

- Xem nhật ký hoạt động
- Filter theo user, action, thời gian

**Tab Policies:**

- Quản lý VPD policies
- Quản lý FGA policies
- Xem MAC context

---

## 7.2 Hướng dẫn chỉnh sửa

### 7.2.1 Thay đổi Port Server

**File: ChatServer/Program.cs**

```csharp
// Thay đổi port từ 9000 sang port khác
var server = new SocketServerService(chatService, 9001);
```

**File: ChatClient/Services/SocketClientService.cs**

```csharp
// Đồng bộ với server
private const int ServerPort = 9001;
```

### 7.2.2 Thay đổi Database Connection

**File: ChatServer/Database/DbContext.cs**

```csharp
private const string ConnectionString =
    "User Id=YOUR_USER;" +
    "Password=YOUR_PASSWORD;" +
    "Data Source=(DESCRIPTION=" +
        "(ADDRESS=(PROTOCOL=TCP)(HOST=YOUR_HOST)(PORT=1521))" +
        "(CONNECT_DATA=(SERVICE_NAME=YOUR_SERVICE)))";
```

### 7.2.3 Thay đổi AES Key

**QUAN TRỌNG:** Phải thay đổi ở CẢ HAI file:

**File: ChatServer/Utils/EncryptionHelper.cs**

```csharp
private static readonly byte[] AesKey =
    Encoding.UTF8.GetBytes("YOUR_NEW_32_BYTE_KEY_HERE!@#$"); // Đúng 32 chars
private static readonly byte[] AesIv =
    Encoding.UTF8.GetBytes("YOUR_16BYTE_IV!!");  // Đúng 16 chars
```

**File: ChatClient/Utils/EncryptionHelper.cs**

```csharp
// PHẢI GIỐNG SERVER
private static readonly byte[] AesKey =
    Encoding.UTF8.GetBytes("YOUR_NEW_32_BYTE_KEY_HERE!@#$");
private static readonly byte[] AesIv =
    Encoding.UTF8.GetBytes("YOUR_16BYTE_IV!!");
```

### 7.2.4 Thay đổi Email Service

**File: ChatServer/Services/EmailService.cs**

```csharp
private const string SmtpHost = "smtp.gmail.com";  // Hoặc SMTP server khác
private const int SmtpPort = 587;
private const string SenderEmail = "your-email@gmail.com";
private const string SenderPassword = "your-app-password";
```

### 7.2.5 Thêm Action mới

**Bước 1: Thêm handler ở Server**

```csharp
// File: ChatServer/Services/ChatProcessingService.cs

// Trong switch statement:
"YourNewAction" => await HandleYourNewActionAsync(request),

// Thêm method:
private async Task<string> HandleYourNewActionAsync(ChatRequest request)
{
    // Your logic here
    return JsonSerializer.Serialize(new ServerResponse
    {
        Success = true,
        Message = "Done"
    });
}
```

**Bước 2: Thêm method ở Client**

```csharp
// File: ChatClient/Services/SocketClientService.cs

public async Task<ServerResponse?> YourNewActionAsync(User user, string param)
{
    var request = new ChatRequest
    {
        Action = "YourNewAction",
        SenderUsername = user.Username,
        Content = param
    };

    var responseJson = await SendRequestAsync(request);
    if (responseJson == null) return null;

    return JsonSerializer.Deserialize<ServerResponse>(responseJson);
}
```

### 7.2.6 Thêm trường mới vào ChatRequest

**File: ChatServer/Services/ChatProcessingService.cs (cuối file)**

```csharp
public class ChatRequest
{
    // ... existing fields ...

    // Thêm trường mới
    public string YourNewField { get; set; } = string.Empty;
}
```

**File: ChatClient/Services/SocketClientService.cs (cuối file)**

```csharp
public class ChatRequest
{
    // ... existing fields ...

    // PHẢI GIỐNG SERVER
    public string YourNewField { get; set; } = string.Empty;
}
```

### 7.2.7 Thêm VPD Policy mới

**File: ChatServer/Database/04_policies.sql**

```sql
-- Tạo policy function
CREATE OR REPLACE FUNCTION VPD_YOUR_TABLE_READ(
    p_schema VARCHAR2,
    p_obj VARCHAR2
) RETURN VARCHAR2 AS
BEGIN
    -- Return WHERE clause
    RETURN 'YOUR_COLUMN = ''' || SYS_CONTEXT('MAC_CTX', 'USERNAME') || '''';
END;
/

-- Đăng ký policy
BEGIN
    DBMS_RLS.ADD_POLICY(
        object_schema   => USER,
        object_name     => 'YOUR_TABLE',
        policy_name     => 'VPD_YOUR_TABLE_POLICY',
        function_schema => USER,
        policy_function => 'VPD_YOUR_TABLE_READ',
        statement_types => 'SELECT'
    );
END;
/
```

### 7.2.8 Thêm FGA Policy mới

```sql
BEGIN
    DBMS_FGA.ADD_POLICY(
        object_schema   => USER,
        object_name     => 'YOUR_TABLE',
        policy_name     => 'FGA_YOUR_TABLE_AUDIT',
        audit_condition => 'YOUR_COLUMN = ''sensitive_value''',
        audit_column    => 'YOUR_COLUMN',
        statement_types => 'SELECT,INSERT,UPDATE,DELETE',
        enable          => TRUE
    );
END;
/
```

---

## 7.3 Troubleshooting

### 7.3.1 Lỗi kết nối Database

**Lỗi: ORA-12541: TNS:no listener**

```bash
# Khởi động listener
lsnrctl start
```

**Lỗi: ORA-01017: invalid username/password**

```sql
-- Reset password
ALTER USER ChatApplication IDENTIFIED BY "123";
```

**Lỗi: ORA-28001: password has expired**

```sql
-- Tắt password expiration
ALTER PROFILE CHAT_ADMIN_PROFILE LIMIT PASSWORD_LIFE_TIME UNLIMITED;
ALTER USER ChatApplication IDENTIFIED BY "123";
```

### 7.3.2 Lỗi VPD không hoạt động

**Kiểm tra context đã được set:**

```sql
SELECT SYS_CONTEXT('MAC_CTX', 'USERNAME') AS USERNAME,
       SYS_CONTEXT('MAC_CTX', 'CLEARANCE_LEVEL') AS LEVEL
FROM DUAL;
```

**Nếu NULL, gọi procedure:**

```sql
BEGIN SET_MAC_CONTEXT('TK001', 3); END;
/
```

### 7.3.3 Lỗi mã hóa

**Lỗi: Padding is invalid**

- Kiểm tra KEY và IV giống nhau ở Client và Server
- Kiểm tra encoding UTF-8

**Lỗi: Invalid Base64**

- Kiểm tra message không bị truncate
- Kiểm tra newline character

### 7.3.4 Lỗi Email OTP

**Lỗi: Authentication failed**

1. Bật 2-Step Verification trên Google Account
2. Tạo App Password (không dùng password thường)
3. Cập nhật SenderPassword trong EmailService.cs

**Lỗi: SMTP timeout**

- Kiểm tra firewall cho port 587
- Thử port 465 với SSL

---

## 7.4 Console Log Reference

### 7.4.1 AES Socket Communication

```
[SERVER][AES] <<< FROM CLIENT (encrypted): ...   # Server nhận từ client
[SERVER][AES] --- DECRYPTED: ...                  # Sau khi giải mã
[SERVER][AES] >>> TO CLIENT (encrypted): ...     # Server gửi về client
```

### 7.4.2 RSA Signature

```
[SERVER][RSA] SIGN: data=... => sig=...          # Tạo chữ ký
[SERVER][RSA] VERIFY: data=... => VALID/INVALID  # Xác thực chữ ký
[SERVER][RSA] VERIFY with client key: VALID      # Verify với public key client
```

### 7.4.3 Hybrid Encryption

```
[SERVER][HYBRID] ENCRYPT START: dataSize=37446 bytes
[SERVER][HYBRID] Generated AES-256 session key
[SERVER][HYBRID][AES] Data encrypted: 37446 => 37456 bytes
[SERVER][HYBRID][RSA] AES key encrypted: 32 => 256 bytes
[SERVER][HYBRID] ENCRYPT DONE: totalSize=50314 chars
```

### 7.4.4 VPD Context

```
[VPD] Set context: MATK=TK001, Level=3
```

---

## 7.5 Security Best Practices

### 7.5.1 Production Deployment

1. **Thay đổi tất cả default passwords**
2. **Thay đổi AES Key và IV**
3. **Dùng HTTPS/TLS cho socket connection**
4. **Cấu hình Oracle Wallet cho credential**
5. **Enable Oracle Audit Trail**
6. **Regular backup database**

### 7.5.2 Key Rotation

1. Generate new AES key
2. Update cả Server và Client
3. Restart applications
4. Old messages vẫn đọc được (lưu plaintext trong DB)

### 7.5.3 RSA Key Management

```csharp
// Mỗi user có thể có RSA key pair riêng
// Lưu public key trong NGUOIDUNG table
// Private key lưu encrypted ở client

ALTER TABLE NGUOIDUNG ADD (
    RSA_PUBLIC_KEY CLOB,
    RSA_KEY_CREATED TIMESTAMP
);
```

---

## 7.6 Performance Tuning

### 7.6.1 Database Indexes

```sql
-- Đã có trong 02_schema.sql
CREATE INDEX IDX_TINNHAN_MACTC ON TINNHAN(MACTC);
CREATE INDEX IDX_TINNHAN_SECURITY ON TINNHAN(SECURITYLABEL);
CREATE INDEX IDX_THANHVIEN_MATK ON THANHVIEN(MATK);
```

### 7.6.2 Connection Pooling

```csharp
// Trong connection string
"Min Pool Size=5;Max Pool Size=50;Connection Timeout=30;"
```

### 7.6.3 Message Pagination

```csharp
// Client request
request.Limit = 50;  // Chỉ lấy 50 messages mới nhất

// Server query
SELECT * FROM (
    SELECT * FROM TINNHAN
    WHERE MACTC = :mactc
    ORDER BY THOIGIAN DESC
) WHERE ROWNUM <= :limit
```

---

_HẾT TÀI LIỆU_

---

**Thông tin liên hệ:**

- Project: ChatApplication
- Version: 1.0.0
- Last Updated: 2025-01-10
