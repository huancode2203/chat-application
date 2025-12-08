using System;

namespace ChatClient.Models
{
    /// <summary>
    /// Model cuộc trò chuyện trên client.
    /// Đồng bộ với bảng CUOCTROCHUYEN trong database.
    /// </summary>
    public class Conversation
    {
        // ========== THÔNG TIN CƠ BẢN ==========
        public string Mactc { get; set; } = string.Empty;           // MACTC - Mã cuộc trò chuyện
        public string Tenctc { get; set; } = string.Empty;          // TENCTC - Tên cuộc trò chuyện
        public string Maloaictc { get; set; } = "GROUP";            // MALOAICTC (GROUP, PRIVATE, CHANNEL...)
        public bool IsPrivate { get; set; }                         // IS_PRIVATE
        public string Nguoiql { get; set; } = string.Empty;         // NGUOIQL - Người quản lý (owner)
        public string CreatedBy { get; set; } = string.Empty;       // CREATED_BY
        public DateTime NgayTao { get; set; }                       // NGAYTAO
        public string Mota { get; set; } = string.Empty;            // MOTA - Mô tả
        public string AvatarUrl { get; set; } = string.Empty;       // AVATAR_URL
        
        // ========== BẢO MẬT MAC ==========
        public int MinClearance { get; set; } = 1;                  // MIN_CLEARANCE (1-5)
        
        // ========== MÃ HÓA ==========
        public bool IsEncrypted { get; set; }                       // IS_ENCRYPTED
        
        // ========== TRẠNG THÁI ==========
        public bool IsArchived { get; set; }                        // IS_ARCHIVED
        public DateTime? ArchivedAt { get; set; }                   // ARCHIVED_AT
        public DateTime? ThoigianTinnhanCuoi { get; set; }          // THOIGIANTINNHANCUOI
        
        // ========== THỐNG KÊ (từ query) ==========
        public int MemberCount { get; set; }                        // Số thành viên
        public int MessageCount { get; set; }                       // Số tin nhắn
        public int UnreadCount { get; set; }                        // Số tin chưa đọc
        
        // ========== THÀNH VIÊN HIỆN TẠI ==========
        public string CurrentUserRole { get; set; } = "member";     // Vai trò của user hiện tại
        public bool CurrentUserIsBanned { get; set; }
        public bool CurrentUserIsMuted { get; set; }
        
        // ========== HELPER ==========
        public string DisplayName => !string.IsNullOrEmpty(Tenctc) ? Tenctc : Mactc;
        public bool IsGroup => Maloaictc == "GROUP";
        public bool IsChannel => Maloaictc == "CHANNEL";
    }
    
    /// <summary>
    /// Thông tin thành viên trong cuộc trò chuyện.
    /// Đồng bộ với bảng THANHVIEN.
    /// </summary>
    public class ConversationMember
    {
        public string Mactc { get; set; } = string.Empty;           // MACTC
        public string Matk { get; set; } = string.Empty;            // MATK
        public string Username { get; set; } = string.Empty;        // TENTK (join từ TAIKHOAN)
        public string Hovaten { get; set; } = string.Empty;         // HOVATEN (join từ NGUOIDUNG)
        public string Quyen { get; set; } = "member";               // QUYEN (owner, admin, member)
        public string Maphanquyen { get; set; } = "MEMBER";         // MAPHANQUYEN (OWNER, ADMIN, MODERATOR, MEMBER)
        public DateTime NgayThamGia { get; set; }                   // NGAYTHAMGIA
        public bool IsBanned { get; set; }                          // IS_BANNED
        public bool IsMuted { get; set; }                           // IS_MUTED
        public bool DeletedByMember { get; set; }                   // DELETED_BY_MEMBER
        public string? Nickname { get; set; }                       // NICKNAME
        public string? AvatarUrl { get; set; }                      // AVATAR_URL (join từ NGUOIDUNG)
        
        // ========== HELPER ==========
        public string DisplayName => !string.IsNullOrEmpty(Nickname) 
            ? Nickname 
            : (!string.IsNullOrEmpty(Hovaten) ? Hovaten : Username);
        public bool IsOwner => Quyen == "owner" || Maphanquyen == "OWNER";
        public bool IsAdmin => Quyen == "admin" || Maphanquyen == "ADMIN";
    }
    
    /// <summary>
    /// Các loại cuộc trò chuyện
    /// </summary>
    public static class ConversationTypes
    {
        public const string Group = "GROUP";
        public const string Private = "PRIVATE";
        public const string Channel = "CHANNEL";
        public const string Broadcast = "BROADCAST";
        public const string Support = "SUPPORT";
    }
    
    /// <summary>
    /// Các quyền trong nhóm
    /// </summary>
    public static class MemberRoles
    {
        public const string Owner = "OWNER";
        public const string Admin = "ADMIN";
        public const string Moderator = "MODERATOR";
        public const string Member = "MEMBER";
    }
}
