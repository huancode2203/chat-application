# 💬 CHAT APPLICATION - Secure Enterprise Messaging System

Ứng dụng chat bảo mật cấp doanh nghiệp với Oracle Database, hỗ trợ MAC (Mandatory Access Control), VPD, FGA và mã hóa đa lớp.

---

## 🌟 Tính năng chính

### 🔐 Bảo mật

- **MAC (Mandatory Access Control)**: 5 mức bảo mật (1-5)
- **VPD (Virtual Private Database)**: Row-level security
- **FGA (Fine-Grained Auditing)**: Audit mọi truy cập
- **3-Layer Encryption**: AES-256, RSA-2048, Hybrid
- **Digital Signatures**: Xác thực tính toàn vẹn
- **OTP Verification**: Email-based 2FA

### 💬 Chat Features

- **Private Chat**: 1-1 messaging
- **Group Chat**: Unlimited members
- **File Attachments**: Encrypted file sharing
- **Message Security Labels**: 5 levels (1-5)
- **Member Roles**: Owner, Admin, Moderator, Member
- **Ban/Mute**: Per-conversation moderation

### 👥 User Management

- **Clearance Levels**: 1 (LOW) → 5 (CLASSIFIED)
- **Departments & Positions**: Organizational structure
- **Global Ban**: System-wide user blocking
- **Profile Management**: Email, phone, bio, avatar

### 🛡️ Admin Panel

- **User Management**: Create, edit, ban users
- **Conversation Monitoring**: View all conversations
- **Message Moderation**: Delete inappropriate content
- **Audit Logs**: Complete activity tracking
- **VPD/FGA Management**: Security policy control

---

## 🏗️ Kiến trúc

### Tech Stack

- **Frontend**: Windows Forms (.NET 9.0)
- **Backend**: C# Socket Server
- **Database**: Oracle 19c+
- **Encryption**: AES-256, RSA-2048
- **Communication**: TCP Sockets with AES encryption

### Encryption Architecture

```
┌─────────────────────────────────────────────┐
│  CLIENT                    SERVER           │
│                                              │
│  ┌──────────┐            ┌──────────┐      │
│  │ Message  │──AES-256──→│ Message  │      │
│  │  JSON    │←─AES-256───│  JSON    │      │
│  └──────────┘            └──────────┘      │
│                                              │
│  ┌──────────┐            ┌──────────┐      │
│  │   File   │──Hybrid───→│ Encrypted│      │
│  │  Bytes   │  RSA+AES   │  Package │      │
│  └──────────┘            └──────────┘      │
│                                              │
│  ┌──────────┐            ┌──────────┐      │
│  │Critical  │──RSA Sign─→│  Verify  │      │
│  │  Data    │            │Signature │      │
│  └──────────┘            └──────────┘      │
└─────────────────────────────────────────────┘
```

---

## 🚀 Quick Start

### 1. Database Setup

```bash
# Kết nối SYS
sqlplus sys/password@localhost:1521/ORCLPDB as sysdba

# Chạy schema (Phần 1-4)
@Database/Scripts/schema_complete.sql

# Kết nối ChatApplication
sqlplus ChatApplication/123@localhost:1521/ORCLPDB

# Chạy seeds
@Database/Scripts/seeds_complete.sql
```

### 2. Build & Run

```bash
# Build
dotnet build

# Terminal 1: Run Server
cd ChatServer
dotnet run

# Terminal 2: Run Client
cd ChatClient
dotnet run
```

### 3. Login

**Default Accounts** (password: `123456`):

- `giamdoc` - Clearance 5 (CLASSIFIED)
- `quantrivien` - Clearance 4 (TOP SECRET)
- `truongphongit` - Clearance 4 (TOP SECRET)
- `nhanvienketoan` - Clearance 3 (HIGH)
- `nhanvienit` - Clearance 2 (MEDIUM)
- `thuctapsinh` - Clearance 1 (LOW)

---

## 📖 Documentation

| Document                                     | Mô tả                     |
| -------------------------------------------- | ------------------------- |
| [ENCRYPTION_GUIDE.md](ENCRYPTION_GUIDE.md)   | Chi tiết về 3 loại mã hóa |
| [PROJECT_STRUCTURE.md](PROJECT_STRUCTURE.md) | Cấu trúc project          |
| [REFACTOR_SUMMARY.md](REFACTOR_SUMMARY.md)   | Tổng kết refactor         |

---

## 🔍 Kiểm tra Encryption

### Test 1: Socket Encryption (AES-256)

```bash
# Bật Wireshark, capture localhost traffic
# Gửi tin nhắn từ client
# → Sẽ thấy encrypted data, không đọc được plaintext
```

### Test 2: File Encryption (Hybrid)

```sql
-- Upload file qua ChatClient
-- Kiểm tra database
SELECT ATTACH_ID, FILENAME, IS_ENCRYPTED,
       DBMS_LOB.SUBSTR(FILEDATA, 50, 1) AS ENCRYPTED_PREVIEW
FROM ATTACHMENT
WHERE IS_ENCRYPTED = 1;

-- Kết quả: ENCRYPTED_PREVIEW sẽ là gibberish (base64 encrypted)
```

### Test 3: MAC Policy

```bash
# Login với user level 2
# Gửi tin nhắn level 3 → ❌ Bị chặn
# Đọc tin nhắn level 3 → ❌ Không thấy (VPD filter)
# Gửi tin nhắn level 1 → ✅ OK
```

---

## 🛠️ Troubleshooting

### Build Errors

```bash
# File locked
→ Đóng ChatServer/ChatClient đang chạy

# Missing procedures
→ Chạy lại schema_complete.sql

# Invalid Number
→ Đã fix: dùng OracleDbType.Decimal
```

### Runtime Errors

```bash
# ORA-20001: Write-up violation
→ User level thấp hơn message security label

# ORA-20100: Không thể xóa một phía
→ Chỉ áp dụng cho chat riêng tư

# Connection refused
→ Kiểm tra ChatServer đang chạy
```

---

## 📊 Database Statistics

- **Tables**: 18
- **Procedures**: 30+
- **Triggers**: 3
- **Indexes**: 15+
- **VPD Policies**: 1
- **FGA Policies**: 1

---

## 🎯 Security Highlights

1. ✅ **No plaintext passwords** - SHA-256 hashing
2. ✅ **Encrypted communication** - AES-256 for all socket data
3. ✅ **Encrypted files** - Hybrid RSA+AES
4. ✅ **MAC enforcement** - No read-up, no write-down
5. ✅ **Complete audit trail** - All actions logged
6. ✅ **VPD row filtering** - Automatic security filtering
7. ✅ **OTP verification** - Email-based 2FA

---

## 👨‍💻 Development

### Requirements

- .NET 9.0 SDK
- Oracle Database 19c+
- Visual Studio 2022 / VS Code
- Oracle.ManagedDataAccess.Core NuGet package

### Project Structure

```
ChatApplication/
├── ChatClient/          # Windows Forms Client
├── ChatServer/          # Socket Server + Admin Panel
├── Database/Scripts/    # SQL Scripts (2 files)
└── *.md                 # Documentation (4 files)
```

---

## 📝 License

Educational/Internal Use Only

---

## 🤝 Contributors

- Database Design: Oracle PL/SQL
- Security: MAC, VPD, FGA implementation
- Encryption: AES-256, RSA-2048, Hybrid
- UI/UX: Modern Windows Forms

---

**Version**: 1.0  
**Last Updated**: December 2025  
**Build Status**: ✅ 0 ERRORS
