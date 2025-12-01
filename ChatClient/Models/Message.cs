using System;

namespace ChatClient.Models
{
    /// <summary>
    /// Model tin nhắn trên client.
    /// SecurityLabel thể hiện nhãn bảo mật của tin nhắn (ví dụ: 1=LOW, 2=MEDIUM, 3=HIGH).
    /// Việc kiểm tra nhãn sẽ được thực hiện ở server qua MACService + VPD.
    /// </summary>
    public class Message
    {
        public int MessageId { get; set; }
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public string SenderUsername { get; set; } = string.Empty;
        public string ReceiverUsername { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int SecurityLabel { get; set; }
        public DateTime Timestamp { get; set; }
    }
}


