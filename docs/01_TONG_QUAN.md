# CHATAPPLICATION - TÀI LIỆU KỸ THUẬT CHI TIẾT

## PHẦN 1: TỔNG QUAN HỆ THỐNG

### 1.1 Giới thiệu

ChatApplication là ứng dụng chat desktop được xây dựng trên nền tảng .NET 9.0 với Windows Forms, tích hợp các tính năng bảo mật đa lớp bao gồm:

- **Mã hóa 3 lớp**: AES-256 (Socket), RSA-2048 (Chữ ký số), Hybrid RSA+AES (File)
- **Oracle Database Security**: VPD, FGA, MAC/DAC, RBAC, Profiles
- **Xác thực OTP** qua Email
- **Audit Logging** toàn diện

### 1.2 Kiến trúc tổng thể

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           CHAT APPLICATION                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   ┌─────────────────┐       TCP/Socket (Port 9000)    ┌─────────────────┐  │
│   │                 │ ◄──────────────────────────────►│                 │  │
│   │   CHAT CLIENT   │      AES-256-CBC Encrypted      │   CHAT SERVER   │  │
│   │   (WinForms)    │                                 │   (WinForms)    │  │
│   │                 │                                 │                 │  │
│   └─────────────────┘                                 └────────┬────────┘  │
│                                                                │            │
│   Các Form:                                                    │            │
│   - LoginForm                                                  │            │
│   - RegisterForm                                               ▼            │
│   - ChatFormNew                                      ┌─────────────────┐   │
│   - VerifyOtpForm                                    │                 │   │
│   - UserProfileForm                                  │ ORACLE DATABASE │   │
│                                                      │                 │   │
│                                                      │ - VPD Policies  │   │
│   Server Forms:                                      │ - FGA Auditing  │   │
│   - AdminPanelForm                                   │ - MAC Context   │   │
│   - PolicyManagementForm                             │ - Stored Procs  │   │
│   - ServerDashboard                                  │                 │   │
│                                                      └─────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 1.3 Cấu trúc thư mục

```
ChatApplication-main/
│
├── ChatServer/                          # SERVER PROJECT
│   ├── Database/
│   │   ├── 01_sys_setup.sql            # Tablespace, Profile, User, Context
│   │   ├── 02_schema.sql               # Tables, Sequences, Indexes
│   │   ├── 03_procedures.sql           # Stored Procedures
│   │   ├── 04_policies.sql             # VPD, FGA, MAC Policies
│   │   ├── 05_seeds.sql                # Sample Data
│   │   ├── 06_grants.sql               # Permissions
│   │   └── DbContext.cs                # Database Access Layer (C#)
│   │
│   ├── Forms/
│   │   ├── AdminPanelForm.cs           # Quản trị viên
│   │   ├── PolicyManagementForm.cs     # Quản lý policies
│   │   └── ServerDashboard.cs          # Dashboard server
│   │
│   ├── Services/
│   │   ├── ChatProcessingService.cs    # Xử lý business logic
│   │   ├── SocketServerService.cs      # TCP Server
│   │   └── EmailService.cs             # Gửi OTP qua email
│   │
│   └── Utils/
│       ├── EncryptionHelper.cs         # AES/RSA/Hybrid encryption
│       └── PasswordHelper.cs           # SHA256 password hashing
│
├── ChatClient/                          # CLIENT PROJECT
│   ├── Forms/
│   │   ├── LoginForm.cs                # Đăng nhập
│   │   ├── RegisterForm.cs             # Đăng ký
│   │   ├── ChatFormNew.cs              # Chat chính
│   │   ├── VerifyOtpForm.cs            # Xác thực OTP
│   │   └── UserProfileForm.cs          # Thông tin cá nhân
│   │
│   ├── Services/
│   │   ├── SocketClientService.cs      # TCP Client
│   │   └── EncryptionService.cs        # Hybrid encryption
│   │
│   └── Utils/
│       └── EncryptionHelper.cs         # AES/RSA encryption
│
├── docs/                                # TÀI LIỆU
│   ├── 01_TONG_QUAN.md
│   ├── 02_CAI_DAT.md
│   ├── 03_MA_HOA.md
│   ├── 04_DATABASE.md
│   ├── 05_SERVER.md
│   ├── 06_CLIENT.md
│   └── 07_HUONG_DAN.md
│
└── README.md                            # Tổng quan
```

### 1.4 Công nghệ sử dụng

| Thành phần | Công nghệ                               |
| ---------- | --------------------------------------- |
| Framework  | .NET 9.0                                |
| UI         | Windows Forms                           |
| Database   | Oracle 21c XE                           |
| ORM        | ODP.NET (Oracle.ManagedDataAccess.Core) |
| Encryption | System.Security.Cryptography            |
| Email      | System.Net.Mail (SMTP)                  |
| Protocol   | TCP/IP Socket                           |

### 1.5 Các tính năng chính

| #   | Tính năng         | Mô tả                                        |
| --- | ----------------- | -------------------------------------------- |
| 1   | Đăng ký tài khoản | MATK tự động (TK001, TK002,...), OTP email   |
| 2   | Đăng nhập         | Khóa sau 5 lần sai, RSA signature (optional) |
| 3   | Chat riêng        | 1-1 conversation                             |
| 4   | Chat nhóm         | Multi-member groups                          |
| 5   | Gửi file          | Hybrid encryption cho attachments            |
| 6   | Admin Panel       | Quản lý users, conversations, audit logs     |
| 7   | VPD               | Lọc tin nhắn theo clearance level            |
| 8   | FGA               | Audit truy cập dữ liệu nhạy cảm              |
| 9   | MAC               | Mandatory Access Control context             |

---

_Tiếp theo: 02_CAI_DAT.md - Hướng dẫn cài đặt_
