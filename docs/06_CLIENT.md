# PHẦN 6: CLIENT-SIDE COMPONENTS

## 6.1 Kiến trúc Client

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           CHAT CLIENT                                       │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   ┌─────────────────┐                                                       │
│   │   Program.cs    │ ─── Entry Point                                       │
│   │   (Main)        │     - Application.Run(LoginForm)                      │
│   └────────┬────────┘                                                       │
│            │                                                                │
│            ▼                                                                │
│   ┌─────────────────────────────────────────────────────────────────────┐  │
│   │                          FORMS                                       │  │
│   ├─────────────────┬─────────────────┬─────────────────────────────────┤  │
│   │   LoginForm     │  RegisterForm   │  VerifyOtpForm                  │  │
│   │   - Username    │  - Username     │  - OTP Input                    │  │
│   │   - Password    │  - Password     │  - Countdown Timer              │  │
│   │   - Login btn   │  - Email        │  - Resend btn                   │  │
│   │                 │  - Họ tên       │                                 │  │
│   │                 │  - SĐT          │                                 │  │
│   └────────┬────────┴─────────────────┴─────────────────────────────────┘  │
│            │                                                                │
│            ▼ (Login Success)                                                │
│   ┌─────────────────────────────────────────────────────────────────────┐  │
│   │                       ChatFormNew                                    │  │
│   ├─────────────────────────────────────────────────────────────────────┤  │
│   │  ┌───────────────┐  ┌───────────────────────────────────────────┐  │  │
│   │  │ Conversations │  │            Message Area                    │  │  │
│   │  │ List (Left)   │  │  - Message bubbles                        │  │  │
│   │  │               │  │  - Attachments                            │  │  │
│   │  │ - Private     │  │  - Timestamps                             │  │  │
│   │  │ - Groups      │  │                                           │  │  │
│   │  │               │  ├───────────────────────────────────────────┤  │  │
│   │  │               │  │  Input Area                               │  │  │
│   │  │               │  │  [Message...] [Attach] [Send]             │  │  │
│   │  └───────────────┘  └───────────────────────────────────────────┘  │  │
│   └─────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│   ┌─────────────────┐                                                       │
│   │SocketClientSvc  │ ─── TCP Client                                        │
│   │                 │     - Connect to server                               │
│   │                 │     - AES Encrypt request                             │
│   │                 │     - AES Decrypt response                            │
│   └─────────────────┘                                                       │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 6.2 SocketClientService

### 6.2.1 Kết nối đến Server

**File: ChatClient/Services/SocketClientService.cs (Line 30-80)**

```csharp
public class SocketClientService : IDisposable
{
    private TcpClient? _client;
    private NetworkStream? _stream;
    private StreamReader? _reader;
    private StreamWriter? _writer;
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    private const string ServerHost = "127.0.0.1";
    private const int ServerPort = 9000;

    /// <summary>
    /// Kết nối đến server
    /// </summary>
    public async Task<bool> ConnectAsync()
    {
        try
        {
            _client = new TcpClient();
            await _client.ConnectAsync(ServerHost, ServerPort);

            _stream = _client.GetStream();
            _reader = new StreamReader(_stream, Encoding.UTF8);
            _writer = new StreamWriter(_stream, Encoding.UTF8) { AutoFlush = true };

            Console.WriteLine($"[CLIENT] Connected to {ServerHost}:{ServerPort}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CLIENT] Connection failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Ngắt kết nối
    /// </summary>
    public void Disconnect()
    {
        _reader?.Dispose();
        _writer?.Dispose();
        _stream?.Dispose();
        _client?.Close();
        _client?.Dispose();

        _reader = null;
        _writer = null;
        _stream = null;
        _client = null;

        Console.WriteLine("[CLIENT] Disconnected");
    }

    public void Dispose() => Disconnect();
}
```

### 6.2.2 Gửi Request (với AES Encryption)

**File: ChatClient/Services/SocketClientService.cs (Line 310-336)**

