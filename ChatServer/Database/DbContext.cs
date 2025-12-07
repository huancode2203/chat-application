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
                SELECT MATK, PASSWORD_HASH, CLEARANCELEVEL, IS_BANNED_GLOBAL
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
            // Oracle trả về OracleDecimal, cần chuyển đúng kiểu
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
            // Xử lý đúng kiểu OracleDecimal từ OUT parameter
            if (outParam.Value is OracleDecimal oracleDecimal)
            {
                return oracleDecimal.ToInt32();
            }
            return Convert.ToInt32(outParam.Value);
        }

        // ========== ATTACHMENT METHODS ==========

        public async Task<int> UploadAttachmentAsync(string matk, string fileName, string mimeType, long fileSize, byte[] data)
        {
            using var cmd = Connection.CreateCommand();
            cmd.CommandText = "BEGIN SP_UPLOAD_ATTACHMENT(:p_matk, :p_filename, :p_mimetype, :p_filesize, :p_filedata, :p_attach_id); END;";
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });
            cmd.Parameters.Add(new OracleParameter("p_filename", OracleDbType.Varchar2) { Value = fileName });
            cmd.Parameters.Add(new OracleParameter("p_mimetype", OracleDbType.Varchar2) { Value = mimeType });
            cmd.Parameters.Add(new OracleParameter("p_filesize", OracleDbType.Int64) { Value = fileSize });
            cmd.Parameters.Add(new OracleParameter("p_filedata", OracleDbType.Blob) { Value = data });
            var outParam = new OracleParameter("p_attach_id", OracleDbType.Int32) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(outParam);
            await cmd.ExecuteNonQueryAsync();
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
            cmd.CommandText = @"
                SELECT a.ATTACH_ID, a.FILENAME, a.MIMETYPE, a.FILESIZE, a.FILEDATA
                FROM TINNHAN_ATTACH ta
                JOIN ATTACHMENT a ON ta.ATTACH_ID = a.ATTACH_ID
                WHERE ta.MATN = :p_matn";
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.Add(new OracleParameter("p_matn", OracleDbType.Int32) { Value = matn });

            using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess);
            if (!await reader.ReadAsync())
            {
                return null;
            }

            var attachmentId = reader.GetInt32(0);
            var fileName = reader.GetString(1);
            var mimeType = reader.IsDBNull(2) ? "application/octet-stream" : reader.GetString(2);
            var fileSize = reader.IsDBNull(3) ? 0L : reader.GetInt64(3);

            byte[] data;
            if (reader.IsDBNull(4) || fileSize <= 0)
            {
                data = Array.Empty<byte>();
            }
            else
            {
                var length = (int)Math.Min(fileSize, int.MaxValue);
                data = new byte[length];

                long bytesReadTotal = 0;
                var bufferOffset = 0;
                const int bufferSize = 8192;

                while (bytesReadTotal < length)
                {
                    var bytesToRead = (int)Math.Min(bufferSize, length - bytesReadTotal);
                    var bytesRead = (int)reader.GetBytes(4, bytesReadTotal, data, bufferOffset, bytesToRead);
                    if (bytesRead <= 0)
                    {
                        break;
                    }

                    bytesReadTotal += bytesRead;
                    bufferOffset += bytesRead;
                }
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
                SELECT tv.QUYEN, tv.MAPHANQUYEN, tv.IS_BANNED, tv.IS_MUTED,
                       pq.CAN_ADD, pq.CAN_REMOVE, pq.CAN_DELETE, pq.CAN_BAN, pq.CAN_MUTE, pq.CAN_PROMOTE
                FROM THANHVIEN tv
                LEFT JOIN PHAN_QUYEN_NHOM pq ON tv.MAPHANQUYEN = pq.MAPHANQUYEN
                WHERE tv.MACTC = :p_mactc AND tv.MATK = :p_matk";
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