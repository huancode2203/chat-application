# ChatApplication - Ứng dụng Chat Bảo mật với Oracle Database

## Tổng quan

Ứng dụng chat desktop được xây dựng trên .NET 9.0 với các tính năng bảo mật đa lớp:

- **Mã hóa 3 lớp**: AES-256 (Socket), RSA-2048 (Chữ ký số), Hybrid RSA+AES (File)
- **Oracle Database Security**: VPD, FGA, MAC/DAC, RBAC, Profiles
- **Xác thực OTP** qua Email
- **Audit Logging** toàn diện

---

## Kiến trúc hệ thống

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           CHAT APPLICATION                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   ┌─────────────┐         TCP/Socket           ┌─────────────────────┐     │
│   │   CLIENT    │ ◄────────────────────────► │      SERVER         │     │
│   │  (WinForms) │     AES-256-CBC Encrypted   │    (WinForms)       │     │
│   └─────────────┘                              └──────────┬──────────┘     │
│                                                           │                 │
│                                                           ▼                 │
│                                                 ┌─────────────────────┐     │
│                                                 │   ORACLE DATABASE   │     │
│                                                 │  (VPD/FGA/MAC/DAC)  │     │
│                                                 └─────────────────────┘     │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 1. HỆ THỐNG MÃ HÓA

### 1.1 AES-256-CBC (Symmetric Encryption)

**Mục đích**: Mã hóa toàn bộ giao tiếp socket giữa Client và Server.

**File**:

- `ChatServer/Utils/EncryptionHelper.cs`
- `ChatClient/Utils/EncryptionHelper.cs`

**Cách hoạt động**:

```
CLIENT                                    SERVER
   │                                         │
   │ 1. JSON Request                         │
   │    {"Action":"Login",...}               │
   │         │                               │
   │         ▼                               │
   │ 2. AES Encrypt                          │
   │    EncryptionHelper.Encrypt()           │
   │         │                               │
   │         ▼                               │
   │ ════════════════════════════════════════│
   │     Encrypted Base64 String             │
   │ ════════════════════════════════════════│
   │                                         │
   │                                    3. AES Decrypt
   │                                    EncryptionHelper.Decrypt()
   │                                         │
   │                                         ▼
   │                                    4. Process JSON
   │                                         │
   │                                         ▼
   │                                    5. AES Encrypt Response
   │ ◄═══════════════════════════════════════│
   │     Encrypted Response                  │
   │         │                               │
   │         ▼                               │
   │ 6. AES Decrypt                          │
   │    Display to user                      │
   └─────────────────────────────────────────┘
```

**Code trích dẫn**:

```csharp
// ChatServer/Utils/EncryptionHelper.cs (Line 32-49)
private static readonly byte[] AesKey = Encoding.UTF8.GetBytes("ChatApp_AES_Key_32bytes_Long!@#$"); // 32 bytes = 256-bit
private static readonly byte[] AesIv = Encoding.UTF8.GetBytes("ChatApp_AES_IV!!");  // 16 bytes

public static string Encrypt(string plainText)
{
    return AesEncrypt(plainText, AesKey, AesIv);
}

public static string Decrypt(string cipherText)
{
    return AesDecrypt(cipherText, AesKey, AesIv);
}
```

```csharp
// ChatServer/Services/SocketServerService.cs (Line 66-77)
// === ENCRYPTION LOG ===
Console.WriteLine($"[SERVER][AES] <<< FROM CLIENT (encrypted): {encryptedLine.Substring(0, Math.Min(60, encryptedLine.Length))}...");

var json = EncryptionHelper.Decrypt(encryptedLine);
Console.WriteLine($"[SERVER][AES] --- DECRYPTED: {json.Substring(0, Math.Min(100, json.Length))}...");

var responseJson = await _chatProcessingService.HandleRequestAsync(json);
var responseEncrypted = EncryptionHelper.Encrypt(responseJson);

Console.WriteLine($"[SERVER][AES] >>> TO CLIENT (encrypted): {responseEncrypted.Substring(0, Math.Min(60, responseEncrypted.Length))}...");
```

**Console Output mẫu**:

```
[SERVER][AES] <<< FROM CLIENT (encrypted): UCs1VxKDcvgWVV2I5klQ6mDzhWtGsode...
[SERVER][AES] --- DECRYPTED: {"Action":"Login","SenderUsername":"giamdoc",...
[SERVER][AES] >>> TO CLIENT (encrypted): 4VLy2C6jxEC7tsf4wzmf0rSZDlypl72Y...
```

---

