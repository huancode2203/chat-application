using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using ChatServer.Services;

namespace ChatServer.Database
{
    /// <summary>
    /// DbContext đơn giản để làm việc với Oracle dựa trên script mới (TAIKHOAN, TINNHAN, AUDIT_LOGS).
    /// - Mở kết nối Oracle.
    /// - Thiết lập SESSION_USER_LEVEL cho VPD/MAC (thông qua package MAC_CTX_PKG).
    /// - Thực hiện insert/select vào bảng TINNHAN, AUDIT_LOGS, v.v.
    ///
    /// VPD (Virtual Private Database):
    /// - Trên Oracle, policy VPD gắn vào bảng TINNHAN dùng SYS_CONTEXT('MAC_CTX', 'USER_LEVEL')
    ///   để tự động thêm điều kiện WHERE cho từng user (no read up).
    /// </summary>
    public class DbContext : IDisposable
    {
        private readonly string _connectionString;
        private OracleConnection? _connection;

        public DbContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task OpenAsync()
        {
            _connection = new OracleConnection(_connectionString);
            await _connection.OpenAsync();
        }

        public OracleConnection Connection =>
            _connection ?? throw new InvalidOperationException("Connection is not opened.");

        /// <summary>
        /// Thiết lập SESSION_USER_LEVEL cho VPD/MAC.
        /// Trên DB đã có package MAC_CTX_PKG theo script của bạn:
        ///   MAC_CTX_PKG.SET_USER_LEVEL(p_user, p_level);
        /// </summary>
        public async Task SetSessionUserLevelAsync(string username, int level)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = "BEGIN MAC_CTX_PKG.SET_USER_LEVEL(:p_user, :p_level); END;";
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.Add(new OracleParameter("p_user", OracleDbType.Varchar2) { Value = username });
            cmd.Parameters.Add(new OracleParameter("p_level", OracleDbType.Int32) { Value = level });
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Ghi một tin nhắn vào bảng TINNHAN.
        ///
        /// Với schema mới, ta đơn giản lưu:
        /// - MATK: mã tài khoản (user gửi)
        /// - NOIDUNG: nội dung tin nhắn
        /// - SECURITYLABEL: nhãn bảo mật
        ///
        /// Các cột MACTC, MALOAITN, MATRANGTHAI có thể để NULL nếu không dùng conversation phức tạp.
        /// </summary>
        public async Task InsertMessageAsync(string senderMatk, string content, int securityLabel)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO TINNHAN (MATK, NOIDUNG, SECURITYLABEL)
                VALUES (:p_matk, :p_content, :p_label)";
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = senderMatk });
            cmd.Parameters.Add(new OracleParameter("p_content", OracleDbType.Clob) { Value = content });
            cmd.Parameters.Add(new OracleParameter("p_label", OracleDbType.Int32) { Value = securityLabel });
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Lấy danh sách tin nhắn của một user (theo MATK).
        /// VPD trên TINNHAN sẽ tự động filter theo SECURITYLABEL và SESSION_USER_LEVEL.
        /// Demo: chỉ lấy các tin mà MATK = :p_matk (user là người gửi).
        /// Bạn có thể mở rộng thêm điều kiện MACTC để hỗ trợ cuộc trò chuyện nhóm, v.v.
        /// </summary>
        public async Task<List<ChatMessageRecord>> GetMessagesForUserAsync(string matk)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = @"
                SELECT t.MATN,
                       t.MATK,
                       t.NOIDUNG,
                       t.SECURITYLABEL,
                       t.NGAYGUI
                  FROM TINNHAN t
                 WHERE t.MATK = :p_matk
                 ORDER BY t.NGAYGUI";
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });

            var result = new List<ChatMessageRecord>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new ChatMessageRecord
                {
                    MessageId = reader.GetInt32(0),
                    SenderMatk = reader.GetString(1),
                    Content = reader.GetString(2),
                    SecurityLabel = reader.GetInt32(3),
                    Timestamp = reader.GetDateTime(4)
                });
            }
            return result;
        }

        /// <summary>
        /// Lấy thông tin tài khoản (MATK, CLEARANCELEVEL) từ bảng TAIKHOAN.
        /// MATK chính là username mà client dùng để đăng nhập.
        /// </summary>
        public async Task<UserAccount?> GetUserAccountAsync(string matk)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = @"
                SELECT MATK, PASSWORD_HASH, CLEARANCELEVEL
                  FROM TAIKHOAN
                 WHERE MATK = :p_matk";
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });
            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }

            return new UserAccount
            {
                Matk = reader.GetString(0),
                PasswordHash = reader.GetString(1),
                ClearanceLevel = reader.GetInt32(2)
            };
        }

        /// <summary>
        /// Ghi audit log vào bảng AUDIT_LOGS.
        /// LOGID dùng identity nên không cần sequence.
        /// </summary>
        public async Task WriteAuditLogAsync(string matk, string action, string target, int securityLabel)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO AUDIT_LOGS (MATK, ACTION, TARGET, SECURITYLABEL)
                VALUES (:p_matk, :p_action, :p_target, :p_label)";
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });
            cmd.Parameters.Add(new OracleParameter("p_action", OracleDbType.Varchar2) { Value = action });
            cmd.Parameters.Add(new OracleParameter("p_target", OracleDbType.Varchar2) { Value = target });
            cmd.Parameters.Add(new OracleParameter("p_label", OracleDbType.Int32) { Value = securityLabel });
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Tạo tài khoản mới (gọi stored procedure SP_TAO_TAIKHOAN).
        /// </summary>
        public async Task CreateAccountAsync(string matk, string tentk, string passwordHash, string? mavaitro, int clearanceLevel)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = "BEGIN SP_TAO_TAIKHOAN(:p_matk, :p_tentk, :p_password_hash, :p_mavaitro, :p_clearance); END;";
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });
            cmd.Parameters.Add(new OracleParameter("p_tentk", OracleDbType.Varchar2) { Value = tentk });
            cmd.Parameters.Add(new OracleParameter("p_password_hash", OracleDbType.Varchar2) { Value = passwordHash });
            cmd.Parameters.Add(new OracleParameter("p_mavaitro", OracleDbType.Varchar2) { Value = (object?)mavaitro ?? DBNull.Value });
            cmd.Parameters.Add(new OracleParameter("p_clearance", OracleDbType.Int32) { Value = clearanceLevel });
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Tạo OTP mới trong bảng XACTHUCOTP.
        /// </summary>
        public async Task<int> CreateOtpAsync(string matk, string email, string otpHash, int expiryMinutes = 10)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO XACTHUCOTP (MATK, EMAIL, MAXTOTP, THOIGIANTONTAI)
                VALUES (:p_matk, :p_email, :p_otp_hash, SYSTIMESTAMP + NUMTODSINTERVAL(:p_expiry, 'MINUTE'))
                RETURNING MAOTP INTO :p_out_maotp";
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });
            cmd.Parameters.Add(new OracleParameter("p_email", OracleDbType.Varchar2) { Value = email });
            cmd.Parameters.Add(new OracleParameter("p_otp_hash", OracleDbType.Varchar2) { Value = otpHash });
            cmd.Parameters.Add(new OracleParameter("p_expiry", OracleDbType.Int32) { Value = expiryMinutes });
            var outParam = new OracleParameter("p_out_maotp", OracleDbType.Int32) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(outParam);
            await cmd.ExecuteNonQueryAsync();
            return Convert.ToInt32(outParam.Value);
        }

        /// <summary>
        /// Xác minh OTP: kiểm tra OTP có đúng và chưa hết hạn, chưa được xác minh.
        /// </summary>
        public async Task<bool> VerifyOtpAsync(string matk, string otpHash)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = @"
                SELECT COUNT(*) FROM XACTHUCOTP
                WHERE MATK = :p_matk
                  AND MAXTOTP = :p_otp_hash
                  AND THOIGIANTONTAI > SYSTIMESTAMP
                  AND DAXACMINH = 0";
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });
            cmd.Parameters.Add(new OracleParameter("p_otp_hash", OracleDbType.Varchar2) { Value = otpHash });
            var result = await cmd.ExecuteScalarAsync();
            if (result == null || result == DBNull.Value) return false;
            var count = Convert.ToInt32(result);
            if (count > 0)
            {
                // Đánh dấu OTP đã được xác minh
                using var updateCmd = Connection.CreateCommand();
                updateCmd.CommandText = @"
                    UPDATE XACTHUCOTP SET DAXACMINH = 1
                    WHERE MATK = :p_matk AND MAXTOTP = :p_otp_hash";
                updateCmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });
                updateCmd.Parameters.Add(new OracleParameter("p_otp_hash", OracleDbType.Varchar2) { Value = otpHash });
                await updateCmd.ExecuteNonQueryAsync();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Đổi mật khẩu (gọi stored procedure SP_DOI_MATKHAU).
        /// </summary>
        public async Task UpdatePasswordAsync(string matk, string newPasswordHash)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = "BEGIN SP_DOI_MATKHAU(:p_matk, :p_new_password_hash); END;";
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });
            cmd.Parameters.Add(new OracleParameter("p_new_password_hash", OracleDbType.Varchar2) { Value = newPasswordHash });
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Lấy email từ bảng NGUOIDUNG hoặc từ tham số khi tạo OTP.
        /// </summary>
        public async Task<string?> GetEmailByMatkAsync(string matk)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = "SELECT EMAIL FROM NGUOIDUNG WHERE MATK = :p_matk";
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });
            var result = await cmd.ExecuteScalarAsync();
            if (result == null || result == DBNull.Value) return null;
            return result.ToString();
        }

        /// <summary>
        /// Kiểm tra tài khoản đã tồn tại chưa.
        /// </summary>
        public async Task<bool> AccountExistsAsync(string matk)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM TAIKHOAN WHERE MATK = :p_matk";
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });
            var result = await cmd.ExecuteScalarAsync();
            if (result == null || result == DBNull.Value) return false;
            return Convert.ToInt32(result) > 0;
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }
    }

    public class ChatMessageRecord
    {
        public int MessageId { get; set; }
        public string SenderMatk { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int SecurityLabel { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Thông tin tài khoản đọc từ bảng TAIKHOAN.
    /// </summary>
    public class UserAccount
    {
        public string Matk { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public int ClearanceLevel { get; set; }
    }
}


