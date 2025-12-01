# Chat Application - Ứng Dụng Chat Nội Bộ

Ứng dụng chat nội bộ với WinForms client và C# console server, sử dụng Oracle Database với MAC/VPD (Mandatory Access Control / Virtual Private Database).

## Cấu Trúc Dự Án

```
ChatApplication/
├── ChatClient/          # WinForms Client
│   ├── Forms/           # Các form (Login, Register, Chat, ...)
│   ├── Models/          # User, Message models
│   ├── Services/        # SocketClientService
│   └── Utils/           # EncryptionHelper
├── ChatServer/          # Console Server
│   ├── Services/        # SocketServerService, ChatProcessingService, MACService, EmailService
│   ├── Database/        # DbContext
│   └── Utils/           # PasswordHelper
├── Database/
│   └── Scripts/         # Oracle database scripts
└── Docs/
    └── Architecture.md  # Tài liệu kiến trúc
```

## Yêu Cầu

- .NET 6.0 SDK
- Oracle Database 12c+ (hoặc Oracle XE)
- Visual Studio 2022 (hoặc VS Code với C# extension)

## Cài Đặt

### 1. Database

Chạy script Oracle trong `Database/Scripts/schema.sql`:

```sql
-- Tạo user (chạy bằng DBA)
CREATE USER ChatNoiBo_DoAn1 IDENTIFIED BY "123";
GRANT CREATE SESSION, CREATE TABLE, CREATE SEQUENCE, CREATE PROCEDURE, CREATE VIEW TO ChatNoiBo_DoAn1;
ALTER USER ChatNoiBo_DoAn1 QUOTA UNLIMITED ON USERS;

-- Connect as ChatNoiBo_DoAn1 và chạy phần còn lại của script
```

### 2. Server Configuration

Mở file `ChatServer/appsettings.json` và cấu hình:

```json
{
  "EmailSettings": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUsername": "your_email@gmail.com",
    "SmtpPassword": "your_app_password",
    "FromEmail": "your_email@gmail.com",
    "FromName": "Chat Application"
  },
  "Database": {
    "ConnectionString": "User Id=ChatNoiBo_DoAn1;Password=123;Data Source=localhost:1521/XE"
  },
  "Server": {
    "Port": 9000
  }
}
```

**Lưu ý về Gmail:**
- Bật 2FA cho tài khoản Gmail
- Tạo App Password: Google Account → Security → App passwords
- Dùng App Password (16 ký tự) cho `SmtpPassword`, không dùng mật khẩu đăng nhập

**Nếu không cấu hình email:** Server sẽ log OTP ra console (demo mode).

### 3. Build và Chạy

#### Server:
```bash
cd ChatServer
dotnet restore
dotnet build
dotnet run
```

#### Client:
```bash
cd ChatClient
dotnet restore
dotnet build
dotnet run
```

Hoặc mở solution trong Visual Studio và chạy từ đó.

## Sử Dụng

### Đăng Ký Tài Khoản

1. Chạy Client, click "Đăng ký"
2. Điền thông tin: Username, Password, Email, Mức độ bảo mật (1-3)
3. Click "Đăng ký"
4. Kiểm tra email (hoặc console server) để lấy mã OTP
5. Nhập OTP để xác minh

### Đăng Nhập

1. Nhập Username và Password
2. Click "Đăng nhập"
3. Nếu thành công, mở form Chat

### Chat

- Nhập username người nhận
- Chọn Security Label (1=LOW, 2=MEDIUM, 3=HIGH)
- Nhập nội dung tin nhắn
- Click "Send"
- Click "Làm mới" để tải tin nhắn mới

### Quên Mật Khẩu

1. Click "Quên mật khẩu" ở form Login
2. Nhập Username và Email
3. Kiểm tra email để lấy OTP
4. Nhập OTP và mật khẩu mới
5. Click "Đặt lại"

## MAC (Mandatory Access Control)

- **No Read Up**: User chỉ đọc được tin có `SecurityLabel <= ClearanceLevel`
- **No Write Down**: User chỉ gửi được tin có `SecurityLabel >= ClearanceLevel`

Ví dụ:
- User có `ClearanceLevel = 2` (MEDIUM):
  - Đọc được: Label 1, 2
  - Gửi được: Label 2, 3

## Forms Designer

Tất cả các form đã được chuyển sang Designer.cs, bạn có thể:
- Mở form trong Visual Studio Designer
- Kéo thả controls
- Chỉnh sửa properties
- Code-behind giữ nguyên logic

## Troubleshooting

### Server không kết nối được Oracle
- Kiểm tra connection string trong `appsettings.json`
- Đảm bảo Oracle service đang chạy
- Kiểm tra TNS names hoặc connection string format

### Email không gửi được
- Kiểm tra SMTP settings trong `appsettings.json`
- Với Gmail: dùng App Password, không dùng mật khẩu thường
- Kiểm tra firewall/antivirus có chặn port 587 không

### Form hiển thị trống trong Designer
- Đảm bảo file `.Designer.cs` tồn tại và có `InitializeComponent()`
- Rebuild solution
- Đóng và mở lại Visual Studio

## License

Dự án này được tạo cho mục đích học tập và nghiên cứu.

