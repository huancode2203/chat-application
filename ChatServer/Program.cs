using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ChatServer.Database;
using ChatServer.Forms;
using ChatServer.Services;
using Microsoft.Extensions.Configuration;

namespace ChatServer
{
    internal class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllocConsole();
        /// <summary>
        /// Điểm vào của server console.
        /// Server sẽ:
        /// - Đọc cấu hình từ appsettings.json.
        /// - Mở kết nối Oracle.
        /// - Tạo MACService + ChatProcessingService.
        /// - Khởi động SocketServerService lắng nghe TCP.
        /// </summary>
        [STAThread]
        private static async Task Main(string[] args)
        {
            // Allocate console window for WinExe
            if (args.Length == 0 || !args.Contains("--no-console"))
            {
                AllocConsole();
            }

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
            var verificationBaseUrl = configuration["EmailSettings:VerificationBaseUrl"] ?? "chatapp://verify";

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
                fromName: fromName,
                verificationBaseUrl: verificationBaseUrl
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
            Console.WriteLine("Type 'admin' to open admin panel.");

            // Start server in background
            var serverTask = socketServer.StartAsync(cts.Token);

            // Initialize WinForms for admin panel
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Create a hidden form to maintain UI thread context
            var hiddenForm = new Form { WindowState = FormWindowState.Minimized, ShowInTaskbar = false };
            hiddenForm.Load += (_, _) => hiddenForm.Hide();
            
            AdminPanelForm? adminFormRef = null;

            // Check for admin command
            var inputTask = Task.Run(() =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var input = Console.ReadLine();
                        if (string.IsNullOrEmpty(input) || input.Trim().ToLower() != "admin")
                            continue;

                        // Open admin panel directly without login
                        hiddenForm.Invoke(new Action(() =>
                        {
                            try
                            {
                                if (adminFormRef == null || adminFormRef.IsDisposed)
                                {
                                    adminFormRef = new AdminPanelForm(dbContext, "SYSTEM", 3);
                                    adminFormRef.Show();
                                }
                                else
                                {
                                    adminFormRef.BringToFront();
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error opening admin panel: {ex.Message}");
                            }
                        }));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Input error: {ex.Message}");
                    }
                }
            });

            // Run message loop for WinForms
            var formsTask = Task.Run(() => Application.Run(hiddenForm));

            await Task.WhenAny(serverTask, formsTask);

            Console.WriteLine("Chat server stopped.");
        }
    }
}


