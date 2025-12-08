using System;

namespace ChatClient.Models
{
    /// <summary>
    /// Thông tin người dùng trên client.
    /// Đồng bộ với bảng TAIKHOAN và NGUOIDUNG trong database.
    /// </summary>
    public class User
    {
        // ========== TAIKHOAN ==========
        public string Matk { get; set; } = string.Empty;           // MATK - Mã tài khoản
        public string Username { get; set; } = string.Empty;       // TENTK - Tên tài khoản
        public string Password { get; set; } = string.Empty;       // Plain text (client), hash (server)
        public int ClearanceLevel { get; set; }                    // CLEARANCELEVEL - MAC level (1-5)
        public string Mavaitro { get; set; } = string.Empty;       // MAVAITRO - Mã vai trò
        public bool IsBannedGlobal { get; set; }                   // IS_BANNED_GLOBAL
        public bool IsOtpVerified { get; set; }                    // IS_OTP_VERIFIED
        public string ProfileName { get; set; } = string.Empty;    // PROFILE_NAME
        public DateTime NgayTao { get; set; }                      // NGAYTAO
        public DateTime? LastLogin { get; set; }                   // LAST_LOGIN
        public string PublicKey { get; set; } = string.Empty;      // PUBLIC_KEY (cho RSA encryption)
        
        // ========== NGUOIDUNG ==========
        public string Mapb { get; set; } = string.Empty;           // MAPB - Mã phòng ban
        public string Macv { get; set; } = string.Empty;           // MACV - Mã chức vụ
        public string Hovaten { get; set; } = string.Empty;        // HOVATEN
        public string Email { get; set; } = string.Empty;          // EMAIL
        public string Sdt { get; set; } = string.Empty;            // SDT - Số điện thoại
        public DateTime? Ngaysinh { get; set; }                    // NGAYSINH
        public string Diachi { get; set; } = string.Empty;         // DIACHI
        public string AvatarUrl { get; set; } = string.Empty;      // AVATAR_URL
        public string Bio { get; set; } = string.Empty;            // BIO
        
        // ========== HELPER ==========
        public string DisplayName => !string.IsNullOrEmpty(Hovaten) ? Hovaten : Username;
    }
}


