using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ChatServer.Services;
using ChatServer.Utils;

namespace ChatServer.Services
{
    /// <summary>
    /// Service TCP server.
    /// - Lắng nghe client trên một port (ví dụ 9000).
    /// - Mỗi khi client kết nối, tạo task xử lý.
    /// - Nhận chuỗi base64 (cipher), giải mã AES, parse JSON, chuyển cho ChatProcessingService.
    /// - Nhận response JSON, mã hóa AES, gửi lại cho client.
    ///
    /// Socket protocol (logic đơn giản):
    /// - Client gửi: Encrypt( JSON(ChatRequest) ) + "\n"
    /// - Server đọc 1 dòng, Decrypt, xử lý, Serialize(ServerResponse), Encrypt, gửi về + "\n".
    /// </summary>
    public class SocketServerService
    {
        private readonly ChatProcessingService _chatProcessingService;
        private readonly TcpListener _listener;

        public SocketServerService(ChatProcessingService chatProcessingService, int port)
        {
            _chatProcessingService = chatProcessingService;
            _listener = new TcpListener(IPAddress.Any, port);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _listener.Start();
            Console.WriteLine("Chat server listening on port " + ((_listener.LocalEndpoint as IPEndPoint)?.Port ?? 0));

            while (!cancellationToken.IsCancellationRequested)
            {
                var client = await _listener.AcceptTcpClientAsync(cancellationToken);
                _ = HandleClientAsync(client, cancellationToken);
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            Console.WriteLine("Client connected: " + client.Client.RemoteEndPoint);
            using var _ = client;
            try
            {
                using var stream = client.GetStream();
                using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
                using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

                while (!cancellationToken.IsCancellationRequested && client.Connected)
                {
                    var encryptedLine = await reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(encryptedLine))
                    {
                        break;
                    }

                    // === ENCRYPTION LOG ===
                    Console.WriteLine($"[SERVER][AES] <<< FROM CLIENT (encrypted): {encryptedLine.Substring(0, Math.Min(60, encryptedLine.Length))}...");
                    
                    var json = EncryptionHelper.Decrypt(encryptedLine);
                    Console.WriteLine($"[SERVER][AES] --- DECRYPTED: {json.Substring(0, Math.Min(100, json.Length))}...");
                    
                    var responseJson = await _chatProcessingService.HandleRequestAsync(json);
                    var responseEncrypted = EncryptionHelper.Encrypt(responseJson);
                    
                    Console.WriteLine($"[SERVER][AES] >>> TO CLIENT (encrypted): {responseEncrypted.Substring(0, Math.Min(60, responseEncrypted.Length))}...");
                    
                    await writer.WriteLineAsync(responseEncrypted);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error handling client: " + ex);
            }
            Console.WriteLine("Client disconnected.");
        }
    }
}


