using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
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

        public async Task<UserAccount?> GetUserAccountAsync(string matk)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = @"
                SELECT MATK, PASSWORD_HASH, CLEARANCELEVEL, IS_BANNED_GLOBAL
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
                ClearanceLevel = reader.GetInt32(2),
                IsBannedGlobal = reader.GetInt32(3) == 1
            };
        }

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

        public async Task CreateAccountAsync(string matk, string tentk, string passwordHash, string? mavaitro, int clearanceLevel)
        {
            try
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
                SELECT COUNT(*) FROM XACTHUCOTP
                WHERE MATK = :p_matk
                  AND DAXACMINH = 1";
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });
            var result = await cmd.ExecuteScalarAsync();
            if (result == null || result == DBNull.Value) return false;
            return Convert.ToInt32(result) > 0;
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
            cmd.CommandText = "SELECT COUNT(*) FROM TAIKHOAN WHERE MATK = :p_matk";
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });
            var result = await cmd.ExecuteScalarAsync();
            if (result == null || result == DBNull.Value) return false;
            return Convert.ToInt32(result) > 0;
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

        public async Task AddMemberAsync(string mactc, string matk, string quyen = "member", string maphanquyen = "MEMBER")
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = "BEGIN SP_THEM_THANHVIEN(:p_mactc, :p_matk, :p_quyen, :p_maphanquyen); END;";
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.Add(new OracleParameter("p_mactc", OracleDbType.Varchar2) { Value = mactc });
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });
            cmd.Parameters.Add(new OracleParameter("p_quyen", OracleDbType.Varchar2) { Value = quyen });
            cmd.Parameters.Add(new OracleParameter("p_maphanquyen", OracleDbType.Varchar2) { Value = maphanquyen });
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task RemoveMemberAsync(string mactc, string matk)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = "BEGIN SP_XOA_THANHVIEN(:p_mactc, :p_matk); END;";
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.Add(new OracleParameter("p_mactc", OracleDbType.Varchar2) { Value = mactc });
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });
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

        public async Task<List<ConversationInfo>> GetUserConversationsAsync(string matk)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = @"
                SELECT DISTINCT c.MACTC, c.TENCTC, c.IS_PRIVATE, c.NGAYTAO, 
                       (SELECT COUNT(*) FROM THANHVIEN tv WHERE tv.MACTC = c.MACTC) AS MEMBER_COUNT
                FROM CUOCTROCHUYEN c
                JOIN THANHVIEN tv ON c.MACTC = tv.MACTC
                WHERE tv.MATK = :p_matk AND tv.DELETED_BY_MEMBER = 0
                ORDER BY c.NGAYTAO DESC";
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });

            var result = new List<ConversationInfo>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new ConversationInfo
                {
                    Mactc = reader.GetString(0),
                    Tenctc = reader.GetString(1),
                    IsPrivate = reader.GetString(2) == "Y",
                    NgayTao = reader.GetDateTime(3),
                    MemberCount = reader.GetInt32(4)
                });
            }
            return result;
        }

        public async Task<List<ChatMessageRecord>> GetConversationMessagesAsync(string mactc, int limit = 100)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = @"
                SELECT * FROM (
                    SELECT t.MATN, t.MACTC, t.MATK, t.NOIDUNG, t.SECURITYLABEL, t.NGAYGUI
                    FROM TINNHAN t
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
                    Content = reader.GetString(3),
                    SecurityLabel = reader.GetInt32(4),
                    Timestamp = reader.GetDateTime(5)
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
            return Convert.ToInt32(outParam.Value);
        }

        public async Task<MemberPermission?> GetMemberPermissionAsync(string mactc, string matk)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = @"
                SELECT tv.QUYEN, tv.MAPHANQUYEN, tv.IS_BANNED, tv.IS_MUTED,
                       pq.CAN_ADD, pq.CAN_REMOVE, pq.CAN_DELETE, pq.CAN_BAN, pq.CAN_MUTE
                FROM THANHVIEN tv
                LEFT JOIN PHAN_QUYEN_NHOM pq ON tv.MAPHANQUYEN = pq.MAPHANQUYEN
                WHERE tv.MACTC = :p_mactc AND tv.MATK = :p_matk";
            cmd.Parameters.Add(new OracleParameter("p_mactc", OracleDbType.Varchar2) { Value = mactc });
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });

            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }

            return new MemberPermission
            {
                Quyen = reader.GetString(0),
                IsBanned = reader.GetInt32(2) == 1,
                IsMuted = reader.GetInt32(3) == 1,
                CanAdd = reader.IsDBNull(4) ? false : reader.GetInt32(4) == 1,
                CanRemove = reader.IsDBNull(5) ? false : reader.GetInt32(5) == 1,
                CanDelete = reader.IsDBNull(6) ? false : reader.GetInt32(6) == 1,
                CanBan = reader.IsDBNull(7) ? false : reader.GetInt32(7) == 1,
                CanMute = reader.IsDBNull(8) ? false : reader.GetInt32(8) == 1
            };
        }

        public async Task BanMemberAsync(string mactc, string matk)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = "BEGIN SP_BAN_MEMBER(:p_mactc, :p_matk); END;";
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.Add(new OracleParameter("p_mactc", OracleDbType.Varchar2) { Value = mactc });
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UnbanMemberAsync(string mactc, string matk)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = "BEGIN SP_UNBAN_MEMBER(:p_mactc, :p_matk); END;";
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.Add(new OracleParameter("p_mactc", OracleDbType.Varchar2) { Value = mactc });
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task MuteMemberAsync(string mactc, string matk)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = "BEGIN SP_MUTE_MEMBER(:p_mactc, :p_matk); END;";
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.Add(new OracleParameter("p_mactc", OracleDbType.Varchar2) { Value = mactc });
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UnmuteMemberAsync(string mactc, string matk)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = "BEGIN SP_UNMUTE_MEMBER(:p_mactc, :p_matk); END;";
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.Add(new OracleParameter("p_mactc", OracleDbType.Varchar2) { Value = mactc });
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });
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

        // ========== ADMIN METHODS ==========

        public async Task<List<AdminUserInfo>> GetAllUsersAsync()
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
                    IsOtpVerified = reader.GetInt32(9) == 1
                });
            }
            return result;
        }

        public async Task<AdminUserInfo?> GetUserDetailsAsync(string matk)
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
                WHERE tk.MATK = :p_matk";
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

        public async Task UpdateUserInfoAsync(string matk, string? email, string? hovaten, string? phone, int? clearanceLevel, string? mavaitro)
        {
            using var cmd = Connection.CreateCommand();
            var updates = new List<string>();
            var parameters = new List<OracleParameter> { new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk } };

            if (email != null)
            {
                updates.Add("EMAIL = :p_email");
                parameters.Add(new OracleParameter("p_email", OracleDbType.Varchar2) { Value = email });
            }
            if (hovaten != null)
            {
                updates.Add("HOVATEN = :p_hovaten");
                parameters.Add(new OracleParameter("p_hovaten", OracleDbType.Varchar2) { Value = hovaten });
            }
            if (phone != null)
            {
                updates.Add("SDT = :p_phone");
                parameters.Add(new OracleParameter("p_phone", OracleDbType.Varchar2) { Value = phone });
            }
            if (clearanceLevel.HasValue)
            {
                updates.Add("CLEARANCELEVEL = :p_clearance");
                parameters.Add(new OracleParameter("p_clearance", OracleDbType.Int32) { Value = clearanceLevel.Value });
            }
            if (mavaitro != null)
            {
                updates.Add("MAVAITRO = :p_mavaitro");
                parameters.Add(new OracleParameter("p_mavaitro", OracleDbType.Varchar2) { Value = (object?)mavaitro ?? DBNull.Value });
            }

            if (updates.Count == 0) return;

            // Update NGUOIDUNG
            if (email != null || hovaten != null || phone != null)
            {
                var userUpdates = new List<string>();
                if (email != null) userUpdates.Add("EMAIL = :p_email");
                if (hovaten != null) userUpdates.Add("HOVATEN = :p_hovaten");
                if (phone != null) userUpdates.Add("SDT = :p_phone");

                cmd.CommandText = $@"
                    MERGE INTO NGUOIDUNG n
                    USING (SELECT :p_matk AS MATK FROM DUAL) t
                    ON (n.MATK = t.MATK)
                    WHEN MATCHED THEN
                        UPDATE SET {string.Join(", ", userUpdates)}
                    WHEN NOT MATCHED THEN
                        INSERT (MATK, EMAIL, HOVATEN, SDT)
                        VALUES (:p_matk, :p_email, :p_hovaten, :p_phone)";
                cmd.Parameters.Clear();
                cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });
                if (email != null) cmd.Parameters.Add(new OracleParameter("p_email", OracleDbType.Varchar2) { Value = email });
                if (hovaten != null) cmd.Parameters.Add(new OracleParameter("p_hovaten", OracleDbType.Varchar2) { Value = hovaten });
                if (phone != null) cmd.Parameters.Add(new OracleParameter("p_phone", OracleDbType.Varchar2) { Value = phone });
                await cmd.ExecuteNonQueryAsync();
            }

            // Update TAIKHOAN
            if (clearanceLevel.HasValue || mavaitro != null)
            {
                var accountUpdates = new List<string>();
                cmd.Parameters.Clear();
                cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });
                if (clearanceLevel.HasValue)
                {
                    accountUpdates.Add("CLEARANCELEVEL = :p_clearance");
                    cmd.Parameters.Add(new OracleParameter("p_clearance", OracleDbType.Int32) { Value = clearanceLevel.Value });
                }
                if (mavaitro != null)
                {
                    accountUpdates.Add("MAVAITRO = :p_mavaitro");
                    cmd.Parameters.Add(new OracleParameter("p_mavaitro", OracleDbType.Varchar2) { Value = (object?)mavaitro ?? DBNull.Value });
                }

                cmd.CommandText = $"UPDATE TAIKHOAN SET {string.Join(", ", accountUpdates)} WHERE MATK = :p_matk";
                await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task BanUserGlobalAsync(string matk)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = "UPDATE TAIKHOAN SET IS_BANNED_GLOBAL = 1 WHERE MATK = :p_matk";
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UnbanUserGlobalAsync(string matk)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = "UPDATE TAIKHOAN SET IS_BANNED_GLOBAL = 0 WHERE MATK = :p_matk";
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
                    Tenctc = reader.GetString(1),
                    Maloaictc = reader.GetString(2),
                    IsPrivate = reader.GetString(3) == "Y",
                    Nguoiql = reader.GetString(4),
                    NgayTao = reader.GetDateTime(5),
                    MemberCount = reader.GetInt32(6),
                    MessageCount = reader.GetInt32(7)
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

        public async Task DeleteMessageAsync(int matn)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = "DELETE FROM TINNHAN WHERE MATN = :p_matn";
            cmd.Parameters.Add(new OracleParameter("p_matn", OracleDbType.Int32) { Value = matn });
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

        public void Dispose()
        {
            _connection?.Dispose();
        }
    }

    public class ChatMessageRecord
    {
        public int MessageId { get; set; } // MATN
        public string ConversationId { get; set; } = string.Empty; // MACTC
        public string SenderMatk { get; set; } = string.Empty; // MATK
        public string Content { get; set; } = string.Empty; // NOIDUNG
        public int SecurityLabel { get; set; } // SECURITYLABEL
        public DateTime Timestamp { get; set; } // NGAYGUI
    }

    public class UserAccount
    {
        public string Matk { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public int ClearanceLevel { get; set; }
        public bool IsBannedGlobal { get; set; }
    }

    public class ConversationInfo
    {
        public string Mactc { get; set; } = string.Empty;
        public string Tenctc { get; set; } = string.Empty;
        public bool IsPrivate { get; set; }
        public DateTime NgayTao { get; set; }
        public int MemberCount { get; set; }
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
}