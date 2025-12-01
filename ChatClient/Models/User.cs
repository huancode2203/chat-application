using System;

namespace ChatClient.Models
{
    /// <summary>
    /// Thông tin người dùng trên client.
    /// ClearanceLevel dùng cho MAC (Mandatory Access Control).
    /// </summary>
    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty; // Ở client giữ dạng plain, server hash.
        public int ClearanceLevel { get; set; }
    }
}