```csharp
/// <summary>
/// Gửi request đến server (internal)
/// - Serialize JSON
/// - AES Encrypt
/// - Send qua socket
/// - Receive encrypted response
/// - AES Decrypt
/// - Return JSON
/// </summary>
private async Task<string?> SendRequestAsync(ChatRequest request)
{
    if (_stream == null)
        throw new InvalidOperationException("Chưa kết nối tới server.");
    if (_reader == null || _writer == null)
        throw new InvalidOperationException("Stream reader/writer chưa được khởi tạo.");

    // Lock để đảm bảo thread-safe (nhiều request đồng thời)
    await _sendLock.WaitAsync();
    try
    {
        // 1. Serialize request thành JSON
        var json = JsonSerializer.Serialize(request);

        // 2. AES Encrypt
        var encrypted = EncryptionHelper.Encrypt(json);

        // 3. Gửi qua socket (thêm newline làm delimiter)
        await _writer.WriteLineAsync(encrypted);

        // 4. Đợi response (encrypted)
        var encryptedResponse = await _reader.ReadLineAsync();
        if (string.IsNullOrEmpty(encryptedResponse))
            return null;

        // 5. AES Decrypt
        var decrypted = EncryptionHelper.Decrypt(encryptedResponse);
        return decrypted;
    }
    finally
    {
        _sendLock.Release();
    }
}
```

### 6.2.3 LoginAsync

**File: ChatClient/Services/SocketClientService.cs (Line 100-150)**

```csharp
/// <summary>
/// Đăng nhập
/// </summary>
public async Task<(bool success, string message, User? user)> LoginAsync(
    string username,
    string password)
{
    var request = new ChatRequest
    {
        Action = "Login",
        SenderUsername = username,
        Password = password
    };

    var responseJson = await SendRequestAsync(request);
    if (responseJson == null)
        return (false, "No response from server", null);

    var response = JsonSerializer.Deserialize<ServerResponse>(responseJson);
    if (response == null)
        return (false, "Invalid response format", null);

    if (!response.Success)
        return (false, response.Message, null);

    // Parse user info từ response
    var user = new User
    {
        Matk = response.User?.Matk ?? "",
        Username = response.User?.Username ?? username,
        ClearanceLevel = response.User?.ClearanceLevel ?? 1,
        Role = response.User?.Role ?? "VT003"
    };

    return (true, response.Message, user);
}
```

### 6.2.4 RegisterAsync

**File: ChatClient/Services/SocketClientService.cs (Line 214-260)**

```csharp
/// <summary>
/// Đăng ký tài khoản mới
/// </summary>
public async Task<(bool success, string message)> RegisterAsync(
    string username,
    string password,
    string email,
    string hovaten,
    string sdt)  // Số điện thoại
{
    var request = new ChatRequest
    {
        Action = "Register",
        SenderUsername = username,
        Password = password,
        Email = email,
        Hovaten = hovaten,
        Sdt = sdt,
        ClearanceLevel = 1  // Mặc định level 1
    };

    var responseJson = await SendRequestAsync(request);
    if (responseJson == null)
        return (false, "No response from server");

    var response = JsonSerializer.Deserialize<ServerResponse>(responseJson);
    if (response == null)
        return (false, "Invalid response format");

    return (response.Success, response.Message);
}
```

### 6.2.5 SendMessageAsync

**File: ChatClient/Services/SocketClientService.cs (Line 341-370)**

```csharp
/// <summary>
/// Gửi tin nhắn vào cuộc trò chuyện
/// </summary>
public async Task<ServerResponse?> SendMessageToConversationAsync(
    User currentUser,
    string conversationId,
    string content,
    int securityLabel)
{
    var request = new ChatRequest
    {
        Action = "SendMessage",
        SenderUsername = currentUser.Username,
        ConversationId = conversationId,
        Content = content,
        SecurityLabel = securityLabel,
        ClearanceLevel = currentUser.ClearanceLevel
    };

    var responseJson = await SendRequestAsync(request);
    if (responseJson == null) return null;

    return JsonSerializer.Deserialize<ServerResponse>(responseJson);
}
```

### 6.2.6 GetConversationsAsync

**File: ChatClient/Services/SocketClientService.cs (Line 400-440)**

```csharp
/// <summary>
/// Lấy danh sách cuộc trò chuyện của user
/// </summary>
public async Task<List<ConversationInfo>?> GetConversationsAsync(User currentUser)
{
    var request = new ChatRequest
    {
        Action = "GetConversations",
        SenderUsername = currentUser.Username,
        ClearanceLevel = currentUser.ClearanceLevel
    };

    var responseJson = await SendRequestAsync(request);
    if (responseJson == null) return null;

    var response = JsonSerializer.Deserialize<ServerResponse>(responseJson);
    if (response == null || !response.Success) return null;

    return response.Conversations;
}
```

