using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using ChatServer.Services;

namespace ChatServer.Database
{
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

        public async Task SetSessionUserLevelAsync(string username, int level)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = "BEGIN MAC_CTX_PKG.SET_USER_LEVEL(:p_user, :p_level); END;";
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.Add(new OracleParameter("p_user", OracleDbType.Varchar2) { Value = username });
            cmd.Parameters.Add(new OracleParameter("p_level", OracleDbType.Int32) { Value = level });
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task InsertMessageAsync(string senderMatk, string content, int securityLabel)
        {
            // Note: This method is legacy and should use SP_GUI_TINNHAN instead
            // Messages should always be associated with a conversation (MACTC)
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO TINNHAN (MATK, NOIDUNG, SECURITYLABEL, MALOAITN, MATRANGTHAI)
                VALUES (:p_matk, :p_content, :p_label, 'TEXT', 'ACTIVE')";
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = senderMatk });
            cmd.Parameters.Add(new OracleParameter("p_content", OracleDbType.Clob) { Value = content });
            cmd.Parameters.Add(new OracleParameter("p_label", OracleDbType.Int32) { Value = securityLabel });
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<ChatMessageRecord>> GetMessagesForUserAsync(string matk)
        {
            // Legacy method: Get messages from conversations where user is a member
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = @"
                SELECT t.MATN, t.MACTC, t.MATK, t.NOIDUNG, t.SECURITYLABEL, t.NGAYGUI
                FROM TINNHAN t
                INNER JOIN THANHVIEN tv ON t.MACTC = tv.MACTC
                WHERE tv.MATK = :p_matk AND tv.DELETED_BY_MEMBER = 0
                ORDER BY t.NGAYGUI";
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });

            var result = new List<ChatMessageRecord>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new ChatMessageRecord
                {
                    MessageId = reader.GetInt32(0),
                    ConversationId = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                    SenderMatk = reader.GetString(2),
                    Content = reader.GetString(3),
                    SecurityLabel = reader.GetInt32(4),
                    Timestamp = reader.GetDateTime(5)
                });
            }
            return result;
        }

        public async Task<UserAccount?> GetUserAccountAsync(string usernameOrMatk)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = @"
                SELECT MATK, TENTK, PASSWORD_HASH, MAVAITRO, NVL(CLEARANCELEVEL, 1), 
                       NVL(IS_BANNED_GLOBAL, 0), NVL(IS_OTP_VERIFIED, 0), PROFILE_NAME,
                       NGAYTAO, LAST_LOGIN, LAST_LOGOUT, NVL(LOGIN_COUNT, 0), PUBLIC_KEY
                FROM TAIKHOAN
                WHERE MATK = :p_value OR TENTK = :p_value";
            cmd.Parameters.Add(new OracleParameter("p_value", OracleDbType.Varchar2) { Value = usernameOrMatk });
            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }

            return new UserAccount
            {
                Matk = reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                Username = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                PasswordHash = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                Mavaitro = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                ClearanceLevel = reader.GetInt32(4),
                IsBannedGlobal = reader.GetInt32(5) == 1,
                IsOtpVerified = reader.GetInt32(6) == 1,
                ProfileName = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                NgayTao = reader.IsDBNull(8) ? DateTime.Now : reader.GetDateTime(8),
                LastLogin = reader.IsDBNull(9) ? null : reader.GetDateTime(9),
                LastLogout = reader.IsDBNull(10) ? null : reader.GetDateTime(10),
                LoginCount = reader.GetInt32(11),
                PublicKey = reader.IsDBNull(12) ? null : reader.GetString(12)
            };
        }

        public async Task WriteAuditLogAsync(string matk, string action, string target, int securityLabel)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = "BEGIN SP_WRITE_AUDIT_LOG(:p_matk, :p_action, :p_target, :p_securitylabel); END;";
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });
            cmd.Parameters.Add(new OracleParameter("p_action", OracleDbType.Varchar2) { Value = action });
            cmd.Parameters.Add(new OracleParameter("p_target", OracleDbType.Varchar2) { Value = target });
            cmd.Parameters.Add(new OracleParameter("p_securitylabel", OracleDbType.Decimal) { Value = securityLabel });
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Thiết lập MAC context cho VPD policies. 
        /// Level cao hơn sẽ bypass VPD restrictions.
        /// </summary>
        public async Task SetMacContextAsync(string matk, int clearanceLevel)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = "BEGIN SET_MAC_CONTEXT(:p_matk, :p_level); END;";
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });
            cmd.Parameters.Add(new OracleParameter("p_level", OracleDbType.Int32) { Value = clearanceLevel });
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Clear MAC context (reset về mặc định)
        /// </summary>
        public async Task ClearMacContextAsync()
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = "BEGIN MAC_CTX_PKG.CLEAR_CONTEXT; END;";
            cmd.CommandType = CommandType.Text;
            await cmd.ExecuteNonQueryAsync();
        }

        // === LOGIN ATTEMPT TRACKING ===
        
        /// <summary>
        /// Kiểm tra tài khoản có bị khóa do đăng nhập sai quá nhiều lần không
        /// Nếu lock đã hết hạn, tự động reset FAILED_LOGIN_ATTEMPTS và LOCKED_UNTIL
        /// </summary>
        public async Task<(bool IsLocked, DateTime? LockedUntil, int FailedAttempts)> CheckAccountLockStatusAsync(string username)
        {
            // BƯỚC 1: Đọc dữ liệu hiện tại
            int failedAttempts = 0;
            DateTime? lockedUntil = null;
            
            using (var cmd = Connection.CreateCommand())
            {
                cmd.BindByName = true;
                cmd.CommandText = "SELECT NVL(FAILED_LOGIN_ATTEMPTS, 0), LOCKED_UNTIL FROM TAIKHOAN WHERE TENTK = :p_username OR MATK = :p_username";
                cmd.Parameters.Add(new OracleParameter("p_username", OracleDbType.Varchar2) { Value = username });
                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    failedAttempts = reader.GetInt32(0);
                    lockedUntil = reader.IsDBNull(1) ? null : reader.GetDateTime(1);
                }
                else
                {
                    return (false, null, 0); // User không tồn tại
                }
            }
            
            Console.WriteLine($"[LOGIN] Check: User={username}, FailedAttempts={failedAttempts}, LockedUntil={lockedUntil}");
            
            // BƯỚC 2: Xử lý các trường hợp
            
            // Case 1: Chưa đủ 5 lần sai => KHÔNG KHÓA (dù LOCKED_UNTIL có giá trị gì)
            if (failedAttempts < 5)
            {
                // Nếu có LOCKED_UNTIL nhưng chưa đủ 5 lần => clear nó đi (dữ liệu lỗi)
                if (lockedUntil.HasValue)
                {
                    Console.WriteLine($"[LOGIN] Clearing invalid LOCKED_UNTIL (attempts={failedAttempts} < 5)");
                    using var clearCmd = Connection.CreateCommand();
                    clearCmd.BindByName = true;
                    clearCmd.CommandText = "UPDATE TAIKHOAN SET LOCKED_UNTIL = NULL WHERE TENTK = :p_username OR MATK = :p_username";
                    clearCmd.Parameters.Add(new OracleParameter("p_username", OracleDbType.Varchar2) { Value = username });
                    await clearCmd.ExecuteNonQueryAsync();
                }
                Console.WriteLine($"[LOGIN] NOT locked (attempts={failedAttempts})");
                return (false, null, failedAttempts);
            }
            
            // Case 2: Đủ 5 lần sai, kiểm tra LOCKED_UNTIL
            if (lockedUntil.HasValue && lockedUntil.Value > DateTime.Now)
            {
                // Vẫn đang trong thời gian khóa
                Console.WriteLine($"[LOGIN] LOCKED until {lockedUntil.Value}");
                return (true, lockedUntil, failedAttempts);
            }
            
            // Case 3: Đủ 5 lần nhưng LOCKED_UNTIL đã hết hạn hoặc null => reset
            Console.WriteLine($"[LOGIN] Lock expired or missing, resetting...");
            await ResetFailedLoginAsync(username);
            return (false, null, 0);
        }

        /// <summary>
        /// Tăng số lần đăng nhập sai và khóa tài khoản nếu vượt quá 5 lần
        /// </summary>
        public async Task<(int NewFailedCount, bool IsNowLocked)> IncrementFailedLoginAsync(string username)
        {
            // Step 1: Update FAILED_LOGIN_ATTEMPTS và set LOCKED_UNTIL nếu cần
            using (var updateCmd = Connection.CreateCommand())
            {
                updateCmd.BindByName = true;
                // FIX: Clear LOCKED_UNTIL nếu chưa đủ 5 lần (ELSE NULL thay vì ELSE LOCKED_UNTIL)
                updateCmd.CommandText = @"
                    UPDATE TAIKHOAN 
                    SET FAILED_LOGIN_ATTEMPTS = NVL(FAILED_LOGIN_ATTEMPTS, 0) + 1,
                        LOCKED_UNTIL = CASE 
                            WHEN NVL(FAILED_LOGIN_ATTEMPTS, 0) + 1 >= 5 THEN SYSTIMESTAMP + INTERVAL '30' MINUTE
                            ELSE NULL 
                        END
                    WHERE TENTK = :p_username OR MATK = :p_username";
                updateCmd.Parameters.Add(new OracleParameter("p_username", OracleDbType.Varchar2) { Value = username });
                await updateCmd.ExecuteNonQueryAsync();
            }
            
            // Step 2: Query to get the new count và kiểm tra lock status
            using (var selectCmd = Connection.CreateCommand())
            {
                selectCmd.BindByName = true;
                selectCmd.CommandText = @"
                    SELECT NVL(FAILED_LOGIN_ATTEMPTS, 0), LOCKED_UNTIL 
                    FROM TAIKHOAN 
                    WHERE TENTK = :p_username OR MATK = :p_username";
                selectCmd.Parameters.Add(new OracleParameter("p_username", OracleDbType.Varchar2) { Value = username });
                
                using var reader = await selectCmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var newCount = reader.GetInt32(0);
                    var lockedUntil = reader.IsDBNull(1) ? (DateTime?)null : reader.GetDateTime(1);
                    // Chỉ locked khi count >= 5 VÀ LOCKED_UNTIL > NOW
                    var isLocked = newCount >= 5 && lockedUntil.HasValue && lockedUntil.Value > DateTime.Now;
                    return (newCount, isLocked);
                }
            }
            
            return (0, false);
        }

        /// <summary>
        /// Reset số lần đăng nhập sai khi đăng nhập thành công
        /// </summary>
        public async Task ResetFailedLoginAsync(string username)
        {
            using var cmd = Connection.CreateCommand();
            cmd.BindByName = true;
            cmd.CommandText = @"
                UPDATE TAIKHOAN 
                SET FAILED_LOGIN_ATTEMPTS = 0, LOCKED_UNTIL = NULL 
                WHERE TENTK = :p_username OR MATK = :p_username";
            cmd.Parameters.Add(new OracleParameter("p_username", OracleDbType.Varchar2) { Value = username });
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Mở khóa tài khoản bị khóa do đăng nhập sai (dùng trong Admin Panel)
        /// </summary>
        public async Task UnlockAccountAsync(string matk)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = @"
                UPDATE TAIKHOAN 
                SET FAILED_LOGIN_ATTEMPTS = 0, LOCKED_UNTIL = NULL 
                WHERE MATK = :p_matk";
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Sinh MATK tự động theo format TKxxx (TK009, TK010, ...)
        /// </summary>
        public async Task<string> GenerateNextMatkAsync()
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = @"
                SELECT 'TK' || LPAD(NVL(MAX(TO_NUMBER(SUBSTR(MATK, 3))), 0) + 1, 3, '0')
                FROM TAIKHOAN 
                WHERE REGEXP_LIKE(MATK, '^TK[0-9]+$')";
            var result = await cmd.ExecuteScalarAsync();
            return result?.ToString() ?? "TK001";
        }

        public async Task CreateAccountAsync(string matk, string tentk, string passwordHash, string? mavaitro, int clearanceLevel, bool isVerified = true)
        {
            try
            {
                using var cmd = Connection.CreateCommand();
                // p_is_verified = 1 để user tạo từ admin có thể nhắn tin ngay (không cần OTP)
                cmd.CommandText = "BEGIN SP_TAO_TAIKHOAN(:p_matk, :p_tentk, :p_password_hash, :p_mavaitro, :p_clearance, :p_is_verified); END;";
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });
                cmd.Parameters.Add(new OracleParameter("p_tentk", OracleDbType.Varchar2) { Value = tentk });
                cmd.Parameters.Add(new OracleParameter("p_password_hash", OracleDbType.Varchar2) { Value = passwordHash });
                cmd.Parameters.Add(new OracleParameter("p_mavaitro", OracleDbType.Varchar2) { Value = (object?)mavaitro ?? DBNull.Value });
                cmd.Parameters.Add(new OracleParameter("p_clearance", OracleDbType.Int32) { Value = clearanceLevel });
                cmd.Parameters.Add(new OracleParameter("p_is_verified", OracleDbType.Int32) { Value = isVerified ? 1 : 0 });
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException ex)
            {
                // Chỉ throw message string, không throw OracleException object
                var errorMessage = ex.Message;
                // Nếu là lỗi duplicate key, tài khoản đã tồn tại
                if (ex.Number == 1) // ORA-00001: unique constraint violated
                {
                    throw new Exception("Username already exists.");
                }
                throw new Exception($"Database error: {errorMessage}");
            }
        }

        public async Task<int> CreateOtpAsync(string matk, string email, string otpHash, int expiryMinutes = 10)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = "BEGIN SP_TAO_OTP(:p_matk, :p_email, :p_otp_hash, :p_expiry, :p_maotp); END;";
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });
            cmd.Parameters.Add(new OracleParameter("p_email", OracleDbType.Varchar2) { Value = email });
            cmd.Parameters.Add(new OracleParameter("p_otp_hash", OracleDbType.Varchar2) { Value = otpHash });
            cmd.Parameters.Add(new OracleParameter("p_expiry", OracleDbType.Decimal) { Value = expiryMinutes });
            var outParam = new OracleParameter("p_maotp", OracleDbType.Decimal) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(outParam);
            await cmd.ExecuteNonQueryAsync();
            if (outParam.Value is OracleDecimal oracleDecimal)
            {
                return oracleDecimal.ToInt32();
            }
            return Convert.ToInt32(outParam.Value);
        }

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

        public async Task<bool> IsOtpVerifiedAsync(string matk)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = @"
                SELECT 
                    SUM(CASE WHEN DAXACMINH = 1 THEN 1 ELSE 0 END) AS VERIFIED_COUNT,
                    COUNT(*) AS TOTAL_COUNT
                FROM XACTHUCOTP
                WHERE MATK = :p_matk";
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });

            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                // Không có bản ghi OTP nào => coi như đã verify (tài khoản cũ, được seed sẵn)
                return true;
            }

            var verifiedCount = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
            var totalCount = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);

            // Nếu chưa từng tạo OTP, coi như verified để hỗ trợ các tài khoản được seed sẵn
            if (totalCount == 0)
                return true;

            return verifiedCount > 0;
        }

        public async Task UpdatePasswordAsync(string matk, string newPasswordHash)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = "BEGIN SP_DOI_MATKHAU(:p_matk, :p_new_password_hash); END;";
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });
            cmd.Parameters.Add(new OracleParameter("p_new_password_hash", OracleDbType.Varchar2) { Value = newPasswordHash });
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<string?> GetEmailByMatkAsync(string matk)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = "SELECT EMAIL FROM NGUOIDUNG WHERE MATK = :p_matk";
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });
            var result = await cmd.ExecuteScalarAsync();
            if (result == null || result == DBNull.Value) return null;
            return result.ToString();
        }

        public async Task<bool> AccountExistsAsync(string matk)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM TAIKHOAN WHERE MATK = :p_matk OR TENTK = :p_matk";
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });
            var result = await cmd.ExecuteScalarAsync();
            if (result == null || result == DBNull.Value) return false;
            return Convert.ToInt32(result) > 0;
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = @"
                SELECT COUNT(*) FROM NGUOIDUNG WHERE UPPER(EMAIL) = UPPER(:p_email)
                UNION ALL
                SELECT COUNT(*) FROM XACTHUCOTP WHERE UPPER(EMAIL) = UPPER(:p_email)";
            cmd.Parameters.Add(new OracleParameter("p_email", OracleDbType.Varchar2) { Value = email });
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (Convert.ToInt32(reader.GetValue(0)) > 0) return true;
            }
            return false;
        }

        /// <summary>
        /// Cập nhật LAST_LOGIN và tăng LOGIN_COUNT khi user đăng nhập
        /// </summary>
        public async Task UpdateLoginInfoAsync(string matk, string? ipAddress = null)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = @"
                UPDATE TAIKHOAN 
                SET LAST_LOGIN = SYSTIMESTAMP,
                    LOGIN_COUNT = NVL(LOGIN_COUNT, 0) + 1
                WHERE MATK = :p_matk";
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });
            await cmd.ExecuteNonQueryAsync();
            
            // Tạo phiên đăng nhập mới
            await CreateLoginSessionAsync(matk, ipAddress);
        }

        /// <summary>
        /// Tạo phiên đăng nhập mới trong bảng PHIENDANGNHAP
        /// </summary>
        public async Task<string> CreateLoginSessionAsync(string matk, string? ipAddress = null)
        {
            var sessionId = $"SESSION_{Guid.NewGuid():N}";
            var maphien = sessionId.Length > 50 ? sessionId.Substring(0, 50) : sessionId;
            
            // Lấy clearance level của user
            int clearanceLevel = 1;
            using (var clCmd = Connection.CreateCommand())
            {
                clCmd.CommandText = "SELECT CLEARANCELEVEL FROM TAIKHOAN WHERE MATK = :p_matk";
                clCmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });
                var result = await clCmd.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                    clearanceLevel = Convert.ToInt32(result);
            }
            
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO PHIENDANGNHAP (MAPHIEN, MATK, IP_ADDRESS, THOIDIEM_DANGNHAP, THOIDIEM_HETHAN, TRANG_THAI, CLEARANCELEVEL_SESSION)
                VALUES (:p_maphien, :p_matk, :p_ip, SYSTIMESTAMP, SYSTIMESTAMP + INTERVAL '8' HOUR, 'ACTIVE', :p_clearance)";
            cmd.Parameters.Add(new OracleParameter("p_maphien", OracleDbType.Varchar2) { Value = maphien });
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });
            cmd.Parameters.Add(new OracleParameter("p_ip", OracleDbType.Varchar2) { Value = (object?)ipAddress ?? DBNull.Value });
            cmd.Parameters.Add(new OracleParameter("p_clearance", OracleDbType.Int32) { Value = clearanceLevel });
            await cmd.ExecuteNonQueryAsync();
            
            return maphien;
        }

        /// <summary>
        /// Cập nhật LAST_LOGOUT khi user đăng xuất
        /// </summary>
        public async Task UpdateLogoutInfoAsync(string matk)
        {
            // Cập nhật TAIKHOAN
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = @"
                UPDATE TAIKHOAN 
                SET LAST_LOGOUT = SYSTIMESTAMP
                WHERE MATK = :p_matk";
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });
            await cmd.ExecuteNonQueryAsync();
            
            // Đánh dấu các session là đã logout
            using var sessionCmd = Connection.CreateCommand();
            sessionCmd.CommandText = @"
                UPDATE PHIENDANGNHAP 
                SET TRANG_THAI = 'LOGGED_OUT'
                WHERE MATK = :p_matk AND TRANG_THAI = 'ACTIVE'";
            sessionCmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });
            await sessionCmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Đăng ký RSA public key cho user (dùng cho mã hóa bất đối xứng)
        /// </summary>
        public async Task RegisterPublicKeyAsync(string matk, string publicKeyBase64)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = "BEGIN SP_REGISTER_PUBLIC_KEY(:p_matk, :p_public_key); END;";
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });
            cmd.Parameters.Add(new OracleParameter("p_public_key", OracleDbType.Clob) { Value = publicKeyBase64 });
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Lấy public key của user
        /// </summary>
        public async Task<string?> GetPublicKeyAsync(string matk)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = "SELECT PUBLIC_KEY FROM TAIKHOAN WHERE MATK = :p_matk OR TENTK = :p_matk";
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });
            var result = await cmd.ExecuteScalarAsync();
            if (result == null || result == DBNull.Value) return null;
            return result.ToString();
        }

        /// <summary>
        /// Cập nhật thông tin người dùng (NGUOIDUNG)
        /// </summary>
        public async Task UpdateUserProfileAsync(string matk, string? hovaten = null, string? email = null, 
            string? sdt = null, string? diachi = null, string? bio = null, string? avatarUrl = null)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = @"
                MERGE INTO NGUOIDUNG n
                USING (SELECT :p_matk AS MATK FROM DUAL) src
                ON (n.MATK = src.MATK)
                WHEN MATCHED THEN UPDATE SET
                    HOVATEN = NVL(:p_hovaten, n.HOVATEN),
                    EMAIL = NVL(:p_email, n.EMAIL),
                    SDT = NVL(:p_sdt, n.SDT),
                    DIACHI = NVL(:p_diachi, n.DIACHI),
                    BIO = NVL(:p_bio, n.BIO),
                    AVATAR_URL = NVL(:p_avatar, n.AVATAR_URL),
                    NGAYCAPNHAT = SYSTIMESTAMP
                WHEN NOT MATCHED THEN INSERT (MATK, HOVATEN, EMAIL, SDT, DIACHI, BIO, AVATAR_URL, NGAYCAPNHAT)
                VALUES (:p_matk, :p_hovaten, :p_email, :p_sdt, :p_diachi, :p_bio, :p_avatar, SYSTIMESTAMP)";
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });
            cmd.Parameters.Add(new OracleParameter("p_hovaten", OracleDbType.Varchar2) { Value = (object?)hovaten ?? DBNull.Value });
            cmd.Parameters.Add(new OracleParameter("p_email", OracleDbType.Varchar2) { Value = (object?)email ?? DBNull.Value });
            cmd.Parameters.Add(new OracleParameter("p_sdt", OracleDbType.Varchar2) { Value = (object?)sdt ?? DBNull.Value });
            cmd.Parameters.Add(new OracleParameter("p_diachi", OracleDbType.Varchar2) { Value = (object?)diachi ?? DBNull.Value });
            cmd.Parameters.Add(new OracleParameter("p_bio", OracleDbType.Varchar2) { Value = (object?)bio ?? DBNull.Value });
            cmd.Parameters.Add(new OracleParameter("p_avatar", OracleDbType.Varchar2) { Value = (object?)avatarUrl ?? DBNull.Value });
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Cập nhật thông tin người dùng đầy đủ (NGUOIDUNG) - bao gồm MAPB, MACV, NGAYSINH
        /// </summary>
        public async Task UpdateUserProfileFullAsync(string matk, string? hovaten = null, string? email = null, 
            string? sdt = null, string? diachi = null, string? bio = null, string? avatarUrl = null,
            DateTime? ngaysinh = null, string? mapb = null, string? macv = null)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = @"
                MERGE INTO NGUOIDUNG n
                USING (SELECT :p_matk AS MATK FROM DUAL) src
                ON (n.MATK = src.MATK)
                WHEN MATCHED THEN UPDATE SET
                    HOVATEN = NVL(:p_hovaten, n.HOVATEN),
                    EMAIL = NVL(:p_email, n.EMAIL),
                    SDT = NVL(:p_sdt, n.SDT),
                    DIACHI = NVL(:p_diachi, n.DIACHI),
                    BIO = NVL(:p_bio, n.BIO),
                    AVATAR_URL = NVL(:p_avatar, n.AVATAR_URL),
                    NGAYSINH = NVL(:p_ngaysinh, n.NGAYSINH),
                    MAPB = NVL(:p_mapb, n.MAPB),
                    MACV = NVL(:p_macv, n.MACV),
                    NGAYCAPNHAT = SYSTIMESTAMP
                WHEN NOT MATCHED THEN INSERT (MATK, HOVATEN, EMAIL, SDT, DIACHI, BIO, AVATAR_URL, NGAYSINH, MAPB, MACV, NGAYCAPNHAT)
                VALUES (:p_matk, :p_hovaten, :p_email, :p_sdt, :p_diachi, :p_bio, :p_avatar, :p_ngaysinh, :p_mapb, :p_macv, SYSTIMESTAMP)";
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });
            cmd.Parameters.Add(new OracleParameter("p_hovaten", OracleDbType.Varchar2) { Value = (object?)hovaten ?? DBNull.Value });
            cmd.Parameters.Add(new OracleParameter("p_email", OracleDbType.Varchar2) { Value = (object?)email ?? DBNull.Value });
            cmd.Parameters.Add(new OracleParameter("p_sdt", OracleDbType.Varchar2) { Value = (object?)sdt ?? DBNull.Value });
            cmd.Parameters.Add(new OracleParameter("p_diachi", OracleDbType.Varchar2) { Value = (object?)diachi ?? DBNull.Value });
            cmd.Parameters.Add(new OracleParameter("p_bio", OracleDbType.Varchar2) { Value = (object?)bio ?? DBNull.Value });
            cmd.Parameters.Add(new OracleParameter("p_avatar", OracleDbType.Varchar2) { Value = (object?)avatarUrl ?? DBNull.Value });
            cmd.Parameters.Add(new OracleParameter("p_ngaysinh", OracleDbType.Date) { Value = (object?)ngaysinh ?? DBNull.Value });
            cmd.Parameters.Add(new OracleParameter("p_mapb", OracleDbType.Varchar2) { Value = (object?)mapb ?? DBNull.Value });
            cmd.Parameters.Add(new OracleParameter("p_macv", OracleDbType.Varchar2) { Value = (object?)macv ?? DBNull.Value });
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Lấy thông tin chi tiết người dùng đầy đủ (TAIKHOAN + NGUOIDUNG)
        /// </summary>
        public async Task<UserDetailsFull?> GetUserDetailsFullAsync(string usernameOrMatk)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = @"
                SELECT tk.MATK, tk.TENTK, NVL(tk.MAVAITRO, ''), tk.CLEARANCELEVEL, 
                       tk.IS_BANNED_GLOBAL, NVL(tk.IS_OTP_VERIFIED, 0), tk.NGAYTAO,
                       n.MAPB, n.MACV, n.HOVATEN, n.EMAIL, n.SDT, n.NGAYSINH, 
                       n.DIACHI, n.AVATAR_URL, n.BIO
                FROM TAIKHOAN tk
                LEFT JOIN NGUOIDUNG n ON tk.MATK = n.MATK
                WHERE tk.MATK = :p_value OR tk.TENTK = :p_value";
            cmd.Parameters.Add(new OracleParameter("p_value", OracleDbType.Varchar2) { Value = usernameOrMatk });
            
            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return new UserDetailsFull
            {
                Matk = reader.GetString(0),
                Username = reader.GetString(1),
                Mavaitro = reader.GetString(2),
                ClearanceLevel = reader.GetInt32(3),
                IsBannedGlobal = reader.GetInt32(4) == 1,
                IsOtpVerified = reader.GetInt32(5) == 1,
                NgayTao = reader.GetDateTime(6),
                Mapb = reader.IsDBNull(7) ? null : reader.GetString(7),
                Macv = reader.IsDBNull(8) ? null : reader.GetString(8),
                Hovaten = reader.IsDBNull(9) ? null : reader.GetString(9),
                Email = reader.IsDBNull(10) ? null : reader.GetString(10),
                Sdt = reader.IsDBNull(11) ? null : reader.GetString(11),
                Ngaysinh = reader.IsDBNull(12) ? null : reader.GetDateTime(12),
                Diachi = reader.IsDBNull(13) ? null : reader.GetString(13),
                AvatarUrl = reader.IsDBNull(14) ? null : reader.GetString(14),
                Bio = reader.IsDBNull(15) ? null : reader.GetString(15)
            };
        }

        // === ENCRYPTION KEY MANAGEMENT ===
        
        /// <summary>
        /// Lưu encryption key vào bảng ENCRYPTION_KEYS
        /// </summary>
        public async Task StoreEncryptionKeyAsync(string? matk, string keyType, string keyValue, 
            string? description = null, DateTime? expiresAt = null)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO ENCRYPTION_KEYS (MATK, KEY_TYPE, KEY_VALUE, EXPIRES_AT, IS_ACTIVE)
                VALUES (:p_matk, :p_key_type, :p_key_value, :p_expires, 1)";
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = (object?)matk ?? DBNull.Value });
            cmd.Parameters.Add(new OracleParameter("p_key_type", OracleDbType.Varchar2) { Value = keyType });
            cmd.Parameters.Add(new OracleParameter("p_key_value", OracleDbType.Clob) { Value = keyValue });
            cmd.Parameters.Add(new OracleParameter("p_expires", OracleDbType.TimeStamp) { Value = (object?)expiresAt ?? DBNull.Value });
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Lấy encryption key từ bảng ENCRYPTION_KEYS
        /// </summary>
        public async Task<string?> GetEncryptionKeyAsync(string keyType, string? matk = null)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = @"
                SELECT KEY_VALUE FROM ENCRYPTION_KEYS 
                WHERE KEY_TYPE = :p_key_type 
                  AND IS_ACTIVE = 1
                  AND (EXPIRES_AT IS NULL OR EXPIRES_AT > SYSTIMESTAMP)
                  AND (:p_matk IS NULL OR MATK = :p_matk)
                ORDER BY CREATED_AT DESC
                FETCH FIRST 1 ROW ONLY";
            cmd.Parameters.Add(new OracleParameter("p_key_type", OracleDbType.Varchar2) { Value = keyType });
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = (object?)matk ?? DBNull.Value });
            
            var result = await cmd.ExecuteScalarAsync();
            return result == null || result == DBNull.Value ? null : result.ToString();
        }

        // === CONVERSATION MANAGEMENT ===
        public async Task<string> CreateConversationAsync(string mactc, string maloaictc, string tenctc,
            string nguoiql, string isPrivate, string createdBy)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = "BEGIN SP_TAO_CUOCTROCHUYEN(:p_mactc, :p_maloaictc, :p_tenctc, :p_nguoiql, :p_is_private, :p_created_by); END;";
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.Add(new OracleParameter("p_mactc", OracleDbType.Varchar2) { Value = mactc });
            cmd.Parameters.Add(new OracleParameter("p_maloaictc", OracleDbType.Varchar2) { Value = maloaictc });
            cmd.Parameters.Add(new OracleParameter("p_tenctc", OracleDbType.Varchar2) { Value = tenctc });
            cmd.Parameters.Add(new OracleParameter("p_nguoiql", OracleDbType.Varchar2) { Value = nguoiql });
            cmd.Parameters.Add(new OracleParameter("p_is_private", OracleDbType.Varchar2) { Value = isPrivate });
            cmd.Parameters.Add(new OracleParameter("p_created_by", OracleDbType.Varchar2) { Value = createdBy });
            await cmd.ExecuteNonQueryAsync();
            return mactc;
        }

        public async Task AddMemberAsync(string mactc, string usernameOrMatk, string quyen = "member", string maphanquyen = "MEMBER")
        {
            // Hỗ trợ cả MATK (TK001) và TENTK (giamdoc, nguoidung1, ...) để thân thiện với ChatClient
            string resolvedMatk;
            using (var lookupCmd = Connection.CreateCommand())
            {
                lookupCmd.CommandText = @"
                    SELECT MATK
                    FROM TAIKHOAN
                    WHERE MATK = :p_value OR TENTK = :p_value";
                lookupCmd.Parameters.Add(new OracleParameter("p_value", OracleDbType.Varchar2) { Value = usernameOrMatk });

                var result = await lookupCmd.ExecuteScalarAsync();
                if (result == null || result == DBNull.Value)
                {
                    throw new Exception($"Tài khoản '{usernameOrMatk}' không tồn tại trong hệ thống.");
                }

                resolvedMatk = Convert.ToString(result) ?? usernameOrMatk;
            }

            using (var cmd = Connection.CreateCommand())
            {
                cmd.CommandText = "BEGIN SP_THEM_THANHVIEN(:p_mactc, :p_matk, :p_quyen, :p_maphanquyen); END;";
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.Add(new OracleParameter("p_mactc", OracleDbType.Varchar2) { Value = mactc });
                cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = resolvedMatk });
                cmd.Parameters.Add(new OracleParameter("p_quyen", OracleDbType.Varchar2) { Value = quyen });
                cmd.Parameters.Add(new OracleParameter("p_maphanquyen", OracleDbType.Varchar2) { Value = maphanquyen });
                await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task RemoveMemberAsync(string mactc, string usernameOrMatk)
        {
            // Hỗ trợ cả MATK (TK001) và TENTK (giamdoc, nguoidung1, ...) để thân thiện với ChatClient
            string resolvedMatk;
            using (var lookupCmd = Connection.CreateCommand())
            {
                lookupCmd.CommandText = @"
                    SELECT MATK
                    FROM TAIKHOAN
                    WHERE MATK = :p_value OR TENTK = :p_value";
                lookupCmd.Parameters.Add(new OracleParameter("p_value", OracleDbType.Varchar2) { Value = usernameOrMatk });

                var result = await lookupCmd.ExecuteScalarAsync();
                if (result == null || result == DBNull.Value)
                {
                    throw new Exception($"Tài khoản '{usernameOrMatk}' không tồn tại trong hệ thống.");
                }

                resolvedMatk = Convert.ToString(result) ?? usernameOrMatk;
            }

            using var cmd = Connection.CreateCommand();
            cmd.CommandText = "BEGIN SP_XOA_THANHVIEN(:p_mactc, :p_matk); END;";
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.Add(new OracleParameter("p_mactc", OracleDbType.Varchar2) { Value = mactc });
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = resolvedMatk });
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task SetTruongNhomAsync(string mactc, string usernameOrMatk)
        {
            // Hỗ trợ cả MATK (TK001) và TENTK (giamdoc, nguoidung1, ...) để thân thiện với ChatClient
            string resolvedMatk;
            using (var lookupCmd = Connection.CreateCommand())
            {
                lookupCmd.CommandText = @"
                    SELECT MATK
                    FROM TAIKHOAN
                    WHERE MATK = :p_value OR TENTK = :p_value";
                lookupCmd.Parameters.Add(new OracleParameter("p_value", OracleDbType.Varchar2) { Value = usernameOrMatk });

                var result = await lookupCmd.ExecuteScalarAsync();
                if (result == null || result == DBNull.Value)
                {
                    throw new Exception($"Tài khoản '{usernameOrMatk}' không tồn tại trong hệ thống.");
                }

                resolvedMatk = Convert.ToString(result) ?? usernameOrMatk;
            }

            using var cmd = Connection.CreateCommand();
            cmd.CommandText = "BEGIN SP_SET_TRUONGNHOM(:p_mactc, :p_matk); END;";
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.Add(new OracleParameter("p_mactc", OracleDbType.Varchar2) { Value = mactc });
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = resolvedMatk });
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteConversationAsync(string mactc)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = "BEGIN SP_XOA_CUOCTROCHUYEN(:p_mactc); END;";
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.Add(new OracleParameter("p_mactc", OracleDbType.Varchar2) { Value = mactc });
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<ConversationInfo>> GetUserConversationsAsync(string matkOrUsername)
        {
            // Resolve username to MATK if needed
            var matk = await ResolveToMatkAsync(matkOrUsername);
            
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = @"
                SELECT DISTINCT c.MACTC, c.TENCTC, NVL(c.MALOAICTC, 'GROUP'), c.IS_PRIVATE, 
                       NVL(c.NGUOIQL, ''), NVL(c.CREATED_BY, ''), c.NGAYTAO,
                       NVL(c.MIN_CLEARANCE, 1), NVL(c.IS_ENCRYPTED, 0), NVL(c.IS_ARCHIVED, 0),
                       c.THOIGIANTINNHANCUOI,
                       (SELECT COUNT(*) FROM THANHVIEN tv2 WHERE tv2.MACTC = c.MACTC AND tv2.DELETED_BY_MEMBER = 0) AS MEMBER_COUNT,
                       (SELECT COUNT(*) FROM TINNHAN t WHERE t.MACTC = c.MACTC) AS MESSAGE_COUNT
                FROM CUOCTROCHUYEN c
                JOIN THANHVIEN tv ON c.MACTC = tv.MACTC
                WHERE tv.MATK = :p_matk AND tv.DELETED_BY_MEMBER = 0 AND NVL(c.IS_ARCHIVED, 0) = 0
                ORDER BY NVL(c.THOIGIANTINNHANCUOI, c.NGAYTAO) DESC";
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });

            var result = new List<ConversationInfo>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new ConversationInfo
                {
                    Mactc = reader.GetString(0),
                    Tenctc = reader.GetString(1),
                    Maloaictc = reader.GetString(2),
                    IsPrivate = reader.GetString(3) == "Y",
                    Nguoiql = reader.GetString(4),
                    CreatedBy = reader.GetString(5),
                    NgayTao = reader.GetDateTime(6),
                    MinClearance = reader.GetInt32(7),
                    IsEncrypted = reader.GetInt32(8) == 1,
                    IsArchived = reader.GetInt32(9) == 1,
                    ThoigianTinnhanCuoi = reader.IsDBNull(10) ? null : reader.GetDateTime(10),
                    MemberCount = reader.GetInt32(11),
                    MessageCount = reader.GetInt32(12)
                });
            }
            return result;
        }

        public async Task<List<ChatMessageRecord>> GetConversationMessagesAsync(string mactc, int limit = 100, string? userMatk = null)
        {
            // Set MAC context if userMatk provided to allow VPD policy to work
            if (!string.IsNullOrEmpty(userMatk))
            {
                try
                {
                    using var ctxCmd = Connection.CreateCommand();
                    ctxCmd.CommandText = "BEGIN SET_MAC_CONTEXT(:p_matk); END;";
                    ctxCmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = userMatk });
                    await ctxCmd.ExecuteNonQueryAsync();
                }
                catch
                {
                    // If SET_MAC_CONTEXT fails, continue anyway (context might not exist)
                }
            }

            using var cmd = Connection.CreateCommand();
            cmd.CommandText = @"
                SELECT * FROM (
                    SELECT t.MATN, t.MACTC, t.MATK, tk.TENTK, t.NOIDUNG, t.NGAYGUI,
                           NVL(t.MALOAITN, 'TEXT') AS MALOAITN, NVL(t.MATRANGTHAI, 'ACTIVE') AS MATRANGTHAI,
                           NVL(t.IS_PINNED, 0) AS IS_PINNED, t.EDITED_AT,
                           t.SECURITYLABEL, NVL(t.IS_ENCRYPTED, 0) AS IS_ENCRYPTED, 
                           NVL(t.ENCRYPTION_TYPE, 'NONE') AS ENCRYPTION_TYPE,
                           t.ENCRYPTED_CONTENT, t.ENCRYPTED_KEY, t.ENCRYPTION_IV, t.SIGNATURE
                    FROM TINNHAN t
                    JOIN TAIKHOAN tk ON t.MATK = tk.MATK
                    WHERE t.MACTC = :p_mactc
                    ORDER BY t.NGAYGUI DESC
                ) WHERE ROWNUM <= :p_limit
                ORDER BY NGAYGUI ASC";
            cmd.Parameters.Add(new OracleParameter("p_mactc", OracleDbType.Varchar2) { Value = mactc });
            cmd.Parameters.Add(new OracleParameter("p_limit", OracleDbType.Int32) { Value = limit });

            var result = new List<ChatMessageRecord>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new ChatMessageRecord
                {
                    MessageId = reader.GetInt32(0),
                    ConversationId = reader.GetString(1),
                    SenderMatk = reader.GetString(2),
                    SenderUsername = reader.GetString(3),
                    Content = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                    Timestamp = reader.GetDateTime(5),
                    MessageType = reader.GetString(6),
                    Status = reader.GetString(7),
                    IsPinned = reader.GetInt32(8) == 1,
                    EditedAt = reader.IsDBNull(9) ? null : reader.GetDateTime(9),
                    SecurityLabel = reader.GetInt32(10),
                    IsEncrypted = reader.GetInt32(11) == 1,
                    EncryptionType = reader.GetString(12),
                    EncryptedContent = reader.IsDBNull(13) ? null : (byte[])reader.GetValue(13),
                    EncryptedKey = reader.IsDBNull(14) ? null : reader.GetString(14),
                    EncryptionIv = reader.IsDBNull(15) ? null : reader.GetString(15),
                    Signature = reader.IsDBNull(16) ? null : reader.GetString(16)
                });
            }
            return result;
        }

        public async Task<int> SendMessageToConversationAsync(string mactc, string matk, string content, int securityLabel)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = "BEGIN SP_GUI_TINNHAN(:p_mactc, :p_matk, :p_noidung, :p_securitylabel, :p_matn); END;";
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.Add(new OracleParameter("p_mactc", OracleDbType.Varchar2) { Value = mactc });
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });
            cmd.Parameters.Add(new OracleParameter("p_noidung", OracleDbType.Clob) { Value = content });
            cmd.Parameters.Add(new OracleParameter("p_securitylabel", OracleDbType.Int32) { Value = securityLabel });
            var outParam = new OracleParameter("p_matn", OracleDbType.Int32) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(outParam);
            await cmd.ExecuteNonQueryAsync();
            // Xử lý đúng kiểu OracleDecimal từ OUT parameter
            if (outParam.Value is OracleDecimal oracleDecimal)
            {
                return oracleDecimal.ToInt32();
            }
            return Convert.ToInt32(outParam.Value);
        }

        // ========== ATTACHMENT METHODS ==========

        public async Task<int> UploadAttachmentAsync(string matk, string fileName, string mimeType, long fileSize, byte[] data, int isEncrypted = 0)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = "BEGIN SP_UPLOAD_ATTACHMENT(:p_matk, :p_filename, :p_mimetype, :p_filesize, :p_filedata, :p_attach_id, :p_is_encrypted); END;";
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });
            cmd.Parameters.Add(new OracleParameter("p_filename", OracleDbType.Varchar2) { Value = fileName });
            cmd.Parameters.Add(new OracleParameter("p_mimetype", OracleDbType.Varchar2) { Value = mimeType });
            cmd.Parameters.Add(new OracleParameter("p_filesize", OracleDbType.Decimal) { Value = fileSize });
            cmd.Parameters.Add(new OracleParameter("p_filedata", OracleDbType.Blob) { Value = data });
            
            // Add output parameter BEFORE p_is_encrypted to match SP signature
            var outParam = new OracleParameter("p_attach_id", OracleDbType.Decimal) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(outParam);
            cmd.Parameters.Add(new OracleParameter("p_is_encrypted", OracleDbType.Decimal) { Value = isEncrypted });
            
            await cmd.ExecuteNonQueryAsync();
            
            // Handle output - check for DBNull
            if (outParam.Value == null || outParam.Value == DBNull.Value)
            {
                throw new Exception("Failed to get attachment ID from database");
            }
            
            if (outParam.Value is OracleDecimal attachDec)
            {
                return attachDec.ToInt32();
            }
            return Convert.ToInt32(outParam.Value);
        }


        public async Task<int> SendMessageWithAttachmentAsync(string mactc, string matk, string content, int securityLabel, int attachmentId)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = "BEGIN SP_GUI_TINNHAN_WITH_ATTACH(:p_mactc, :p_matk, :p_noidung, :p_securitylabel, :p_attach_id, :p_matn); END;";
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.Add(new OracleParameter("p_mactc", OracleDbType.Varchar2) { Value = mactc });
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });
            cmd.Parameters.Add(new OracleParameter("p_noidung", OracleDbType.Clob) { Value = content });
            cmd.Parameters.Add(new OracleParameter("p_securitylabel", OracleDbType.Int32) { Value = securityLabel });
            cmd.Parameters.Add(new OracleParameter("p_attach_id", OracleDbType.Int32) { Value = attachmentId });
            var outParam = new OracleParameter("p_matn", OracleDbType.Int32) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(outParam);
            await cmd.ExecuteNonQueryAsync();
            if (outParam.Value is OracleDecimal oracleDecimal)
            {
                return oracleDecimal.ToInt32();
            }
            return Convert.ToInt32(outParam.Value);
        }

        public async Task<(int AttachmentId, string FileName, string MimeType, long FileSize, byte[] Data)?> GetAttachmentByMessageIdAsync(int matn)
        {
            using var cmd = Connection.CreateCommand();
            // Lấy attachment với thông tin encryption
            cmd.CommandText = @"
                SELECT a.ATTACH_ID, a.FILENAME, a.MIMETYPE, a.FILESIZE, a.FILEDATA,
                       NVL(a.ENCRYPTION_KEY, '') AS ENCRYPTION_KEY, 
                       NVL(a.ENCRYPTION_IV, '') AS ENCRYPTION_IV,
                       NVL(a.IS_ENCRYPTED, 0) AS IS_ENCRYPTED
                FROM TINNHAN_ATTACH ta
                JOIN ATTACHMENT a ON ta.ATTACH_ID = a.ATTACH_ID
                WHERE ta.MATN = :p_matn";
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.Add(new OracleParameter("p_matn", OracleDbType.Decimal) { Value = matn });

            using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess);
            if (!await reader.ReadAsync())
            {
                return null;
            }

            var attachmentId = reader.GetInt32(0);
            var fileName = reader.GetString(1);
            var mimeType = reader.IsDBNull(2) ? "application/octet-stream" : reader.GetString(2);
            var fileSize = reader.IsDBNull(3) ? 0L : reader.GetInt64(3);

            // Đọc encrypted data trước
            byte[] encryptedData;
            if (reader.IsDBNull(4))
            {
                encryptedData = Array.Empty<byte>();
            }
            else
            {
                // Đọc toàn bộ BLOB
                using var blobStream = reader.GetStream(4);
                using var ms = new System.IO.MemoryStream();
                blobStream.CopyTo(ms);
                encryptedData = ms.ToArray();
            }

            // Lấy encryption info (cần đọc theo thứ tự sau khi đọc BLOB)
            var encryptionKey = "";
            var encryptionIv = "";
            var isEncrypted = 0;
            
            try
            {
                // Đọc lại để lấy encryption info
                cmd.Parameters.Clear();
                cmd.CommandText = @"
                    SELECT NVL(a.ENCRYPTION_KEY, ''), NVL(a.ENCRYPTION_IV, ''), NVL(a.IS_ENCRYPTED, 0)
                    FROM TINNHAN_ATTACH ta
                    JOIN ATTACHMENT a ON ta.ATTACH_ID = a.ATTACH_ID
                    WHERE ta.MATN = :p_matn";
                cmd.Parameters.Add(new OracleParameter("p_matn", OracleDbType.Decimal) { Value = matn });
                
                using var reader2 = await cmd.ExecuteReaderAsync();
                if (await reader2.ReadAsync())
                {
                    encryptionKey = reader2.IsDBNull(0) ? "" : reader2.GetString(0);
                    encryptionIv = reader2.IsDBNull(1) ? "" : reader2.GetString(1);
                    isEncrypted = reader2.IsDBNull(2) ? 0 : reader2.GetInt32(2);
                }
            }
            catch { /* Ignore if columns don't exist */ }

            // Decrypt nếu cần
            byte[] data;
            if (isEncrypted == 1 && encryptedData.Length > 0)
            {
                try
                {
                    // Hybrid decrypt: giải mã package (data|key|iv)
                    var encryptedPackage = System.Text.Encoding.ASCII.GetString(encryptedData);
                    data = Utils.EncryptionHelper.HybridDecrypt(encryptedPackage);
                }
                catch
                {
                    // Nếu decrypt lỗi, trả về data gốc
                    data = encryptedData;
                }
            }
            else
            {
                data = encryptedData;
            }

            return (attachmentId, fileName, mimeType, fileSize, data);
        }

        public async Task<MemberPermission?> GetMemberPermissionAsync(string mactc, string usernameOrMatk)
        {
            // Hỗ trợ cả MATK (TK001) và TENTK (giamdoc, nguoidung1, ...) để thân thiện với ChatClient
            string resolvedMatk;
            using (var lookupCmd = Connection.CreateCommand())
            {
                lookupCmd.CommandText = @"
                    SELECT MATK
                    FROM TAIKHOAN
                    WHERE MATK = :p_value OR TENTK = :p_value";
                lookupCmd.Parameters.Add(new OracleParameter("p_value", OracleDbType.Varchar2) { Value = usernameOrMatk });

                var result = await lookupCmd.ExecuteScalarAsync();
                if (result == null || result == DBNull.Value)
                {
                    return null;
                }

                resolvedMatk = Convert.ToString(result) ?? usernameOrMatk;
            }

            using var cmd = Connection.CreateCommand();
            cmd.CommandText = @"
                SELECT tv.QUYEN, tv.MAPHANQUYEN, NVL(tv.IS_BANNED, 0), NVL(tv.IS_MUTED, 0),
                       NVL(pq.CAN_ADD, 0), NVL(pq.CAN_REMOVE, 0), NVL(pq.CAN_DELETE, 0), 
                       NVL(pq.CAN_BAN, 0), NVL(pq.CAN_MUTE, 0), NVL(pq.CAN_PROMOTE, 0)
                FROM THANHVIEN tv
                LEFT JOIN PHAN_QUYEN_NHOM pq ON tv.MAPHANQUYEN = pq.MAPHANQUYEN
                WHERE tv.MACTC = :p_mactc AND tv.MATK = :p_matk AND NVL(tv.DELETED_BY_MEMBER, 0) = 0";
            cmd.Parameters.Add(new OracleParameter("p_mactc", OracleDbType.Varchar2) { Value = mactc });
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = resolvedMatk });

            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }

            var quyen = reader.GetString(0);
            var isOwner = quyen.ToLower() == "owner";
            
            // Owner luôn có tất cả quyền, các role khác đọc từ PHAN_QUYEN_NHOM
            var canAddFromDb = reader.IsDBNull(4) ? false : reader.GetInt32(4) == 1;
            var canRemoveFromDb = reader.IsDBNull(5) ? false : reader.GetInt32(5) == 1;
            var canDeleteFromDb = reader.IsDBNull(6) ? false : reader.GetInt32(6) == 1;
            var canBanFromDb = reader.IsDBNull(7) ? false : reader.GetInt32(7) == 1;
            var canMuteFromDb = reader.IsDBNull(8) ? false : reader.GetInt32(8) == 1;
            var canPromoteFromDb = reader.IsDBNull(9) ? false : reader.GetInt32(9) == 1;

            return new MemberPermission
            {
                Quyen = quyen,
                IsBanned = reader.GetInt32(2) == 1,
                IsMuted = reader.GetInt32(3) == 1,
                CanAdd = isOwner || canAddFromDb,
                CanRemove = isOwner || canRemoveFromDb,
                CanDelete = isOwner || canDeleteFromDb,
                CanBan = isOwner || canBanFromDb,
                CanMute = isOwner || canMuteFromDb,
                CanPromote = isOwner || canPromoteFromDb
            };
        }

        public async Task BanMemberAsync(string mactc, string usernameOrMatk)
        {
            // Resolve to MATK if a username was provided
            string resolvedMatk;
            using (var lookupCmd = Connection.CreateCommand())
            {
                lookupCmd.CommandText = @"
                    SELECT MATK
                    FROM TAIKHOAN
                    WHERE MATK = :p_value OR TENTK = :p_value";
                lookupCmd.Parameters.Add(new OracleParameter("p_value", OracleDbType.Varchar2) { Value = usernameOrMatk });

                var result = await lookupCmd.ExecuteScalarAsync();
                if (result == null || result == DBNull.Value)
                {
                    throw new Exception($"Tài khoản '{usernameOrMatk}' không tồn tại trong hệ thống.");
                }

                resolvedMatk = Convert.ToString(result) ?? usernameOrMatk;
            }

            using var cmd = Connection.CreateCommand();
            cmd.CommandText = "BEGIN SP_BAN_MEMBER(:p_mactc, :p_matk); END;";
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.Add(new OracleParameter("p_mactc", OracleDbType.Varchar2) { Value = mactc });
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = resolvedMatk });
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UnbanMemberAsync(string mactc, string usernameOrMatk)
        {
            string resolvedMatk;
            using (var lookupCmd = Connection.CreateCommand())
            {
                lookupCmd.CommandText = @"
                    SELECT MATK
                    FROM TAIKHOAN
                    WHERE MATK = :p_value OR TENTK = :p_value";
                lookupCmd.Parameters.Add(new OracleParameter("p_value", OracleDbType.Varchar2) { Value = usernameOrMatk });

                var result = await lookupCmd.ExecuteScalarAsync();
                if (result == null || result == DBNull.Value)
                {
                    throw new Exception($"Tài khoản '{usernameOrMatk}' không tồn tại trong hệ thống.");
                }

                resolvedMatk = Convert.ToString(result) ?? usernameOrMatk;
            }

            using var cmd = Connection.CreateCommand();
            cmd.CommandText = "BEGIN SP_UNBAN_MEMBER(:p_mactc, :p_matk); END;";
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.Add(new OracleParameter("p_mactc", OracleDbType.Varchar2) { Value = mactc });
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = resolvedMatk });
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task MuteMemberAsync(string mactc, string usernameOrMatk)
        {
            string resolvedMatk;
            using (var lookupCmd = Connection.CreateCommand())
            {
                lookupCmd.CommandText = @"
                    SELECT MATK
                    FROM TAIKHOAN
                    WHERE MATK = :p_value OR TENTK = :p_value";
                lookupCmd.Parameters.Add(new OracleParameter("p_value", OracleDbType.Varchar2) { Value = usernameOrMatk });

                var result = await lookupCmd.ExecuteScalarAsync();
                if (result == null || result == DBNull.Value)
                {
                    throw new Exception($"Tài khoản '{usernameOrMatk}' không tồn tại trong hệ thống.");
                }

                resolvedMatk = Convert.ToString(result) ?? usernameOrMatk;
            }

            using var cmd = Connection.CreateCommand();
            cmd.CommandText = "BEGIN SP_MUTE_MEMBER(:p_mactc, :p_matk); END;";
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.Add(new OracleParameter("p_mactc", OracleDbType.Varchar2) { Value = mactc });
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = resolvedMatk });
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UnmuteMemberAsync(string mactc, string usernameOrMatk)
        {
            string resolvedMatk;
            using (var lookupCmd = Connection.CreateCommand())
            {
                lookupCmd.CommandText = @"
                    SELECT MATK
                    FROM TAIKHOAN
                    WHERE MATK = :p_value OR TENTK = :p_value";
                lookupCmd.Parameters.Add(new OracleParameter("p_value", OracleDbType.Varchar2) { Value = usernameOrMatk });

                var result = await lookupCmd.ExecuteScalarAsync();
                if (result == null || result == DBNull.Value)
                {
                    throw new Exception($"Tài khoản '{usernameOrMatk}' không tồn tại trong hệ thống.");
                }

                resolvedMatk = Convert.ToString(result) ?? usernameOrMatk;
            }

            using var cmd = Connection.CreateCommand();
            cmd.CommandText = "BEGIN SP_UNMUTE_MEMBER(:p_mactc, :p_matk); END;";
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.Add(new OracleParameter("p_mactc", OracleDbType.Varchar2) { Value = mactc });
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = resolvedMatk });
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<MemberInfo>> GetConversationMembersAsync(string mactc)
        {
            // Use direct query instead of stored procedure with cursor for simplicity
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = @"
                SELECT tv.MATK, tk.TENTK, tv.QUYEN, tv.MAPHANQUYEN,
                       tv.IS_BANNED, tv.IS_MUTED, tv.NGAYTHAMGIA,
                       n.HOVATEN, n.EMAIL
                FROM THANHVIEN tv
                JOIN TAIKHOAN tk ON tv.MATK = tk.MATK
                LEFT JOIN NGUOIDUNG n ON tv.MATK = n.MATK
                WHERE tv.MACTC = :p_mactc AND tv.DELETED_BY_MEMBER = 0
                ORDER BY tv.NGAYTHAMGIA";
            cmd.Parameters.Add(new OracleParameter("p_mactc", OracleDbType.Varchar2) { Value = mactc });

            var result = new List<MemberInfo>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new MemberInfo
                {
                    Matk = reader.GetString(0),
                    Username = reader.GetString(1),
                    Role = reader.GetString(2),
                    Maphanquyen = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    IsBanned = reader.GetInt32(4) == 1,
                    IsMuted = reader.GetInt32(5) == 1,
                    NgayThamGia = reader.GetDateTime(6),
                    Hovaten = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                    Email = reader.IsDBNull(8) ? string.Empty : reader.GetString(8)
                });
            }
            return result;
        }

        // ========== USER LIST FOR CHAT ==========
        
        public async Task<AdminUserInfo?> GetUserProfileAsync(string matk)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = @"
                SELECT tk.MATK, tk.TENTK, 
                       NVL(n.EMAIL, ''), NVL(n.HOVATEN, ''), NVL(n.SDT, ''),
                       tk.CLEARANCELEVEL, tk.IS_BANNED_GLOBAL, NVL(tk.MAVAITRO, ''),
                       tk.NGAYTAO,
                       CASE WHEN tk.IS_OTP_VERIFIED = 1 
                            OR EXISTS (SELECT 1 FROM XACTHUCOTP x WHERE x.MATK = tk.MATK AND x.DAXACMINH = 1) 
                            THEN 1 ELSE 0 END AS IS_OTP_VERIFIED
                FROM TAIKHOAN tk
                LEFT JOIN NGUOIDUNG n ON tk.MATK = n.MATK
                WHERE tk.MATK = :p_matk OR tk.TENTK = :p_matk";
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });
            
            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return new AdminUserInfo
            {
                Matk = reader.GetString(0),
                Username = reader.GetString(1),
                Email = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                Hovaten = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                Phone = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                ClearanceLevel = reader.GetInt32(5),
                IsBannedGlobal = reader.GetInt32(6) == 1,
                Mavaitro = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                NgayTao = reader.GetDateTime(8),
                IsOtpVerified = reader.GetInt32(9) == 1
            };
        }

        public async Task<List<string>> GetUsersForChatAsync(string excludeMatk)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = @"
                SELECT tk.TENTK 
                FROM TAIKHOAN tk
                WHERE tk.MATK != :p_exclude 
                  AND tk.TENTK != :p_exclude
                  AND tk.IS_BANNED_GLOBAL = 0
                  AND (tk.IS_OTP_VERIFIED = 1 
                       OR EXISTS (SELECT 1 FROM XACTHUCOTP x WHERE x.MATK = tk.MATK AND x.DAXACMINH = 1))
                ORDER BY tk.TENTK";
            cmd.Parameters.Add(new OracleParameter("p_exclude", OracleDbType.Varchar2) { Value = excludeMatk });
            
            var result = new List<string>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(reader.GetString(0));
            }
            return result;
        }

        // ========== ADMIN METHODS ==========

        public async Task<List<AdminUserInfo>> GetAllUsersAsync()
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = @"
                SELECT tk.MATK, tk.TENTK, 
                       NVL(n.EMAIL, ''), NVL(n.HOVATEN, ''), NVL(n.SDT, ''),
                       tk.CLEARANCELEVEL, tk.IS_BANNED_GLOBAL, NVL(tk.MAVAITRO, ''),
                       tk.NGAYTAO,
                       CASE WHEN EXISTS (SELECT 1 FROM XACTHUCOTP x WHERE x.MATK = tk.MATK AND x.DAXACMINH = 1) THEN 1 ELSE 0 END AS IS_OTP_VERIFIED,
                       NVL(tk.FAILED_LOGIN_ATTEMPTS, 0),
                       tk.LOCKED_UNTIL,
                       NVL(cv.TENCV, ''),
                       NVL(pb.TENPB, '')
                FROM TAIKHOAN tk
                LEFT JOIN NGUOIDUNG n ON tk.MATK = n.MATK
                LEFT JOIN CHUCVU cv ON n.MACV = cv.MACV
                LEFT JOIN PHONGBAN pb ON n.MAPB = pb.MAPB
                ORDER BY tk.NGAYTAO DESC";
            
            var result = new List<AdminUserInfo>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new AdminUserInfo
                {
                    Matk = reader.GetString(0),
                    Username = reader.GetString(1),
                    Email = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    Hovaten = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    Phone = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                    ClearanceLevel = reader.GetInt32(5),
                    IsBannedGlobal = reader.GetInt32(6) == 1,
                    Mavaitro = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                    NgayTao = reader.GetDateTime(8),
                    IsOtpVerified = reader.GetInt32(9) == 1,
                    FailedLoginAttempts = reader.GetInt32(10),
                    LockedUntil = reader.IsDBNull(11) ? null : reader.GetDateTime(11),
                    Chucvu = reader.IsDBNull(12) ? string.Empty : reader.GetString(12),
                    Phongban = reader.IsDBNull(13) ? string.Empty : reader.GetString(13)
                });
            }
            return result;
        }

        public async Task<AdminUserInfo?> GetUserDetailsAsync(string matkOrUsername)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = @"
                SELECT tk.MATK, tk.TENTK, 
                       NVL(n.EMAIL, ''), NVL(n.HOVATEN, ''), NVL(n.SDT, ''),
                       tk.CLEARANCELEVEL, tk.IS_BANNED_GLOBAL, NVL(tk.MAVAITRO, ''),
                       tk.NGAYTAO,
                       CASE WHEN EXISTS (SELECT 1 FROM XACTHUCOTP x WHERE x.MATK = tk.MATK AND x.DAXACMINH = 1) THEN 1 ELSE 0 END AS IS_OTP_VERIFIED
                FROM TAIKHOAN tk
                LEFT JOIN NGUOIDUNG n ON tk.MATK = n.MATK
                WHERE tk.MATK = :p_value OR tk.TENTK = :p_value";
            cmd.Parameters.Add(new OracleParameter("p_value", OracleDbType.Varchar2) { Value = matkOrUsername });
            
            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return new AdminUserInfo
            {
                Matk = reader.GetString(0),
                Username = reader.GetString(1),
                Email = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                Hovaten = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                Phone = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                ClearanceLevel = reader.GetInt32(5),
                IsBannedGlobal = reader.GetInt32(6) == 1,
                Mavaitro = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                NgayTao = reader.GetDateTime(8),
                IsOtpVerified = reader.GetInt32(9) == 1
            };
        }

        public async Task UpdateUserInfoAsync(string matkOrUsername, string? email, string? hovaten, string? phone, int? clearanceLevel, string? mavaitro, string? macv = null, string? mapb = null)
        {
            // Resolve MATK from username if needed
            var matk = await ResolveToMatkAsync(matkOrUsername);
            
            using var cmd = Connection.CreateCommand();
            // Sử dụng stored procedure SP_CAPNHAT_NGUOIDUNG_ADMIN
            cmd.CommandText = "BEGIN SP_CAPNHAT_NGUOIDUNG_ADMIN(:p_matk, :p_email, :p_hovaten, :p_sdt, :p_clearance, :p_mavaitro, :p_macv, :p_mapb); END;";
            cmd.CommandType = CommandType.Text;
            
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });
            cmd.Parameters.Add(new OracleParameter("p_email", OracleDbType.Varchar2) { Value = (object?)email ?? DBNull.Value });
            cmd.Parameters.Add(new OracleParameter("p_hovaten", OracleDbType.Varchar2) { Value = (object?)hovaten ?? DBNull.Value });
            cmd.Parameters.Add(new OracleParameter("p_sdt", OracleDbType.Varchar2) { Value = (object?)phone ?? DBNull.Value });
            // Dùng OracleDbType.Decimal cho NUMBER
            cmd.Parameters.Add(new OracleParameter("p_clearance", OracleDbType.Decimal) { Value = clearanceLevel.HasValue ? (object)clearanceLevel.Value : DBNull.Value });
            cmd.Parameters.Add(new OracleParameter("p_mavaitro", OracleDbType.Varchar2) { Value = (object?)mavaitro ?? DBNull.Value });
            cmd.Parameters.Add(new OracleParameter("p_macv", OracleDbType.Varchar2) { Value = (object?)macv ?? DBNull.Value });
            cmd.Parameters.Add(new OracleParameter("p_mapb", OracleDbType.Varchar2) { Value = (object?)mapb ?? DBNull.Value });
            
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task BanUserGlobalAsync(string matk)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = "BEGIN SP_BAN_USER_GLOBAL(:p_matk); END;";
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UnbanUserGlobalAsync(string matk)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = "BEGIN SP_UNBAN_USER_GLOBAL(:p_matk); END;";
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<AdminConversationInfo>> GetAllConversationsAsync()
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = @"
                SELECT c.MACTC, c.TENCTC, c.MALOAICTC, c.IS_PRIVATE, c.NGUOIQL, c.NGAYTAO,
                       (SELECT COUNT(*) FROM THANHVIEN tv WHERE tv.MACTC = c.MACTC AND tv.DELETED_BY_MEMBER = 0) AS MEMBER_COUNT,
                       (SELECT COUNT(*) FROM TINNHAN t WHERE t.MACTC = c.MACTC) AS MESSAGE_COUNT
                FROM CUOCTROCHUYEN c
                ORDER BY c.NGAYTAO DESC";
            
            var result = new List<AdminConversationInfo>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new AdminConversationInfo
                {
                    Mactc = reader.GetString(0),
                    Tenctc = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    Maloaictc = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    IsPrivate = !reader.IsDBNull(3) && reader.GetString(3) == "Y",
                    Nguoiql = reader.IsDBNull(4) ? "N/A" : reader.GetString(4),
                    NgayTao = reader.GetDateTime(5),
                    MemberCount = reader.IsDBNull(6) ? 0 : reader.GetInt32(6),
                    MessageCount = reader.IsDBNull(7) ? 0 : reader.GetInt32(7)
                });
            }
            return result;
        }

        public async Task<List<AdminMessageInfo>> GetConversationMessagesAdminAsync(string mactc, int limit = 100)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = @"
                SELECT t.MATN, t.MACTC, t.MATK, tk.TENTK, t.NOIDUNG, t.SECURITYLABEL, t.NGAYGUI, t.MALOAITN, t.MATRANGTHAI
                FROM TINNHAN t
                JOIN TAIKHOAN tk ON t.MATK = tk.MATK
                WHERE t.MACTC = :p_mactc
                ORDER BY t.NGAYGUI DESC
                FETCH FIRST :p_limit ROWS ONLY";
            cmd.Parameters.Add(new OracleParameter("p_mactc", OracleDbType.Varchar2) { Value = mactc });
            cmd.Parameters.Add(new OracleParameter("p_limit", OracleDbType.Int32) { Value = limit });
            
            var result = new List<AdminMessageInfo>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new AdminMessageInfo
                {
                    Matn = reader.GetInt32(0),
                    Mactc = reader.GetString(1),
                    Matk = reader.GetString(2),
                    Username = reader.GetString(3),
                    Noidung = reader.GetString(4),
                    SecurityLabel = reader.GetInt32(5),
                    Ngaygui = reader.GetDateTime(6),
                    Maloaitn = reader.GetString(7),
                    Matrangthai = reader.GetString(8)
                });
            }
            return result;
        }

        public async Task<ChatMessageRecord?> GetMessageByIdAsync(int matn)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = @"
                SELECT MATN, MACTC, MATK, NOIDUNG, SECURITYLABEL, NGAYGUI
                FROM TINNHAN
                WHERE MATN = :p_matn";
            cmd.Parameters.Add(new OracleParameter("p_matn", OracleDbType.Int32) { Value = matn });
            
            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }

            return new ChatMessageRecord
            {
                MessageId = reader.GetInt32(0),
                ConversationId = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                SenderMatk = reader.GetString(2),
                Content = reader.GetString(3),
                SecurityLabel = reader.GetInt32(4),
                Timestamp = reader.GetDateTime(5)
            };
        }

        public async Task DeleteMessageAsync(int matn)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = "BEGIN SP_XOA_TINNHAN(:p_matn); END;";
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.Add(new OracleParameter("p_matn", OracleDbType.Decimal) { Value = matn });
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<AuditLogInfo>> GetAuditLogsAsync(int limit = 100)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = @"
                SELECT LOG_ID, MATK, ACTION, TARGET, SECURITYLABEL, TIMESTAMP
                FROM AUDIT_LOGS
                ORDER BY TIMESTAMP DESC
                FETCH FIRST :p_limit ROWS ONLY";
            cmd.Parameters.Add(new OracleParameter("p_limit", OracleDbType.Int32) { Value = limit });
            
            var result = new List<AuditLogInfo>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new AuditLogInfo
                {
                    LogId = reader.GetInt32(0),
                    Matk = reader.GetString(1),
                    Action = reader.GetString(2),
                    Target = reader.GetString(3),
                    SecurityLabel = reader.GetInt32(4),
                    Timestamp = reader.GetDateTime(5)
                });
            }
            return result;
        }

        // ============================================================================
        // GROUP/CONVERSATION MANAGEMENT METHODS
        // ============================================================================

        /// <summary>
        /// Xóa chat riêng tư một phía - người còn lại vẫn thấy cuộc trò chuyện
        /// </summary>
        public async Task DeletePrivateChatOneSideAsync(string mactc, string usernameOrMatk)
        {
            var matk = await ResolveToMatkAsync(usernameOrMatk);
            
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = @"
                DECLARE
                  v_is_private VARCHAR2(1);
                BEGIN
                  SELECT IS_PRIVATE INTO v_is_private FROM CUOCTROCHUYEN WHERE MACTC = :p_mactc;
                  IF v_is_private != 'Y' THEN
                    RAISE_APPLICATION_ERROR(-20100, 'Chỉ có thể xóa một phía với chat riêng tư.');
                  END IF;
                  UPDATE THANHVIEN SET DELETED_BY_MEMBER = 1 WHERE MACTC = :p_mactc AND MATK = :p_matk;
                  INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET, SECURITYLABEL) VALUES(:p_matk, 'DELETE_PRIVATE_CHAT_ONESIDE', :p_mactc, 0);
                  COMMIT;
                END;";
            cmd.Parameters.Add(new OracleParameter("p_mactc", OracleDbType.Varchar2) { Value = mactc });
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Rời nhóm (member, không phải owner)
        /// </summary>
        public async Task LeaveGroupAsync(string mactc, string usernameOrMatk)
        {
            var matk = await ResolveToMatkAsync(usernameOrMatk);
            
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = @"
                DECLARE
                  v_quyen VARCHAR2(100);
                  v_is_private VARCHAR2(1);
                BEGIN
                  SELECT IS_PRIVATE INTO v_is_private FROM CUOCTROCHUYEN WHERE MACTC = :p_mactc;
                  IF v_is_private = 'Y' THEN
                    RAISE_APPLICATION_ERROR(-20101, 'Không thể rời chat riêng tư. Hãy sử dụng chức năng xóa cuộc trò chuyện.');
                  END IF;
                  SELECT QUYEN INTO v_quyen FROM THANHVIEN WHERE MACTC = :p_mactc AND MATK = :p_matk;
                  IF v_quyen = 'owner' THEN
                    RAISE_APPLICATION_ERROR(-20102, 'Chủ nhóm không thể rời nhóm. Hãy chuyển quyền chủ nhóm hoặc xóa nhóm.');
                  END IF;
                  DELETE FROM THANHVIEN WHERE MACTC = :p_mactc AND MATK = :p_matk;
                  INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET, SECURITYLABEL) VALUES(:p_matk, 'LEAVE_GROUP', :p_mactc, 0);
                  COMMIT;
                END;";
            cmd.Parameters.Add(new OracleParameter("p_mactc", OracleDbType.Varchar2) { Value = mactc });
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Xóa/Archive nhóm (chỉ owner) - tất cả thành viên không thể nhắn tin
        /// </summary>
        public async Task DeleteGroupAsync(string mactc, string usernameOrMatk)
        {
            var matk = await ResolveToMatkAsync(usernameOrMatk);
            
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = @"
                DECLARE
                  v_quyen VARCHAR2(100);
                  v_is_private VARCHAR2(1);
                BEGIN
                  SELECT IS_PRIVATE INTO v_is_private FROM CUOCTROCHUYEN WHERE MACTC = :p_mactc;
                  IF v_is_private = 'Y' THEN
                    RAISE_APPLICATION_ERROR(-20104, 'Không thể xóa chat riêng tư bằng chức năng này.');
                  END IF;
                  SELECT QUYEN INTO v_quyen FROM THANHVIEN WHERE MACTC = :p_mactc AND MATK = :p_matk;
                  IF v_quyen != 'owner' THEN
                    RAISE_APPLICATION_ERROR(-20105, 'Chỉ chủ nhóm mới có thể xóa nhóm.');
                  END IF;
                  UPDATE CUOCTROCHUYEN SET IS_ARCHIVED = 1, ARCHIVED_AT = SYSTIMESTAMP WHERE MACTC = :p_mactc;
                  INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET, SECURITYLABEL) VALUES(:p_matk, 'ARCHIVE_GROUP', :p_mactc, 0);
                  COMMIT;
                END;";
            cmd.Parameters.Add(new OracleParameter("p_mactc", OracleDbType.Varchar2) { Value = mactc });
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Xóa archive - member tự xóa archived group khỏi danh sách
        /// </summary>
        public async Task DeleteArchiveAsync(string mactc, string usernameOrMatk)
        {
            var matk = await ResolveToMatkAsync(usernameOrMatk);
            
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = @"
                DECLARE
                  v_is_archived NUMBER;
                BEGIN
                  SELECT NVL(IS_ARCHIVED, 0) INTO v_is_archived FROM CUOCTROCHUYEN WHERE MACTC = :p_mactc;
                  IF v_is_archived != 1 THEN
                    RAISE_APPLICATION_ERROR(-20107, 'Nhóm chưa được archive.');
                  END IF;
                  DELETE FROM THANHVIEN WHERE MACTC = :p_mactc AND MATK = :p_matk;
                  INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET, SECURITYLABEL) VALUES(:p_matk, 'DELETE_ARCHIVE', :p_mactc, 0);
                  COMMIT;
                END;";
            cmd.Parameters.Add(new OracleParameter("p_mactc", OracleDbType.Varchar2) { Value = mactc });
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Kiểm tra trạng thái cuộc trò chuyện
        /// </summary>
        public async Task<ConversationStatus> GetConversationStatusAsync(string mactc, string usernameOrMatk)
        {
            var matk = await ResolveToMatkAsync(usernameOrMatk);
            
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = @"
                SELECT c.IS_PRIVATE, NVL(c.IS_ARCHIVED, 0), NVL(tv.DELETED_BY_MEMBER, 0), c.NGUOIQL
                FROM CUOCTROCHUYEN c
                LEFT JOIN THANHVIEN tv ON c.MACTC = tv.MACTC AND tv.MATK = :p_matk
                WHERE c.MACTC = :p_mactc";
            cmd.Parameters.Add(new OracleParameter("p_mactc", OracleDbType.Varchar2) { Value = mactc });
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });
            
            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return new ConversationStatus { Status = "NOT_FOUND" };
            }
            
            var isPrivate = reader.GetString(0) == "Y";
            var isArchived = reader.GetInt32(1) == 1;
            var deletedByMember = reader.GetInt32(2) == 1;
            var nguoiql = reader.IsDBNull(3) ? "" : reader.GetString(3);
            
            string status;
            if (isArchived) status = "ARCHIVED";
            else if (isPrivate && deletedByMember) status = "DELETED_BY_ME";
            else status = "ACTIVE";
            
            return new ConversationStatus
            {
                Status = status,
                IsPrivate = isPrivate,
                IsArchived = isArchived,
                IsOwner = nguoiql == matk
            };
        }

        /// <summary>
        /// Helper: Resolve username to MATK
        /// </summary>
        private async Task<string> ResolveToMatkAsync(string usernameOrMatk)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = "SELECT MATK FROM TAIKHOAN WHERE MATK = :p OR TENTK = :p";
            cmd.Parameters.Add(new OracleParameter("p", OracleDbType.Varchar2) { Value = usernameOrMatk });
            var result = await cmd.ExecuteScalarAsync();
            if (result == null || result == DBNull.Value)
                throw new Exception($"Tài khoản '{usernameOrMatk}' không tồn tại.");
            return result.ToString()!;
        }

        // ========== ADMIN POLICY MANAGEMENT ==========

        /// <summary>
        /// Lấy danh sách tất cả policies từ bảng ADMIN_POLICY
        /// </summary>
        public async Task<List<AdminPolicyInfo>> GetAdminPoliciesAsync(string? policyType = null)
        {
            var policies = new List<AdminPolicyInfo>();
            
            try
            {
                using var cmd = Connection.CreateCommand();
                cmd.CommandText = @"
                    SELECT POLICY_ID, POLICY_NAME, POLICY_TYPE, TABLE_NAME, 
                           NVL(DESCRIPTION, '') AS DESCRIPTION,
                           NVL(POLICY_FUNCTION, '') AS POLICY_FUNCTION,
                           NVL(STATEMENT_TYPES, '') AS STATEMENT_TYPES,
                           NVL(IS_ENABLED, 0) AS IS_ENABLED,
                           NVL(CREATED_BY, '') AS CREATED_BY,
                           CREATED_AT, UPDATED_AT
                    FROM ADMIN_POLICY
                    WHERE (:p_type IS NULL OR POLICY_TYPE = :p_type)
                    ORDER BY POLICY_TYPE, POLICY_NAME";
                cmd.Parameters.Add(new OracleParameter("p_type", OracleDbType.Varchar2) 
                { 
                    Value = string.IsNullOrEmpty(policyType) ? (object)DBNull.Value : policyType 
                });

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    policies.Add(new AdminPolicyInfo
                    {
                        PolicyId = reader.GetInt32(0),
                        PolicyName = reader.GetString(1),
                        PolicyType = reader.GetString(2),
                        TableName = reader.GetString(3),
                        Description = reader.GetString(4),
                        PolicyFunction = reader.GetString(5),
                        StatementTypes = reader.GetString(6),
                        IsEnabled = reader.GetInt32(7) == 1,
                        CreatedBy = reader.GetString(8),
                        CreatedAt = reader.GetDateTime(9),
                        UpdatedAt = reader.IsDBNull(10) ? null : reader.GetDateTime(10)
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetAdminPoliciesAsync error: {ex.Message}");
                // Nếu bảng không tồn tại, trả về danh sách rỗng
            }
            
            return policies;
        }

        /// <summary>
        /// Thêm policy mới vào bảng ADMIN_POLICY
        /// </summary>
        public async Task<int> CreateAdminPolicyAsync(string policyName, string policyType, string tableName, 
            string description, string? policyFunction = null, string? statementTypes = null, string? createdBy = null)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO ADMIN_POLICY(POLICY_NAME, POLICY_TYPE, TABLE_NAME, DESCRIPTION, 
                                         POLICY_FUNCTION, STATEMENT_TYPES, IS_ENABLED, CREATED_BY, CREATED_AT)
                VALUES(:p_name, :p_type, :p_table, :p_desc, :p_func, :p_stmt, 1, :p_by, SYSTIMESTAMP)
                RETURNING POLICY_ID INTO :p_id";
            cmd.Parameters.Add(new OracleParameter("p_name", OracleDbType.Varchar2) { Value = policyName });
            cmd.Parameters.Add(new OracleParameter("p_type", OracleDbType.Varchar2) { Value = policyType });
            cmd.Parameters.Add(new OracleParameter("p_table", OracleDbType.Varchar2) { Value = tableName });
            cmd.Parameters.Add(new OracleParameter("p_desc", OracleDbType.Varchar2) { Value = description ?? (object)DBNull.Value });
            cmd.Parameters.Add(new OracleParameter("p_func", OracleDbType.Varchar2) { Value = policyFunction ?? (object)DBNull.Value });
            cmd.Parameters.Add(new OracleParameter("p_stmt", OracleDbType.Varchar2) { Value = statementTypes ?? (object)DBNull.Value });
            cmd.Parameters.Add(new OracleParameter("p_by", OracleDbType.Varchar2) { Value = createdBy ?? (object)DBNull.Value });
            var outParam = new OracleParameter("p_id", OracleDbType.Decimal) { Direction = System.Data.ParameterDirection.Output };
            cmd.Parameters.Add(outParam);

            await cmd.ExecuteNonQueryAsync();
            
            if (outParam.Value is OracleDecimal dec)
                return dec.ToInt32();
            return Convert.ToInt32(outParam.Value);
        }

        /// <summary>
        /// Cập nhật policy trong bảng ADMIN_POLICY
        /// </summary>
        public async Task UpdateAdminPolicyAsync(int policyId, string? description = null, 
            string? statementTypes = null, bool? isEnabled = null)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = @"
                UPDATE ADMIN_POLICY
                SET DESCRIPTION = NVL(:p_desc, DESCRIPTION),
                    STATEMENT_TYPES = NVL(:p_stmt, STATEMENT_TYPES),
                    IS_ENABLED = NVL(:p_enabled, IS_ENABLED),
                    UPDATED_AT = SYSTIMESTAMP
                WHERE POLICY_ID = :p_id";
            cmd.Parameters.Add(new OracleParameter("p_desc", OracleDbType.Varchar2) { Value = description ?? (object)DBNull.Value });
            cmd.Parameters.Add(new OracleParameter("p_stmt", OracleDbType.Varchar2) { Value = statementTypes ?? (object)DBNull.Value });
            cmd.Parameters.Add(new OracleParameter("p_enabled", OracleDbType.Decimal) { Value = isEnabled.HasValue ? (isEnabled.Value ? 1 : 0) : (object)DBNull.Value });
            cmd.Parameters.Add(new OracleParameter("p_id", OracleDbType.Decimal) { Value = policyId });

            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Xóa policy khỏi bảng ADMIN_POLICY
        /// </summary>
        public async Task DeleteAdminPolicyAsync(int policyId)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = "DELETE FROM ADMIN_POLICY WHERE POLICY_ID = :p_id";
            cmd.Parameters.Add(new OracleParameter("p_id", OracleDbType.Decimal) { Value = policyId });
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Bật/tắt policy trong bảng ADMIN_POLICY
        /// </summary>
        public async Task ToggleAdminPolicyAsync(int policyId, bool enable)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = @"
                UPDATE ADMIN_POLICY
                SET IS_ENABLED = :p_enabled,
                    UPDATED_AT = SYSTIMESTAMP
                WHERE POLICY_ID = :p_id";
            cmd.Parameters.Add(new OracleParameter("p_enabled", OracleDbType.Decimal) { Value = enable ? 1 : 0 });
            cmd.Parameters.Add(new OracleParameter("p_id", OracleDbType.Decimal) { Value = policyId });
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Lấy lịch sử thay đổi policy
        /// </summary>
        public async Task<List<PolicyChangeLogInfo>> GetPolicyChangeLogsAsync(int? policyId = null, int limit = 100)
        {
            var logs = new List<PolicyChangeLogInfo>();
            
            try
            {
                using var cmd = Connection.CreateCommand();
                cmd.CommandText = @"
                    SELECT * FROM (
                        SELECT l.LOG_ID, l.POLICY_ID, NVL(p.POLICY_NAME, 'N/A') AS POLICY_NAME,
                               l.ACTION, NVL(l.CHANGED_BY, '') AS CHANGED_BY, l.CHANGED_AT,
                               NVL(l.OLD_VALUE, '') AS OLD_VALUE, NVL(l.NEW_VALUE, '') AS NEW_VALUE,
                               NVL(l.REASON, '') AS REASON
                        FROM POLICY_CHANGE_LOG l
                        LEFT JOIN ADMIN_POLICY p ON l.POLICY_ID = p.POLICY_ID
                        WHERE (:p_id IS NULL OR l.POLICY_ID = :p_id)
                        ORDER BY l.CHANGED_AT DESC
                    ) WHERE ROWNUM <= :p_limit";
                cmd.Parameters.Add(new OracleParameter("p_id", OracleDbType.Decimal) 
                { 
                    Value = policyId.HasValue ? policyId.Value : (object)DBNull.Value 
                });
                cmd.Parameters.Add(new OracleParameter("p_limit", OracleDbType.Decimal) { Value = limit });

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    logs.Add(new PolicyChangeLogInfo
                    {
                        LogId = reader.GetInt32(0),
                        PolicyId = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                        PolicyName = reader.GetString(2),
                        Action = reader.GetString(3),
                        ChangedBy = reader.GetString(4),
                        ChangedAt = reader.GetDateTime(5),
                        OldValue = reader.GetString(6),
                        NewValue = reader.GetString(7),
                        Reason = reader.GetString(8)
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetPolicyChangeLogsAsync error: {ex.Message}");
            }
            
            return logs;
        }

        /// <summary>
        /// Ghi log thay đổi policy
        /// </summary>
        public async Task LogPolicyChangeAsync(int policyId, string action, string changedBy, 
            string? oldValue = null, string? newValue = null, string? reason = null)
        {
            try
            {
                using var cmd = Connection.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO POLICY_CHANGE_LOG(POLICY_ID, ACTION, CHANGED_BY, CHANGED_AT, OLD_VALUE, NEW_VALUE, REASON)
                    VALUES(:p_id, :p_action, :p_by, SYSTIMESTAMP, :p_old, :p_new, :p_reason)";
                cmd.Parameters.Add(new OracleParameter("p_id", OracleDbType.Decimal) { Value = policyId });
                cmd.Parameters.Add(new OracleParameter("p_action", OracleDbType.Varchar2) { Value = action });
                cmd.Parameters.Add(new OracleParameter("p_by", OracleDbType.Varchar2) { Value = changedBy });
                cmd.Parameters.Add(new OracleParameter("p_old", OracleDbType.Clob) { Value = oldValue ?? (object)DBNull.Value });
                cmd.Parameters.Add(new OracleParameter("p_new", OracleDbType.Clob) { Value = newValue ?? (object)DBNull.Value });
                cmd.Parameters.Add(new OracleParameter("p_reason", OracleDbType.Varchar2) { Value = reason ?? (object)DBNull.Value });
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LogPolicyChangeAsync error: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }
    }

    public class ConversationStatus
    {
        public string Status { get; set; } = string.Empty; // ACTIVE, ARCHIVED, DELETED_BY_ME, NOT_FOUND
        public bool IsPrivate { get; set; }
        public bool IsArchived { get; set; }
        public bool IsOwner { get; set; }
    }

    /// <summary>
    /// Record tin nhắn - đồng bộ với bảng TINNHAN
    /// </summary>
    public class ChatMessageRecord
    {
        // Thông tin cơ bản
        public int MessageId { get; set; }                          // MATN
        public string ConversationId { get; set; } = string.Empty;  // MACTC
        public string SenderMatk { get; set; } = string.Empty;      // MATK
        public string SenderUsername { get; set; } = string.Empty;  // TENTK (join)
        public string Content { get; set; } = string.Empty;         // NOIDUNG
        public DateTime Timestamp { get; set; }                     // NGAYGUI
        
        // Loại và trạng thái
        public string MessageType { get; set; } = "TEXT";           // MALOAITN
        public string Status { get; set; } = "ACTIVE";              // MATRANGTHAI
        public bool IsPinned { get; set; }                          // IS_PINNED
        public DateTime? EditedAt { get; set; }                     // EDITED_AT
        
        // Bảo mật MAC
        public int SecurityLabel { get; set; }                      // SECURITYLABEL
        
        // Mã hóa
        public bool IsEncrypted { get; set; }                       // IS_ENCRYPTED
        public string EncryptionType { get; set; } = "NONE";        // ENCRYPTION_TYPE
        public byte[]? EncryptedContent { get; set; }               // ENCRYPTED_CONTENT
        public string? EncryptedKey { get; set; }                   // ENCRYPTED_KEY
        public string? EncryptionIv { get; set; }                   // ENCRYPTION_IV
        public string? Signature { get; set; }                      // SIGNATURE
        
        // Attachment
        public int? AttachmentId { get; set; }
    }

    /// <summary>
    /// Tài khoản người dùng - đồng bộ với bảng TAIKHOAN
    /// </summary>
    public class UserAccount
    {
        public string Matk { get; set; } = string.Empty;            // MATK
        public string Username { get; set; } = string.Empty;        // TENTK
        public string PasswordHash { get; set; } = string.Empty;    // PASSWORD_HASH
        public string Mavaitro { get; set; } = string.Empty;        // MAVAITRO
        public int ClearanceLevel { get; set; }                     // CLEARANCELEVEL
        public bool IsBannedGlobal { get; set; }                    // IS_BANNED_GLOBAL
        public bool IsOtpVerified { get; set; }                     // IS_OTP_VERIFIED
        public string ProfileName { get; set; } = string.Empty;     // PROFILE_NAME
        public DateTime NgayTao { get; set; }                       // NGAYTAO
        public DateTime? LastLogin { get; set; }                    // LAST_LOGIN
        public DateTime? LastLogout { get; set; }                   // LAST_LOGOUT
        public int LoginCount { get; set; }                         // LOGIN_COUNT
        public string? PublicKey { get; set; }                      // PUBLIC_KEY
    }

    /// <summary>
    /// Thông tin cuộc trò chuyện - đồng bộ với bảng CUOCTROCHUYEN
    /// </summary>
    public class ConversationInfo
    {
        public string Mactc { get; set; } = string.Empty;           // MACTC
        public string Tenctc { get; set; } = string.Empty;          // TENCTC
        public string Maloaictc { get; set; } = "GROUP";            // MALOAICTC
        public bool IsPrivate { get; set; }                         // IS_PRIVATE
        public string Nguoiql { get; set; } = string.Empty;         // NGUOIQL
        public string CreatedBy { get; set; } = string.Empty;       // CREATED_BY
        public DateTime NgayTao { get; set; }                       // NGAYTAO
        public int MinClearance { get; set; } = 1;                  // MIN_CLEARANCE
        public bool IsEncrypted { get; set; }                       // IS_ENCRYPTED
        public bool IsArchived { get; set; }                        // IS_ARCHIVED
        public DateTime? ThoigianTinnhanCuoi { get; set; }          // THOIGIANTINNHANCUOI
        public int MemberCount { get; set; }
        public int MessageCount { get; set; }
    }

    public class MemberPermission
    {
        public string Quyen { get; set; } = string.Empty;
        public bool IsBanned { get; set; }
        public bool IsMuted { get; set; }
        public bool CanAdd { get; set; }
        public bool CanRemove { get; set; }
        public bool CanDelete { get; set; }
        public bool CanBan { get; set; }
        public bool CanMute { get; set; }
        public bool CanPromote { get; set; }
    }

    // Admin models
    public class AdminUserInfo
    {
        public string Matk { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Hovaten { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int ClearanceLevel { get; set; }
        public bool IsBannedGlobal { get; set; }
        public string Mavaitro { get; set; } = string.Empty;
        public DateTime NgayTao { get; set; }
        public bool IsOtpVerified { get; set; }
        public int FailedLoginAttempts { get; set; }
        public DateTime? LockedUntil { get; set; }
        public bool IsAccountLocked => LockedUntil.HasValue && LockedUntil.Value > DateTime.Now;
        public string Chucvu { get; set; } = string.Empty;     // Tên chức vụ
        public string Phongban { get; set; } = string.Empty;   // Tên phòng ban
    }

    public class AdminConversationInfo
    {
        public string Mactc { get; set; } = string.Empty;
        public string Tenctc { get; set; } = string.Empty;
        public string Maloaictc { get; set; } = string.Empty;
        public bool IsPrivate { get; set; }
        public string Nguoiql { get; set; } = string.Empty;
        public DateTime NgayTao { get; set; }
        public int MemberCount { get; set; }
        public int MessageCount { get; set; }
    }

    public class AdminMessageInfo
    {
        public int Matn { get; set; }
        public string Mactc { get; set; } = string.Empty;
        public string Matk { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Noidung { get; set; } = string.Empty;
        public int SecurityLabel { get; set; }
        public DateTime Ngaygui { get; set; }
        public string Maloaitn { get; set; } = string.Empty;
        public string Matrangthai { get; set; } = string.Empty;
    }

    public class AuditLogInfo
    {
        public int LogId { get; set; }
        public string Matk { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public int SecurityLabel { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class MemberInfo
    {
        public string Matk { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Maphanquyen { get; set; } = string.Empty;
        public bool IsBanned { get; set; }
        public bool IsMuted { get; set; }
        public DateTime NgayThamGia { get; set; }
        public string Hovaten { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    // Policy Management models
    public class AdminPolicyInfo
    {
        public int PolicyId { get; set; }
        public string PolicyName { get; set; } = string.Empty;
        public string PolicyType { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PolicyFunction { get; set; } = string.Empty;
        public string StatementTypes { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class PolicyChangeLogInfo
    {
        public int LogId { get; set; }
        public int PolicyId { get; set; }
        public string PolicyName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string ChangedBy { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; }
        public string OldValue { get; set; } = string.Empty;
        public string NewValue { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Thông tin chi tiết người dùng - đồng bộ TAIKHOAN + NGUOIDUNG
    /// </summary>
    public class UserDetailsFull
    {
        // TAIKHOAN
        public string Matk { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Mavaitro { get; set; } = string.Empty;
        public int ClearanceLevel { get; set; }
        public bool IsBannedGlobal { get; set; }
        public bool IsOtpVerified { get; set; }
        public DateTime NgayTao { get; set; }
        
        // NGUOIDUNG
        public string? Mapb { get; set; }
        public string? Macv { get; set; }
        public string? Hovaten { get; set; }
        public string? Email { get; set; }
        public string? Sdt { get; set; }
        public DateTime? Ngaysinh { get; set; }
        public string? Diachi { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Bio { get; set; }
    }
}