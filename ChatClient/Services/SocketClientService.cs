using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ChatClient.Models;
using ChatClient.Utils;
using System.Threading;

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
        private StreamReader? _reader;
        private StreamWriter? _writer;
        private readonly SemaphoreSlim _sendLock = new(1, 1);

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
            _reader = new StreamReader(_stream, Encoding.UTF8, leaveOpen: true);
            _writer = new StreamWriter(_stream, Encoding.UTF8) { AutoFlush = true };
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
        /// Lấy thông tin chi tiết người dùng (yêu cầu quyền admin trên server).
        /// </summary>
        public async Task<ServerResponse?> GetUserDetailsAsync(User currentUser, string targetUsername)
        {
            var request = new ChatRequest
            {
                Action = "GetUserDetails",
                SenderUsername = currentUser.Username,
                TargetUsername = targetUsername,
                ClearanceLevel = currentUser.ClearanceLevel
            };

            var responseJson = await SendRequestAsync(request);
            if (responseJson == null) return null;

            return JsonSerializer.Deserialize<ServerResponse>(responseJson);
        }

        /// <summary>
        /// Cập nhật thông tin người dùng (email/clearance) - server hiện tại chỉ cập nhật email/clearance.
        /// </summary>
        public async Task<ServerResponse?> UpdateUserAsync(User currentUser, string targetUsername, string? email = null, int? clearanceLevel = null)
        {
            var request = new ChatRequest
            {
                Action = "UpdateUser",
                SenderUsername = currentUser.Username,
                TargetUsername = targetUsername,
                Email = email ?? string.Empty,
                ClearanceLevel = clearanceLevel ?? currentUser.ClearanceLevel
            };

            var responseJson = await SendRequestAsync(request);
            if (responseJson == null) return null;

            return JsonSerializer.Deserialize<ServerResponse>(responseJson);
        }

        private static string GetMimeTypeFromFileName(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            return ext switch
            {
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                ".txt" => "text/plain",
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                _ => "application/octet-stream"
            };
        }

        /// <summary>
        /// Tải file đính kèm của một tin nhắn.
        /// </summary>
        public async Task<ServerResponse?> DownloadAttachmentAsync(User currentUser, int messageId)
        {
            if (messageId <= 0)
            {
                return new ServerResponse
                {
                    Success = false,
                    Message = "Invalid message ID."
                };
            }

            var request = new ChatRequest
            {
                Action = "DownloadAttachment",
                SenderUsername = currentUser.Username,
                MessageId = messageId,
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
        public async Task<ServerResponse?> RegisterAsync(string username, string password, string email, int clearanceLevel = 1, string hovaten = "")
        {
            var request = new ChatRequest
            {
                Action = "Register",
                SenderUsername = username,
                Password = password,
                Email = email,
                ClearanceLevel = clearanceLevel,
                Hovaten = hovaten
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
            if (_reader == null || _writer == null)
                throw new InvalidOperationException("Stream reader/writer chưa được khởi tạo.");

            await _sendLock.WaitAsync();
            try
            {
                var json = JsonSerializer.Serialize(request);
                var encrypted = EncryptionHelper.Encrypt(json);

                await _writer.WriteLineAsync(encrypted);

                var encryptedResponse = await _reader.ReadLineAsync();
                if (string.IsNullOrEmpty(encryptedResponse))
                    return null;

                var decrypted = EncryptionHelper.Decrypt(encryptedResponse);
                return decrypted;
            }
            finally
            {
                _sendLock.Release();
            }
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
        /// Lấy danh sách người dùng để chat (trừ user hiện tại).
        /// </summary>
        public async Task<ServerResponse?> GetUsersForChatAsync(User currentUser)
        {
            var request = new ChatRequest
            {
                Action = "GetUsersForChat",
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
        /// Rời khỏi cuộc trò chuyện (xóa phía mình).
        /// </summary>
        public async Task<ServerResponse?> LeaveConversationAsync(User currentUser, string conversationId)
        {
            var request = new ChatRequest
            {
                Action = "LeaveConversation",
                SenderUsername = currentUser.Username,
                ConversationId = conversationId,
                ClearanceLevel = currentUser.ClearanceLevel
            };

            var responseJson = await SendRequestAsync(request);
            if (responseJson == null) return null;

            return JsonSerializer.Deserialize<ServerResponse>(responseJson);
        }

        /// <summary>
        /// Xóa cuộc trò chuyện (chủ nhóm xóa).
        /// </summary>
        public async Task<ServerResponse?> DeleteConversationAsync(User currentUser, string conversationId)
        {
            var request = new ChatRequest
            {
                Action = "DeleteConversation",
                SenderUsername = currentUser.Username,
                ConversationId = conversationId,
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
        /// Tải file đính kèm lên server (từ đường dẫn).
        /// </summary>
        public async Task<ServerResponse?> UploadAttachmentAsync(User currentUser, string filePath)
        {
            if (!File.Exists(filePath))
            {
                return new ServerResponse
                {
                    Success = false,
                    Message = "File không tồn tại."
                };
            }

            var bytes = await File.ReadAllBytesAsync(filePath);
            var fileName = Path.GetFileName(filePath);
            return await UploadAttachmentAsync(currentUser, fileName, bytes);
        }

        /// <summary>
        /// Tải file đính kèm lên server (từ bytes).
        /// </summary>
        public async Task<ServerResponse?> UploadAttachmentAsync(User currentUser, string fileName, byte[] bytes)
        {
            var mimeType = GetMimeTypeFromFileName(fileName);

            var request = new ChatRequest
            {
                Action = "UploadAttachment",
                SenderUsername = currentUser.Username,
                Content = Convert.ToBase64String(bytes),
                FileName = fileName,
                MimeType = mimeType,
                FileSize = bytes.LongLength,
                SecurityLabel = currentUser.ClearanceLevel,
                ClearanceLevel = currentUser.ClearanceLevel
            };

            var responseJson = await SendRequestAsync(request);
            if (responseJson == null) return null;

            return JsonSerializer.Deserialize<ServerResponse>(responseJson);
        }

        /// <summary>
        /// Gửi tin nhắn kèm file đính kèm.
        /// </summary>
        public async Task<ServerResponse?> SendMessageWithAttachmentAsync(User currentUser, string conversationId, string content, int securityLabel, int attachmentId)
        {
            var request = new ChatRequest
            {
                Action = "SendMessageWithAttachment",
                SenderUsername = currentUser.Username,
                ConversationId = conversationId,
                Content = content,
                SecurityLabel = securityLabel,
                ClearanceLevel = currentUser.ClearanceLevel,
                AttachmentId = attachmentId
            };

            var responseJson = await SendRequestAsync(request);
            if (responseJson == null) return null;

            return JsonSerializer.Deserialize<ServerResponse>(responseJson);
        }

        /// <summary>
        /// Xóa tin nhắn.
        /// </summary>
        public async Task<ServerResponse?> DeleteMessageAsync(User currentUser, string messageId)
        {
            if (!int.TryParse(messageId, out var msgId))
            {
                return new ServerResponse
                {
                    Success = false,
                    Message = "Invalid message ID."
                };
            }

            var request = new ChatRequest
            {
                Action = "DeleteMessage",
                SenderUsername = currentUser.Username,
                MessageId = msgId,
                ClearanceLevel = currentUser.ClearanceLevel
            };

            var responseJson = await SendRequestAsync(request);
            if (responseJson == null) return null;

            return JsonSerializer.Deserialize<ServerResponse>(responseJson);
        }

        // ============================================================================
        // GROUP/CONVERSATION MANAGEMENT METHODS
        // ============================================================================

        /// <summary>
        /// Rời nhóm (không phải owner)
        /// </summary>
        public async Task<ServerResponse?> LeaveGroupAsync(User currentUser, string conversationId)
        {
            var request = new ChatRequest
            {
                Action = "LeaveGroup",
                SenderUsername = currentUser.Username,
                ConversationId = conversationId,
                ClearanceLevel = currentUser.ClearanceLevel
            };

            var responseJson = await SendRequestAsync(request);
            if (responseJson == null) return null;

            return JsonSerializer.Deserialize<ServerResponse>(responseJson);
        }

        /// <summary>
        /// Xóa chat riêng tư một phía
        /// </summary>
        public async Task<ServerResponse?> DeletePrivateChatOneSideAsync(User currentUser, string conversationId)
        {
            var request = new ChatRequest
            {
                Action = "DeletePrivateChatOneSide",
                SenderUsername = currentUser.Username,
                ConversationId = conversationId,
                ClearanceLevel = currentUser.ClearanceLevel
            };

            var responseJson = await SendRequestAsync(request);
            if (responseJson == null) return null;

            return JsonSerializer.Deserialize<ServerResponse>(responseJson);
        }

        /// <summary>
        /// Xóa/Archive nhóm (chỉ owner)
        /// </summary>
        public async Task<ServerResponse?> DeleteGroupAsync(User currentUser, string conversationId)
        {
            var request = new ChatRequest
            {
                Action = "DeleteGroup",
                SenderUsername = currentUser.Username,
                ConversationId = conversationId,
                ClearanceLevel = currentUser.ClearanceLevel
            };

            var responseJson = await SendRequestAsync(request);
            if (responseJson == null) return null;

            return JsonSerializer.Deserialize<ServerResponse>(responseJson);
        }

        /// <summary>
        /// Xóa archive (nhóm đã archive)
        /// </summary>
        public async Task<ServerResponse?> DeleteArchiveAsync(User currentUser, string conversationId)
        {
            var request = new ChatRequest
            {
                Action = "DeleteArchive",
                SenderUsername = currentUser.Username,
                ConversationId = conversationId,
                ClearanceLevel = currentUser.ClearanceLevel
            };

            var responseJson = await SendRequestAsync(request);
            if (responseJson == null) return null;

            return JsonSerializer.Deserialize<ServerResponse>(responseJson);
        }

        /// <summary>
        /// Kiểm tra trạng thái cuộc trò chuyện
        /// </summary>
        public async Task<ServerResponse?> GetConversationStatusAsync(User currentUser, string conversationId)
        {
            var request = new ChatRequest
            {
                Action = "GetConversationStatus",
                SenderUsername = currentUser.Username,
                ConversationId = conversationId,
                ClearanceLevel = currentUser.ClearanceLevel
            };

            var responseJson = await SendRequestAsync(request);
            if (responseJson == null) return null;

            return JsonSerializer.Deserialize<ServerResponse>(responseJson);
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
        public string Hovaten { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        // Conversation fields
        public string ConversationId { get; set; } = string.Empty; // MACTC
        public string ConversationName { get; set; } = string.Empty; // TENCTC
        public bool IsPrivateConversation { get; set; }
        public string TargetUsername { get; set; } = string.Empty; // MATK của người dùng đích
        public int Limit { get; set; }
        public int MessageId { get; set; }
        // Attachment fields
        public string FileName { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public int AttachmentId { get; set; }
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
        public string AttachmentFileName { get; set; } = string.Empty;
        public string AttachmentMimeType { get; set; } = string.Empty;
        public string AttachmentContentBase64 { get; set; } = string.Empty;
        public AdminUserDto? AdminUser { get; set; }
        public string[] UserList { get; set; } = Array.Empty<string>();
        public ConversationStatusDto? ConversationStatus { get; set; }
    }

    public class ConversationStatusDto
    {
        public string Status { get; set; } = string.Empty; // ACTIVE, ARCHIVED, DELETED_BY_ME, NOT_FOUND
        public bool IsPrivate { get; set; }
        public bool IsArchived { get; set; }
        public bool IsOwner { get; set; }
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
        public string Email { get; set; } = string.Empty; // EMAIL
        public string Hovaten { get; set; } = string.Empty; // HOVATEN
    }

    public class AdminUserDto
    {
        public string Matk { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Hovaten { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int ClearanceLevel { get; set; }
        public bool IsBannedGlobal { get; set; }
        public string Mavaitro { get; set; } = string.Empty;
        public DateTime NgayTao { get; set; }
        public bool IsOtpVerified { get; set; }
    }
}


