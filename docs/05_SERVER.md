# PHẦN 5: SERVER-SIDE COMPONENTS

## 5.1 Kiến trúc Server

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           CHAT SERVER                                       │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   ┌─────────────────┐                                                       │
│   │  Program.cs     │ ─── Entry Point                                       │
│   │  (Main)         │                                                       │
│   └────────┬────────┘                                                       │
│            │                                                                │
│            ▼                                                                │
│   ┌─────────────────┐                                                       │
│   │SocketServerSvc  │ ─── TCP Listener (Port 9000)                         │
│   │                 │     - Accept connections                              │
│   │                 │     - AES Decrypt request                             │
│   │                 │     - AES Encrypt response                            │
│   └────────┬────────┘                                                       │
│            │                                                                │
│            ▼                                                                │
│   ┌─────────────────┐                                                       │
│   │ChatProcessingSvc│ ─── Business Logic                                    │
│   │                 │     - HandleLoginAsync                                │
│   │                 │     - HandleRegisterAsync                             │
│   │                 │     - HandleSendMessageAsync                          │
│   │                 │     - HandleGetConversationsAsync                     │
│   │                 │     - ... 30+ handlers                                │
│   └────────┬────────┘                                                       │
│            │                                                                │
│            ▼                                                                │
│   ┌─────────────────┐       ┌─────────────────┐                            │
│   │   DbContext     │       │  EmailService   │                            │
│   │                 │       │                 │                            │
│   │ - Oracle Conn   │       │ - SMTP Client   │                            │
│   │ - Stored Procs  │       │ - Send OTP      │                            │
│   │ - VPD Context   │       │                 │                            │
│   └────────┬────────┘       └─────────────────┘                            │
│            │                                                                │
│            ▼                                                                │
│   ┌─────────────────┐                                                       │
│   │ Oracle Database │                                                       │
│   └─────────────────┘                                                       │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 5.2 SocketServerService

### 5.2.1 Khởi tạo TCP Listener

**File: ChatServer/Services/SocketServerService.cs (Line 25-46)**

```csharp
public class SocketServerService
{
    private readonly ChatProcessingService _chatProcessingService;
    private readonly TcpListener _listener;

    public SocketServerService(ChatProcessingService chatProcessingService, int port)
    {
        _chatProcessingService = chatProcessingService;
        _listener = new TcpListener(IPAddress.Any, port);  // Lắng nghe tất cả interfaces
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _listener.Start();
        Console.WriteLine("Chat server listening on port " +
            ((_listener.LocalEndpoint as IPEndPoint)?.Port ?? 0));

        // Loop chấp nhận connections
        while (!cancellationToken.IsCancellationRequested)
        {
            var client = await _listener.AcceptTcpClientAsync(cancellationToken);
            // Mỗi client một task riêng (concurrent)
            _ = HandleClientAsync(client, cancellationToken);
        }
    }
}
```

### 5.2.2 Xử lý Client Connection

**File: ChatServer/Services/SocketServerService.cs (Line 48-85)**

```csharp
private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
{
    Console.WriteLine("Client connected: " + client.Client.RemoteEndPoint);
    using var _ = client;

    try
    {
        using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

        // Loop đọc messages từ client
        while (!cancellationToken.IsCancellationRequested && client.Connected)
        {
            // 1. Đọc encrypted line
            var encryptedLine = await reader.ReadLineAsync();
            if (string.IsNullOrEmpty(encryptedLine))
                break;

            // 2. Log và Decrypt
            Console.WriteLine($"[SERVER][AES] <<< FROM CLIENT (encrypted): {encryptedLine.Substring(0, 60)}...");
            var json = EncryptionHelper.Decrypt(encryptedLine);
            Console.WriteLine($"[SERVER][AES] --- DECRYPTED: {json.Substring(0, 100)}...");

            // 3. Process request
            var responseJson = await _chatProcessingService.HandleRequestAsync(json);

            // 4. Encrypt và gửi response
            var responseEncrypted = EncryptionHelper.Encrypt(responseJson);
            Console.WriteLine($"[SERVER][AES] >>> TO CLIENT (encrypted): {responseEncrypted.Substring(0, 60)}...");

            await writer.WriteLineAsync(responseEncrypted);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error handling client: " + ex);
    }

    Console.WriteLine("Client disconnected.");
}
```

