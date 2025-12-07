# 📁 CẤU TRÚC PROJECT - CHAT APPLICATION

## 🎯 Tổng quan

Ứng dụng chat bảo mật với Oracle Database, hỗ trợ MAC (Mandatory Access Control), VPD, FGA.

---

## 📂 Cấu trúc thư mục

```
ChatApplication/
├── ChatClient/              # Windows Forms Client
│   ├── Forms/              # UI Forms
│   │   ├── LoginForm.cs           ✅ Đăng nhập
│   │   ├── RegisterForm.cs        ✅ Đăng ký
│   │   ├── ForgotPasswordForm.cs  ✅ Quên mật khẩu
│   │   ├── ResetPasswordForm.cs   ✅ Reset mật khẩu
│   │   ├── VerifyOtpForm.cs       ✅ Xác minh OTP
│   │   ├── ChatFormNew.cs         ✅ Giao diện chat chính
│   │   ├── MembersDialog.cs       ✅ Danh sách thành viên
│   │   ├── CreateGroupDialog.cs   ✅ Tạo nhóm
│   │   ├── UserProfileForm.cs     ✅ Hồ sơ người dùng
│   │   └── ProgressDialog.cs      ✅ Progress bar
│   ├── Services/           # Business Logic
│   │   ├── SocketClientService.cs ✅ Kết nối socket
│   │   └── EncryptionService.cs   ✅ Mã hóa (Client-side)
│   ├── Utils/
│   │   └── EncryptionHelper.cs    ✅ AES-256 socket encryption
│   └── Controls/
│       └── MessageBubble.cs       ✅ Custom message bubble
│
├── ChatServer/             # Server Application
│   ├── Forms/              # Admin UI
│   │   ├── AdminPanelForm.cs      ✅ Quản lý hệ thống
│   │   ├── UserEditForm.cs        ✅ Tạo/sửa user
│   │   └── VPDPolicyManagementForm.cs ✅ Quản lý VPD/FGA
│   ├── Services/           # Business Logic
│   │   ├── SocketServerService.cs ✅ Socket server
│   │   ├── ChatProcessingService.cs ✅ Xử lý requests
│   │   ├── MACService.cs          ✅ MAC policy enforcement
│   │   └── EmailService.cs        ✅ Gửi OTP email
│   ├── Database/
│   │   └── DbContext.cs           ✅ Oracle database access
│   └── Utils/
│       ├── EncryptionHelper.cs    ✅ AES/RSA/Hybrid encryption
│       └── PasswordHelper.cs      ✅ SHA-256 password hashing
│
└── Database/               # SQL Scripts
    └── Scripts/
        ├── schema_complete.sql    ✅ Schema + ALL Procedures
        └── seeds_complete.sql     ✅ Dữ liệu mẫu
```

---

## 🗑️ Files đã xóa (không cần thiết)

### Client

- ❌ `ChatForm.cs` - Thay bằng ChatFormNew.cs
- ❌ `ChatForm.Designer.cs`
- ❌ `ChatForm.*.cs` - Các partial files
- ❌ `ChatForm.*.resx` - Resource files

### Server

- ❌ `AdminLoginForm.cs` - Không được sử dụng
- ❌ `AdminLoginForm.Designer.cs`
- ❌ `AdminLoginForm.resx`
- ❌ `FileEncryptionHelper.cs` - Trùng với EncryptionHelper

### Database Scripts

- ❌ `admin_procedures.sql` - Đã gộp vào schema_complete.sql
- ❌ `group_management.sql` - Đã gộp vào schema_complete.sql

---

## 🔧 Stored Procedures (schema_complete.sql)

### User Management

- `SP_TAO_TAIKHOAN` - Tạo tài khoản
- `SP_DOI_MATKHAU` - Đổi mật khẩu
- `SP_CAPNHAT_NGUOIDUNG_ADMIN` - Admin cập nhật user
- `SP_CAPNHAT_THONGTIN_CANHAN` - User tự cập nhật
- `SP_BAN_USER_GLOBAL` / `SP_UNBAN_USER_GLOBAL`

### Conversation Management

- `SP_TAO_CUOCTROCHUYEN` - Tạo cuộc trò chuyện
- `SP_THEM_THANHVIEN` / `SP_XOA_THANHVIEN`
- `SP_XOA_CHAT_RIENGTU_MOTPHIA` - Xóa chat riêng tư 1 phía
- `SP_ROI_NHOM` - Rời nhóm
- `SP_XOA_NHOM` - Archive nhóm
- `SP_XOA_ARCHIVE` - Xóa archive

### Message Management

- `SP_GUI_TINNHAN` - Gửi tin nhắn
- `SP_GUI_TINNHAN_WITH_ATTACH` - Gửi tin với attachment
- `SP_GUI_TINNHAN_RIENG` - Gửi tin riêng tư
- `SP_LAY_TINNHAN_CUOCTROCHUYEN` - Lấy tin nhắn
- `SP_XOA_TINNHAN` - Xóa tin nhắn (Admin)

