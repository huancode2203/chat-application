## Kiến trúc tổng quan

Ứng dụng gồm 3 phần chính:

- **Client WinForms (`ChatClient`)**: giao diện người dùng, gửi/nhận tin nhắn qua TCP socket, mã hóa end-to-end.
- **Server Console (`ChatServer`)**: xử lý nghiệp vụ chat, kiểm soát truy cập bắt buộc (MAC), truy vấn Oracle, ghi audit log.
- **Database Oracle (`Database/Scripts`)**: lưu USERS, MESSAGES, FILES, AUDIT_LOGS và policy VPD cho MAC.

### Luồng dữ liệu

1. **Client**:
   - Người dùng đăng nhập trên `LoginForm`, chọn `ClearanceLevel`.
   - `ChatForm` tạo `SocketClientService` kết nối TCP tới server (`127.0.0.1:9000`).
   - Khi gửi tin nhắn:
     - Tạo `ChatRequest` (Action=`SendMessage`, SenderUsername, ReceiverUsername, Content, SecurityLabel, ClearanceLevel).
     - Serialize sang JSON, **mã hóa AES** bằng `EncryptionHelper` trên client.
     - Gửi chuỗi Base64 (cipher) qua TCP (kết thúc bằng `\n`).

2. **Server**:
   - `SocketServerService` lắng nghe TCP.
   - Mỗi kết nối client, server:
     - Đọc 1 dòng cipher, giải mã AES (`ChatServer.Utils.EncryptionHelper`), thu được JSON.
     - Gọi `ChatProcessingService.HandleRequestAsync(json)`.
   - `ChatProcessingService`:
     - Parse JSON vào `ChatRequest`.
     - Gọi `DbContext.SetSessionUserLevelAsync(username, clearanceLevel)` để set context cho VPD/MAC trên DB.
     - Tùy `Action`:
       - **SendMessage**:
         - Kiểm tra MAC: `MACService.CanWrite(clearance, securityLabel)` (no write down).
         - Lấy `SenderID`, `ReceiverID` từ bảng `USERS`.
         - Ghi bản ghi mới vào `MESSAGES` (có `SecurityLabel`).
         - Ghi nhật ký `AUDIT_LOGS` với action `SEND_MESSAGE`.
       - **GetMessages**:
         - Lấy danh sách tin nhắn liên quan user từ `MESSAGES` qua `DbContext.GetMessagesForUserAsync`.
         - VPD trên DB đã filter theo `SESSION_USER_LEVEL`.
         - Kiểm tra MAC lần nữa trên code: `MACService.CanRead(clearance, message.SecurityLabel)` (no read up).
         - Trả về mảng `ChatMessageDto` phù hợp.
     - Tạo `ServerResponse` (Success, Message, Messages[]), serialize JSON.
   - `SocketServerService` mã hóa JSON response bằng AES và gửi lại cho client.

3. **Client hiển thị**:
   - `ChatForm` dùng `SocketClientService.GetMessagesAsync` để lấy danh sách tin.
   - Response sau khi giải mã là JSON `ServerResponse`:
     - Nếu `Success = true`, client hiển thị `Messages[]` lên GUI.

## Mandatory Access Control (MAC) & VPD

### Khái niệm

- **MAC (Mandatory Access Control)**:
  - Mỗi **user** có một cấp độ bảo mật `ClearanceLevel` (1=LOW, 2=MEDIUM, 3=HIGH).
  - Mỗi **tin nhắn/file** gắn một nhãn bảo mật `SecurityLabel` (1=LOW, 2=MEDIUM, 3=HIGH).
  - Mô hình Bell-LaPadula:
    - **No read up**: user không được đọc đối tượng có nhãn **cao hơn** clearance của mình.
    - **No write down**: user không được ghi (tạo tin) xuống mức nhãn **thấp hơn** clearance.

- **VPD (Virtual Private Database)**:
  - Trên Oracle, policy VPD tự động bổ sung điều kiện WHERE vào câu SQL dựa trên context (`SYS_CONTEXT`).
  - Ta dùng context `MAC_CTX` với thuộc tính `USER_LEVEL`.
  - Hàm policy `MESSAGES_POLICY_FN` trả về điều kiện `SECURITYLABEL <= USER_LEVEL`.
  - Như vậy, với cùng một câu lệnh `SELECT * FROM MESSAGES ...`, Oracle sẽ tự động chỉ trả về hàng phù hợp level của user.

### MACService

- Được cài đặt trong `ChatServer/Services/MACService.cs`:
  - `CanRead(userClearance, objectLabel)` → enforce **no read up** (`objectLabel <= clearance`).
  - `CanWrite(userClearance, objectLabel)` → enforce **no write down** (`objectLabel >= clearance`).
- Dùng trong:
  - `HandleSendMessageAsync` để kiểm tra nhãn tin gửi.
  - `HandleGetMessagesAsync` để lọc thêm trên code (bổ sung cho VPD).

### DbContext & VPD

- `ChatServer/Database/DbContext.cs`:
  - `SetSessionUserLevelAsync(username, level)`:
    - Gọi package `MAC_CTX_PKG.SET_USER_LEVEL` trên Oracle.
    - Package này gọi `DBMS_SESSION.SET_CONTEXT('MAC_CTX', 'USERNAME', p_user)` và `'USER_LEVEL', p_level`.
  - Các truy vấn `GetMessagesForUserAsync`, `InsertMessageAsync`, ... hoạt động **trong session** đã có context, nên VPD policy sẽ áp dụng tự động.
