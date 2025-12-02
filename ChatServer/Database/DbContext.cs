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
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO TINNHAN (MATK, NOIDUNG, SECURITYLABEL)
                VALUES (:p_matk, :p_content, :p_label)";
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = senderMatk });
            cmd.Parameters.Add(new OracleParameter("p_content", OracleDbType.Clob) { Value = content });
            cmd.Parameters.Add(new OracleParameter("p_label", OracleDbType.Int32) { Value = securityLabel });
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<ChatMessageRecord>> GetMessagesForUserAsync(string matk)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = @"
                SELECT t.MATN, t.MATK, t.NOIDUNG, t.SECURITYLABEL, t.NGAYGUI
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
                    SELECT t.MATN, t.MATK, t.NOIDUNG, t.SECURITYLABEL, t.NGAYGUI
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
                    SenderMatk = reader.GetString(1),
                    Content = reader.GetString(2),
                    SecurityLabel = reader.GetInt32(3),
                    Timestamp = reader.GetDateTime(4)
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
}