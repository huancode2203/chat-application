using System;

namespace ChatServer.Services
{
    /// <summary>
    /// MAC (Mandatory Access Control) - Cải tiến cho enterprise chat:
    /// - No read up: User chỉ đọc được tin nhắn có label <= clearance.
    /// - Write flexibility: User có thể gửi tin với label <= clearance (linh hoạt hơn).
    /// 
    /// Ví dụ: User clearance 3 có thể gửi tin với label 1, 2, hoặc 3.
    /// ClearanceLevel / SecurityLabel: 1=LOW, 2=MEDIUM, 3=HIGH, 4=TOP SECRET, 5=CLASSIFIED
    /// </summary>
    public class MACService
    {
        public bool CanRead(int userClearanceLevel, int objectSecurityLabel)
        {
            // No read up: chỉ đọc được object có label <= clearance
            return objectSecurityLabel <= userClearanceLevel;
        }

        public bool CanWrite(int userClearanceLevel, int objectSecurityLabel)
        {
            // Cho phép ghi với label <= clearance (user cao có thể gửi tin thấp)
            // Ví dụ: User level 3 có thể gửi message level 1, 2, 3
            return objectSecurityLabel <= userClearanceLevel;
        }
    }
}


