# PHẦN 2: HƯỚNG DẪN CÀI ĐẶT

## 2.1 Yêu cầu hệ thống

### Phần cứng

- CPU: Intel Core i3 trở lên
- RAM: 8GB trở lên
- Disk: 10GB trống

### Phần mềm

- Windows 10/11 64-bit
- .NET 9.0 SDK
- Oracle Database 21c XE (hoặc higher)
- SQL\*Plus hoặc SQL Developer

---

## 2.2 Cài đặt Oracle Database

### Bước 1: Download Oracle XE

```
https://www.oracle.com/database/technologies/xe-downloads.html
```

### Bước 2: Cài đặt

- Chạy setup.exe với quyền Administrator
- Ghi nhớ password cho SYS/SYSTEM

### Bước 3: Kiểm tra

```bash
sqlplus sys/yourpassword@XE as sysdba
SQL> SELECT * FROM V$VERSION;
```

---

## 2.3 Cài đặt Database Schema

### Bước 1: Chạy với SYS AS SYSDBA

```bash
cd ChatApplication-main/ChatServer/Database
sqlplus sys/yourpassword@XE as sysdba
```

```sql
-- Chạy script setup hệ thống
@01_sys_setup.sql
```

**Nội dung 01_sys_setup.sql:**

```sql
-- File: ChatServer/Database/01_sys_setup.sql (Line 1-50)

-- 1. TABLESPACE
CREATE TABLESPACE CHAT_DATA_TS
    DATAFILE 'chat_data.dbf' SIZE 100M AUTOEXTEND ON;

CREATE TABLESPACE CHAT_AUDIT_TS
    DATAFILE 'chat_audit.dbf' SIZE 50M AUTOEXTEND ON;

-- 2. PROFILE cho Admin
CREATE PROFILE CHAT_ADMIN_PROFILE LIMIT
    SESSIONS_PER_USER 5
    CPU_PER_SESSION UNLIMITED
    CONNECT_TIME 480
    IDLE_TIME 60
    FAILED_LOGIN_ATTEMPTS 5
    PASSWORD_LIFE_TIME 90
    PASSWORD_LOCK_TIME 1/24;

-- 3. USER ChatApplication
CREATE USER ChatApplication IDENTIFIED BY "123"
    DEFAULT TABLESPACE CHAT_DATA_TS
    QUOTA UNLIMITED ON CHAT_DATA_TS
    QUOTA UNLIMITED ON CHAT_AUDIT_TS
    PROFILE CHAT_ADMIN_PROFILE;

-- 4. CONTEXT cho MAC
CREATE OR REPLACE CONTEXT MAC_CTX USING MAC_CTX_PKG ACCESSED GLOBALLY;

-- 5. GRANTS
GRANT CREATE SESSION, CREATE TABLE, CREATE PROCEDURE,
      CREATE SEQUENCE, CREATE TRIGGER, CREATE VIEW,
      CREATE CONTEXT, EXECUTE ANY PROCEDURE TO ChatApplication;

GRANT EXECUTE ON DBMS_SESSION TO ChatApplication;
GRANT EXECUTE ON DBMS_RLS TO ChatApplication;
GRANT EXECUTE ON DBMS_FGA TO ChatApplication;
GRANT EXECUTE ON DBMS_CRYPTO TO ChatApplication;
```

### Bước 2: Chạy với ChatApplication user

```bash
sqlplus ChatApplication/123@XE
```

```sql
-- Chạy lần lượt các scripts
@02_schema.sql      -- Tạo tables
@03_procedures.sql  -- Stored procedures
@04_policies.sql    -- VPD, FGA policies
@05_seeds.sql       -- Dữ liệu mẫu
@06_grants.sql      -- Permissions
```

---

## 2.4 Cấu hình kết nối Database

### File: ChatServer/Database/DbContext.cs (Line 25-45)

