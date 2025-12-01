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

        public EmailService(
            string smtpHost = "smtp.gmail.com",
            int smtpPort = 587,
            string smtpUsername = "",
            string smtpPassword = "",
            string fromEmail = "",
            string fromName = "Chat Application")
        {
            _smtpHost = smtpHost;
            _smtpPort = smtpPort;
            _smtpUsername = smtpUsername;
            _smtpPassword = smtpPassword;
            _fromEmail = fromEmail;
            _fromName = fromName;
        }

        /// <summary>
        /// Gửi email OTP cho đăng ký tài khoản.
        /// </summary>
        public async Task SendRegistrationOtpAsync(string toEmail, string username, string otp)
        {
            var subject = "Xác minh đăng ký tài khoản";
            var body = $@"
Xin chào {username},

Mã OTP để xác minh đăng ký tài khoản của bạn là: {otp}

Mã này có hiệu lực trong 10 phút.

Trân trọng,
Chat Application";

            await SendEmailAsync(toEmail, subject, body);
        }

        /// <summary>
        /// Gửi email OTP cho quên mật khẩu.
        /// </summary>
        public async Task SendPasswordResetOtpAsync(string toEmail, string username, string otp)
        {
            var subject = "Đặt lại mật khẩu";
            var body = $@"
Xin chào {username},

Mã OTP để đặt lại mật khẩu của bạn là: {otp}

Mã này có hiệu lực trong 10 phút.

Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.

Trân trọng,
Chat Application";

            await SendEmailAsync(toEmail, subject, body);
        }

        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            // Nếu không cấu hình SMTP, chỉ log ra console (demo).
            if (string.IsNullOrEmpty(_smtpUsername) || string.IsNullOrEmpty(_smtpPassword))
            {
                Console.WriteLine($"[EMAIL DEMO] To: {toEmail}, Subject: {subject}");
                Console.WriteLine($"Body: {body}");
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
                    Body = body,
                    IsBodyHtml = false
                };
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
    }
}