### 1.2 RSA-2048 (Asymmetric Encryption)

**Mục đích**: Chữ ký số để xác thực người gửi, trao đổi khóa an toàn.

**File**: `ChatServer/Utils/EncryptionHelper.cs` (Line 201-338)

**Các chức năng**:

| Function                     | Mục đích                         |
| ---------------------------- | -------------------------------- |
| `RsaSign(data)`              | Tạo chữ ký số                    |
| `RsaVerify(data, signature)` | Xác thực chữ ký                  |
| `RsaEncrypt(plainText)`      | Mã hóa dữ liệu nhỏ (< 200 bytes) |
| `RsaDecrypt(cipherText)`     | Giải mã RSA                      |
| `RsaVerifyWithPublicKey()`   | Verify với public key từ client  |

**Ứng dụng - Xác thực Login**:

```csharp
// ChatServer/Services/ChatProcessingService.cs (Line 162-186)
// ========== RSA SIGNATURE VERIFICATION (Optional) ==========
if (!string.IsNullOrEmpty(request.Signature) && !string.IsNullOrEmpty(request.PublicKey))
{
    try
    {
        var dataToVerify = $"{request.SenderUsername}:{request.Password}";
        var isValid = EncryptionHelper.RsaVerifyWithPublicKey(dataToVerify, request.Signature, request.PublicKey);
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
    }
}
```

---

### 1.3 Hybrid Encryption (RSA + AES)

**Mục đích**: Mã hóa file/attachment lớn với hiệu suất cao.

**Cách hoạt động**:

```
┌─────────────────────────────────────────────────────────────────┐
│                    HYBRID ENCRYPTION                            │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   1. Generate random AES-256 session key                       │
│                    │                                            │
│                    ▼                                            │
│   2. Encrypt FILE DATA with AES (fast)                         │
│      [FILE] ──► [AES] ──► [ENCRYPTED_DATA]                     │
│                    │                                            │
│                    ▼                                            │
│   3. Encrypt AES KEY with RSA (secure key exchange)            │
│      [AES_KEY] ──► [RSA] ──► [ENCRYPTED_KEY]                   │
│                    │                                            │
│                    ▼                                            │
│   4. Package: ENCRYPTED_DATA | ENCRYPTED_KEY | IV              │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

**Code trích dẫn**:

```csharp
// ChatServer/Utils/EncryptionHelper.cs (Line 352-384)
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
```

**Console Output mẫu**:

```
[SERVER][HYBRID] ENCRYPT START: dataSize=37446 bytes
[SERVER][HYBRID] Generated AES-256 session key
[SERVER][HYBRID][AES] Data encrypted: 37446 => 37456 bytes
[SERVER][HYBRID][RSA] AES key encrypted: 32 => 256 bytes
[SERVER][HYBRID] ENCRYPT DONE: totalSize=50314 chars
```

---

## 2. ORACLE DATABASE SECURITY

### 2.1 Schema & Tables

**File**: `ChatServer/Database/02_schema.sql`

```sql
-- Bảng chính
TAIKHOAN      -- Tài khoản người dùng (MATK, TENTK, PASSWORD_HASH, CLEARANCELEVEL)
NGUOIDUNG     -- Thông tin cá nhân (MATK, HOVATEN, EMAIL, SDT, MACV, MAPB)
CUOCTROCHUYEN -- Cuộc trò chuyện (MACTC, TENCTC, IS_PRIVATE, MIN_CLEARANCE)
TINNHAN       -- Tin nhắn (MATN, MACTC, MATK, NOIDUNG, SECURITYLABEL)
THANHVIEN     -- Thành viên nhóm (MACTC, MATK, QUYEN)
ATTACHMENT    -- File đính kèm (ATTACH_ID, FILEDATA, IS_ENCRYPTED)
AUDIT_LOGS    -- Nhật ký kiểm toán (MATK, ACTION, TARGET, TIMESTAMP)
```

### 2.2 VPD (Virtual Private Database) / RLS

**Mục đích**: Lọc dữ liệu tự động theo clearance level của user.

**File**: `ChatServer/Database/04_policies.sql`

```sql
-- VPD Policy cho TINNHAN (Bell-LaPadula Model)
-- User chỉ đọc được tin nhắn có SECURITYLABEL <= CLEARANCELEVEL của mình
CREATE OR REPLACE FUNCTION VPD_TINNHAN_READ(p_schema VARCHAR2, p_obj VARCHAR2)
RETURN VARCHAR2 AS
    v_level NUMBER;
