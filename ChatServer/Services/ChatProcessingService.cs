using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ChatServer.Database;
using ChatServer.Utils;

namespace ChatServer.Services
{
    public class ChatProcessingService
    {
        private readonly DbContext _dbContext;
        private readonly MACService _macService;
        private readonly EmailService _emailService;

        public ChatProcessingService(DbContext dbContext, MACService macService, EmailService emailService)
        {
            _dbContext = dbContext;
            _macService = macService;
            _emailService = emailService;
        }

        public async Task<string> HandleRequestAsync(string requestJson)
        {
            var request = JsonSerializer.Deserialize<ChatRequest>(requestJson);
            if (request == null)
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "Invalid request"
                });
            }

            // Actions không cần authentication
            if (request.Action == "Register" || request.Action == "Login" || 
                request.Action == "ForgotPasswordRequest" || request.Action == "ResetPassword" || 
                request.Action == "VerifyOtp")
            {
                return request.Action switch
                {
                    "Login" => await HandleLoginAsync(request),
                    "Register" => await HandleRegisterAsync(request),
                    "VerifyOtp" => await HandleVerifyOtpAsync(request),
                    "ForgotPasswordRequest" => await HandleForgotPasswordRequestAsync(request),
                    "ResetPassword" => await HandleResetPasswordAsync(request),
                    _ => JsonSerializer.Serialize(new ServerResponse { Success = false, Message = $"Unknown action: {request.Action}" })
                };
            }

            // Actions cần authentication
            if (!string.IsNullOrEmpty(request.SenderUsername))
            {
                // Kiểm tra banned global
                var account = await _dbContext.GetUserAccountAsync(request.SenderUsername);
                if (account != null && account.IsBannedGlobal)
                {
                    return JsonSerializer.Serialize(new ServerResponse
                    {
                        Success = false,
                        Message = "Your account has been banned globally."
                    });
                }

                await _dbContext.SetSessionUserLevelAsync(request.SenderUsername, request.ClearanceLevel);
            }

            return request.Action switch
            {
                "SendMessage" => await HandleSendMessageAsync(request),
                "GetMessages" => await HandleGetMessagesAsync(request),
                "CreateConversation" => await HandleCreateConversationAsync(request),
                "GetConversations" => await HandleGetConversationsAsync(request),
                "GetConversationMessages" => await HandleGetConversationMessagesAsync(request),
                "AddMember" => await HandleAddMemberAsync(request),
                "RemoveMember" => await HandleRemoveMemberAsync(request),
                "DeleteConversation" => await HandleDeleteConversationAsync(request),
                "BanMember" => await HandleBanMemberAsync(request),
                "UnbanMember" => await HandleUnbanMemberAsync(request),
                "MuteMember" => await HandleMuteMemberAsync(request),
                "UnmuteMember" => await HandleUnmuteMemberAsync(request),
                _ => JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = $"Unknown action: {request.Action}"
                })
            };
        }

        private async Task<string> HandleLoginAsync(ChatRequest request)
        {
            if (string.IsNullOrEmpty(request.SenderUsername) || string.IsNullOrEmpty(request.Password))
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "Username and password are required."
                });
            }

            var account = await _dbContext.GetUserAccountAsync(request.SenderUsername);
            if (account == null)
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "Invalid username or password."
                });
            }

            if (account.IsBannedGlobal)
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "Your account has been banned."
                });
            }

            if (!PasswordHelper.VerifyPassword(request.Password, account.PasswordHash))
            {
                await _dbContext.WriteAuditLogAsync(request.SenderUsername, "LOGIN_FAILED", "Invalid password", 0);
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "Invalid username or password."
                });
            }

            await _dbContext.WriteAuditLogAsync(request.SenderUsername, "LOGIN_SUCCESS", request.SenderUsername, account.ClearanceLevel);

            return JsonSerializer.Serialize(new ServerResponse
            {
                Success = true,
                Message = "Login successful.",
                ClearanceLevel = account.ClearanceLevel
            });
        }

        private async Task<string> HandleRegisterAsync(ChatRequest request)
        {
            if (string.IsNullOrEmpty(request.SenderUsername) || string.IsNullOrEmpty(request.Password) || 
                string.IsNullOrEmpty(request.Email))
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "Username, password, and email are required."
                });
            }

            if (await _dbContext.AccountExistsAsync(request.SenderUsername))
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "Username already exists."
                });
            }

            var passwordHash = PasswordHelper.HashPassword(request.Password);
            var clearanceLevel = request.ClearanceLevel > 0 ? request.ClearanceLevel : 1;

            try
            {
                await _dbContext.CreateAccountAsync(
                    request.SenderUsername,
                    request.SenderUsername,
                    passwordHash,
                    null,
                    clearanceLevel
                );

                var otp = PasswordHelper.GenerateOtp();
                var otpHash = PasswordHelper.HashPassword(otp);
                await _dbContext.CreateOtpAsync(request.SenderUsername, request.Email, otpHash);

                await _emailService.SendRegistrationOtpAsync(request.Email, request.SenderUsername, otp);

                await _dbContext.WriteAuditLogAsync(request.SenderUsername, "REGISTER", request.SenderUsername, clearanceLevel);

                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = true,
                    Message = "Registration successful. Please verify OTP sent to your email."
                });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = $"Registration failed: {ex.Message}"
                });
            }
        }

        private async Task<string> HandleVerifyOtpAsync(ChatRequest request)
        {
            if (string.IsNullOrEmpty(request.SenderUsername) || string.IsNullOrEmpty(request.Otp))
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "Username and OTP are required."
                });
            }

            var otpHash = PasswordHelper.HashPassword(request.Otp);
            var isValid = await _dbContext.VerifyOtpAsync(request.SenderUsername, otpHash);

            if (isValid)
            {
                await _dbContext.WriteAuditLogAsync(request.SenderUsername, "VERIFY_OTP_SUCCESS", request.SenderUsername, 0);
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = true,
                    Message = "OTP verified successfully."
                });
            }

            await _dbContext.WriteAuditLogAsync(request.SenderUsername, "VERIFY_OTP_FAILED", request.SenderUsername, 0);
            return JsonSerializer.Serialize(new ServerResponse
            {
                Success = false,
                Message = "Invalid or expired OTP."
            });
        }

        private async Task<string> HandleForgotPasswordRequestAsync(ChatRequest request)
        {
            if (string.IsNullOrEmpty(request.SenderUsername) || string.IsNullOrEmpty(request.Email))
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "Username and email are required."
                });
            }

            if (!await _dbContext.AccountExistsAsync(request.SenderUsername))
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = true,
                    Message = "If the account exists, an OTP has been sent to your email."
                });
            }

            var storedEmail = await _dbContext.GetEmailByMatkAsync(request.SenderUsername);
            if (storedEmail != request.Email)
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "Email does not match the account."
                });
            }

            var otp = PasswordHelper.GenerateOtp();
            var otpHash = PasswordHelper.HashPassword(otp);
            await _dbContext.CreateOtpAsync(request.SenderUsername, request.Email, otpHash);

            await _emailService.SendPasswordResetOtpAsync(request.Email, request.SenderUsername, otp);
            await _dbContext.WriteAuditLogAsync(request.SenderUsername, "FORGOT_PASSWORD_REQUEST", request.SenderUsername, 0);

            return JsonSerializer.Serialize(new ServerResponse
            {
                Success = true,
                Message = "OTP has been sent to your email."
            });
        }

        private async Task<string> HandleResetPasswordAsync(ChatRequest request)
        {
            if (string.IsNullOrEmpty(request.SenderUsername) || string.IsNullOrEmpty(request.Otp) || 
                string.IsNullOrEmpty(request.NewPassword))
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "Username, OTP, and new password are required."
                });
            }

            var otpHash = PasswordHelper.HashPassword(request.Otp);
            var isValid = await _dbContext.VerifyOtpAsync(request.SenderUsername, otpHash);

            if (!isValid)
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "Invalid or expired OTP."
                });
            }

            var newPasswordHash = PasswordHelper.HashPassword(request.NewPassword);
            await _dbContext.UpdatePasswordAsync(request.SenderUsername, newPasswordHash);

            await _dbContext.WriteAuditLogAsync(request.SenderUsername, "RESET_PASSWORD", request.SenderUsername, 0);

            return JsonSerializer.Serialize(new ServerResponse
            {
                Success = true,
                Message = "Password reset successfully."
            });
        }

        private async Task<string> HandleSendMessageAsync(ChatRequest request)
        {
            if (!_macService.CanWrite(request.ClearanceLevel, request.SecurityLabel))
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "MAC policy violation: no write down."
                });
            }

            var senderAccount = await _dbContext.GetUserAccountAsync(request.SenderUsername);
            if (senderAccount == null)
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "Sender not found."
                });
            }

            // Kiểm tra permission trong conversation
            if (!string.IsNullOrEmpty(request.ConversationId))
            {
                var permission = await _dbContext.GetMemberPermissionAsync(request.ConversationId, request.SenderUsername);
                if (permission == null)
                {
                    return JsonSerializer.Serialize(new ServerResponse
                    {
                        Success = false,
                        Message = "You are not a member of this conversation."
                    });
                }

                if (permission.IsBanned)
                {
                    return JsonSerializer.Serialize(new ServerResponse
                    {
                        Success = false,
                        Message = "You have been banned from this conversation."
                    });
                }

                if (permission.IsMuted)
                {
                    return JsonSerializer.Serialize(new ServerResponse
                    {
                        Success = false,
                        Message = "You have been muted in this conversation."
                    });
                }

                var messageId = await _dbContext.SendMessageToConversationAsync(
                    request.ConversationId, 
                    senderAccount.Matk, 
                    request.Content, 
                    request.SecurityLabel
                );

                await _dbContext.WriteAuditLogAsync(senderAccount.Matk, "SEND_MESSAGE", request.ConversationId, request.SecurityLabel);

                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = true,
                    Message = "Message sent.",
                    MessageId = messageId
                });
            }

            // Legacy: send to own MATK table
            await _dbContext.InsertMessageAsync(senderAccount.Matk, request.Content, request.SecurityLabel);
            await _dbContext.WriteAuditLogAsync(senderAccount.Matk, "SEND_MESSAGE", request.ReceiverUsername, request.SecurityLabel);

            return JsonSerializer.Serialize(new ServerResponse
            {
                Success = true,
                Message = "Message sent."
            });
        }

        private async Task<string> HandleGetMessagesAsync(ChatRequest request)
        {
            var userAccount = await _dbContext.GetUserAccountAsync(request.SenderUsername);
            if (userAccount == null)
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "User not found."
                });
            }

            var records = await _dbContext.GetMessagesForUserAsync(userAccount.Matk);

            var visible = new List<ChatMessageDto>();
            foreach (var r in records)
            {
                if (_macService.CanRead(request.ClearanceLevel, r.SecurityLabel))
                {
                    visible.Add(new ChatMessageDto
                    {
                        Sender = r.SenderMatk,
                        Receiver = r.SenderMatk,
                        Content = r.Content,
                        SecurityLabel = r.SecurityLabel,
                        Timestamp = r.Timestamp
                    });
                }
            }

            await _dbContext.WriteAuditLogAsync(userAccount.Matk, "VIEW_MESSAGES", request.SenderUsername, request.ClearanceLevel);

            return JsonSerializer.Serialize(new ServerResponse
            {
                Success = true,
                Message = "Messages loaded.",
                Messages = visible.ToArray()
            });
        }

        private async Task<string> HandleCreateConversationAsync(ChatRequest request)
        {
            if (string.IsNullOrEmpty(request.ConversationName))
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "Conversation name is required."
                });
            }

            var mactc = "CTC_" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
            var isPrivate = request.IsPrivateConversation ? "Y" : "N";
            var maloaictc = request.IsPrivateConversation ? "PRIVATE" : "GROUP";

            try
            {
                await _dbContext.CreateConversationAsync(
                    mactc,
                    maloaictc,
                    request.ConversationName,
                    request.SenderUsername,
                    isPrivate,
                    request.SenderUsername
                );

                await _dbContext.WriteAuditLogAsync(request.SenderUsername, "CREATE_CONVERSATION", mactc, request.ClearanceLevel);

                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = true,
                    Message = "Conversation created.",
                    ConversationId = mactc
                });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = $"Failed to create conversation: {ex.Message}"
                });
            }
        }

        private async Task<string> HandleGetConversationsAsync(ChatRequest request)
        {
            try
            {
                var conversations = await _dbContext.GetUserConversationsAsync(request.SenderUsername);

                var convDtos = conversations.Select(c => new ConversationDto
                {
                    ConversationId = c.Mactc,
                    ConversationName = c.Tenctc,
                    IsPrivate = c.IsPrivate,
                    CreatedAt = c.NgayTao,
                    MemberCount = c.MemberCount
                }).ToArray();

                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = true,
                    Message = "Conversations loaded.",
                    Conversations = convDtos
                });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = $"Failed to load conversations: {ex.Message}"
                });
            }
        }

        private async Task<string> HandleGetConversationMessagesAsync(ChatRequest request)
        {
            if (string.IsNullOrEmpty(request.ConversationId))
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "Conversation ID is required."
                });
            }

            // Kiểm tra member permission
            var permission = await _dbContext.GetMemberPermissionAsync(request.ConversationId, request.SenderUsername);
            if (permission == null)
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "You are not a member of this conversation."
                });
            }

            var records = await _dbContext.GetConversationMessagesAsync(request.ConversationId, 100);

            var visible = new List<ChatMessageDto>();
            foreach (var r in records)
            {
                if (_macService.CanRead(request.ClearanceLevel, r.SecurityLabel))
                {
                    visible.Add(new ChatMessageDto
                    {
                        MessageId = r.MessageId,
                        Sender = r.SenderMatk,
                        Content = r.Content,
                        SecurityLabel = r.SecurityLabel,
                        Timestamp = r.Timestamp
                    });
                }
            }

            return JsonSerializer.Serialize(new ServerResponse
            {
                Success = true,
                Message = "Messages loaded.",
                Messages = visible.ToArray()
            });
        }

        private async Task<string> HandleAddMemberAsync(ChatRequest request)
        {
            if (string.IsNullOrEmpty(request.ConversationId) || string.IsNullOrEmpty(request.TargetUsername))
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "Conversation ID and target username are required."
                });
            }

            var permission = await _dbContext.GetMemberPermissionAsync(request.ConversationId, request.SenderUsername);
            if (permission == null || !permission.CanAdd)
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "You don't have permission to add members."
                });
            }

            try
            {
                await _dbContext.AddMemberAsync(request.ConversationId, request.TargetUsername);
                await _dbContext.WriteAuditLogAsync(request.SenderUsername, "ADD_MEMBER", 
                    $"{request.ConversationId}:{request.TargetUsername}", request.ClearanceLevel);

                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = true,
                    Message = "Member added successfully."
                });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = $"Failed to add member: {ex.Message}"
                });
            }
        }

        private async Task<string> HandleRemoveMemberAsync(ChatRequest request)
        {
            if (string.IsNullOrEmpty(request.ConversationId) || string.IsNullOrEmpty(request.TargetUsername))
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "Conversation ID and target username are required."
                });
            }

            var permission = await _dbContext.GetMemberPermissionAsync(request.ConversationId, request.SenderUsername);
            if (permission == null || !permission.CanRemove)
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "You don't have permission to remove members."
                });
            }

            try
            {
                await _dbContext.RemoveMemberAsync(request.ConversationId, request.TargetUsername);
                await _dbContext.WriteAuditLogAsync(request.SenderUsername, "REMOVE_MEMBER", 
                    $"{request.ConversationId}:{request.TargetUsername}", request.ClearanceLevel);

                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = true,
                    Message = "Member removed successfully."
                });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = $"Failed to remove member: {ex.Message}"
                });
            }
        }

        private async Task<string> HandleDeleteConversationAsync(ChatRequest request)
        {
            if (string.IsNullOrEmpty(request.ConversationId))
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "Conversation ID is required."
                });
            }

            var permission = await _dbContext.GetMemberPermissionAsync(request.ConversationId, request.SenderUsername);
            if (permission == null || !permission.CanDelete)
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "You don't have permission to delete this conversation."
                });
            }

            try
            {
                await _dbContext.DeleteConversationAsync(request.ConversationId);
                await _dbContext.WriteAuditLogAsync(request.SenderUsername, "DELETE_CONVERSATION", 
                    request.ConversationId, request.ClearanceLevel);

                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = true,
                    Message = "Conversation deleted successfully."
                });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = $"Failed to delete conversation: {ex.Message}"
                });
            }
        }

        private async Task<string> HandleBanMemberAsync(ChatRequest request)
        {
            if (string.IsNullOrEmpty(request.ConversationId) || string.IsNullOrEmpty(request.TargetUsername))
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "Conversation ID and target username are required."
                });
            }

            var permission = await _dbContext.GetMemberPermissionAsync(request.ConversationId, request.SenderUsername);
            if (permission == null || !permission.CanBan)
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "You don't have permission to ban members."
                });
            }

            try
            {
                await _dbContext.BanMemberAsync(request.ConversationId, request.TargetUsername);
                await _dbContext.WriteAuditLogAsync(request.SenderUsername, "BAN_MEMBER", 
                    $"{request.ConversationId}:{request.TargetUsername}", request.ClearanceLevel);

                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = true,
                    Message = "Member banned successfully."
                });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = $"Failed to ban member: {ex.Message}"
                });
            }
        }

        private async Task<string> HandleUnbanMemberAsync(ChatRequest request)
        {
            if (string.IsNullOrEmpty(request.ConversationId) || string.IsNullOrEmpty(request.TargetUsername))
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "Conversation ID and target username are required."
                });
            }

            var permission = await _dbContext.GetMemberPermissionAsync(request.ConversationId, request.SenderUsername);
            if (permission == null || !permission.CanBan)
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "You don't have permission to unban members."
                });
            }

            try
            {
                await _dbContext.UnbanMemberAsync(request.ConversationId, request.TargetUsername);
                await _dbContext.WriteAuditLogAsync(request.SenderUsername, "UNBAN_MEMBER", 
                    $"{request.ConversationId}:{request.TargetUsername}", request.ClearanceLevel);

                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = true,
                    Message = "Member unbanned successfully."
                });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = $"Failed to unban member: {ex.Message}"
                });
            }
        }

        private async Task<string> HandleMuteMemberAsync(ChatRequest request)
        {
            if (string.IsNullOrEmpty(request.ConversationId) || string.IsNullOrEmpty(request.TargetUsername))
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "Conversation ID and target username are required."
                });
            }

            var permission = await _dbContext.GetMemberPermissionAsync(request.ConversationId, request.SenderUsername);
            if (permission == null || !permission.CanMute)
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "You don't have permission to mute members."
                });
            }

            try
            {
                await _dbContext.MuteMemberAsync(request.ConversationId, request.TargetUsername);
                await _dbContext.WriteAuditLogAsync(request.SenderUsername, "MUTE_MEMBER", 
                    $"{request.ConversationId}:{request.TargetUsername}", request.ClearanceLevel);

                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = true,
                    Message = "Member muted successfully."
                });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = $"Failed to mute member: {ex.Message}"
                });
            }
        }

        private async Task<string> HandleUnmuteMemberAsync(ChatRequest request)
        {
            if (string.IsNullOrEmpty(request.ConversationId) || string.IsNullOrEmpty(request.TargetUsername))
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "Conversation ID and target username are required."
                });
            }

            var permission = await _dbContext.GetMemberPermissionAsync(request.ConversationId, request.SenderUsername);
            if (permission == null || !permission.CanMute)
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "You don't have permission to unmute members."
                });
            }

            try
            {
                await _dbContext.UnmuteMemberAsync(request.ConversationId, request.TargetUsername);
                await _dbContext.WriteAuditLogAsync(request.SenderUsername, "UNMUTE_MEMBER", 
                    $"{request.ConversationId}:{request.TargetUsername}", request.ClearanceLevel);

                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = true,
                    Message = "Member unmuted successfully."
                });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = $"Failed to unmute member: {ex.Message}"
                });
            }
        }
    }

    // DTOs
    public class ChatRequest
    {
        public string Action { get; set; } = string.Empty;
        public string SenderUsername { get; set; } = string.Empty;
        public string ReceiverUsername { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int SecurityLabel { get; set; }
        public int ClearanceLevel { get; set; }
        public string Password { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        // Conversation fields
        public string ConversationId { get; set; } = string.Empty;
        public string ConversationName { get; set; } = string.Empty;
        public bool IsPrivateConversation { get; set; }
        public string TargetUsername { get; set; } = string.Empty;
    }

    public class ServerResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public ChatMessageDto[] Messages { get; set; } = Array.Empty<ChatMessageDto>();
        public ConversationDto[] Conversations { get; set; } = Array.Empty<ConversationDto>();
        public int ClearanceLevel { get; set; }
        public string ConversationId { get; set; } = string.Empty;
        public int MessageId { get; set; }
    }

    public class ChatMessageDto
    {
        public int MessageId { get; set; }
        public string Sender { get; set; } = string.Empty;
        public string Receiver { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int SecurityLabel { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class ConversationDto
    {
        public string ConversationId { get; set; } = string.Empty;
        public string ConversationName { get; set; } = string.Empty;
        public bool IsPrivate { get; set; }
        public DateTime CreatedAt { get; set; }
        public int MemberCount { get; set; }
    }
}