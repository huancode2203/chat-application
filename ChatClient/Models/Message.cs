using System;

namespace ChatClient.Models
{
    /// <summary>
    /// Model tin nhắn trên client.
    /// SecurityLabel thể hiện nhãn bảo mật của tin nhắn (ví dụ: 1=LOW, 2=MEDIUM, 3=HIGH).
    /// Việc kiểm tra nhãn sẽ được thực hiện ở server qua MACService + VPD.
    /// Matk: Mã tài khoản (tương ứng MATK trong database)
    /// Mactc: Mã cuộc trò chuyện (tương ứng MACTC trong database)
    /// </summary>
    public class Message
    {
        public int MessageId { get; set; } // MATN
        public string SenderMatk { get; set; } = string.Empty; // MATK người gửi
        public string ReceiverMatk { get; set; } = string.Empty; // MATK người nhận (nếu có)
        public string ConversationId { get; set; } = string.Empty; // MACTC - Mã cuộc trò chuyện
        public string SenderUsername { get; set; } = string.Empty; // TENTK người gửi
        public string ReceiverUsername { get; set; } = string.Empty; // TENTK người nhận (nếu có)
        public string Content { get; set; } = string.Empty; // NOIDUNG
        public int SecurityLabel { get; set; } // SECURITYLABEL
        public DateTime Timestamp { get; set; } // NGAYGUI
    }
}


