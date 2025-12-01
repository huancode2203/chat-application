using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ChatServer.Database;
using ChatServer.Utils;

namespace ChatServer.Services
{
    /// <summary>
    /// Xử lý nghiệp vụ chat và authentication:
    /// - Parse request JSON từ SocketServerService.
    /// - Thiết lập MAC session (SetSessionUserLevelAsync) và/hoặc kiểm tra MAC thủ công.
    /// - Ghi/đọc tin nhắn từ Oracle DB.
    /// - Xử lý đăng nhập, đăng ký, OTP, quên mật khẩu.
    /// - Ghi audit log cho mọi hành động nhạy cảm.
    /// </summary>
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

            // Các action không cần authentication trước: Register, Login, ForgotPasswordRequest, ResetPassword, VerifyOtp
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

            // Các action cần authentication: thiết lập context cho VPD/MAC.
            if (!string.IsNullOrEmpty(request.SenderUsername))
            {
                await _dbContext.SetSessionUserLevelAsync(request.SenderUsername, request.ClearanceLevel);
            }

            return request.Action switch
            {
                "SendMessage" => await HandleSendMessageAsync(request),
                "GetMessages" => await HandleGetMessagesAsync(request),
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
                    request.SenderUsername, // TENTK = MATK (có thể mở rộng)
                    passwordHash,
                    null, // MAVAITRO
                    clearanceLevel
                );

                // Tạo OTP và gửi email
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
                // Không tiết lộ user không tồn tại (security best practice)
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

            // Tạo OTP và gửi email
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
            // Kiểm tra MAC: No write down.
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
                    Message = "Sender not found in TAIKHOAN table."
                });
            }

            // Demo: chỉ lưu tin nhắn với người gửi (MATK) và nội dung, chưa phân tách theo cuộc trò chuyện.
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

            // Thêm lớp MAC thủ công (bên cạnh VPD trên DB) để minh họa rõ ràng:
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
    }

    // DTO tương đồng với phía client để giao tiếp qua JSON.
    public class ChatRequest
    {
        public string Action { get; set; } = string.Empty;
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