---

## 6.3 LoginForm

### 6.3.1 UI Layout

**File: ChatClient/Forms/LoginForm.Designer.cs**

```csharp
private void InitializeComponent()
{
    this.Text = "ChatApp - Đăng nhập";
    this.Size = new Size(400, 300);
    this.StartPosition = FormStartPosition.CenterScreen;

    // Username
    lblUsername = new Label { Text = "Tên đăng nhập:", Location = new Point(50, 50) };
    txtUsername = new TextBox { Location = new Point(150, 47), Width = 180 };

    // Password
    lblPassword = new Label { Text = "Mật khẩu:", Location = new Point(50, 90) };
    txtPassword = new TextBox {
        Location = new Point(150, 87),
        Width = 180,
        PasswordChar = '*'
    };

    // Buttons
    btnLogin = new Button {
        Text = "Đăng nhập",
        Location = new Point(150, 130),
        Width = 85
    };
    btnRegister = new Button {
        Text = "Đăng ký",
        Location = new Point(245, 130),
        Width = 85
    };

    // Status
    lblStatus = new Label {
        Location = new Point(50, 180),
        Width = 300,
        ForeColor = Color.Red
    };

    this.Controls.AddRange(new Control[] {
        lblUsername, txtUsername,
        lblPassword, txtPassword,
        btnLogin, btnRegister,
        lblStatus
    });
}
```

### 6.3.2 Login Logic

**File: ChatClient/Forms/LoginForm.cs**

```csharp
public partial class LoginForm : Form
{
    private readonly SocketClientService _socketService;

    public LoginForm()
    {
        InitializeComponent();
        _socketService = new SocketClientService();

        btnLogin.Click += async (s, e) => await HandleLoginAsync();
        btnRegister.Click += (s, e) => OpenRegisterForm();
    }

    private async Task HandleLoginAsync()
    {
        var username = txtUsername.Text.Trim();
        var password = txtPassword.Text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            lblStatus.Text = "Vui lòng nhập đầy đủ thông tin";
            return;
        }

        btnLogin.Enabled = false;
        lblStatus.Text = "Đang đăng nhập...";

        try
        {
            // 1. Kết nối server
            if (!await _socketService.ConnectAsync())
            {
                lblStatus.Text = "Không thể kết nối server";
                btnLogin.Enabled = true;
                return;
            }

            // 2. Gửi request login
            var (success, message, user) = await _socketService.LoginAsync(username, password);

            if (!success)
            {
                // Check if OTP required
                if (message == "OTP_REQUIRED")
                {
                    var otpForm = new VerifyOtpForm(_socketService, username);
                    if (otpForm.ShowDialog() == DialogResult.OK)
                    {
                        // Retry login after OTP verified
                        (success, message, user) = await _socketService.LoginAsync(username, password);
                    }
                }

                if (!success)
                {
                    lblStatus.Text = message;
                    btnLogin.Enabled = true;
                    return;
                }
            }

            // 3. Login thành công - mở ChatForm
            this.Hide();
            var chatForm = new ChatFormNew(_socketService, user!);
            chatForm.FormClosed += (s, e) => this.Close();
            chatForm.Show();
        }
        catch (Exception ex)
        {
            lblStatus.Text = $"Lỗi: {ex.Message}";
            btnLogin.Enabled = true;
        }
    }

    private void OpenRegisterForm()
    {
        var registerForm = new RegisterForm(_socketService);
        registerForm.ShowDialog();
    }
}
```

---

## 6.4 RegisterForm

### 6.4.1 UI với Số điện thoại

**File: ChatClient/Forms/RegisterForm.Designer.cs**