### Member Management

- `SP_BAN_MEMBER` / `SP_UNBAN_MEMBER`
- `SP_MUTE_MEMBER` / `SP_UNMUTE_MEMBER`

### Attachment & OTP

- `SP_UPLOAD_ATTACHMENT` - Upload file
- `SP_UPLOAD_ATTACHMENT_ENCRYPTED` - Upload với encryption
- `SP_TAO_OTP` - Tạo OTP
- `SP_WRITE_AUDIT_LOG` - Ghi audit log

### Utility Procedures

- `SET_MAC_CONTEXT` - Set security context
- `SP_LAY_THANHVIEN_CHITIET` - Lấy chi tiết thành viên
- `SP_LAY_THONGTIN_NGUOIDUNG` - Lấy thông tin user
- `SP_LAY_DANHSACH_PHONGBAN` / `SP_LAY_DANHSACH_CHUCVU`

---

## 🔐 Security Features

### 1. MAC (Mandatory Access Control)

- **Clearance Levels**: 1 (LOW) → 5 (CLASSIFIED)
- **No Read Up**: User level 2 không đọc được message level 3
- **No Write Down**: User level 4 không ghi được message level 2
- **Context**: `MAC_CTX` package quản lý user level

### 2. VPD (Virtual Private Database)

- **Policy**: `TINNHAN_MAC_POLICY`
- **Function**: `TINNHAN_POLICY_FN`
- **Filter**: `SECURITYLABEL <= user_level`

### 3. FGA (Fine-Grained Auditing)

- **Policy**: `FGA_TINNHAN_SELECT_AUDIT`
- **Audit**: Tất cả SELECT trên TINNHAN

### 4. Triggers

- `TRG_TINNHAN_CHECK_WRITE_UP` - Ngăn write-up
- `TRG_TINNHAN_AUDIT` - Audit log
- `TRG_THANHVIEN_PRIVATE_CHECK_INS` - Giới hạn 2 người cho chat riêng tư

---

## 🚀 Deployment

### 1. Setup Database

```bash
# Kết nối SYS
sqlplus sys/password@localhost:1521/ORCLPDB as sysdba

# Chạy schema (Phần 1 + 2)
@Database/Scripts/schema_complete.sql

# Kết nối ChatApplication
sqlplus ChatApplication/123@localhost:1521/ORCLPDB

# Chạy schema (Phần 3 + 4)
@Database/Scripts/schema_complete.sql

# Chạy seeds
@Database/Scripts/seeds_complete.sql
```

### 2. Build & Run

```bash
# Build
dotnet build

# Run Server
cd ChatServer
dotnet run

# Run Client (terminal khác)
cd ChatClient
dotnet run
```

---

## 📊 Database Tables

### Core Tables (11)

1. `VAITRO` - Vai trò hệ thống
2. `TAIKHOAN` - Tài khoản
3. `NGUOIDUNG` - Thông tin chi tiết
4. `PHONGBAN` - Phòng ban
5. `CHUCVU` - Chức vụ
6. `CUOCTROCHUYEN` - Cuộc trò chuyện
7. `THANHVIEN` - Thành viên nhóm
8. `TINNHAN` - Tin nhắn
9. `ATTACHMENT` - File đính kèm
10. `XACTHUCOTP` - OTP verification
11. `AUDIT_LOGS` - Audit trail

### Support Tables (7)

- `LOAICTC` - Loại cuộc trò chuyện
- `LOAITN` - Loại tin nhắn
- `TRANGTHAI` - Trạng thái tin nhắn
- `PHAN_QUYEN_NHOM` - Phân quyền trong nhóm
- `TINNHAN_ATTACH` - Liên kết tin nhắn - attachment
- `ENCRYPTION_KEYS` - Quản lý khóa mã hóa
- `USER_SETTINGS` - Cài đặt người dùng

---

## 🎨 UI Components

### Client Forms

- **LoginForm**: Modern flat design, green/blue buttons
- **ChatFormNew**: Main chat interface với ListView
- **MembersDialog**: Hiển thị email, role, ban status
- **UserProfileForm**: Xem/sửa profile
- **CreateGroupDialog**: Tạo nhóm với member selection

### Server Forms

- **AdminPanelForm**: 4 tabs (Users, Conversations, Messages, Audit)
- **UserEditForm**: 720x750, font 12F, spacing 80px
- **VPDPolicyManagementForm**: Quản lý VPD/FGA policies

---

## 📈 Performance

- **Socket**: Persistent TCP connection
- **Encryption**: Hardware-accelerated AES
- **Database**: Connection pooling, prepared statements
- **UI**: Async/await, non-blocking operations

---

**Build Status**: ✅ 0 Errors, 15 Warnings (nullability only)
