using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ChatServer.Database;
using ChatServer.Services;
using Microsoft.Extensions.Configuration;

namespace ChatServer
{
    internal class Program
    {
        /// <summary>
        /// Điểm vào của server console.
        /// Server sẽ:
        /// - Đọc cấu hình từ appsettings.json.
        /// - Mở kết nối Oracle.
        /// - Tạo MACService + ChatProcessingService.
        /// - Khởi động SocketServerService lắng nghe TCP.
        /// </summary>
        private static async Task Main(string[] args)
        {
            // Đọc cấu hình từ appsettings.json
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var connectionString = configuration["Database:ConnectionString"] 
                ?? "User Id=ChatNoiBo_DoAn1;Password=123;Data Source=localhost:1521/XE";
            var serverPort = int.Parse(configuration["Server:Port"] ?? "9000");

            var emailHost = configuration["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
            var emailPort = int.Parse(configuration["EmailSettings:SmtpPort"] ?? "587");
            var emailUsername = configuration["EmailSettings:SmtpUsername"] ?? "";
            var emailPassword = configuration["EmailSettings:SmtpPassword"] ?? "";
            var fromEmail = configuration["EmailSettings:FromEmail"] ?? "";
            var fromName = configuration["EmailSettings:FromName"] ?? "Chat Application";

            Console.WriteLine("=== Chat Server Starting ===");
            Console.WriteLine($"Database: {connectionString.Split(';')[0]}");
            Console.WriteLine($"Server Port: {serverPort}");
            if (!string.IsNullOrEmpty(emailUsername))
            {
                Console.WriteLine($"Email: {emailUsername} (SMTP configured)");
            }
            else
            {
                Console.WriteLine("Email: Not configured (OTP will be logged to console)");
            }
            Console.WriteLine();

            using var dbContext = new DbContext(connectionString);
            await dbContext.OpenAsync();

            var macService = new MACService();
            var emailService = new EmailService(
                smtpHost: emailHost,
                smtpPort: emailPort,
                smtpUsername: emailUsername,
                smtpPassword: emailPassword,
                fromEmail: fromEmail,
                fromName: fromName
            );
            var chatProcessing = new ChatProcessingService(dbContext, macService, emailService);
            var socketServer = new SocketServerService(chatProcessing, port: serverPort);

            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            Console.WriteLine("Starting chat server. Press Ctrl+C to stop.");
            await socketServer.StartAsync(cts.Token);

            Console.WriteLine("Chat server stopped.");
        }
    }
}