BEGIN
    v_level := NVL(SYS_CONTEXT('MAC_CTX', 'CLEARANCE_LEVEL'), 1);
    RETURN 'SECURITYLABEL <= ' || v_level;
END;
/

BEGIN
    DBMS_RLS.ADD_POLICY(
        object_schema   => USER,
        object_name     => 'TINNHAN',
        policy_name     => 'VPD_TINNHAN_READ_POLICY',
        function_schema => USER,
        policy_function => 'VPD_TINNHAN_READ',
        statement_types => 'SELECT'
    );
END;
/
```

**Cách hoạt động**:

```
User ClearanceLevel=2 query: SELECT * FROM TINNHAN
                                    │
                                    ▼
                    VPD tự động thêm: WHERE SECURITYLABEL <= 2
                                    │
                                    ▼
                    Chỉ thấy tin nhắn Level 1 và 2
```

### 2.3 FGA (Fine-Grained Auditing)

**Mục đích**: Ghi log chi tiết khi truy cập dữ liệu nhạy cảm.

```sql
-- FGA Policy cho TINNHAN
BEGIN
    DBMS_FGA.ADD_POLICY(
        object_schema   => USER,
        object_name     => 'TINNHAN',
        policy_name     => 'FGA_TINNHAN_ACCESS',
        audit_condition => 'SECURITYLABEL >= 3',
        audit_column    => 'NOIDUNG',
        statement_types => 'SELECT,INSERT,UPDATE,DELETE'
    );
END;
/
```

### 2.4 MAC Context (Mandatory Access Control)

**File**: `ChatServer/Database/DbContext.cs` (Line 131-152)

```csharp
public async Task SetMacContextAsync(string matk, int clearanceLevel)
{
    using var cmd = Connection.CreateCommand();
    cmd.CommandText = "BEGIN SET_MAC_CONTEXT(:p_matk, :p_level); END;";
    cmd.CommandType = CommandType.Text;
    cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });
    cmd.Parameters.Add(new OracleParameter("p_level", OracleDbType.Int32) { Value = clearanceLevel });
    await cmd.ExecuteNonQueryAsync();
}
```

```sql
-- Oracle Procedure
CREATE OR REPLACE PROCEDURE SET_MAC_CONTEXT(
    p_matk VARCHAR2,
    p_level NUMBER DEFAULT NULL
) AS
    v_level NUMBER;
BEGIN
    IF p_level IS NOT NULL THEN
        v_level := p_level;
    ELSE
        SELECT CLEARANCELEVEL INTO v_level FROM TAIKHOAN WHERE MATK = p_matk;
    END IF;

    DBMS_SESSION.SET_CONTEXT('MAC_CTX', 'USERNAME', p_matk);
    DBMS_SESSION.SET_CONTEXT('MAC_CTX', 'CLEARANCE_LEVEL', TO_CHAR(v_level));
END;
/
```

---

## 3. ĐĂNG KÝ TÀI KHOẢN

### 3.1 Flow đăng ký

```
┌─────────────────────────────────────────────────────────────────┐
│                    REGISTRATION FLOW                            │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   CLIENT                              SERVER                    │
│   RegisterForm                        ChatProcessingService     │
│        │                                    │                   │
│   1. Fill form:                             │                   │
│      - Username                             │                   │
│      - Password                             │                   │
│      - Email                                │                   │
│      - Họ tên                               │                   │
│      - Số điện thoại                        │                   │
│        │                                    │                   │
│        ▼                                    │                   │
│   2. Action="Register"  ──────────────────► │                   │
│        │                                    ▼                   │
│        │                           3. Generate MATK             │
│        │                              (TK009, TK010,...)        │
│        │                                    │                   │
│        │                                    ▼                   │
│        │                           4. Create TAIKHOAN           │
│        │                              (MAVAITRO=VT003, User)    │
│        │                                    │                   │
│        │                                    ▼                   │
│        │                           5. Create NGUOIDUNG          │
│        │                              (MACV=CV005, Thực tập)    │
│        │                                    │                   │
│        │                                    ▼                   │
│        │                           6. Generate OTP              │
│        │                              Send Email                │
│        │                                    │                   │
│   ◄────────────────────────────────────────│                   │
│   7. Show VerifyOtpForm                     │                   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 3.2 MATK tự động

**File**: `ChatServer/Database/DbContext.cs` (Line 296-305)