```csharp
private void InitializeComponent()
{
    this.Text = "ChatApp - Đăng ký";
    this.Size = new Size(400, 400);

    int y = 30;
    int labelX = 30, inputX = 140;

    // Username
    lblUsername = new Label { Text = "Tên đăng nhập:", Location = new Point(labelX, y) };
    txtUsername = new TextBox { Location = new Point(inputX, y - 3), Width = 200 };
    y += 40;

    // Password
    lblPassword = new Label { Text = "Mật khẩu:", Location = new Point(labelX, y) };
    txtPassword = new TextBox {
        Location = new Point(inputX, y - 3),
        Width = 200,
        PasswordChar = '*'
    };
    y += 40;

    // Email
    lblEmail = new Label { Text = "Email:", Location = new Point(labelX, y) };
    txtEmail = new TextBox { Location = new Point(inputX, y - 3), Width = 200 };
    y += 40;

    // Họ tên
    lblHovaten = new Label { Text = "Họ và tên:", Location = new Point(labelX, y) };
    txtHovaten = new TextBox { Location = new Point(inputX, y - 3), Width = 200 };
    y += 40;

    // Số điện thoại
    lblSdt = new Label { Text = "Số điện thoại:", Location = new Point(labelX, y) };
    txtSdt = new TextBox { Location = new Point(inputX, y - 3), Width = 200 };
    y += 50;

    // Buttons
    btnRegister = new Button {
        Text = "Đăng ký",
        Location = new Point(inputX, y),
        Width = 100
    };
    btnCancel = new Button {
        Text = "Hủy",
        Location = new Point(inputX + 110, y),
        Width = 90
    };
    y += 50;

    // Status
    lblStatus = new Label {
        Location = new Point(labelX, y),
        Width = 320,
        ForeColor = Color.Red
    };

    this.Controls.AddRange(new Control[] {
        lblUsername, txtUsername,
        lblPassword, txtPassword,
        lblEmail, txtEmail,
        lblHovaten, txtHovaten,
        lblSdt, txtSdt,
        btnRegister, btnCancel,
        lblStatus
    });
}
```

### 6.4.2 Register Logic

**File: ChatClient/Forms/RegisterForm.cs**

```csharp
public partial class RegisterForm : Form
{
    private readonly SocketClientService _socketService;

    public RegisterForm(SocketClientService socketService)
    {
        InitializeComponent();
        _socketService = socketService;

        btnRegister.Click += async (s, e) => await HandleRegisterAsync();
        btnCancel.Click += (s, e) => this.Close();
    }

    private async Task HandleRegisterAsync()
    {
        // 1. Validate
        var username = txtUsername.Text.Trim();
        var password = txtPassword.Text;
        var email = txtEmail.Text.Trim();
        var hovaten = txtHovaten.Text.Trim();
        var sdt = txtSdt.Text.Trim();

        if (string.IsNullOrEmpty(username) ||
            string.IsNullOrEmpty(password) ||
            string.IsNullOrEmpty(email))
        {
            lblStatus.Text = "Vui lòng nhập đầy đủ thông tin bắt buộc";
            return;
        }

        if (!IsValidEmail(email))
        {
            lblStatus.Text = "Email không hợp lệ";
            return;
        }

        btnRegister.Enabled = false;
        lblStatus.Text = "Đang đăng ký...";

        try
        {
            // 2. Kết nối nếu chưa
            if (!_socketService.IsConnected)
            {
                if (!await _socketService.ConnectAsync())
                {
                    lblStatus.Text = "Không thể kết nối server";
                    btnRegister.Enabled = true;
                    return;
                }
            }

            // 3. Gửi request
            var (success, message) = await _socketService.RegisterAsync(
                username, password, email, hovaten, sdt
            );

            if (success)
            {
                MessageBox.Show(
                    "Đăng ký thành công!\nVui lòng kiểm tra email để xác thực OTP.",
                    "Thành công",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                this.Close();
            }
            else
            {
                lblStatus.Text = message;
                btnRegister.Enabled = true;
            }
        }
        catch (Exception ex)
        {
            lblStatus.Text = $"Lỗi: {ex.Message}";
            btnRegister.Enabled = true;
        }
    }

    private bool IsValidEmail(string email)
    {
        return email.Contains("@") && email.Contains(".");
    }
}
```

---

## 6.5 ChatFormNew

### 6.5.1 Load Conversations

**File: ChatClient/Forms/ChatFormNew.cs (Line 100-150)**

```csharp
private async Task LoadConversationsAsync()
{
    try
    {
        var conversations = await _socketService.GetConversationsAsync(_currentUser);
        if (conversations == null) return;

        // Clear existing
        listConversations.Items.Clear();

        foreach (var conv in conversations)
        {
            var item = new ListViewItem(conv.Name)
            {
                Tag = conv,
                ImageIndex = conv.IsPrivate ? 0 : 1  // 0=Private, 1=Group
            };
            item.SubItems.Add(conv.LastMessageTime?.ToString("HH:mm") ?? "");
            listConversations.Items.Add(item);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error loading conversations: {ex.Message}");
    }
}
```

