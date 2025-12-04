using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace ChatServer.Services
{
    /// <summary>
    /// Service gửi email OTP.
    /// Lưu ý: Cấu hình SMTP trong constructor hoặc appsettings.json.
    /// Demo: dùng Gmail SMTP (cần bật "Less secure app access" hoặc dùng App Password).
    /// </summary>
    public class EmailService
    {
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly string _verificationBaseUrl;

        public EmailService(
            string smtpHost = "smtp.gmail.com",
            int smtpPort = 587,
            string smtpUsername = "",
            string smtpPassword = "",
            string fromEmail = "",
            string fromName = "Chat Application",
            string verificationBaseUrl = "chatapp://verify")
        {
            _smtpHost = smtpHost;
            _smtpPort = smtpPort;
            _smtpUsername = smtpUsername;
            _smtpPassword = smtpPassword;
            _fromEmail = fromEmail;
            _fromName = fromName;
            _verificationBaseUrl = verificationBaseUrl;
        }

        public string GetVerificationBaseUrl() => _verificationBaseUrl;

        /// <summary>
        /// Gửi email OTP cho đăng ký tài khoản với HTML template đẹp.
        /// </summary>
        public async Task SendRegistrationOtpAsync(string toEmail, string username, string otp, string? verificationLink = null)
        {
            var subject = "🔐 Xác minh đăng ký tài khoản - Chat Application";
            var htmlBody = GenerateRegistrationOtpHtml(username, otp, verificationLink);
            var textBody = $@"Xin chào {username},

Mã OTP để xác minh đăng ký tài khoản của bạn là: {otp}

Mã này có hiệu lực trong 10 phút.

Nếu bạn không đăng ký tài khoản này, vui lòng bỏ qua email này.

Trân trọng,
Chat Application";

            await SendEmailAsync(toEmail, subject, htmlBody, textBody);
        }

        /// <summary>
        /// Gửi email OTP cho quên mật khẩu với HTML template đẹp.
        /// </summary>
        public async Task SendPasswordResetOtpAsync(string toEmail, string username, string otp, string? verificationLink = null)
        {
            var subject = "🔑 Đặt lại mật khẩu - Chat Application";
            var htmlBody = GeneratePasswordResetOtpHtml(username, otp, verificationLink);
            var textBody = $@"Xin chào {username},

Mã OTP để đặt lại mật khẩu của bạn là: {otp}

Mã này có hiệu lực trong 10 phút.

Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.

Trân trọng,
Chat Application";

            await SendEmailAsync(toEmail, subject, htmlBody, textBody);
        }

        private async Task SendEmailAsync(string toEmail, string subject, string htmlBody, string? textBody = null)
        {
            // Nếu không cấu hình SMTP, chỉ log ra console (demo).
            if (string.IsNullOrEmpty(_smtpUsername) || string.IsNullOrEmpty(_smtpPassword))
            {
                Console.WriteLine($"[EMAIL DEMO] To: {toEmail}, Subject: {subject}");
                Console.WriteLine($"HTML Body: {htmlBody}");
                if (!string.IsNullOrEmpty(textBody))
                {
                    Console.WriteLine($"Text Body: {textBody}");
                }
                return;
            }

            try
            {
                using var client = new SmtpClient(_smtpHost, _smtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(_smtpUsername, _smtpPassword)
                };

                using var message = new MailMessage
                {
                    From = new MailAddress(_fromEmail, _fromName),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };
                
                // Thêm text alternative cho email clients không hỗ trợ HTML
                if (!string.IsNullOrEmpty(textBody))
                {
                    var textView = AlternateView.CreateAlternateViewFromString(textBody, null, "text/plain");
                    message.AlternateViews.Add(textView);
                }
                
                message.To.Add(toEmail);

                await client.SendMailAsync(message);
                Console.WriteLine($"Email sent to {toEmail}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email to {toEmail}: {ex.Message}");
                throw;
            }
        }

        private string GenerateRegistrationOtpHtml(string username, string otp, string? verificationLink)
        {
            var linkHtml = string.IsNullOrEmpty(verificationLink) 
                ? "" 
                : $@"
                <table role=""presentation"" style=""width: 100%; margin: 30px 0;"">
                    <tr>
                        <td align=""center"">
                            <a href=""{verificationLink}"" 
                               style=""display: inline-block; padding: 14px 32px; background-color: #667eea; 
                                      color: #ffffff; text-decoration: none; border-radius: 8px; font-weight: 600; font-size: 16px;"">
                                ✅ Xác minh ngay
                            </a>
                        </td>
                    </tr>
                </table>";

            return $@"
<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Xác minh đăng ký</title>
</head>
<body style=""margin: 0; padding: 0; font-family: Arial, Helvetica, sans-serif; background-color: #f5f7fa;"">
    <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""width: 100%; background-color: #f5f7fa; padding: 20px;"">
        <tr>
            <td align=""center"">
                <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""max-width: 600px; width: 100%; background-color: #ffffff; border-radius: 8px; overflow: hidden;"">
                    <!-- Header -->
                    <tr>
                        <td style=""background-color: #667eea; padding: 40px 30px; text-align: center;"">
                            <h1 style=""margin: 0; color: #ffffff; font-size: 28px; font-weight: bold; font-family: Arial, Helvetica, sans-serif;"">
                                🔐 Xác minh đăng ký
                            </h1>
                        </td>
                    </tr>
                    
                    <!-- Content -->
                    <tr>
                        <td style=""padding: 40px 30px;"">
                            <p style=""margin: 0 0 20px 0; color: #333333; font-size: 16px; line-height: 24px; font-family: Arial, Helvetica, sans-serif;"">
                                Xin chào <strong style=""color: #667eea;"">{username}</strong>,
                            </p>
                            
                            <p style=""margin: 0 0 30px 0; color: #555555; font-size: 15px; line-height: 22px; font-family: Arial, Helvetica, sans-serif;"">
                                Cảm ơn bạn đã đăng ký tài khoản tại <strong>Chat Application</strong>! 
                                Để hoàn tất quá trình đăng ký, vui lòng sử dụng mã OTP bên dưới:
                            </p>
                            
                            <!-- OTP Box -->
                            <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""width: 100%; background-color: #f5f7fa; border-radius: 8px; padding: 30px; margin: 30px 0; border: 2px dashed #667eea;"">
                                <tr>
                                    <td align=""center"">
                                        <p style=""margin: 0 0 10px 0; color: #666666; font-size: 14px; font-weight: bold; text-transform: uppercase; letter-spacing: 1px; font-family: Arial, Helvetica, sans-serif;"">
                                            Mã xác minh của bạn
                                        </p>
                                        <p style=""margin: 0; color: #667eea; font-size: 42px; font-weight: bold; letter-spacing: 8px; font-family: 'Courier New', Courier, monospace;"">
                                            {otp}
                                        </p>
                                    </td>
                                </tr>
                            </table>
                            
                            {linkHtml}
                            
                            <p style=""margin: 30px 0 0 0; color: #888888; font-size: 13px; line-height: 20px; text-align: center; font-family: Arial, Helvetica, sans-serif;"">
                                ⏰ Mã này có hiệu lực trong <strong style=""color: #e74c3c;"">10 phút</strong>
                            </p>
                            
                            <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""width: 100%; margin-top: 40px; padding-top: 30px; border-top: 1px solid #e0e0e0;"">
                                <tr>
                                    <td>
                                        <p style=""margin: 0 0 15px 0; color: #999999; font-size: 13px; line-height: 20px; font-family: Arial, Helvetica, sans-serif;"">
                                            <strong>⚠️ Lưu ý bảo mật:</strong>
                                        </p>
                                        <ul style=""margin: 0; padding-left: 20px; color: #777777; font-size: 13px; line-height: 22px; font-family: Arial, Helvetica, sans-serif;"">
                                            <li>Không chia sẻ mã OTP với bất kỳ ai</li>
                                            <li>Mã OTP chỉ sử dụng một lần</li>
                                            <li>Nếu bạn không đăng ký tài khoản này, vui lòng bỏ qua email này</li>
                                        </ul>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    
                    <!-- Footer -->
                    <tr>
                        <td style=""background-color: #f8f9fa; padding: 25px 30px; text-align: center; border-top: 1px solid #e0e0e0;"">
                            <p style=""margin: 0 0 10px 0; color: #999999; font-size: 12px; font-family: Arial, Helvetica, sans-serif;"">
                                Email này được gửi tự động, vui lòng không trả lời.
                            </p>
                            <p style=""margin: 0; color: #bbbbbb; font-size: 11px; font-family: Arial, Helvetica, sans-serif;"">
                                © {DateTime.Now.Year} Chat Application. All rights reserved.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
        }

        private string GeneratePasswordResetOtpHtml(string username, string otp, string? verificationLink)
        {
            var linkHtml = string.IsNullOrEmpty(verificationLink) 
                ? "" 
                : $@"
                <div style=""text-align: center; margin: 30px 0;"">
                    <a href=""{verificationLink}"" 
                       style=""display: inline-block; padding: 14px 32px; background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%); 
                              color: #ffffff; text-decoration: none; border-radius: 8px; font-weight: 600; font-size: 16px;
                              box-shadow: 0 4px 15px rgba(245, 87, 108, 0.4);"">
                        🔑 Đặt lại mật khẩu
                    </a>
                </div>";

            return $@"
<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Đặt lại mật khẩu</title>
</head>
<body style=""margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f5f7fa;"">
    <table role=""presentation"" style=""width: 100%; border-collapse: collapse; background-color: #f5f7fa; padding: 20px;"">
        <tr>
            <td align=""center"">
                <table role=""presentation"" style=""max-width: 600px; width: 100%; background-color: #ffffff; border-radius: 12px; 
                                                    box-shadow: 0 4px 20px rgba(0, 0, 0, 0.1); overflow: hidden;"">
                    <!-- Header -->
                    <tr>
                        <td style=""background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%); padding: 40px 30px; text-align: center;"">
                            <h1 style=""margin: 0; color: #ffffff; font-size: 28px; font-weight: 700;"">
                                🔑 Đặt lại mật khẩu
                            </h1>
                        </td>
                    </tr>
                    
                    <!-- Content -->
                    <tr>
                        <td style=""padding: 40px 30px;"">
                            <p style=""margin: 0 0 20px 0; color: #333333; font-size: 16px; line-height: 1.6;"">
                                Xin chào <strong style=""color: #f5576c;"">{username}</strong>,
                            </p>
                            
                            <p style=""margin: 0 0 30px 0; color: #555555; font-size: 15px; line-height: 1.6;"">
                                Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn. 
                                Vui lòng sử dụng mã OTP bên dưới để tiếp tục:
                            </p>
                            
                            <!-- OTP Box -->
                            <div style=""background: linear-gradient(135deg, #fef5e7 0%, #fdebd0 100%); 
                                        border-radius: 12px; padding: 30px; text-align: center; margin: 30px 0;
                                        border: 2px dashed #f5576c;"">
                                <p style=""margin: 0 0 10px 0; color: #666666; font-size: 14px; font-weight: 600; text-transform: uppercase; letter-spacing: 1px;"">
                                    Mã xác minh của bạn
                                </p>
                                <p style=""margin: 0; color: #f5576c; font-size: 42px; font-weight: 700; letter-spacing: 8px; font-family: 'Courier New', monospace;"">
                                    {otp}
                                </p>
                            </div>
                            
                            {linkHtml}
                            
                            <p style=""margin: 30px 0 0 0; color: #888888; font-size: 13px; line-height: 1.6; text-align: center;"">
                                ⏰ Mã này có hiệu lực trong <strong style=""color: #e74c3c;"">10 phút</strong>
                            </p>
                            
                            <div style=""margin-top: 40px; padding: 20px; background-color: #fff3cd; border-radius: 8px; border-left: 4px solid #ffc107;"">
                                <p style=""margin: 0; color: #856404; font-size: 13px; line-height: 1.6;"">
                                    <strong>⚠️ Cảnh báo bảo mật:</strong> Nếu bạn không yêu cầu đặt lại mật khẩu, 
                                    vui lòng bỏ qua email này và kiểm tra tài khoản của bạn ngay lập tức.
                                </p>
                            </div>
                        </td>
                    </tr>
                    
                    <!-- Footer -->
                    <tr>
                        <td style=""background-color: #f8f9fa; padding: 25px 30px; text-align: center; border-top: 1px solid #e0e0e0;"">
                            <p style=""margin: 0 0 10px 0; color: #999999; font-size: 12px;"">
                                Email này được gửi tự động, vui lòng không trả lời.
                            </p>
                            <p style=""margin: 0; color: #bbbbbb; font-size: 11px;"">
                                © {DateTime.Now.Year} Chat Application. All rights reserved.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
        }
    }
}