```csharp
public async Task<string> GenerateNextMatkAsync()
{
    using var cmd = Connection.CreateCommand();
    cmd.CommandText = @"
        SELECT 'TK' || LPAD(NVL(MAX(TO_NUMBER(SUBSTR(MATK, 3))), 0) + 1, 3, '0')
        FROM TAIKHOAN
        WHERE REGEXP_LIKE(MATK, '^TK[0-9]+$')";
    var result = await cmd.ExecuteScalarAsync();
    return result?.ToString() ?? "TK001";
}
```

**Kết quả**: TK001, TK002, ... TK009, TK010, TK011, ...

### 3.3 Chức vụ mặc định

**File**: `ChatServer/Database/03_procedures.sql` (Line 391-393)

```sql
-- Khi INSERT mới, mặc định CV005 (Thực tập sinh)
WHEN NOT MATCHED THEN
    INSERT (MATK, EMAIL, HOVATEN, SDT, MACV, MAPB)
    VALUES (p_matk, p_email, p_hovaten, p_sdt, NVL(p_macv, 'CV005'), p_mapb);
```

---

## 4. XÁC THỰC OTP

### 4.1 Flow

```
1. Server generate OTP (6 số)
2. Hash OTP với SHA256
3. Lưu hash vào XACTHUCOTP
4. Gửi OTP plaintext qua Email
5. User nhập OTP
6. Server hash input, so sánh với DB
7. Nếu khớp: IS_OTP_VERIFIED = 1
```

**File**: `ChatServer/Services/ChatProcessingService.cs` (HandleVerifyOtpAsync)

```csharp
var otpHash = PasswordHelper.HashPassword(request.Otp);
var isValid = await _dbContext.VerifyOtpAsync(request.SenderUsername, otpHash);
if (!isValid)
{
    return JsonSerializer.Serialize(new ServerResponse
    {
        Success = false,
        Message = "OTP không hợp lệ hoặc đã hết hạn."
    });
}
```

---

## 5. ADMIN PANEL

### 5.1 Tính năng

| Tab               | Chức năng                                                      |
| ----------------- | -------------------------------------------------------------- |
| **Users**         | Xem danh sách user, tạo user, ban/unban, unlock, sửa thông tin |
| **Conversations** | Xem tất cả cuộc trò chuyện, xóa                                |
| **Messages**      | Xem tin nhắn trong cuộc trò chuyện                             |
| **Audit Logs**    | Xem nhật ký hoạt động                                          |
| **Policies**      | Quản lý VPD, FGA, MAC                                          |

### 5.2 Hiển thị User với Chức vụ, Phòng ban

**File**: `ChatServer/Database/DbContext.cs` (Line 1369-1410)

```csharp
public async Task<List<AdminUserInfo>> GetAllUsersAsync()
{
    using var cmd = Connection.CreateCommand();
    cmd.CommandText = @"
        SELECT tk.MATK, tk.TENTK,
               NVL(n.EMAIL, ''), NVL(n.HOVATEN, ''), NVL(n.SDT, ''),
               tk.CLEARANCELEVEL, tk.IS_BANNED_GLOBAL, NVL(tk.MAVAITRO, ''),
               tk.NGAYTAO,
               CASE WHEN EXISTS (SELECT 1 FROM XACTHUCOTP x WHERE x.MATK = tk.MATK AND x.DAXACMINH = 1) THEN 1 ELSE 0 END AS IS_OTP_VERIFIED,
               NVL(tk.FAILED_LOGIN_ATTEMPTS, 0),
               tk.LOCKED_UNTIL,
               NVL(cv.TENCV, ''),
               NVL(pb.TENPB, '')
        FROM TAIKHOAN tk
        LEFT JOIN NGUOIDUNG n ON tk.MATK = n.MATK
        LEFT JOIN CHUCVU cv ON n.MACV = cv.MACV
        LEFT JOIN PHONGBAN pb ON n.MAPB = pb.MAPB
        ORDER BY tk.NGAYTAO DESC";
    // ...
}
```

---

## 6. BẢO MẬT TÀI KHOẢN

### 6.1 Khóa tài khoản khi nhập sai mật khẩu

**Logic**:

- 5 lần nhập sai → Khóa 30 phút
- Admin có thể unlock thủ công

**File**: `ChatServer/Database/DbContext.cs` (IncrementFailedLoginAsync)

```csharp
public async Task<(int newCount, bool isNowLocked)> IncrementFailedLoginAsync(string matkOrUsername)
{
    // Tăng FAILED_LOGIN_ATTEMPTS
    // Nếu >= 5: SET LOCKED_UNTIL = SYSDATE + 30/(24*60)
}
```

### 6.2 Password Hashing (SHA256)

**File**: `ChatServer/Utils/PasswordHelper.cs`

