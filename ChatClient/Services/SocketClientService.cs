using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ChatClient.Models;
using ChatClient.Utils;

namespace ChatClient.Services
{
    /// <summary>
    /// Service TCP client.
    /// - Kết nối tới server qua TCP.
    /// - Đóng gói request dạng JSON, mã hóa AES, gửi qua socket.
    /// - Nhận response, giải mã và parse JSON.
    /// </summary>
    public class SocketClientService : IDisposable
    {
        private readonly string _host;
        private readonly int _port;
        private TcpClient? _client;
        private NetworkStream? _stream;

        public SocketClientService(string host, int port)
        {
            _host = host;
            _port = port;
        }

        public async Task ConnectAsync()
        {
            _client = new TcpClient();
            await _client.ConnectAsync(_host, _port);
            _stream = _client.GetStream();
        }

        public bool IsConnected => _client is { Connected: true };

        /// <summary>
        /// Gửi request đơn giản lên server.
        /// </summary>
        public async Task<ServerResponse?> SendChatMessageAsync(User currentUser, string receiverUsername, string content, int securityLabel)
        {
            var request = new ChatRequest
            {
                Action = "SendMessage",
                SenderUsername = currentUser.Username,
                ReceiverUsername = receiverUsername,
                Content = content,
                SecurityLabel = securityLabel,
                ClearanceLevel = currentUser.ClearanceLevel
            };

            var responseJson = await SendRequestAsync(request);
            if (responseJson == null) return null;

            return JsonSerializer.Deserialize<ServerResponse>(responseJson);
        }

        /// <summary>
        /// Lấy danh sách tin nhắn người dùng (server sẽ áp dụng MAC/VPD để chỉ trả về tin được phép xem).
        /// </summary>
        public async Task<ServerResponse?> GetMessagesAsync(User currentUser)
        {
            var request = new ChatRequest
            {
                Action = "GetMessages",
                SenderUsername = currentUser.Username,
                ClearanceLevel = currentUser.ClearanceLevel
            };

            var responseJson = await SendRequestAsync(request);
            if (responseJson == null) return null;

            return JsonSerializer.Deserialize<ServerResponse>(responseJson);
        }

        /// <summary>
        /// Đăng nhập.
        /// </summary>
        public async Task<ServerResponse?> LoginAsync(string username, string password)
        {
            var request = new ChatRequest
            {
                Action = "Login",
                SenderUsername = username,
                Password = password
            };

            var responseJson = await SendRequestAsync(request);
            if (responseJson == null) return null;

            return JsonSerializer.Deserialize<ServerResponse>(responseJson);
        }

        /// <summary>
        /// Đăng ký tài khoản mới.
        /// </summary>
        public async Task<ServerResponse?> RegisterAsync(string username, string password, string email, int clearanceLevel = 1)
        {
            var request = new ChatRequest
            {
                Action = "Register",
                SenderUsername = username,
                Password = password,
                Email = email,
                ClearanceLevel = clearanceLevel
            };

            var responseJson = await SendRequestAsync(request);
            if (responseJson == null) return null;

            return JsonSerializer.Deserialize<ServerResponse>(responseJson);
        }

        /// <summary>
        /// Xác minh OTP.
        /// </summary>
        public async Task<ServerResponse?> VerifyOtpAsync(string username, string otp)
        {
            var request = new ChatRequest
            {
                Action = "VerifyOtp",
                SenderUsername = username,
                Otp = otp
            };

            var responseJson = await SendRequestAsync(request);
            if (responseJson == null) return null;

            return JsonSerializer.Deserialize<ServerResponse>(responseJson);
        }

        /// <summary>
        /// Yêu cầu quên mật khẩu (gửi OTP qua email).
        /// </summary>
        public async Task<ServerResponse?> ForgotPasswordRequestAsync(string username, string email)
        {
            var request = new ChatRequest
            {
                Action = "ForgotPasswordRequest",
                SenderUsername = username,
                Email = email
            };

            var responseJson = await SendRequestAsync(request);
            if (responseJson == null) return null;

            return JsonSerializer.Deserialize<ServerResponse>(responseJson);
        }

        /// <summary>
        /// Đặt lại mật khẩu với OTP.
        /// </summary>
        public async Task<ServerResponse?> ResetPasswordAsync(string username, string otp, string newPassword)
        {
            var request = new ChatRequest
            {
                Action = "ResetPassword",
                SenderUsername = username,
                Otp = otp,
                NewPassword = newPassword
            };

            var responseJson = await SendRequestAsync(request);
            if (responseJson == null) return null;

            return JsonSerializer.Deserialize<ServerResponse>(responseJson);
        }

        private async Task<string?> SendRequestAsync(ChatRequest request)
        {
            if (_stream == null)
                throw new InvalidOperationException("Chưa kết nối tới server.");

            var json = JsonSerializer.Serialize(request);
            var encrypted = EncryptionHelper.Encrypt(json);

            var data = Encoding.UTF8.GetBytes(encrypted + "\n");
            await _stream.WriteAsync(data, 0, data.Length);
            await _stream.FlushAsync();

            // Đọc theo từng dòng.
            using var reader = new StreamReader(_stream, Encoding.UTF8, leaveOpen: true);
            var encryptedResponse = await reader.ReadLineAsync();
            if (string.IsNullOrEmpty(encryptedResponse))
                return null;

            var decrypted = EncryptionHelper.Decrypt(encryptedResponse);
            return decrypted;
        }

        public void Dispose()
        {
            _stream?.Dispose();
            _client?.Dispose();
        }
    }

    /// <summary>
    /// DTO request gửi qua socket (sau đó sẽ được mã hóa).
    /// </summary>
    public class ChatRequest
    {
        public string Action { get; set; } = string.Empty; // "SendMessage", "GetMessages", "Login", ...
        public string SenderUsername { get; set; } = string.Empty;
        public string ReceiverUsername { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int SecurityLabel { get; set; }
        public int ClearanceLevel { get; set; }
        // Thêm các field cho authentication
        public string Password { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO response server trả về (trước khi mã hóa).
    /// </summary>
    public class ServerResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public ChatMessageDto[] Messages { get; set; } = Array.Empty<ChatMessageDto>();
        public int ClearanceLevel { get; set; }
    }

    public class ChatMessageDto
    {
        public string Sender { get; set; } = string.Empty;
        public string Receiver { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int SecurityLabel { get; set; }
        public DateTime Timestamp { get; set; }
    }
}


