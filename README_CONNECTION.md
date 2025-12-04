# Hướng Dẫn Kết Nối Client với Server

## 1. Khởi động Server

### Bước 1: Kiểm tra cấu hình Database
Đảm bảo file `ChatServer/appsettings.json` có cấu hình đúng:

```json
{
  "Database": {
    "ConnectionString": "User Id=ChatApplication;Password=123;Data Source=localhost:1521/orclpdb"
  },
  "Server": {
    "Port": 9000
  }
}
```

### Bước 2: Chạy Server
1. Mở terminal/command prompt
2. Di chuyển đến thư mục `ChatServer`
3. Chạy lệnh:
   ```bash
   dotnet run
   ```

Server sẽ hiển thị:
```
=== Chat Server Starting ===
Database: User Id=ChatApplication
Server Port: 9000
Starting chat server. Press Ctrl+C to stop.
```

## 2. Khởi động Client

### Bước 1: Cấu hình kết nối
File `ChatClient/Forms/ChatForm.cs` và `ChatClient/Services/SocketClientService.cs` đã được cấu hình để kết nối với:
- **Host**: `127.0.0.1` (localhost)
- **Port**: `9000`

Nếu muốn thay đổi, sửa trong:
- `ChatForm.cs` dòng 24: `new SocketClientService("127.0.0.1", 9000)`
- `LoginForm.cs` dòng 56: `new SocketClientService("127.0.0.1", 9000)`
- `RegisterForm.cs` dòng 61: `new SocketClientService("127.0.0.1", 9000)`
- Các form khác tương tự

### Bước 2: Chạy Client
1. Mở terminal/command prompt
2. Di chuyển đến thư mục `ChatClient`
3. Chạy lệnh:
   ```bash
   dotnet run
   ```

Hoặc build và chạy file `.exe`:
```bash
dotnet build
.\bin\Debug\net9.0-windows\ChatClient.exe
```

## 3. Quy trình kết nối

### Khi Client khởi động:
1. **LoginForm** hiển thị
2. Người dùng nhập username và password
3. Click "Đăng nhập"
4. Client tự động:
   - Tạo `SocketClientService` với host `127.0.0.1` và port `9000`
   - Gọi `ConnectAsync()` để kết nối TCP với server
   - Gửi request `Login` với username/password (được mã hóa AES)
   - Nhận response từ server (được giải mã)
   - Nếu thành công, mở `ChatForm`

### Trong ChatForm:
1. Khi form được hiển thị, tự động gọi `ConnectToServerAsync()`
2. Kết nối TCP với server (nếu chưa kết nối)
3. Load danh sách conversations
4. Bắt đầu auto-refresh timer (mỗi 5 giây)

## 4. Kiểm tra kết nối

### Nếu không kết nối được:

1. **Kiểm tra Server đang chạy:**
   - Xem console của server có hiển thị "Chat server listening on port 9000"
   - Kiểm tra port 9000 có bị chiếm bởi ứng dụng khác không

2. **Kiểm tra Firewall:**
   - Windows Firewall có thể chặn kết nối
   - Thêm exception cho port 9000 hoặc tắt firewall tạm thời

3. **Kiểm tra Network:**
   - Nếu server chạy trên máy khác, đảm bảo:
     - Cùng mạng LAN hoặc có thể ping được
     - Thay `127.0.0.1` bằng IP thực của server (ví dụ: `192.168.1.100`)

4. **Kiểm tra Database:**
   - Server cần kết nối được với Oracle database
   - Kiểm tra connection string trong `appsettings.json`
   - Đảm bảo Oracle service `orclpdb` đang chạy

## 5. Protocol kết nối

### TCP Socket Protocol:
- **Format**: Mỗi message là một dòng text (kết thúc bằng `\n`)
- **Encryption**: Tất cả data được mã hóa AES trước khi gửi
- **Request Format**: JSON được serialize, sau đó mã hóa, gửi qua TCP
- **Response Format**: Server gửi JSON đã mã hóa, client giải mã và deserialize

### Ví dụ flow:
```
Client → Server: Encrypt(JSON({Action: "Login", SenderUsername: "user1", Password: "pass"})) + "\n"
Server → Client: Encrypt(JSON({Success: true, Message: "Login successful", ClearanceLevel: 3})) + "\n"
```

## 6. Troubleshooting

### Lỗi "Chưa kết nối tới server":
- Server chưa được khởi động
- Port 9000 bị chiếm
- Firewall chặn kết nối

### Lỗi "Connection refused":
- Server không lắng nghe trên port 9000
- IP/Port không đúng

### Lỗi "Timeout":
- Server quá tải
- Network có vấn đề
- Firewall chặn

### Lỗi Database:
- Kiểm tra Oracle service đang chạy
- Kiểm tra connection string
- Kiểm tra user `ChatApplication` có tồn tại và có quyền

