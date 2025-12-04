using System;

namespace ChatClient.Models
{
    /// <summary>
    /// Thông tin người dùng trên client.
    /// Matk: Mã tài khoản (tương ứng MATK trong database)
    /// Username: Tên tài khoản (tương ứng TENTK trong database)
    /// ClearanceLevel dùng cho MAC (Mandatory Access Control).
    /// </summary>
    public class User
    {
        public string Matk { get; set; } = string.Empty; // Mã tài khoản (MATK)
        public string Username { get; set; } = string.Empty; // Tên tài khoản (TENTK)
        public string Password { get; set; } = string.Empty; // Ở client giữ dạng plain, server hash.
        public int ClearanceLevel { get; set; }
    }
}