---

## 5.3 ChatProcessingService

### 5.3.1 Request Router

**File: ChatServer/Services/ChatProcessingService.cs (Line 50-149)**

```csharp
public class ChatProcessingService
{
    private readonly DbContext _dbContext;
    private readonly EmailService _emailService;

    public ChatProcessingService(DbContext dbContext, EmailService emailService)
    {
        _dbContext = dbContext;
        _emailService = emailService;
    }

    public async Task<string> HandleRequestAsync(string json)
    {
        try
        {
            var request = JsonSerializer.Deserialize<ChatRequest>(json);
            if (request == null)
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "Invalid request format."
                });
            }

            // Route to appropriate handler based on Action
            return request.Action switch
            {
                // Authentication
                "Login" => await HandleLoginAsync(request),
                "Register" => await HandleRegisterAsync(request),
                "VerifyOtp" => await HandleVerifyOtpAsync(request),
                "ResendOtp" => await HandleResendOtpAsync(request),
                "ChangePassword" => await HandleChangePasswordAsync(request),
                "ForgotPassword" => await HandleForgotPasswordAsync(request),

                // User Profile
                "GetUserProfile" => await HandleGetUserProfileAsync(request),
                "UpdateUserProfile" => await HandleUpdateUserProfileAsync(request),

                // Conversations
                "GetConversations" => await HandleGetConversationsAsync(request),
                "CreatePrivateConversation" => await HandleCreatePrivateConversationAsync(request),
                "CreateGroupConversation" => await HandleCreateGroupConversationAsync(request),
                "GetConversationMessages" => await HandleGetConversationMessagesAsync(request),
                "GetConversationMembers" => await HandleGetConversationMembersAsync(request),

                // Messages
                "SendMessage" => await HandleSendMessageAsync(request),
                "DeleteMessage" => await HandleDeleteMessageAsync(request),

                // Attachments
                "UploadAttachment" => await HandleUploadAttachmentAsync(request),
                "DownloadAttachment" => await HandleDownloadAttachmentAsync(request),
                "SendMessageWithAttachment" => await HandleSendMessageWithAttachmentAsync(request),

                // Search
                "SearchUsers" => await HandleSearchUsersAsync(request),

                _ => JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = $"Unknown action: {request.Action}"
                })
            };
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new ServerResponse
            {
                Success = false,
                Message = $"Server error: {ex.Message}"
            });
        }
    }
}
```

### 5.3.2 HandleLoginAsync

**File: ChatServer/Services/ChatProcessingService.cs (Line 151-280)**

