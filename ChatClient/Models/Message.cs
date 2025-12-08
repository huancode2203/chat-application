using System;

namespace ChatClient.Models
{
    /// <summary>
    /// Model tin nhắn trên client.
    /// Đồng bộ với bảng TINNHAN trong database.
    /// SecurityLabel thể hiện nhãn bảo mật MAC (1-5).
    /// </summary>
    public class Message
    {
        // ========== THÔNG TIN CƠ BẢN ==========
        public int MessageId { get; set; }                          // MATN
        public string ConversationId { get; set; } = string.Empty;  // MACTC
        public string SenderMatk { get; set; } = string.Empty;      // MATK người gửi
        public string SenderUsername { get; set; } = string.Empty;  // TENTK người gửi (join từ TAIKHOAN)
        public string Content { get; set; } = string.Empty;         // NOIDUNG
        public DateTime Timestamp { get; set; }                     // NGAYGUI
        
        // ========== LOẠI VÀ TRẠNG THÁI ==========
        public string MessageType { get; set; } = "TEXT";           // MALOAITN (TEXT, IMAGE, VIDEO, FILE, ENCRYPTED...)
        public string Status { get; set; } = "ACTIVE";              // MATRANGTHAI (ACTIVE, DELETED, EDITED...)
        public bool IsPinned { get; set; }                          // IS_PINNED
        public DateTime? EditedAt { get; set; }                     // EDITED_AT
        
        // ========== BẢO MẬT MAC ==========
        public int SecurityLabel { get; set; }                      // SECURITYLABEL (1-5)
        
        // ========== MÃ HÓA ==========
        public bool IsEncrypted { get; set; }                       // IS_ENCRYPTED
        public string EncryptionType { get; set; } = "NONE";        // ENCRYPTION_TYPE (NONE, AES, RSA, HYBRID)
        public byte[]? EncryptedContent { get; set; }               // ENCRYPTED_CONTENT
        public string? EncryptedKey { get; set; }                   // ENCRYPTED_KEY (session key cho Hybrid)
        public string? EncryptionIv { get; set; }                   // ENCRYPTION_IV
        public string? Signature { get; set; }                      // SIGNATURE (chữ ký RSA)
        
        // ========== RECEIVER (cho private chat) ==========
        public string ReceiverMatk { get; set; } = string.Empty;    // MATK người nhận
        public string ReceiverUsername { get; set; } = string.Empty;// TENTK người nhận
        
        // ========== ATTACHMENT ==========
        public int? AttachmentId { get; set; }                      // ATTACH_ID (nếu có file đính kèm)
        public string? AttachmentName { get; set; }                 // Tên file
        
        // ========== HELPER ==========
        public string DisplayContent => IsEncrypted ? "[Tin nhắn đã mã hóa]" : Content;
        public bool HasAttachment => AttachmentId.HasValue && AttachmentId > 0;
    }
    
    /// <summary>
    /// Các loại tin nhắn hỗ trợ
    /// </summary>
    public static class MessageTypes
    {
        public const string Text = "TEXT";
        public const string Image = "IMAGE";
        public const string Video = "VIDEO";
        public const string Audio = "AUDIO";
        public const string File = "FILE";
        public const string Location = "LOCATION";
        public const string Contact = "CONTACT";
        public const string Encrypted = "ENCRYPTED";
        public const string System = "SYSTEM";
    }
    
    /// <summary>
    /// Các loại mã hóa
    /// </summary>
    public static class EncryptionTypes
    {
        public const string None = "NONE";
        public const string Aes = "AES";           // Mã hóa đối xứng
        public const string Rsa = "RSA";           // Mã hóa bất đối xứng
        public const string Hybrid = "HYBRID";     // Mã hóa lai (AES + RSA)
    }
}


