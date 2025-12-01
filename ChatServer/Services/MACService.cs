using System;

namespace ChatServer.Services
{
    /// <summary>
    /// MAC (Mandatory Access Control) theo mô hình đơn giản kiểu Bell-LaPadula:
    /// - No read up: user chỉ được đọc đối tượng có nhãn <= clearance của mình.
    /// - No write down: user không được ghi (tạo tin nhắn/file) xuống mức nhãn thấp hơn clearance.
    /// 
    /// ClearanceLevel / SecurityLabel là số nguyên: 1=LOW, 2=MEDIUM, 3=HIGH.
    /// </summary>
    public class MACService
    {
        public bool CanRead(int userClearanceLevel, int objectSecurityLabel)
        {
            // No read up: chỉ đọc được object có label <= clearance.
            return objectSecurityLabel <= userClearanceLevel;
        }

        public bool CanWrite(int userClearanceLevel, int objectSecurityLabel)
        {
            // No write down: chỉ ghi được tới label >= clearance.
            return objectSecurityLabel >= userClearanceLevel;
        }
    }
}