```csharp
private async Task<string> HandleLoginAsync(ChatRequest request)
{
    // 1. Validate input
    if (string.IsNullOrEmpty(request.SenderUsername) || string.IsNullOrEmpty(request.Password))
    {
        return JsonSerializer.Serialize(new ServerResponse
        {
            Success = false,
            Message = "Username and password are required."
        });
    }

    // 2. RSA Signature Verification (Optional)
    if (!string.IsNullOrEmpty(request.Signature) && !string.IsNullOrEmpty(request.PublicKey))
    {
        var dataToVerify = $"{request.SenderUsername}:{request.Password}";
        var isValid = EncryptionHelper.RsaVerifyWithPublicKey(dataToVerify, request.Signature, request.PublicKey);
        if (!isValid)
        {
            Console.WriteLine($"[SERVER][RSA] INVALID signature for login: {request.SenderUsername}");
            return JsonSerializer.Serialize(new ServerResponse
            {
                Success = false,
                Message = "RSA signature verification failed."
            });
        }
        Console.WriteLine($"[SERVER][RSA] VALID signature for login: {request.SenderUsername}");
    }

    // 3. Check account lock status
    var lockStatus = await _dbContext.CheckAccountLockStatusAsync(request.SenderUsername);
    if (lockStatus.IsLocked)
    {
        var remainingMinutes = (int)(lockStatus.LockedUntil!.Value - DateTime.Now).TotalMinutes + 1;
        return JsonSerializer.Serialize(new ServerResponse
        {
            Success = false,
            Message = $"Tài khoản bị khóa. Thử lại sau {remainingMinutes} phút."
        });
    }

    // 4. Get account from database
    var account = await _dbContext.GetUserAccountAsync(request.SenderUsername);
    if (account == null)
    {
        return JsonSerializer.Serialize(new ServerResponse
        {
            Success = false,
            Message = "Invalid username or password."
        });
    }

    // 5. Check if banned
    if (account.IsBannedGlobal)
    {
        return JsonSerializer.Serialize(new ServerResponse
        {
            Success = false,
            Message = "Your account has been banned."
        });
    }

    // 6. Verify password
    if (!PasswordHelper.VerifyPassword(request.Password, account.PasswordHash))
    {
        var (newCount, isNowLocked) = await _dbContext.IncrementFailedLoginAsync(request.SenderUsername);
        await _dbContext.WriteAuditLogAsync(request.SenderUsername, "LOGIN_FAILED", $"Attempt {newCount}", 0);

        if (isNowLocked)
        {
            return JsonSerializer.Serialize(new ServerResponse
            {
                Success = false,
                Message = $"Tài khoản bị khóa 30 phút do nhập sai {newCount} lần."
            });
        }

        return JsonSerializer.Serialize(new ServerResponse
        {
            Success = false,
            Message = $"Sai mật khẩu. Còn {5 - newCount} lần thử."
        });
    }

    // 7. Check OTP verified
    if (!account.IsOtpVerified)
    {
        return JsonSerializer.Serialize(new ServerResponse
        {
            Success = false,
            Message = "OTP_REQUIRED",
            Data = new { RequiresOtp = true }
        });
    }

    // 8. Reset failed attempts và set MAC context
    await _dbContext.ResetFailedLoginAttemptsAsync(request.SenderUsername);
    await _dbContext.SetMacContextAsync(account.Matk, account.ClearanceLevel);
    await _dbContext.WriteAuditLogAsync(request.SenderUsername, "LOGIN_SUCCESS", "", 0);

    // 9. Return success with user info
    return JsonSerializer.Serialize(new ServerResponse
    {
        Success = true,
        Message = "Login successful",
        User = new UserInfo
        {
            Matk = account.Matk,
            Username = account.Username,
            ClearanceLevel = account.ClearanceLevel,
            Role = account.Mavaitro
        }
    });
}
```

### 5.3.3 HandleRegisterAsync

**File: ChatServer/Services/ChatProcessingService.cs (Line 290-380)**

```csharp
private async Task<string> HandleRegisterAsync(ChatRequest request)
{
    // 1. Validate
    if (string.IsNullOrEmpty(request.SenderUsername) ||
        string.IsNullOrEmpty(request.Password) ||
        string.IsNullOrEmpty(request.Email))
    {
        return JsonSerializer.Serialize(new ServerResponse
        {
            Success = false,
            Message = "Username, password, and email are required."
        });
    }

    // 2. Check if username exists
    var existing = await _dbContext.GetUserAccountAsync(request.SenderUsername);
    if (existing != null)
    {
        return JsonSerializer.Serialize(new ServerResponse
        {
            Success = false,
            Message = "Username already exists."
        });
    }

    // 3. Validate clearance level (max 2 for self-registration)
    var clearanceLevel = Math.Min(request.ClearanceLevel, 2);
    if (request.ClearanceLevel > 2)
    {
        return JsonSerializer.Serialize(new ServerResponse
        {
            Success = false,
            Message = "Cannot self-register with clearance level > 2."
        });
    }

    // 4. Hash password
    var passwordHash = PasswordHelper.HashPassword(request.Password);

    // 5. Generate MATK (TK001, TK002, ...)
    var matk = await _dbContext.GenerateNextMatkAsync();

    // 6. Create account
    await _dbContext.CreateAccountAsync(
        matk,
        request.SenderUsername,
        passwordHash,
        "VT003",        // Role: User
        clearanceLevel,
        false           // Not banned
    );

    // 7. Create user profile with default position (CV005 = Thực tập sinh)
    var hovaten = !string.IsNullOrEmpty(request.Hovaten) ? request.Hovaten : request.SenderUsername;
    var sdt = request.Sdt ?? "";

    await _dbContext.UpdateUserInfoAsync(
        request.SenderUsername,
        request.Email,
        hovaten,
        sdt,
        null,           // diachi
        null,           // bio
        "CV005",        // macv = Thực tập sinh
        null            // mapb
    );

    // 8. Generate and send OTP
    var otp = new Random().Next(100000, 999999).ToString();
    var otpHash = PasswordHelper.HashPassword(otp);
    await _dbContext.CreateOtpAsync(matk, request.Email, otpHash, 10);

    try
    {
        await _emailService.SendOtpEmailAsync(request.Email, otp);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to send OTP email: {ex.Message}");
    }

    // 9. Audit log
    await _dbContext.WriteAuditLogAsync(matk, "REGISTER", request.SenderUsername, 0);

    return JsonSerializer.Serialize(new ServerResponse
    {
        Success = true,
        Message = "Registration successful. Please verify OTP.",
        Data = new { Matk = matk, RequiresOtp = true }
    });
}
```