### 6.5.2 Load Messages

**File: ChatClient/Forms/ChatFormNew.cs (Line 180-230)**

```csharp
private async Task LoadMessagesAsync(string conversationId)
{
    try
    {
        var messages = await _socketService.GetConversationMessagesAsync(
            _currentUser,
            conversationId
        );
        if (messages == null) return;

        // Clear existing
        panelMessages.Controls.Clear();

        int y = 10;
        foreach (var msg in messages)
        {
            var bubble = CreateMessageBubble(msg, ref y);
            panelMessages.Controls.Add(bubble);
        }

        // Scroll to bottom
        panelMessages.ScrollControlIntoView(panelMessages.Controls[^1]);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error loading messages: {ex.Message}");
    }
}

private Panel CreateMessageBubble(MessageInfo msg, ref int y)
{
    bool isMine = msg.SenderMatk == _currentUser.Matk;

    var bubble = new Panel
    {
        BackColor = isMine ? Color.LightBlue : Color.White,
        BorderStyle = BorderStyle.FixedSingle,
        Padding = new Padding(10),
        Width = 300,
        Location = new Point(isMine ? panelMessages.Width - 320 : 10, y)
    };

    var lblContent = new Label
    {
        Text = msg.Content,
        AutoSize = true,
        MaximumSize = new Size(280, 0)
    };

    var lblTime = new Label
    {
        Text = msg.Timestamp.ToString("HH:mm"),
        ForeColor = Color.Gray,
        Font = new Font(Font.FontFamily, 8),
        Location = new Point(0, lblContent.Height + 5)
    };

    bubble.Controls.Add(lblContent);
    bubble.Controls.Add(lblTime);
    bubble.Height = lblContent.Height + lblTime.Height + 20;

    y += bubble.Height + 10;
    return bubble;
}
```

### 6.5.3 Send Message

**File: ChatClient/Forms/ChatFormNew.cs (Line 280-320)**

```csharp
private async Task SendMessageAsync()
{
    var content = txtMessage.Text.Trim();
    if (string.IsNullOrEmpty(content)) return;
    if (_selectedConversation == null) return;

    btnSend.Enabled = false;
    txtMessage.Enabled = false;

    try
    {
        // Security label = user's clearance level by default
        var securityLabel = (int)numSecurityLabel.Value;

        var response = await _socketService.SendMessageToConversationAsync(
            _currentUser,
            _selectedConversation.Mactc,
            content,
            securityLabel
        );

        if (response?.Success == true)
        {
            txtMessage.Clear();
            await LoadMessagesAsync(_selectedConversation.Mactc);
        }
        else
        {
            MessageBox.Show(response?.Message ?? "Lỗi gửi tin nhắn");
        }
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Lỗi: {ex.Message}");
    }
    finally
    {
        btnSend.Enabled = true;
        txtMessage.Enabled = true;
        txtMessage.Focus();
    }
}
```

---

## 6.6 EncryptionHelper (Client)

**File: ChatClient/Utils/EncryptionHelper.cs**

```csharp
/// <summary>
/// Client-side encryption helper
/// PHẢI ĐỒNG BỘ KEY/IV VỚI SERVER
/// </summary>
public static class EncryptionHelper
{
    // KEY VÀ IV PHẢI GIỐNG SERVER
    private static readonly byte[] AesKey =
        Encoding.UTF8.GetBytes("ChatApp_AES_Key_32bytes_Long!@#$");
    private static readonly byte[] AesIv =
        Encoding.UTF8.GetBytes("ChatApp_AES_IV!!");

    public static string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = AesKey;
        aes.IV = AesIv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs, Encoding.UTF8))
        {
            sw.Write(plainText);
        }
        return Convert.ToBase64String(ms.ToArray());
    }

    public static string Decrypt(string cipherText)
    {
        var buffer = Convert.FromBase64String(cipherText);
        using var aes = Aes.Create();
        aes.Key = AesKey;
        aes.IV = AesIv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        using var ms = new MemoryStream(buffer);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs, Encoding.UTF8);
        return sr.ReadToEnd();
    }
}
```

---

_Tiếp theo: 07_HUONG_DAN.md - Hướng dẫn sử dụng và mở rộng_