- Script trong `Database/Scripts/schema.sql`:
  - Tạo bảng `USERS`, `MESSAGES`, `FILES`, `AUDIT_LOGS` và sequence.
  - Tạo package `MAC_CTX_PKG`, context `MAC_CTX`.
  - Tạo hàm policy `MESSAGES_POLICY_FN` và gắn policy bằng `DBMS_RLS.ADD_POLICY`.

## Socket protocol & ví dụ gửi/nhận tin nhắn

### Định dạng message

- **Request từ client** (sau khi giải mã) là JSON dạng:

```json
{
  "Action": "SendMessage",
  "SenderUsername": "alice",
  "ReceiverUsername": "bob",
  "Content": "Hello Bob",
  "SecurityLabel": 2,
  "ClearanceLevel": 2
}
```

- **Response từ server** (trước khi mã hóa) là JSON dạng:

```json
{
  "Success": true,
  "Message": "Message sent.",
  "Messages": []
}
```

- Khi client gọi `GetMessages`, `Action` thay đổi:

```json
{
  "Action": "GetMessages",
  "SenderUsername": "alice",
  "ClearanceLevel": 2
}
```

- Response chứa danh sách tin:

```json
{
  "Success": true,
  "Message": "Messages loaded.",
  "Messages": [
    {
      "Sender": "alice",
      "Receiver": "bob",
      "Content": "Hello Bob",
      "SecurityLabel": 2,
      "Timestamp": "2025-01-01T10:00:00"
    }
  ]
}
```

### Mã hóa end-to-end

- Trên **client** (`ChatClient/Utils/EncryptionHelper.cs`) và **server** (`ChatServer/Utils/EncryptionHelper.cs`) dùng chung:
  - Thuật toán: AES.
  - Key/IV demo: chuỗi 16 byte (`DemoChatAppKey16`, `DemoChatAppIv_16`).
  - Thực tế nên:
    - Dùng key riêng cho từng user/phiên.
    - Sử dụng cơ chế trao đổi key an toàn (Diffie-Hellman, TLS, ...).
    - Không hard-code key trong code.

### Ví dụ luồng gửi/nhận

1. Người dùng "alice" (ClearanceLevel=2) gửi tin cho "bob" với nhãn `SecurityLabel=2`.
2. `ChatForm` gọi:
   - `_socketClient.SendChatMessageAsync(currentUser, "bob", "Hello Bob", 2)`.
3. `SocketClientService`:
   - Serialize `ChatRequest` → JSON.
   - Gọi `EncryptionHelper.Encrypt(json)` → cipher Base64.
   - Gửi `cipher + "\n"` qua TCP.
4. `SocketServerService`:
   - Đọc 1 dòng cipher, decrypt → JSON.
   - Gọi `ChatProcessingService.HandleRequestAsync(json)`.
5. `ChatProcessingService`:
   - Gọi `DbContext.SetSessionUserLevelAsync("alice", 2)` → set context `USER_LEVEL=2`.
   - Kiểm tra MAC no write down: `CanWrite(2, 2)` → cho phép.
   - Ghi bản ghi vào `MESSAGES` với `SECURITYLABEL=2`.
   - Ghi audit log `SEND_MESSAGE`.
   - Trả `ServerResponse` JSON.
6. `SocketServerService`:
   - Mã hóa `ServerResponse` JSON, gửi về client.
7. `ChatForm` có thể gọi `GetMessagesAsync` để tải tin:
   - Server chỉ trả các tin có `SECURITYLABEL <= 2` (nhờ VPD + MACService).
   - Client hiển thị tin trong `ListBox`.

## Cấu trúc thư mục

- `ChatClient/`
  - `Forms/LoginForm.cs` – form đăng nhập.
  - `Forms/ChatForm.cs` – form chat chính.
  - `Models/User.cs`, `Models/Message.cs` – model dữ liệu trên client.
  - `Services/SocketClientService.cs` – service TCP client.
  - `Utils/EncryptionHelper.cs` – mã hóa AES end-to-end.
  - `Program.cs` – entry point WinForms.

- `ChatServer/`
  - `Services/SocketServerService.cs` – TCP server lắng nghe client.
  - `Services/ChatProcessingService.cs` – xử lý nghiệp vụ chat, gọi DbContext + MAC.
  - `Services/MACService.cs` – logic MAC (no read up / no write down).
  - `Database/DbContext.cs` – làm việc với Oracle, set SESSION_USER_LEVEL cho VPD.
  - `Utils/EncryptionHelper.cs` – mã hóa/giải mã AES.
  - `Program.cs` – entry point server console.

- `Database/Scripts/schema.sql` – script tạo bảng, sequence, context MAC_CTX, policy VPD.

Với skeleton này, bạn có thể mở solution trong Visual Studio, chỉnh sửa connection string Oracle, bổ sung xử lý login thật sự (kiểm tra PASSWORDHASH trong bảng USERS) và mở rộng hỗ trợ gửi file dựa trên bảng FILES, sử dụng lại cùng mô hình MAC/VPD. 