```csharp
public static string HashPassword(string password)
{
    using var sha256 = SHA256.Create();
    var bytes = Encoding.UTF8.GetBytes(password);
    var hash = sha256.ComputeHash(bytes);
    var sb = new StringBuilder();
    foreach (var b in hash)
        sb.Append(b.ToString("x2"));
    return sb.ToString();
}
```

---

## 7. CẤU TRÚC THƯ MỤC

```
ChatApplication-main/
├── ChatServer/
│   ├── Database/
│   │   ├── 01_sys_setup.sql      # Tablespace, Profile, User, Context
│   │   ├── 02_schema.sql         # Tables, Sequences, Indexes
│   │   ├── 03_procedures.sql     # Stored Procedures
│   │   ├── 04_policies.sql       # VPD, FGA, MAC Policies
│   │   ├── 05_seeds.sql          # Sample Data
│   │   ├── 06_grants.sql         # Permissions
│   │   └── DbContext.cs          # Database Access Layer
│   ├── Forms/
│   │   ├── AdminPanelForm.cs     # Admin UI
│   │   ├── PolicyManagementForm.cs
│   │   └── ...
│   ├── Services/
│   │   ├── ChatProcessingService.cs  # Business Logic
│   │   ├── SocketServerService.cs    # TCP Server
│   │   └── EmailService.cs           # OTP Email
│   └── Utils/
│       ├── EncryptionHelper.cs   # AES/RSA/Hybrid
│       └── PasswordHelper.cs     # SHA256 Hash
│
├── ChatClient/
│   ├── Forms/
│   │   ├── LoginForm.cs
│   │   ├── RegisterForm.cs
│   │   ├── ChatFormNew.cs
│   │   └── ...
│   ├── Services/
│   │   ├── SocketClientService.cs  # TCP Client
│   │   └── EncryptionService.cs
│   └── Utils/
│       └── EncryptionHelper.cs
│
└── README.md
```

---

## 8. CHẠY ỨNG DỤNG

### 8.1 Cài đặt Database

```bash
# Chạy với SYS AS SYSDBA
sqlplus sys/password@XE as sysdba @01_sys_setup.sql

# Chạy với ChatApplication user
sqlplus ChatApplication/123@XE @02_schema.sql
sqlplus ChatApplication/123@XE @03_procedures.sql
sqlplus ChatApplication/123@XE @04_policies.sql
sqlplus ChatApplication/123@XE @05_seeds.sql
sqlplus ChatApplication/123@XE @06_grants.sql
```

### 8.2 Build & Run

```bash
cd ChatApplication-main
dotnet build

# Chạy Server trước
cd ChatServer
dotnet run

# Chạy Client
cd ChatClient
dotnet run
```

### 8.3 Tài khoản mẫu

| Username    | Password | Role    | Clearance |
| ----------- | -------- | ------- | --------- |
| giamdoc     | 123      | Admin   | 5         |
| truongphong | 123      | Manager | 4         |
| nhanvien1   | 123      | User    | 3         |
| thuctapsinh | 123      | Intern  | 1         |

---

## 9. CONSOLE LOG FORMAT

```
[SERVER][AES] <<< FROM CLIENT (encrypted): ...   # Nhận từ client, đã mã hóa
[SERVER][AES] --- DECRYPTED: ...                  # Sau khi giải mã
[SERVER][AES] >>> TO CLIENT (encrypted): ...     # Gửi về client, đã mã hóa

[SERVER][RSA] SIGN: ...                           # Tạo chữ ký số
[SERVER][RSA] VERIFY: ... => VALID/INVALID       # Xác thực chữ ký

[SERVER][HYBRID] ENCRYPT START: ...              # Bắt đầu mã hóa file
[SERVER][HYBRID][AES] Data encrypted: ...        # Mã hóa data bằng AES
[SERVER][HYBRID][RSA] AES key encrypted: ...     # Mã hóa key bằng RSA
[SERVER][HYBRID] ENCRYPT DONE: ...               # Hoàn thành
```

---

## 10. BẢO MẬT NOTES

1. **AES Key cố định**: Trong production nên dùng key exchange (Diffie-Hellman)
2. **RSA Key per session**: Mỗi client nên có RSA key pair riêng
3. **OTP Expiry**: OTP hết hạn sau 10 phút
4. **Clearance Level**:
   - Level 1-2: User tự đăng ký được
   - Level 3+: Chỉ Admin mới cấp được
5. **Chức vụ/Phòng ban**: Client không thể tự thay đổi, chỉ Admin mới sửa được

---

_Cập nhật: 2025-01-10_