```csharp
public class DbContext : IDisposable
{
    private OracleConnection? _connection;

    // Connection string - CHỈNH SỬA TẠI ĐÂY
    private const string ConnectionString =
        "User Id=ChatApplication;" +
        "Password=123;" +
        "Data Source=(DESCRIPTION=" +
            "(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))" +
            "(CONNECT_DATA=(SERVICE_NAME=XE)))";

    public OracleConnection Connection
    {
        get
        {
            if (_connection == null)
            {
                _connection = new OracleConnection(ConnectionString);
                _connection.Open();
            }
            return _connection;
        }
    }
}
```

**Cách chỉnh sửa:**

```csharp
// Thay đổi HOST nếu database ở máy khác
"(ADDRESS=(PROTOCOL=TCP)(HOST=192.168.1.100)(PORT=1521))"

// Thay đổi SERVICE_NAME nếu khác XE
"(CONNECT_DATA=(SERVICE_NAME=ORCL))"

// Thay đổi User/Password
"User Id=your_user;Password=your_password;"
```

---

## 2.5 Cấu hình Email Service

### File: ChatServer/Services/EmailService.cs

```csharp
public class EmailService
{
    // CHỈNH SỬA THÔNG TIN EMAIL TẠI ĐÂY
    private const string SmtpHost = "smtp.gmail.com";
    private const int SmtpPort = 587;
    private const string SenderEmail = "your-email@gmail.com";
    private const string SenderPassword = "your-app-password";  // App Password từ Google

    public async Task SendOtpEmailAsync(string toEmail, string otp)
    {
        using var client = new SmtpClient(SmtpHost, SmtpPort)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(SenderEmail, SenderPassword)
        };

        var message = new MailMessage(SenderEmail, toEmail)
        {
            Subject = "Mã OTP xác thực tài khoản ChatApp",
            Body = $"Mã OTP của bạn là: {otp}\nMã có hiệu lực trong 10 phút.",
            IsBodyHtml = false
        };

        await client.SendMailAsync(message);
    }
}
```

**Lấy App Password từ Google:**

1. Vào Google Account → Security
2. Bật 2-Step Verification
3. Vào App passwords → Generate
4. Copy 16-character password

---

## 2.6 Build và chạy ứng dụng

### Bước 1: Restore dependencies

```bash
cd ChatApplication-main
dotnet restore
```

### Bước 2: Build

```bash
dotnet build
```

### Bước 3: Chạy Server (trước)

```bash
cd ChatServer
dotnet run
```

**Output mong đợi:**

```
Chat server listening on port 9000
Database connected successfully
```

### Bước 4: Chạy Client (sau)

```bash
cd ChatClient
dotnet run
```

---

## 2.7 Tài khoản mẫu

Sau khi chạy `05_seeds.sql`, hệ thống có các tài khoản:

| Username    | Password | Vai trò          | Clearance Level |
| ----------- | -------- | ---------------- | --------------- |
| giamdoc     | 123      | Giám đốc (Admin) | 5               |
| truongphong | 123      | Trưởng phòng     | 4               |
| nhanvien1   | 123      | Nhân viên        | 3               |
| thuctapsinh | 123      | Thực tập sinh    | 1               |

---

## 2.8 Troubleshooting

### Lỗi: ORA-12541: TNS:no listener

```bash
# Khởi động Oracle Listener
lsnrctl start
```

### Lỗi: ORA-01017: invalid username/password

```bash
# Kiểm tra password trong connection string
# Hoặc reset password
ALTER USER ChatApplication IDENTIFIED BY "123";
```

### Lỗi: Port 9000 đang được sử dụng

```csharp
// File: ChatServer/Program.cs
// Thay đổi port
var server = new SocketServerService(chatService, 9001);  // Đổi sang 9001
```

```csharp
// File: ChatClient/Services/SocketClientService.cs
// Đồng bộ port với server
private const int ServerPort = 9001;
```

---

_Tiếp theo: 03_MA_HOA.md - Hệ thống mã hóa_