### 5.3.4 HandleSendMessageAsync

**File: ChatServer/Services/ChatProcessingService.cs (Line 550-620)**

```csharp
private async Task<string> HandleSendMessageAsync(ChatRequest request)
{
    // 1. Validate
    if (string.IsNullOrEmpty(request.ConversationId) || string.IsNullOrEmpty(request.Content))
    {
        return JsonSerializer.Serialize(new ServerResponse
        {
            Success = false,
            Message = "Conversation ID and content are required."
        });
    }

    // 2. Get sender's MATK
    var matk = await _dbContext.GetMatkFromUsernameAsync(request.SenderUsername);
    if (string.IsNullOrEmpty(matk))
    {
        return JsonSerializer.Serialize(new ServerResponse
        {
            Success = false,
            Message = "User not found."
        });
    }

    // 3. Check membership
    var isMember = await _dbContext.IsConversationMemberAsync(request.ConversationId, matk);
    if (!isMember)
    {
        return JsonSerializer.Serialize(new ServerResponse
        {
            Success = false,
            Message = "You are not a member of this conversation."
        });
    }

    // 4. Set MAC Context for VPD
    await _dbContext.SetMacContextAsync(matk, request.ClearanceLevel);

    // 5. Determine security label (Bell-LaPadula: No Write Down)
    var securityLabel = Math.Max(request.SecurityLabel, request.ClearanceLevel);

    // 6. Insert message
    var messageId = await _dbContext.InsertMessageAsync(
        request.ConversationId,
        matk,
        request.Content,
        securityLabel
    );

    // 7. Audit log
    await _dbContext.WriteAuditLogAsync(matk, "SEND_MESSAGE", request.ConversationId, securityLabel);

    return JsonSerializer.Serialize(new ServerResponse
    {
        Success = true,
        Message = "Message sent",
        Data = new { MessageId = messageId }
    });
}
```

---

## 5.4 DbContext

### 5.4.1 Connection Management

**File: ChatServer/Database/DbContext.cs (Line 25-80)**

```csharp
public class DbContext : IDisposable
{
    private OracleConnection? _connection;

    private const string ConnectionString =
        "User Id=ChatApplication;Password=123;" +
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
                Console.WriteLine("Database connected");
            }
            else if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }
            return _connection;
        }
    }

    public void Dispose()
    {
        if (_connection != null)
        {
            if (_connection.State == ConnectionState.Open)
                _connection.Close();
            _connection.Dispose();
            _connection = null;
        }
    }
}
```

### 5.4.2 GenerateNextMatkAsync

**File: ChatServer/Database/DbContext.cs (Line 296-305)**

