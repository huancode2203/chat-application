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

        /// <summary>
        /// Gửi lại mã OTP (resend OTP).
        /// </summary>
        public async Task<ServerResponse?> ResendOtpAsync(string username)
        {
            // Sử dụng action Register để gửi lại OTP (server sẽ kiểm tra account đã tồn tại và gửi OTP mới)
            // Hoặc có thể tạo action riêng "ResendOtp" trên server
            var request = new ChatRequest
            {
                Action = "ResendOtp",
                SenderUsername = username
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

        /// <summary>
        /// Gửi tin nhắn vào cuộc trò chuyện.
        /// </summary>
        public async Task<ServerResponse?> SendMessageToConversationAsync(User currentUser, string conversationId, string content, int securityLabel)
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

        /// <summary>
        /// Lấy danh sách cuộc trò chuyện của người dùng.
        /// </summary>
        public async Task<ServerResponse?> GetConversationsAsync(User currentUser)
        {
            var request = new ChatRequest
            {
                Action = "GetConversations",
                SenderUsername = currentUser.Username,
                ClearanceLevel = currentUser.ClearanceLevel
            };

            var responseJson = await SendRequestAsync(request);
            if (responseJson == null) return null;

            return JsonSerializer.Deserialize<ServerResponse>(responseJson);
        }

        /// <summary>
        /// Lấy tin nhắn trong cuộc trò chuyện.
        /// </summary>
        public async Task<ServerResponse?> GetConversationMessagesAsync(User currentUser, string conversationId)
        {
            var request = new ChatRequest
            {
                Action = "GetConversationMessages",
                SenderUsername = currentUser.Username,
                ConversationId = conversationId,
                ClearanceLevel = currentUser.ClearanceLevel
            };

            var responseJson = await SendRequestAsync(request);
            if (responseJson == null) return null;

            return JsonSerializer.Deserialize<ServerResponse>(responseJson);
        }

        /// <summary>
        /// Tạo nhóm chat mới.
        /// </summary>
        public async Task<ServerResponse?> CreateGroupAsync(User currentUser, string groupName, string groupType, string[] members)
        {
            var request = new ChatRequest
            {
                Action = "CreateConversation",
                SenderUsername = currentUser.Username,
                ConversationName = groupName,
                IsPrivateConversation = false,
                ClearanceLevel = currentUser.ClearanceLevel
            };

            var responseJson = await SendRequestAsync(request);
            if (responseJson == null) return null;

            var response = JsonSerializer.Deserialize<ServerResponse>(responseJson);
            if (response == null || !response.Success) return response;

            // Thêm các thành viên
            foreach (var member in members)
            {
                var addRequest = new ChatRequest
                {
                    Action = "AddMember",
                    SenderUsername = currentUser.Username,
                    ConversationId = response.ConversationId,
                    TargetUsername = member,
                    ClearanceLevel = currentUser.ClearanceLevel
                };
                await SendRequestAsync(addRequest);
            }

            return response;
        }

        /// <summary>
        /// Tạo chat riêng tư.
        /// </summary>
        public async Task<ServerResponse?> CreatePrivateChatAsync(User currentUser, string targetUsername)
        {
            var request = new ChatRequest
            {
                Action = "CreateConversation",
                SenderUsername = currentUser.Username,
                ConversationName = $"Chat với {targetUsername}",
                IsPrivateConversation = true,
                TargetUsername = targetUsername,
                ClearanceLevel = currentUser.ClearanceLevel
            };

            var responseJson = await SendRequestAsync(request);
            if (responseJson == null) return null;

            return JsonSerializer.Deserialize<ServerResponse>(responseJson);
        }

        /// <summary>
        /// Lấy danh sách thành viên trong cuộc trò chuyện.
        /// </summary>
        public async Task<ServerResponse?> GetConversationMembersAsync(User currentUser, string conversationId)
        {
            var request = new ChatRequest
            {
                Action = "GetConversationMembers",
                SenderUsername = currentUser.Username,
                ConversationId = conversationId,
                ClearanceLevel = currentUser.ClearanceLevel
            };

            var responseJson = await SendRequestAsync(request);
            if (responseJson == null) return null;

            return JsonSerializer.Deserialize<ServerResponse>(responseJson);
        }

        /// <summary>
        /// Thêm thành viên vào cuộc trò chuyện.
        /// </summary>
        public async Task<ServerResponse?> AddMemberToConversationAsync(User currentUser, string conversationId, string targetUsername)
        {
            var request = new ChatRequest
            {
                Action = "AddMember",
                SenderUsername = currentUser.Username,
                ConversationId = conversationId,
                TargetUsername = targetUsername,
                ClearanceLevel = currentUser.ClearanceLevel
            };

            var responseJson = await SendRequestAsync(request);
            if (responseJson == null) return null;

            return JsonSerializer.Deserialize<ServerResponse>(responseJson);
        }

        /// <summary>
        /// Xóa thành viên khỏi cuộc trò chuyện.
        /// </summary>
        public async Task<ServerResponse?> RemoveMemberFromConversationAsync(User currentUser, string conversationId, string targetUsername)
        {
            var request = new ChatRequest
            {
                Action = "RemoveMember",
                SenderUsername = currentUser.Username,
                ConversationId = conversationId,
                TargetUsername = targetUsername,
                ClearanceLevel = currentUser.ClearanceLevel
            };

            var responseJson = await SendRequestAsync(request);
            if (responseJson == null) return null;

            return JsonSerializer.Deserialize<ServerResponse>(responseJson);
        }

        /// <summary>
        /// Chặn thành viên trong cuộc trò chuyện.
        /// </summary>
        public async Task<ServerResponse?> BanMemberAsync(User currentUser, string conversationId, string targetUsername)
        {
            var request = new ChatRequest
            {
                Action = "BanMember",
                SenderUsername = currentUser.Username,
                ConversationId = conversationId,
                TargetUsername = targetUsername,
                ClearanceLevel = currentUser.ClearanceLevel
            };

            var responseJson = await SendRequestAsync(request);
            if (responseJson == null) return null;

            return JsonSerializer.Deserialize<ServerResponse>(responseJson);
        }

        /// <summary>
        /// Bỏ chặn thành viên.
        /// </summary>
        public async Task<ServerResponse?> UnbanMemberAsync(User currentUser, string conversationId, string targetUsername)
        {
            var request = new ChatRequest
            {
                Action = "UnbanMember",
                SenderUsername = currentUser.Username,
                ConversationId = conversationId,
                TargetUsername = targetUsername,
                ClearanceLevel = currentUser.ClearanceLevel
            };

            var responseJson = await SendRequestAsync(request);
            if (responseJson == null) return null;

            return JsonSerializer.Deserialize<ServerResponse>(responseJson);
        }

        /// <summary>
        /// Tắt tiếng thành viên.
        /// </summary>
        public async Task<ServerResponse?> MuteMemberAsync(User currentUser, string conversationId, string targetUsername)
        {
            var request = new ChatRequest
            {
                Action = "MuteMember",
                SenderUsername = currentUser.Username,
                ConversationId = conversationId,
                TargetUsername = targetUsername,
                ClearanceLevel = currentUser.ClearanceLevel
            };

            var responseJson = await SendRequestAsync(request);
            if (responseJson == null) return null;

            return JsonSerializer.Deserialize<ServerResponse>(responseJson);
        }

        /// <summary>
        /// Bỏ tắt tiếng thành viên.
        /// </summary>
        public async Task<ServerResponse?> UnmuteMemberAsync(User currentUser, string conversationId, string targetUsername)
        {
            var request = new ChatRequest
            {
                Action = "UnmuteMember",
                SenderUsername = currentUser.Username,
                ConversationId = conversationId,
                TargetUsername = targetUsername,
                ClearanceLevel = currentUser.ClearanceLevel
            };

            var responseJson = await SendRequestAsync(request);
            if (responseJson == null) return null;

            return JsonSerializer.Deserialize<ServerResponse>(responseJson);
        }

        /// <summary>
        /// Thăng cấp thành viên (promote).
        /// </summary>
        public async Task<ServerResponse?> PromoteMemberAsync(User currentUser, string conversationId, string targetUsername)
        {
            // Note: This would need a new server action "PromoteMember"
            var request = new ChatRequest
            {
                Action = "PromoteMember",
                SenderUsername = currentUser.Username,
                ConversationId = conversationId,
                TargetUsername = targetUsername,
                ClearanceLevel = currentUser.ClearanceLevel
            };

            var responseJson = await SendRequestAsync(request);
            if (responseJson == null) return null;

            return JsonSerializer.Deserialize<ServerResponse>(responseJson);
        }

        /// <summary>
        /// Tải file đính kèm lên server.
        /// </summary>
        public async Task<ServerResponse?> UploadAttachmentAsync(User currentUser, string filePath)
        {
            // Note: This would need file upload implementation
            // For now, return a placeholder
            return new ServerResponse
            {
                Success = false,
                Message = "File upload not yet implemented"
            };
        }

        /// <summary>
        /// Gửi tin nhắn kèm file đính kèm.
        /// </summary>
        public async Task<ServerResponse?> SendMessageWithAttachmentAsync(User currentUser, string conversationId, string content, int securityLabel, int attachmentId)
        {
            // Note: This would need attachment support in the server
            return new ServerResponse
            {
                Success = false,
                Message = "Attachment support not yet implemented"
            };
        }

        /// <summary>
        /// Xóa tin nhắn.
        /// </summary>
        public async Task<ServerResponse?> DeleteMessageAsync(User currentUser, string messageId)
        {
            // Note: This would need a new server action "DeleteMessage"
            return new ServerResponse
            {
                Success = false,
                Message = "Delete message not yet implemented"
            };
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
        public string SenderUsername { get; set; } = string.Empty; // TENTK (MATK)
        public string ReceiverUsername { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int SecurityLabel { get; set; }
        public int ClearanceLevel { get; set; }
        // Thêm các field cho authentication
        public string Password { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        // Conversation fields
        public string ConversationId { get; set; } = string.Empty; // MACTC
        public string ConversationName { get; set; } = string.Empty; // TENCTC
        public bool IsPrivateConversation { get; set; }
        public string TargetUsername { get; set; } = string.Empty; // MATK của người dùng đích
    }

    /// <summary>
    /// DTO response server trả về (trước khi mã hóa).
    /// </summary>
    public class ServerResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public ChatMessageDto[] Messages { get; set; } = Array.Empty<ChatMessageDto>();
        public ConversationDto[] Conversations { get; set; } = Array.Empty<ConversationDto>();
        public MemberDto[] Members { get; set; } = Array.Empty<MemberDto>();
        public int ClearanceLevel { get; set; }
        public string ConversationId { get; set; } = string.Empty;
        public int MessageId { get; set; }
        public int AttachmentId { get; set; }
    }

    public class ChatMessageDto
    {
        public int MessageId { get; set; } // MATN
        public string ConversationId { get; set; } = string.Empty; // MACTC
        public string Sender { get; set; } = string.Empty; // MATK người gửi
        public string Receiver { get; set; } = string.Empty; // MATK người nhận (nếu có)
        public string Content { get; set; } = string.Empty; // NOIDUNG
        public int SecurityLabel { get; set; } // SECURITYLABEL
        public DateTime Timestamp { get; set; } // NGAYGUI
    }

    public class ConversationDto
    {
        public string ConversationId { get; set; } = string.Empty; // MACTC
        public string ConversationName { get; set; } = string.Empty; // TENCTC
        public bool IsPrivate { get; set; } // IS_PRIVATE
        public DateTime CreatedAt { get; set; } // NGAYTAO
        public int MemberCount { get; set; } // Số lượng thành viên
    }

    public class MemberDto
    {
        public string Username { get; set; } = string.Empty; // TENTK
        public string Matk { get; set; } = string.Empty; // MATK
        public string Role { get; set; } = string.Empty; // QUYEN
        public bool IsBanned { get; set; } // IS_BANNED
        public bool IsMuted { get; set; } // IS_MUTED
        public DateTime JoinedDate { get; set; } // NGAYTHAMGIA
    }
}


