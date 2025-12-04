using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ChatServer.Database;
using ChatServer.Utils;
using Oracle.ManagedDataAccess.Client;

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
                request.Action == "VerifyOtp" || request.Action == "ResendOtp" ||
                request.Action == "AdminLogin")
            {
                return request.Action switch
                {
                    "Login" => await HandleLoginAsync(request),
                    "Register" => await HandleRegisterAsync(request),
                    "VerifyOtp" => await HandleVerifyOtpAsync(request),
                    "ForgotPasswordRequest" => await HandleForgotPasswordRequestAsync(request),
                    "ResetPassword" => await HandleResetPasswordAsync(request),
                    "ResendOtp" => await HandleResendOtpAsync(request),
                    "AdminLogin" => await HandleAdminLoginAsync(request),
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

            // Admin actions (require clearance level >= 3)
            var isAdminAction = request.Action.StartsWith("Admin") || 
                                request.Action == "GetAllUsers" || 
                                request.Action == "GetUserDetails" ||
                                request.Action == "CreateUser" ||
                                request.Action == "UpdateUser" ||
                                request.Action == "DeleteUser" ||
                                request.Action == "BanUserGlobal" ||
                                request.Action == "UnbanUserGlobal" ||
                                request.Action == "GetAllConversations" ||
                                request.Action == "GetConversationMessagesAdmin" ||
                                request.Action == "AdminDeleteConversation" ||
                                request.Action == "AdminDeleteMessage" ||
                                request.Action == "GetAuditLogs";

            if (isAdminAction)
            {
                var account = await _dbContext.GetUserAccountAsync(request.SenderUsername);
                if (account == null || account.ClearanceLevel < 3)
                {
                    return JsonSerializer.Serialize(new ServerResponse
                    {
                        Success = false,
                        Message = "Access denied. Admin privileges required (Clearance Level >= 3)."
                    });
                }
            }

            return request.Action switch
            {
                "SendMessage" => await HandleSendMessageAsync(request),
                "GetMessages" => await HandleGetMessagesAsync(request),
                "CreateConversation" => await HandleCreateConversationAsync(request),
                "GetConversations" => await HandleGetConversationsAsync(request),
                "GetConversationMessages" => await HandleGetConversationMessagesAsync(request),
                "GetConversationMembers" => await HandleGetConversationMembersAsync(request),
                "AddMember" => await HandleAddMemberAsync(request),
                "RemoveMember" => await HandleRemoveMemberAsync(request),
                "DeleteConversation" => await HandleDeleteConversationAsync(request),
                "BanMember" => await HandleBanMemberAsync(request),
                "UnbanMember" => await HandleUnbanMemberAsync(request),
                "MuteMember" => await HandleMuteMemberAsync(request),
                "UnmuteMember" => await HandleUnmuteMemberAsync(request),
                // Admin actions
                "GetAllUsers" => await HandleGetAllUsersAsync(request),
                "GetUserDetails" => await HandleGetUserDetailsAsync(request),
                "CreateUser" => await HandleCreateUserAsync(request),
                "UpdateUser" => await HandleUpdateUserAsync(request),
                "DeleteUser" => await HandleDeleteUserAsync(request),
                "BanUserGlobal" => await HandleBanUserGlobalAsync(request),
                "UnbanUserGlobal" => await HandleUnbanUserGlobalAsync(request),
                "GetAllConversations" => await HandleGetAllConversationsAsync(request),
                "AdminDeleteConversation" => await HandleAdminDeleteConversationAsync(request),
                "GetConversationMessagesAdmin" => await HandleGetConversationMessagesAdminAsync(request),
                "AdminDeleteMessage" => await HandleAdminDeleteMessageAsync(request),
                "GetAuditLogs" => await HandleGetAuditLogsAsync(request),
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

            // Kiểm tra OTP đã được xác minh chưa (bắt buộc cho tài khoản mới đăng ký)
            var isOtpVerified = await _dbContext.IsOtpVerifiedAsync(request.SenderUsername);
            if (!isOtpVerified)
            {
                await _dbContext.WriteAuditLogAsync(request.SenderUsername, "LOGIN_FAILED", "OTP not verified", 0);
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "Please verify your email with OTP before logging in."
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

            // Kiểm tra account đã tồn tại (kể cả chưa verify OTP)
            if (await _dbContext.AccountExistsAsync(request.SenderUsername))
            {
                // Kiểm tra xem account đã verify OTP chưa
                var isVerified = await _dbContext.IsOtpVerifiedAsync(request.SenderUsername);
                if (!isVerified)
                {
                    return JsonSerializer.Serialize(new ServerResponse
                    {
                        Success = false,
                        Message = "Username already exists but not verified. Please verify your email with OTP first, or contact support if you need to resend OTP."
                    });
                }
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "Username already exists."
                });
            }

            var passwordHash = PasswordHelper.HashPassword(request.Password);
            var clearanceLevel = request.ClearanceLevel > 0 ? request.ClearanceLevel : 1;

            // Bảo mật: Không cho phép đăng ký với clearance level >= 3
            // Chỉ admin mới có thể tạo hoặc nâng cấp user lên level 3
            if (clearanceLevel >= 3)
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "Security violation: Cannot register with clearance level 3 or higher. Only administrators can grant high clearance levels."
                });
            }

            // Đảm bảo clearance level hợp lệ (1 hoặc 2)
            if (clearanceLevel < 1 || clearanceLevel > 2)
            {
                clearanceLevel = 1; // Default to LOW
            }

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
                
                try
                {
                    await _dbContext.CreateOtpAsync(request.SenderUsername, request.Email, otpHash);
                }
                catch (Exception ex)
                {
                    // Nếu tạo OTP thất bại, vẫn coi như đăng ký thành công (account đã được tạo)
                    // Log lỗi nhưng không fail registration
                    Console.WriteLine($"Warning: Failed to create OTP for {request.SenderUsername}: {ex.Message}");
                }

                // Tạo verification link
                var verificationLink = $"{_emailService.GetVerificationBaseUrl()}?username={Uri.EscapeDataString(request.SenderUsername)}&otp={otp}";

                try
                {
                    await _emailService.SendRegistrationOtpAsync(request.Email, request.SenderUsername, otp, verificationLink);
                }
                catch (Exception ex)
                {
                    // Nếu gửi email thất bại, vẫn coi như đăng ký thành công
                    Console.WriteLine($"Warning: Failed to send email to {request.Email}: {ex.Message}");
                }

                try
                {
                    await _dbContext.WriteAuditLogAsync(request.SenderUsername, "REGISTER", request.SenderUsername, clearanceLevel);
                }
                catch
                {
                    // Ignore audit log errors
                }

                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = true,
                    Message = "Registration successful. Please verify OTP sent to your email."
                });
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException ex)
            {
                // Xử lý Oracle exception riêng, chỉ lấy message string
                string errorMessage;
                if (ex.Number == 1) // ORA-00001: unique constraint
                {
                    errorMessage = "Username already exists.";
                }
                else if (ex.Message.Contains("ORA-"))
                {
                    errorMessage = "Database error occurred. Please try again or contact administrator.";
                }
                else
                {
                    errorMessage = ex.Message ?? "Database error occurred.";
                }
                
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = $"Registration failed: {errorMessage}"
                });
            }
            catch (Exception ex)
            {
                // Chỉ lấy message string, không serialize exception object
                // Loại bỏ tất cả thông tin Oracle-specific
                var baseEx = ex.GetBaseException();
                string errorMessage = baseEx?.Message ?? ex.Message ?? "Unknown error occurred";
                
                // Loại bỏ Oracle exception references
                if (errorMessage.Contains("Oracle") || errorMessage.Contains("ORA-"))
                {
                    errorMessage = "Database error occurred. Please try again.";
                }
                
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = $"Registration failed: {errorMessage}"
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

            // Tạo verification link
            var verificationLink = $"{_emailService.GetVerificationBaseUrl()}?username={Uri.EscapeDataString(request.SenderUsername)}&otp={otp}";
            await _emailService.SendPasswordResetOtpAsync(request.Email, request.SenderUsername, otp, verificationLink);
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

        private async Task<string> HandleResendOtpAsync(ChatRequest request)
        {
            if (string.IsNullOrEmpty(request.SenderUsername))
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "Username is required."
                });
            }

            if (!await _dbContext.AccountExistsAsync(request.SenderUsername))
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "Account not found."
                });
            }

            var email = await _dbContext.GetEmailByMatkAsync(request.SenderUsername);
            if (string.IsNullOrEmpty(email))
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "Email not found for this account."
                });
            }

            var otp = PasswordHelper.GenerateOtp();
            var otpHash = PasswordHelper.HashPassword(otp);
            await _dbContext.CreateOtpAsync(request.SenderUsername, email, otpHash);

            // Tạo verification link
            var verificationLink = $"{_emailService.GetVerificationBaseUrl()}?username={Uri.EscapeDataString(request.SenderUsername)}&otp={otp}";
            await _emailService.SendRegistrationOtpAsync(email, request.SenderUsername, otp, verificationLink);
            await _dbContext.WriteAuditLogAsync(request.SenderUsername, "RESEND_OTP", request.SenderUsername, 0);

            return JsonSerializer.Serialize(new ServerResponse
            {
                Success = true,
                Message = "OTP has been resent to your email."
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
                        MessageId = r.MessageId,
                        ConversationId = r.ConversationId,
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
                // Kiểm tra MALOAICTC có tồn tại không
                using var checkCmd = _dbContext.Connection.CreateCommand();
                checkCmd.CommandText = "SELECT COUNT(*) FROM LOAICTC WHERE MALOAICTC = :p_maloaictc";
                checkCmd.Parameters.Add(new Oracle.ManagedDataAccess.Client.OracleParameter("p_maloaictc", Oracle.ManagedDataAccess.Client.OracleDbType.Varchar2) { Value = maloaictc });
                var countResult = await checkCmd.ExecuteScalarAsync();
                if (countResult == null || Convert.ToInt32(countResult) == 0)
                {
                    return JsonSerializer.Serialize(new ServerResponse
                    {
                        Success = false,
                        Message = $"Conversation type '{maloaictc}' not found. Please ensure database seeds are loaded."
                    });
                }

                // Kiểm tra MAPHANQUYEN 'OWNER' có tồn tại không
                using var checkPermCmd = _dbContext.Connection.CreateCommand();
                checkPermCmd.CommandText = "SELECT COUNT(*) FROM PHAN_QUYEN_NHOM WHERE MAPHANQUYEN = 'OWNER'";
                var permCountResult = await checkPermCmd.ExecuteScalarAsync();
                if (permCountResult == null || Convert.ToInt32(permCountResult) == 0)
                {
                    return JsonSerializer.Serialize(new ServerResponse
                    {
                        Success = false,
                        Message = "Permission 'OWNER' not found. Please ensure database seeds are loaded."
                    });
                }

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
            catch (Oracle.ManagedDataAccess.Client.OracleException ex) when (ex.Number == 2291) // Parent key not found
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = $"Parent key not found. Please ensure database seeds (LOAICTC, PHAN_QUYEN_NHOM) are loaded. Error: {ex.Message}"
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
                        ConversationId = r.ConversationId,
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

        private async Task<string> HandleGetConversationMembersAsync(ChatRequest request)
        {
            if (string.IsNullOrEmpty(request.ConversationId))
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "Conversation ID is required."
                });
            }

            // Check if user is a member
            var permission = await _dbContext.GetMemberPermissionAsync(request.ConversationId, request.SenderUsername);
            if (permission == null)
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "You are not a member of this conversation."
                });
            }

            try
            {
                var members = await _dbContext.GetConversationMembersAsync(request.ConversationId);
                var memberDtos = members.Select(m => new MemberDto
                {
                    Matk = m.Matk,
                    Username = m.Username,
                    Role = m.Role,
                    IsBanned = m.IsBanned,
                    IsMuted = m.IsMuted,
                    JoinedDate = m.NgayThamGia
                }).ToArray();

                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = true,
                    Message = "Members loaded.",
                    Members = memberDtos
                });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = $"Failed to load members: {ex.Message}"
                });
            }
        }

        // ========== ADMIN HANDLERS ==========

        private async Task<string> HandleAdminLoginAsync(ChatRequest request)
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
            if (account == null || !PasswordHelper.VerifyPassword(request.Password, account.PasswordHash))
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "Invalid username or password."
                });
            }

            if (account.ClearanceLevel < 3)
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "Access denied. Admin privileges required (Clearance Level >= 3)."
                });
            }

            if (!await _dbContext.IsOtpVerifiedAsync(request.SenderUsername))
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "Please verify your email with OTP before logging in."
                });
            }

            return JsonSerializer.Serialize(new ServerResponse
            {
                Success = true,
                Message = "Admin login successful.",
                ClearanceLevel = account.ClearanceLevel
            });
        }

        private async Task<string> HandleGetAllUsersAsync(ChatRequest request)
        {
            try
            {
                var users = await _dbContext.GetAllUsersAsync();
                var userDtos = users.Select(u => new AdminUserDto
                {
                    Matk = u.Matk,
                    Username = u.Username,
                    Email = u.Email,
                    Hovaten = u.Hovaten,
                    Phone = u.Phone,
                    ClearanceLevel = u.ClearanceLevel,
                    IsBannedGlobal = u.IsBannedGlobal,
                    Mavaitro = u.Mavaitro,
                    NgayTao = u.NgayTao,
                    IsOtpVerified = u.IsOtpVerified
                }).ToArray();

                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = true,
                    Message = "Users loaded.",
                    AdminUsers = userDtos
                });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = $"Failed to load users: {ex.Message}"
                });
            }
        }

        private async Task<string> HandleGetUserDetailsAsync(ChatRequest request)
        {
            if (string.IsNullOrEmpty(request.TargetUsername))
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "Target username is required."
                });
            }

            try
            {
                var user = await _dbContext.GetUserDetailsAsync(request.TargetUsername);
                if (user == null)
                {
                    return JsonSerializer.Serialize(new ServerResponse
                    {
                        Success = false,
                        Message = "User not found."
                    });
                }

                var userDto = new AdminUserDto
                {
                    Matk = user.Matk,
                    Username = user.Username,
                    Email = user.Email,
                    Hovaten = user.Hovaten,
                    Phone = user.Phone,
                    ClearanceLevel = user.ClearanceLevel,
                    IsBannedGlobal = user.IsBannedGlobal,
                    Mavaitro = user.Mavaitro,
                    NgayTao = user.NgayTao,
                    IsOtpVerified = user.IsOtpVerified
                };

                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = true,
                    Message = "User details loaded.",
                    AdminUser = userDto
                });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = $"Failed to load user details: {ex.Message}"
                });
            }
        }

        private async Task<string> HandleCreateUserAsync(ChatRequest request)
        {
            // Parse user data from request (assuming JSON in Content field or separate fields)
            if (string.IsNullOrEmpty(request.TargetUsername) || string.IsNullOrEmpty(request.Password))
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "Username and password are required."
                });
            }

            try
            {
                var passwordHash = PasswordHelper.HashPassword(request.Password);
                var clearanceLevel = request.ClearanceLevel > 0 ? request.ClearanceLevel : 1;

                await _dbContext.CreateAccountAsync(
                    request.TargetUsername,
                    request.TargetUsername,
                    passwordHash,
                    null,
                    clearanceLevel
                );

                // Update user info if provided
                if (!string.IsNullOrEmpty(request.Email))
                {
                    await _dbContext.UpdateUserInfoAsync(
                        request.TargetUsername,
                        email: request.Email,
                        hovaten: null,
                        phone: null,
                        clearanceLevel: null,
                        mavaitro: null
                    );
                }

                await _dbContext.WriteAuditLogAsync(request.SenderUsername, "ADMIN_CREATE_USER", request.TargetUsername, clearanceLevel);

                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = true,
                    Message = "User created successfully."
                });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = $"Failed to create user: {ex.Message}"
                });
            }
        }

        private async Task<string> HandleUpdateUserAsync(ChatRequest request)
        {
            if (string.IsNullOrEmpty(request.TargetUsername))
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "Target username is required."
                });
            }

            try
            {
                await _dbContext.UpdateUserInfoAsync(
                    request.TargetUsername,
                    email: string.IsNullOrEmpty(request.Email) ? null : request.Email,
                    hovaten: null,
                    phone: null,
                    clearanceLevel: request.ClearanceLevel > 0 ? request.ClearanceLevel : null,
                    mavaitro: null
                );

                await _dbContext.WriteAuditLogAsync(request.SenderUsername, "ADMIN_UPDATE_USER", request.TargetUsername, 0);

                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = true,
                    Message = "User updated successfully."
                });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = $"Failed to update user: {ex.Message}"
                });
            }
        }

        private async Task<string> HandleDeleteUserAsync(ChatRequest request)
        {
            if (string.IsNullOrEmpty(request.TargetUsername))
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "Target username is required."
                });
            }

            try
            {
                // Use stored procedure to delete user completely
                using var cmd = _dbContext.Connection.CreateCommand();
                cmd.CommandText = "BEGIN SP_XOA_TAIKHOAN_TOAN_BO(:p_matk); END;";
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.Add(new Oracle.ManagedDataAccess.Client.OracleParameter("p_matk", Oracle.ManagedDataAccess.Client.OracleDbType.Varchar2) { Value = request.TargetUsername });
                await cmd.ExecuteNonQueryAsync();

                await _dbContext.WriteAuditLogAsync(request.SenderUsername, "ADMIN_DELETE_USER", request.TargetUsername, 0);

                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = true,
                    Message = "User deleted successfully."
                });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = $"Failed to delete user: {ex.Message}"
                });
            }
        }

        private async Task<string> HandleBanUserGlobalAsync(ChatRequest request)
        {
            if (string.IsNullOrEmpty(request.TargetUsername))
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "Target username is required."
                });
            }

            try
            {
                await _dbContext.BanUserGlobalAsync(request.TargetUsername);
                await _dbContext.WriteAuditLogAsync(request.SenderUsername, "ADMIN_BAN_USER", request.TargetUsername, 0);

                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = true,
                    Message = "User banned successfully."
                });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = $"Failed to ban user: {ex.Message}"
                });
            }
        }

        private async Task<string> HandleUnbanUserGlobalAsync(ChatRequest request)
        {
            if (string.IsNullOrEmpty(request.TargetUsername))
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "Target username is required."
                });
            }

            try
            {
                await _dbContext.UnbanUserGlobalAsync(request.TargetUsername);
                await _dbContext.WriteAuditLogAsync(request.SenderUsername, "ADMIN_UNBAN_USER", request.TargetUsername, 0);

                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = true,
                    Message = "User unbanned successfully."
                });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = $"Failed to unban user: {ex.Message}"
                });
            }
        }

        private async Task<string> HandleGetAllConversationsAsync(ChatRequest request)
        {
            try
            {
                var conversations = await _dbContext.GetAllConversationsAsync();
                var convDtos = conversations.Select(c => new AdminConversationDto
                {
                    Mactc = c.Mactc,
                    Tenctc = c.Tenctc,
                    Maloaictc = c.Maloaictc,
                    IsPrivate = c.IsPrivate,
                    Nguoiql = c.Nguoiql,
                    NgayTao = c.NgayTao,
                    MemberCount = c.MemberCount,
                    MessageCount = c.MessageCount
                }).ToArray();

                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = true,
                    Message = "Conversations loaded.",
                    AdminConversations = convDtos
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

        private async Task<string> HandleAdminDeleteConversationAsync(ChatRequest request)
        {
            if (string.IsNullOrEmpty(request.ConversationId))
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "Conversation ID is required."
                });
            }

            try
            {
                await _dbContext.DeleteConversationAsync(request.ConversationId);
                await _dbContext.WriteAuditLogAsync(request.SenderUsername, "ADMIN_DELETE_CONVERSATION", request.ConversationId, 0);

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

        private async Task<string> HandleGetConversationMessagesAdminAsync(ChatRequest request)
        {
            if (string.IsNullOrEmpty(request.ConversationId))
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "Conversation ID is required."
                });
            }

            try
            {
                var limit = request.Limit > 0 ? request.Limit : 100;
                var messages = await _dbContext.GetConversationMessagesAdminAsync(request.ConversationId, limit);
                var messageDtos = messages.Select(m => new AdminMessageDto
                {
                    Matn = m.Matn,
                    Mactc = m.Mactc,
                    Matk = m.Matk,
                    Username = m.Username,
                    Noidung = m.Noidung,
                    SecurityLabel = m.SecurityLabel,
                    Ngaygui = m.Ngaygui,
                    Maloaitn = m.Maloaitn,
                    Matrangthai = m.Matrangthai
                }).ToArray();

                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = true,
                    Message = "Messages loaded.",
                    AdminMessages = messageDtos
                });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = $"Failed to load messages: {ex.Message}"
                });
            }
        }

        private async Task<string> HandleAdminDeleteMessageAsync(ChatRequest request)
        {
            if (request.MessageId <= 0)
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = "Message ID is required."
                });
            }

            try
            {
                await _dbContext.DeleteMessageAsync(request.MessageId);
                await _dbContext.WriteAuditLogAsync(request.SenderUsername, "ADMIN_DELETE_MESSAGE", request.MessageId.ToString(), 0);

                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = true,
                    Message = "Message deleted successfully."
                });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = $"Failed to delete message: {ex.Message}"
                });
            }
        }

        private async Task<string> HandleGetAuditLogsAsync(ChatRequest request)
        {
            try
            {
                var limit = request.Limit > 0 ? request.Limit : 100;
                var logs = await _dbContext.GetAuditLogsAsync(limit);
                var logDtos = logs.Select(l => new AuditLogDto
                {
                    LogId = l.LogId,
                    Matk = l.Matk,
                    Action = l.Action,
                    Target = l.Target,
                    SecurityLabel = l.SecurityLabel,
                    Timestamp = l.Timestamp
                }).ToArray();

                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = true,
                    Message = "Audit logs loaded.",
                    AuditLogs = logDtos
                });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    Success = false,
                    Message = $"Failed to load audit logs: {ex.Message}"
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
        public int Limit { get; set; }
        public int MessageId { get; set; }
    }

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
        // Admin DTOs
        public AdminUserDto[] AdminUsers { get; set; } = Array.Empty<AdminUserDto>();
        public AdminUserDto? AdminUser { get; set; }
        public AdminConversationDto[] AdminConversations { get; set; } = Array.Empty<AdminConversationDto>();
        public AdminMessageDto[] AdminMessages { get; set; } = Array.Empty<AdminMessageDto>();
        public AuditLogDto[] AuditLogs { get; set; } = Array.Empty<AuditLogDto>();
    }

    // Admin DTOs
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

    public class AdminConversationDto
    {
        public string Mactc { get; set; } = string.Empty;
        public string Tenctc { get; set; } = string.Empty;
        public string Maloaictc { get; set; } = string.Empty;
        public bool IsPrivate { get; set; }
        public string Nguoiql { get; set; } = string.Empty;
        public DateTime NgayTao { get; set; }
        public int MemberCount { get; set; }
        public int MessageCount { get; set; }
    }

    public class AdminMessageDto
    {
        public int Matn { get; set; }
        public string Mactc { get; set; } = string.Empty;
        public string Matk { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Noidung { get; set; } = string.Empty;
        public int SecurityLabel { get; set; }
        public DateTime Ngaygui { get; set; }
        public string Maloaitn { get; set; } = string.Empty;
        public string Matrangthai { get; set; } = string.Empty;
    }

    public class AuditLogDto
    {
        public int LogId { get; set; }
        public string Matk { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public int SecurityLabel { get; set; }
        public DateTime Timestamp { get; set; }
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
        public bool IsPrivate { get; set; } // IS_PRIVATE = 'Y' or 'N'
        public DateTime CreatedAt { get; set; } // NGAYTAO
        public int MemberCount { get; set; } // Số lượng thành viên
    }

    public class MemberDto
    {
        public string Matk { get; set; } = string.Empty; // MATK
        public string Username { get; set; } = string.Empty; // TENTK
        public string Role { get; set; } = string.Empty; // QUYEN
        public bool IsBanned { get; set; } // IS_BANNED
        public bool IsMuted { get; set; } // IS_MUTED
        public DateTime JoinedDate { get; set; } // NGAYTHAMGIA
    }
}