```csharp
/// <summary>
/// Generate MATK tự động: TK001, TK002, ...
/// </summary>
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

### 5.4.3 GetAllUsersAsync (Admin)

**File: ChatServer/Database/DbContext.cs (Line 1369-1410)**

```csharp
public async Task<List<AdminUserInfo>> GetAllUsersAsync()
{
    using var cmd = Connection.CreateCommand();
    cmd.CommandText = @"
        SELECT tk.MATK, tk.TENTK,
               NVL(n.EMAIL, ''), NVL(n.HOVATEN, ''), NVL(n.SDT, ''),
               tk.CLEARANCELEVEL, tk.IS_BANNED_GLOBAL, NVL(tk.MAVAITRO, ''),
               tk.NGAYTAO,
               CASE WHEN EXISTS (
                   SELECT 1 FROM XACTHUCOTP x
                   WHERE x.MATK = tk.MATK AND x.DAXACMINH = 1
               ) THEN 1 ELSE 0 END AS IS_OTP_VERIFIED,
               NVL(tk.FAILED_LOGIN_ATTEMPTS, 0),
               tk.LOCKED_UNTIL,
               NVL(cv.TENCV, ''),   -- Tên chức vụ
               NVL(pb.TENPB, '')    -- Tên phòng ban
        FROM TAIKHOAN tk
        LEFT JOIN NGUOIDUNG n ON tk.MATK = n.MATK
        LEFT JOIN CHUCVU cv ON n.MACV = cv.MACV
        LEFT JOIN PHONGBAN pb ON n.MAPB = pb.MAPB
        ORDER BY tk.NGAYTAO DESC";

    var result = new List<AdminUserInfo>();
    using var reader = await cmd.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        result.Add(new AdminUserInfo
        {
            Matk = reader.GetString(0),
            Username = reader.GetString(1),
            Email = reader.IsDBNull(2) ? "" : reader.GetString(2),
            Hovaten = reader.IsDBNull(3) ? "" : reader.GetString(3),
            Phone = reader.IsDBNull(4) ? "" : reader.GetString(4),
            ClearanceLevel = reader.GetInt32(5),
            IsBannedGlobal = reader.GetInt32(6) == 1,
            Mavaitro = reader.IsDBNull(7) ? "" : reader.GetString(7),
            NgayTao = reader.GetDateTime(8),
            IsOtpVerified = reader.GetInt32(9) == 1,
            FailedLoginAttempts = reader.GetInt32(10),
            LockedUntil = reader.IsDBNull(11) ? null : reader.GetDateTime(11),
            Chucvu = reader.IsDBNull(12) ? "" : reader.GetString(12),
            Phongban = reader.IsDBNull(13) ? "" : reader.GetString(13)
        });
    }

    return result;
}
```

---

## 5.5 EmailService

**File: ChatServer/Services/EmailService.cs**

```csharp
public class EmailService
{
    private const string SmtpHost = "smtp.gmail.com";
    private const int SmtpPort = 587;
    private const string SenderEmail = "your-email@gmail.com";
    private const string SenderPassword = "your-app-password";

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
            Body = $@"
Xin chào,

Mã OTP của bạn là: {otp}

Mã có hiệu lực trong 10 phút.
Không chia sẻ mã này với bất kỳ ai.

Trân trọng,
ChatApp Team
",
            IsBodyHtml = false
        };

        await client.SendMailAsync(message);
        Console.WriteLine($"[EMAIL] OTP sent to {toEmail}");
    }
}
```

---

## 5.6 AdminPanelForm

### 5.6.1 Hiển thị Users với Chức vụ, Phòng ban

**File: ChatServer/Forms/AdminPanelForm.cs (Line 126-146)**

```csharp
private async Task LoadUsersAsync()
{
    try
    {
        btnRefreshUsers.Enabled = false;
        var users = await Task.Run(() => _dbContext.GetAllUsersAsync().Result);

        // Transform data cho DataGridView
        dgvUsers.DataSource = users.Select(u => new
        {
            u.Matk,
            u.Username,
            u.Hovaten,
            u.Chucvu,         // Tên chức vụ
            u.Phongban,       // Tên phòng ban
            u.Email,
            u.Phone,
            u.ClearanceLevel,
            IsBanned = u.IsBannedGlobal ? "Có" : "Không",
            IsVerified = u.IsOtpVerified ? "Có" : "Không",
            AccountLocked = u.IsAccountLocked
                ? $"Khóa ({u.FailedLoginAttempts} lần)"
                : (u.FailedLoginAttempts > 0
                    ? $"{u.FailedLoginAttempts}/5 lần sai"
                    : "Bình thường"),
            u.NgayTao
        }).ToList();

        btnRefreshUsers.Enabled = true;
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Error: {ex.Message}");
        btnRefreshUsers.Enabled = true;
    }
}
```

---

_Tiếp theo: 06_CLIENT.md - Client-side Components_
